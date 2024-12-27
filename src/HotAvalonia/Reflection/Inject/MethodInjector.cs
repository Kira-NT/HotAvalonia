using System.Reflection;
using HotAvalonia.Helpers;
using MonoMod.Core.Platforms;
using MonoMod.RuntimeDetour;

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
    /// Determines whether the injection system is disabled by the user.
    /// </summary>
    /// <returns><c>true</c> if the injection system is disabled; otherwise, <c>false</c>.</returns>
    private static bool IsDisabled()
        => Environment.GetEnvironmentVariable("HOTAVALONIA_DISABLE_INJECTIONS") is "1" or "true";

    /// <summary>
    /// Detects the type of the method injection technique supported by the current runtime environment.
    /// </summary>
    /// <returns>The <see cref="Inject.InjectionType"/> supported by the current runtime environment.</returns>
    private static InjectionType DetectSupportedInjectionType()
    {
        if (IsDisabled())
            return InjectionType.None;

        try
        {
            // Enable dynamic code generation, which is required for MonoMod to function.
            using IDisposable context = AssemblyHelper.ForceAllowDynamicCode();

            // `PlatformTriple.Current` may throw exceptions such as:
            //  - NotImplementedException
            //  - PlatformNotSupportedException
            //  - etc.
            // This happens if the current environment is not (yet) supported.
            if (PlatformTriple.Current is not null)
                return InjectionType.Native;
        }
        catch { }

        return InjectionType.None;
    }
}

/// <summary>
/// Provides functionality to inject a replacement method using native code hooks.
/// </summary>
file sealed class NativeInjection : IInjection
{
    /// <summary>
    /// The hook used for the method injection.
    /// </summary>
    private readonly Hook _hook;

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
        _ = AssemblyHelper.ForceAllowDynamicCode();

        _hook = new(source, replacement, applyByDefault: true);
    }

    /// <summary>
    /// Applies the method injection.
    /// </summary>
    public void Apply() => _hook.Apply();

    /// <summary>
    /// Reverts all the effects caused by the method injection.
    /// </summary>
    public void Undo() => _hook.Undo();

    /// <inheritdoc/>
    public void Dispose() => _hook.Dispose();
}
