using System.Reflection;
using System.Reflection.Emit;
using HotAvalonia.Helpers;
using HotAvalonia.Reflection.Emit;

namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Provides methods for injecting and replacing method implementations at runtime.
/// </summary>
internal static class MethodInjector
{
    /// <summary>
    /// Gets the type of the injection technique supported by the current runtime environment.
    /// </summary>
    public static InjectionType InjectionType { get; } = DetectSupportedInjectionType();

    /// <summary>
    /// Indicates whether method injection is supported in the current runtime environment.
    /// </summary>
    public static bool IsSupported => InjectionType is not InjectionType.None;

    /// <summary>
    /// Indicates whether method injection is supported in optimized assemblies.
    /// </summary>
    public static bool SupportsOptimizedMethods => InjectionType is InjectionType.Native;

    /// <summary>
    /// Injects a replacement method implementation for the specified source method.
    /// </summary>
    /// <param name="source">The method to be replaced.</param>
    /// <param name="replacement">The replacement method implementation.</param>
    /// <returns>An <see cref="IInjection"/> instance representing the method injection.</returns>
    /// <exception cref="InvalidOperationException"/>
    public static IInjection Inject(MethodBase source, MethodInfo replacement) => InjectionType switch
    {
        InjectionType.Native => new NativeInjection(source, replacement),
        _ => ThrowNotSupportedException(),
    };

