using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HotAvalonia.Helpers;

/// <summary>
/// Provides utility methods for interacting with assemblies.
/// </summary>
internal static class AssemblyHelper
{
    private static readonly ModuleBuilder s_moduleBuilder;

    private static readonly ConstructorInfo s_ignoresAccessChecksToAttributeCtor;

    private static readonly Func<IDisposable> s_forceAllowDynamicCode;

    static AssemblyHelper()
    {
        s_forceAllowDynamicCode = CreateForceAllowDynamicCodeDelegate();
        using IDisposable context = s_forceAllowDynamicCode();

        string assemblyName = $"{nameof(HotAvalonia)}.Dynamic";
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new(assemblyName), AssemblyBuilderAccess.RunAndCollect);
        s_moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);

        Type ignoresAccessChecksToAttribute = CreateIgnoresAccessChecksToAttributeType(s_moduleBuilder);
        s_ignoresAccessChecksToAttributeCtor = ignoresAccessChecksToAttribute.GetConstructor([typeof(string)])!;

        assemblyBuilder.AllowAccessTo(AppDomain.CurrentDomain);
    }

    /// <summary>
    /// Gets the <see cref="Type"/> representing the <c>IgnoresAccessChecksToAttribute</c>
    /// used to bypass access checks between assemblies.
    /// </summary>
    internal static Type IgnoresAccessChecksFromAttribute => s_ignoresAccessChecksToAttributeCtor.DeclaringType!;

    /// <summary>
    /// Attempts to load an assembly with the specified name.
    /// </summary>
    /// <param name="name">The name of the assembly to load.</param>
    /// <param name="assembly">
    /// When this method returns, contains the loaded <see cref="Assembly"/>
    /// if the assembly is successfully loaded; otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the assembly is successfully loaded; otherwise, <c>false</c>.</returns>
    public static bool TryLoad(string name, [NotNullWhen(true)] out Assembly? assembly)
    {
        try
        {
            assembly = Assembly.Load(name);
            return true;
        }
        catch
        {
            assembly = null;
            return false;
        }
    }

    /// <summary>
    /// Retrieves all loadable types from a given assembly.
    /// </summary>
    /// <param name="assembly">The assembly from which to retrieve types.</param>
    /// <returns>An enumerable of types available in the provided assembly.</returns>
    /// <remarks>
    /// This method attempts to get all types from the assembly, but in case of a
    /// <see cref="ReflectionTypeLoadException"/>, it will return the types that are loadable.
    /// </remarks>
    public static IEnumerable<Type> GetLoadedTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(static x => x is not null)!;
        }
    }

    private static void AllowAccessTo(this AssemblyBuilder sourceAssembly, string? targetAssemblyName)
    {
        using IDisposable context = ForceAllowDynamicCode();
        sourceAssembly.SetCustomAttribute(new(s_ignoresAccessChecksToAttributeCtor, [targetAssemblyName]));
    }

    /// <summary>
    /// Adds the <c>IgnoresAccessChecksToAttribute</c> to the source assembly,
    /// allowing it to access internal members of the specified target assembly.
    /// </summary>
    /// <param name="sourceAssembly">The assembly to which the attribute is added.</param>
    /// <param name="targetAssemblyName">The name of the assembly whose internal members should be accessible.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, AssemblyName targetAssemblyName)
        => sourceAssembly.AllowAccessTo(targetAssemblyName.Name);

    /// <inheritdoc cref="AllowAccessTo(AssemblyBuilder, AssemblyName)"/>
    /// <param name="targetAssembly">The assembly whose internal members should be accessible.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, Assembly targetAssembly)
        => sourceAssembly.AllowAccessTo(targetAssembly.GetName());

    /// <summary>
    /// Adds the <c>IgnoresAccessChecksToAttribute</c> to the source assembly,
    /// allowing it to access internal members of all assemblies loaded
    /// in the specified application domain.
    /// </summary>
    /// <param name="sourceAssembly">The source assembly to which the attribute is added.</param>
    /// <param name="appDomain">The application domain whose loaded assemblies should be accessible.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, AppDomain appDomain)
    {
        appDomain.AssemblyLoad += (_, x) => sourceAssembly.AllowAccessTo(x.LoadedAssembly);
        foreach (Assembly assembly in appDomain.GetAssemblies())
            sourceAssembly.AllowAccessTo(assembly);
    }

    /// <summary>
    /// Gets the shared dynamic assembly and its associated module.
    /// </summary>
    /// <param name="assembly">When this method returns, contains the <see cref="AssemblyBuilder"/> instance representing the dynamic assembly.</param>
    /// <param name="module">When this method returns, contains the <see cref="ModuleBuilder"/> instance representing the module associated with the dynamic assembly.</param>
    /// <returns>An <see cref="IDisposable"/> object that must be disposed to revoke any dynamic code permissions granted during the method's execution.</returns>
    public static IDisposable GetDynamicAssembly(out AssemblyBuilder assembly, out ModuleBuilder module)
    {
        module = s_moduleBuilder;
        assembly = (AssemblyBuilder)module.Assembly;
        return ForceAllowDynamicCode();
    }

    /// <summary>
    /// Temporarily allows dynamic code generation even when <c>RuntimeFeature.IsDynamicCodeSupported</c> is <c>false</c>.
    /// </summary>
    /// <returns>
    /// An <see cref="IDisposable"/> object that, when disposed, will revert the environment
    /// to its previous state regarding support for dynamic code generation.
    /// </returns>
    /// <remarks>
    /// This is particularly useful in scenarios where the runtime can support emitting dynamic code,
    /// but a feature switch or configuration has disabled it (e.g., <c>PublishAot=true</c> during debugging).
    /// </remarks>
    public static IDisposable ForceAllowDynamicCode()
        => s_forceAllowDynamicCode();

    [MethodImpl(MethodImplOptions.NoOptimization)]
    private static Func<IDisposable> CreateForceAllowDynamicCodeDelegate()
    {
        MethodInfo? forceAllowDynamicCode = typeof(AssemblyBuilder).GetMethod(nameof(ForceAllowDynamicCode), (BindingFlags)(-1), null, [], null);
        if (forceAllowDynamicCode is not null && typeof(IDisposable).IsAssignableFrom(forceAllowDynamicCode.ReturnType))
            return (Func<IDisposable>)forceAllowDynamicCode.CreateDelegate(typeof(Func<IDisposable>));

        // I'm too lazy to create a new type for the stub, so just use an empty
        // `MemoryStream` that can be disposed of an infinite number of times.
        IDisposable disposableInstance = new MemoryStream(Array.Empty<byte>());
        return () => disposableInstance;
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    private static Type CreateIgnoresAccessChecksToAttributeType(ModuleBuilder moduleBuilder)
    {
        string attributeName = "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute";
        TypeBuilder attributeBuilder = moduleBuilder.DefineType(attributeName, TypeAttributes.Class | TypeAttributes.Public, typeof(Attribute));

        PropertyBuilder namePropertyBuilder = attributeBuilder.DefineProperty(nameof(AssemblyName), PropertyAttributes.None, typeof(string), Type.EmptyTypes);
        FieldBuilder nameFieldBuilder = attributeBuilder.DefineField($"<{namePropertyBuilder.Name}>k__BackingField", typeof(string), FieldAttributes.Private);
        MethodBuilder nameGetterBuilder = attributeBuilder.DefineMethod($"get_{namePropertyBuilder.Name}", MethodAttributes.Public, typeof(string), Type.EmptyTypes);
        ILGenerator nameIl = nameGetterBuilder.GetILGenerator();
        nameIl.Emit(OpCodes.Ldarg_0);
        nameIl.Emit(OpCodes.Ldfld, nameFieldBuilder);
        nameIl.Emit(OpCodes.Ret);
        namePropertyBuilder.SetGetMethod(nameGetterBuilder);

        ConstructorBuilder ctorBuilder = attributeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(string)]);
        ILGenerator ctorIl = ctorBuilder.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Ldarg_1);
        ctorIl.Emit(OpCodes.Stfld, nameFieldBuilder);
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(Attribute).GetConstructor((BindingFlags)(-1), null, [], null));
        ctorIl.Emit(OpCodes.Ret);

        CustomAttributeBuilder attributeUsage = new(
            typeof(AttributeUsageAttribute).GetConstructor([typeof(AttributeTargets)]),
            [AttributeTargets.Assembly],

            [typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))],
            [true]
        );
        attributeBuilder.SetCustomAttribute(attributeUsage);

        return attributeBuilder.CreateTypeInfo();
    }
}
