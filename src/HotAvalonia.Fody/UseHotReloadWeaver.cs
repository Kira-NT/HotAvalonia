using Mono.Cecil;
using Mono.Cecil.Cil;
using HotAvalonia.Fody.Helpers;
using System.Reflection;
using HotAvalonia.Fody.Cecil;
using HotAvalonia.Fody.MSBuild;
using HotAvalonia.Fody.Reflection;
using System.Diagnostics.CodeAnalysis;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace HotAvalonia.Fody;

/// <summary>
/// Automatically enables hot reload in the target application.
/// </summary>
internal sealed class UseHotReloadWeaver : FeatureWeaver
{
    /// <summary>
    /// The method reference for <see cref="Assembly.GetName()"/>.
    /// </summary>
    private readonly CecilMethod _assemblyGetName;

    /// <summary>
    /// The property reference for <see cref="AssemblyName.Name"/>.
    /// </summary>
    private readonly CecilProperty _assemblyNameName;

    /// <summary>
    /// The method reference for <see cref="string.Equals(string, string)"/>.
    /// </summary>
    private readonly CecilMethod _stringEquals;

    /// <summary>
    /// The constructor reference for <see cref="Func{Assembly, string?}"/>.
    /// </summary>
    private readonly CecilMethod _pathResolverCtor;

    /// <summary>
    /// Initializes a new instance of the <see cref="UseHotReloadWeaver"/> class.
    /// </summary>
    /// <param name="root">The root module weaver.</param>
    public UseHotReloadWeaver(ModuleWeaver root) : base(root)
    {
        _assemblyGetName = root.GetType(typeof(Assembly)).GetMethod(x => x.GetMethod(nameof(Assembly.GetName), []));
        _assemblyNameName = root.GetType(typeof(AssemblyName)).GetProperty(x => x.GetProperty(nameof(AssemblyName.Name)));
        _stringEquals = root.GetType(typeof(string)).GetMethod(x => x.GetMethod(nameof(string.Equals), [typeof(string), typeof(string)]));
        _pathResolverCtor = root.GetType(typeof(Func<Assembly, string?>)).GetMethod(x => x.GetConstructor(BindingFlag.AnyInstance, [typeof(object), typeof(nint)]));
    }

    /// <summary>
    /// Gets a value indicating whether a path resolver should be
    /// generated in case it doesn't already exist.
    /// </summary>
    private bool GeneratePathResolver => this[nameof(GeneratePathResolver), false];

    /// <inheritdoc/>
    public override void Execute()
    {
        const string UseHotReloadMethodName = "UseHotReload";

        TypeDefinition? hotReloadExtensions = ModuleDefinition.GetType(UnreferencedTypes.HotAvalonia_AvaloniaHotReloadExtensions);
        MethodDefinition? useHotReload = hotReloadExtensions?.GetMethod(UseHotReloadMethodName, BindingFlag.AnyStatic, [UnreferencedTypes.Avalonia_AppBuilder, typeof(Func<Assembly, string?>)], UnreferencedTypes.Avalonia_AppBuilder);
        useHotReload ??= hotReloadExtensions?.GetMethod(UseHotReloadMethodName, BindingFlag.AnyStatic, [UnreferencedTypes.Avalonia_AppBuilder], UnreferencedTypes.Avalonia_AppBuilder);
        if (hotReloadExtensions is null || useHotReload is null)
        {
            WriteError($"'{UnreferencedTypes.HotAvalonia_AvaloniaHotReloadExtensions}.{UseHotReloadMethodName}({UnreferencedTypes.Avalonia_AppBuilder})' not found in '{AssemblyFilePath}'.");
            return;
        }

        if (!TryFindInjectionPoint(ModuleDefinition, out MethodDefinition? target, out Instruction? injectionPoint))
        {
            WriteError($"Unable to enable hot reload: no entry point has been identified.");
            return;
        }

        MethodDefinition? pathResolver = useHotReload.Parameters.Count == 2 ? GetPathResolver(hotReloadExtensions) : null;
        InjectUseHotReload(target, injectionPoint, useHotReload, pathResolver);
    }