    /// <summary>
    /// Throws an exception if method injection is not supported in the current runtime environment.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public static void ThrowIfNotSupported()
    {
        if (!IsSupported)
            ThrowNotSupportedException();
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> indicating that method injection is not supported.
    /// </summary>
    /// <returns>This method never returns; it always throws an exception.</returns>
    /// <exception cref="InvalidOperationException">Always thrown to indicate that method injection is not available.</exception>
    private static IInjection ThrowNotSupportedException()
        => throw new InvalidOperationException("Method injection is not available in the current runtime environment.");

    /// <summary>
    /// Detects the type of the method injection technique supported by the current runtime environment.
    /// </summary>
    /// <returns>The <see cref="Inject.InjectionType"/> supported by the current runtime environment.</returns>
    private static InjectionType DetectSupportedInjectionType()
    {
        if (HotReloadFeatures.DisableInjections)
            return InjectionType.None;

        return NativeInjection.IsSupported ? InjectionType.Native : InjectionType.None;
    }
}

/// <summary>
/// Provides functionality to inject a replacement method using native code hooks.
/// </summary>
file sealed class NativeInjection : IInjection
{
    /// <summary>
    /// A factory function used to instantiate <c>MonoMod.RuntimeDetour.Hook</c> objects.
    /// </summary>
    private static readonly Func<MethodBase, MethodInfo, object>? s_createHook;

    /// <summary>
    /// The method reference for <c>Hook.Apply()</c>.
    /// </summary>
    private static readonly Action<object>? s_applyHook;

    /// <summary>
    /// The method reference for <c>Hook.Undo()</c>.
    /// </summary>
    private static readonly Action<object>? s_undoHook;

    /// <summary>
    /// The method reference for <c>Hook.Dispose()</c>.
    /// </summary>
    private static readonly Action<object>? s_disposeHook;

    /// <summary>
    /// Initializes static members of the <see cref="NativeInjection"/> class.
    /// </summary>
    static NativeInjection()
    {
        if (!AssemblyHelper.TryLoad("MonoMod.RuntimeDetour", out Assembly? runtimeDetour))
            return;

        Type? hook = runtimeDetour.GetType("MonoMod.RuntimeDetour.Hook");
        ConstructorInfo? hookCtor = hook?.GetInstanceConstructor([typeof(MethodBase), typeof(MethodInfo), typeof(bool)]);
        MethodInfo? hookApply = hook?.GetInstanceMethod(nameof(Apply), []);
        MethodInfo? hookUndo = hook?.GetInstanceMethod(nameof(Undo), []);
        MethodInfo? hookDispose = hook?.GetInstanceMethod(nameof(Dispose), []);
        if (hookCtor is null || hookApply is null || hookUndo is null || hookDispose is null)
            return;

        using DynamicMethodBuilder createHook = DynamicAssembly.Shared.DefineMethod("CreateRuntimeDetourHook", typeof(object), [typeof(MethodBase), typeof(MethodInfo)], skipVisibility: true);
        ILGenerator hookIl = createHook.GetILGenerator();
        hookIl.Emit(OpCodes.Ldarg_0);
        hookIl.Emit(OpCodes.Ldarg_1);
        hookIl.Emit(OpCodes.Ldc_I4_1);
        hookIl.Emit(OpCodes.Newobj, hookCtor);
        hookIl.Emit(OpCodes.Ret);

        DisableDebugLog();
        s_createHook = (Func<MethodBase, MethodInfo, object>)createHook.CreateDelegate(typeof(Func<MethodBase, MethodInfo, object>));
        s_applyHook = hookApply.CreateUnsafeDelegate<Action<object>>();
        s_undoHook = hookUndo.CreateUnsafeDelegate<Action<object>>();
        s_disposeHook = hookDispose.CreateUnsafeDelegate<Action<object>>();
    }

    /// <summary>
    /// Disable MonoMod's debug logging.
    /// </summary>
    /// <remarks>
    /// MonoMod starts spamming debug logs if it detects a debugger attached to the current process.
    /// While this is useful when debugging a mod, it's a really annoying anti-feature in our context,
    /// as it drastically slows everything down. Thus, disable it as soon as humanly possible.
    /// </remarks>
    private static void DisableDebugLog()
    {
        object? logger = Type.GetType("MonoMod.Logs.DebugLog, MonoMod.Utils")?.GetStaticField("Instance")?.GetValue(null);
        logger?.GetType().GetInstanceField("globalFilter")?.SetValue(logger, 0);
    }

    /// <summary>
    /// The hook used for the method injection.
    /// </summary>
    private readonly object _hook;

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeInjection"/> class.
    /// </summary>
    /// <param name="source">The method to be replaced.</param>
    /// <param name="replacement">The replacement method implementation.</param>
    public NativeInjection(MethodBase source, MethodInfo replacement)
    {
        // Enable dynamic code generation, which is required for MonoMod to function.
        // Note that we cannot enable it forcefully just once and call it a day,
        // because this only affects the current thread.
        _ = DynamicCodeScope.Create("*", nameof(NativeInjection));

        _hook = s_createHook!(source, replacement);
    }

    /// <summary>
    /// Indicates whether native method injections are supported in the current runtime environment.
    /// </summary>
    public static bool IsSupported
    {
        get
        {
            if (s_createHook is null)
                return false;

            try
            {
                // Enable dynamic code generation, which is required for MonoMod to function.
                using DynamicCodeScope scope = DynamicCodeScope.Create(nameof(IsSupported), nameof(NativeInjection));

                Type? platformTriple = Type.GetType("MonoMod.Core.Platforms.PlatformTriple, MonoMod.Core");
                return platformTriple?.GetStaticProperty("Current")?.GetValue(null) is not null;
            }
            catch
            {
                // `PlatformTriple.Current` may throw exceptions such as:
                //  - NotImplementedException
                //  - PlatformNotSupportedException
                //  - etc.
                // This happens if the current environment is not (yet) supported.
                return false;
            }
        }
    }

    /// <summary>
    /// Applies the method injection.
    /// </summary>
    public void Apply() => s_applyHook!(_hook);

    /// <summary>
    /// Reverts all the effects caused by the method injection.
    /// </summary>
    public void Undo() => s_undoHook!(_hook);

    /// <inheritdoc/>
    public void Dispose() => s_disposeHook!(_hook);
}
