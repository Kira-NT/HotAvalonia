using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Represents a no-op implementation of <see cref="RuntimeCallbackInjector"/>.
/// Does not provide callback injection capabilities.
/// </summary>
internal sealed class NullCallbackInjector : RuntimeCallbackInjector
{
    public override InjectionType InjectionType => InjectionType.None;

    public override bool CanInjectInto([NotNullWhen(true)] MethodBase? target)
        => false;

    protected override IDisposable CreateHook(MethodBase target, MethodInfo callback, object? thisArg = null)
        => throw new InvalidOperationException();

    protected override void LoadDelegate(ILGenerator il, MethodBase target, Type delegateType, ref Action<IDisposable> ctor, MethodBuilder dispose)
        => throw new InvalidOperationException();
}