    /// <summary>
    /// Attempts to locate an injection point in the module where the hot reload call can be injected.
    /// </summary>
    /// <param name="module">The module definition to search.</param>
    /// <param name="target">
    /// When this method returns, contains the target method definition if an injection point is found;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <param name="injectionPoint">
    /// When this method returns, contains the instruction indicating where to inject the hot reload call if found;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if an injection point was successfully located; otherwise, <c>false</c>.
    /// </returns>
    private static bool TryFindInjectionPoint(ModuleDefinition module, [NotNullWhen(true)] out MethodDefinition? target, [NotNullWhen(true)] out Instruction? injectionPoint)
    {
        const string BuildAvaloniaAppMethodName = "BuildAvaloniaApp";
        const string CustomizeAppBuilderMethodName = "CustomizeAppBuilder";

        // Our preferred injection target is `<Program>.BuildAvaloniaApp()`.
        // Sadly, it's just a non-enforceable convention, so it might not exist.
        MethodDefinition? entryPoint = module.EntryPoint;
        MethodDefinition? appBuilder = entryPoint?.DeclaringType.GetMethod(BuildAvaloniaAppMethodName, BindingFlag.AnyStatic, [], UnreferencedTypes.Avalonia_AppBuilder);
        if (appBuilder is not null && appBuilder.TryGetSingleRetInstruction(out Instruction? ret))
        {
            target = appBuilder;
            injectionPoint = ret;
            return true;
        }

        // If we cannot inject into `<Program>.BuildAvaloniaApp()`, attempt to inject directly into `<Program>.<Main>()`.
        // However, in order to do this, we need to find a method call that creates an `AppBuilder` instance first.
        // Fortunately, this is quite straightforward.
        Instruction? afterFactory = entryPoint.TryGetCallInstruction(UnreferencedTypes.Avalonia_AppBuilder, out Instruction? factory) ? factory.Next : null;
        if ((entryPoint, afterFactory) is (not null, not null))
        {
            target = entryPoint;
            injectionPoint = afterFactory;
            return true;
        }

        // At this point, we are either dealing with a mobile app, or the user has a pretty weird setup.
        // Let's be optimistic here and assume that we're working with an iOS/Android project.
        // Both of those usually define a class that serves as the main activity and contains
        // a protected virtual member, `CustomizeAppBuilder(AppBuilder)`.
        // This is exactly what we're looking for.
        IEnumerable<MethodDefinition> appBuilderCandidates = module.GetMethods(CustomizeAppBuilderMethodName, BindingFlag.NonPublicInstance, [UnreferencedTypes.Avalonia_AppBuilder], UnreferencedTypes.Avalonia_AppBuilder);
        appBuilder = appBuilderCandidates.Where(x => x.IsVirtual).Select((x, i) => i == 0 ? x : null).Take(2).LastOrDefault();
        if (appBuilder is not null && appBuilder.TryGetSingleRetInstruction(out ret))
        {
            target = appBuilder;
            injectionPoint = ret;
            return true;
        }

        // If `CustomizeAppBuilder(AppBuilder)` does exist, but has more than one exit point,
        // making it impossible for us to enable hot reload reliably, attempt to inject
        // immediately after the `AppBuilder` instance has been put on the stack.
        Instruction? afterLdarg = appBuilder?.Body?.Instructions.FirstOrDefault(x => x.OpCode.Code is Code.Ldarg_1)?.Next;
        if ((appBuilder, afterLdarg) is (not null, not null))
        {
            target = appBuilder;
            injectionPoint = afterLdarg;
            return true;
        }

        // Welp, we have officially failed.
        target = null;
        injectionPoint = null;
        return false;
    }

