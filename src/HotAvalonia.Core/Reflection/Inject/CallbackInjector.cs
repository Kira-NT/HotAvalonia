using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Provides an API for injecting managed callbacks into methods at runtime.
/// </summary>
internal static class CallbackInjector
{
    private static readonly RuntimeCallbackInjector s_callbackInjector = HotReloadFeatures.InjectionType switch
    {
        InjectionType x when (x & InjectionType.MonoMod) != 0 && MonoModCallbackInjector.IsSupported => new MonoModCallbackInjector(),
        InjectionType x when (x & InjectionType.PointerSwap) != 0 && PointerSwapCallbackInjector.IsSupported => new PointerSwapCallbackInjector(),
        InjectionType x when (x & InjectionType.CodeCave) != 0 && CodeCaveCallbackInjector.IsSupported => new CodeCaveCallbackInjector(),
        _ => new NullCallbackInjector(),
    };

    /// <inheritdoc cref="RuntimeCallbackInjector.InjectionType"/>
    public static InjectionType InjectionType => s_callbackInjector.InjectionType;

    /// <inheritdoc cref="RuntimeCallbackInjector.CanInjectInto(MethodBase?)"/>
    public static bool CanInjectInto(MethodBase target)
        => s_callbackInjector.CanInjectInto(target);

    /// <inheritdoc cref="RuntimeCallbackInjector.TryInject(MethodBase, Delegate, out IDisposable?)"/>
    public static bool TryInject(MethodBase target, Delegate callback, [NotNullWhen(true)] out IDisposable? injection)
        => s_callbackInjector.TryInject(target, callback, out injection);

    /// <inheritdoc cref="RuntimeCallbackInjector.TryInject(MethodBase, MethodInfo, out IDisposable?)"/>
    public static bool TryInject(MethodBase target, MethodInfo callback, [NotNullWhen(true)] out IDisposable? injection)
        => s_callbackInjector.TryInject(target, callback, out injection);

    /// <inheritdoc cref="RuntimeCallbackInjector.TryInject(MethodBase, MethodInfo, object?, out IDisposable?)"/>
    public static bool TryInject(MethodBase target, MethodInfo callback, object? thisArg, [NotNullWhen(true)] out IDisposable? injection)
        => s_callbackInjector.TryInject(target, callback, thisArg, out injection);

    /// <inheritdoc cref="RuntimeCallbackInjector.Inject(MethodBase, Delegate)"/>
    public static IDisposable Inject(MethodBase target, Delegate callback)
        => s_callbackInjector.Inject(target, callback);

    /// <inheritdoc cref="RuntimeCallbackInjector.Inject(MethodBase, MethodInfo)"/>
    public static IDisposable Inject(MethodBase target, MethodInfo callback)
        => s_callbackInjector.Inject(target, callback);

    /// <inheritdoc cref="RuntimeCallbackInjector.Inject(MethodBase, MethodInfo, object?)"/>
    public static IDisposable Inject(MethodBase target, MethodInfo callback, object? thisArg)
        => s_callbackInjector.Inject(target, callback, thisArg);
}
