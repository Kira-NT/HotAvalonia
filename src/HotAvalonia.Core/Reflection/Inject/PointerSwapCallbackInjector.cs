using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotAvalonia.Helpers;

namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Provides callback injection capabilities by swapping the managed function pointer of a target method with that of a callback function.
/// </summary>
internal sealed class PointerSwapCallbackInjector : RuntimeCallbackInjector
{
    /// <summary>
    /// Gets a value indicating whether this injector is supported in the current environment.
    /// </summary>
    public static bool IsSupported => Environment.Version.Major switch
    {
        // Before .NET 7, we could use the `RuntimeHelpers.PrepareMethod` method to immediately promote
        // a method to Tier1. This meant there was a very low chance it would be further optimized and
        // thus break our injection (not impossible, mind you, but that's rarely, if ever, a problem
        // for this project, so good enough, I guess).
        < 7 => true,

        // In .NET 7, however, `RuntimeHelpers.PrepareMethod` effectively became a no-op.
        // As a result, it can no longer be used to promote a method to Tier1, nor even
        // to precompile it at all (i.e., Tier0).
        // Consequently, JIT optimizations MUST be disabled; otherwise, our injection
        // will be dismantled on the very first call to the method being injected into.
        // At that point, can it even be called an injection?
        // https://github.com/dotnet/runtime/issues/83042
        < 9 => IsJitOptimizerDisabled,

        // Unfortunately, .NET 9 completely broke this technique.
        _ => false,
    };

    public override InjectionType InjectionType => InjectionType.PointerSwap;

    public override bool CanInjectInto([NotNullWhen(true)] MethodBase? target)
        => base.CanInjectInto(target) && TryGetFunctionPointerAddress(target, out _);

    protected override IDisposable CreateHook(MethodBase target, MethodInfo callback, object? thisArg = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(callback);
        if (thisArg is not null)
            ArgumentException.Throw(nameof(thisArg), "Bound callbacks are not supported.");
        if (!TryGetFunctionPointerAddress(target, out nint fromFunctionPointerAddress))
            ArgumentException.Throw(nameof(callback), "The provided method does not have a stable managed function pointer.");

        return new Hook(fromFunctionPointerAddress, callback.GetFunctionPointer());
    }

    private static bool TryGetFunctionPointerAddress(MethodBase method, out nint address)
    {
        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        if (method is { IsVirtual: true, DeclaringType: not null })
        {
            nint methodTable = method.DeclaringType.TypeHandle.Value;
            int methodTableOffset = IntPtr.Size == sizeof(int) ? 40 : 64;
            nint firstVirtualMethodAddress = Marshal.ReadIntPtr(methodTable + methodTableOffset);

            nint methodDescriptor = method.MethodHandle.Value;
            int methodIndex = (int)((Marshal.ReadInt64(methodDescriptor) >> 32) & 0xFFFF);
            address = firstVirtualMethodAddress + methodIndex * IntPtr.Size;
        }
        else
        {
            address = method.MethodHandle.Value + sizeof(long);
        }
        return Marshal.ReadIntPtr(address) == method.MethodHandle.GetFunctionPointer();
    }

    protected override void LoadDelegate(ILGenerator il, MethodBase target, Type delegateType, ref Action<IDisposable> ctor, MethodBuilder dispose)
    {
        TypeBuilder builder = (TypeBuilder)dispose.DeclaringType!;
        FieldInfo delegateField = builder.DefineField("s_delegate", delegateType, FieldAttributes.Private | FieldAttributes.Static);
        if (target.IsVirtual)
        {
            // If we use an ldvirtfn pointer to instantiate a delegate, it will "helpfully" resolve
            // the virtual call for us, sending execution into an infinite loop. Because of this,
            // we must create a wrapper method that invokes the target method directly via calli,
            // and then construct a delegate pointing to that wrapper instead.
            MethodBuilder trampoline = CreateCalliTrampoline(builder, target);
            ctor += x => x.GetType().GetField(delegateField.Name, (BindingFlags)(-1)).SetValue(null, x.GetType().GetMethod(trampoline.Name, (BindingFlags)(-1)).CreateUnsafeDelegate(delegateType));
        }
        else
        {
            ctor += x => x.GetType().GetField(delegateField.Name, (BindingFlags)(-1)).SetValue(null, target.CreateUnsafeDelegate(delegateType));
        }

        il.Emit(OpCodes.Ldsfld, delegateField);
        dispose.GetILGenerator().Emit(OpCodes.Ldnull);
        dispose.GetILGenerator().Emit(OpCodes.Stsfld, delegateField);
    }

    private static MethodBuilder CreateCalliTrampoline(TypeBuilder builder, MethodBase method)
    {
        Type returnType = method is MethodInfo m ? m.ReturnType : typeof(void);
        Type? thisType = method.IsStatic ? null : method.DeclaringType!.IsValueType ? method.DeclaringType.MakeByRefType() : method.DeclaringType;
        Type[] parameters = method.GetParameterTypes();
        Type[] trampolineParameters = thisType is null ? parameters : [thisType, .. parameters];

        MethodAttributes attributes = MethodAttributes.Private | MethodAttributes.Static;
        CallingConventions callingConvention = method.CallingConvention & ~(CallingConventions.HasThis | CallingConventions.ExplicitThis);
        MethodBuilder trampoline = builder.DefineMethod($"<&{method.Name}>", attributes, callingConvention, returnType, trampolineParameters);

        ILGenerator il = trampoline.GetILGenerator();
        for (int i = 0; i < trampolineParameters.Length; i++)
            il.Emit(OpCodes.Ldarg, i);

        il.Emit(OpCodes.Ldc_I8, method.GetFunctionPointer());
        il.Emit(OpCodes.Conv_U);
        il.EmitCalli(OpCodes.Calli, method.CallingConvention, returnType, parameters, null);
        il.Emit(OpCodes.Ret);
        return trampoline;
    }

    private sealed class Hook : IDisposable
    {
        private readonly nint _fromFunctionPointerAddress;

        private readonly nint _fromFunctionPointer;

        private readonly nint _toFunctionPointer;

        public Hook(nint fromFunctionPointerAddress, nint toFunctionPointer)
        {
            _fromFunctionPointerAddress = fromFunctionPointerAddress;
            _fromFunctionPointer = Marshal.ReadIntPtr(_fromFunctionPointerAddress);
            _toFunctionPointer = toFunctionPointer;

            Apply();
        }

        ~Hook()
        {
            Undo();
        }

        public void Apply() => Marshal.WriteIntPtr(_fromFunctionPointerAddress, _toFunctionPointer);

        public void Undo() => Marshal.WriteIntPtr(_fromFunctionPointerAddress, _fromFunctionPointer);

        public void Dispose()
        {
            Undo();
            GC.SuppressFinalize(this);
        }
    }
}