    /// <summary>
    /// Injects a call to the <c>UseHotReload</c> method into the specified target method.
    /// </summary>
    /// <param name="target">The target method definition where the injection will occur.</param>
    /// <param name="injectionPoint">The instruction at which the hot reload call is injected.</param>
    /// <param name="useHotReload">The method definition for <c>UseHotReload</c>.</param>
    /// <param name="pathResolver">The method definition for the path resolver, if available; otherwise, <c>null</c>.</param>
    private void InjectUseHotReload(MethodDefinition target, Instruction injectionPoint, MethodDefinition useHotReload, MethodDefinition? pathResolver)
    {
        ILProcessor il = target.Body.GetILProcessor();
        Instruction useHotReloadCall = il.Create(OpCodes.Call, useHotReload);
        il.InsertBefore(injectionPoint, useHotReloadCall);
        if (useHotReload.Parameters.Count != 2)
            return;

        il.InsertBefore(useHotReloadCall, il.Create(OpCodes.Ldnull));
        if (pathResolver is null)
            return;

        il.InsertBefore(useHotReloadCall, il.Create(OpCodes.Ldftn, pathResolver));
        il.InsertBefore(useHotReloadCall, il.Create(OpCodes.Newobj, _pathResolverCtor));
    }

    /// <summary>
    /// Retrieves the method definition for the path resolver from the specified declaring type.
    /// </summary>
    /// <param name="declaringType">The type definition that may contain the path resolver method.</param>
    /// <returns>The <see cref="MethodDefinition"/> for the path resolver if found or generated; otherwise, <c>null</c>.</returns>
    private MethodDefinition? GetPathResolver(TypeDefinition declaringType)
    {
        const string ResolveProjectPathMethodName = "ResolveProjectPath";

        MethodDefinition? pathResolver = declaringType.GetMethod(ResolveProjectPathMethodName, BindingFlag.AnyStatic, [typeof(Assembly)], typeof(string));
        if (pathResolver is not null)
            return pathResolver;

        if (!GeneratePathResolver)
            return null;

        pathResolver = CreatePathResolver(ResolveProjectPathMethodName, ReferencedProjects);
        declaringType.Methods.Add(pathResolver);
        return pathResolver;
    }

    /// <summary>
    /// Creates a new method that matches assembly names with project paths.
    /// </summary>
    /// <param name="name">The name to assign to the new path resolver method.</param>
    /// <param name="projects">A collection of <see cref="MSBuildProject"/> instances used to resolve project paths.</param>
    /// <returns>A new <see cref="MethodDefinition"/> that implements the path resolver.</returns>
    private MethodDefinition CreatePathResolver(string name, IEnumerable<MSBuildProject> projects)
    {
        MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static;
        MethodDefinition pathResolver = new(name, attributes, _stringEquals.DeclaringType);
        pathResolver.Parameters.Add(new(_assemblyGetName.DeclaringType));

        ILProcessor il = pathResolver.Body.GetILProcessor();
        pathResolver.Body.Variables.Add(new(_stringEquals.DeclaringType));

        Instruction nullRet = il.Create(OpCodes.Ldnull);
        Instruction ret = il.Create(OpCodes.Ret);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Brfalse, nullRet);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, _assemblyGetName);
        il.Emit(OpCodes.Brfalse, nullRet);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, _assemblyGetName);
        il.Emit(OpCodes.Call, _assemblyNameName.GetMethod);
        il.Emit(OpCodes.Stloc_0);

        // This is far from optimal.
        // Of course, we could combine strings by length and prefixes
        // in order to optimize the search. However, honestly, how many
        // projects does one need to have for this to become an issue?
        foreach (MSBuildProject project in projects)
        {
            if (string.IsNullOrEmpty(project.AssemblyName))
                continue;

            il.Emit(OpCodes.Ldstr, project.DirectoryName);
            il.Emit(OpCodes.Ldstr, project.AssemblyName);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, _stringEquals);
            il.Emit(OpCodes.Brtrue, ret);
            il.Emit(OpCodes.Pop);
        }

        il.Append(nullRet);
        il.Append(ret);

        return pathResolver;
    }
}
