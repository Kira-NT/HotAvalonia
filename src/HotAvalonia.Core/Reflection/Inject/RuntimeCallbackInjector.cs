using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HotAvalonia.Helpers;

namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Represents a runtime component capable of injecting managed callbacks into methods.
/// </summary>
internal abstract class RuntimeCallbackInjector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeCallbackInjector"/> class.
    /// </summary>
    private protected RuntimeCallbackInjector() { }

    /// <summary>
    /// Gets a value indicating whether JIT optimizations are effectively disabled.
    /// </summary>
    /// <remarks>
    /// <see cref="DebuggableAttribute.IsJITOptimizerDisabled"/> is only guaranteed to be honored when
    /// a debugger or a debugger-like tool (such as <c>dotnet watch</c>) is attached to the process.
    /// </remarks>
    protected static bool IsJitOptimizerDisabled => Debugger.IsAttached || Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1";

    /// <summary>
    /// Gets the injection technique used by this injector.
    /// </summary>
    public abstract InjectionType InjectionType { get; }

    /// <summary>
    /// Determines whether the specified method can be injected into by this injector.
    /// </summary>
    /// <param name="target">The method to inspect.</param>
    /// <returns><c>true</c> if the method can be injected into; otherwise, <c>false</c>.</returns>
    public virtual bool CanInjectInto([NotNullWhen(true)] MethodBase? target)
    {
        if (target is not { IsGenericMethod: false, DeclaringType: not { IsGenericType: true } })
            return false;

        return target.Module.Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled is true;
    }

    /// <inheritdoc cref="TryInject(MethodBase, MethodInfo, object?, out IDisposable?)"/>
    public bool TryInject(MethodBase target, Delegate callback, [NotNullWhen(true)] out IDisposable? injection)
        => TryInject(new(target, callback), out injection);

    /// <inheritdoc cref="TryInject(MethodBase, MethodInfo, object?, out IDisposable?)"/>
    public bool TryInject(MethodBase target, MethodInfo callback, [NotNullWhen(true)] out IDisposable? injection)
        => TryInject(new(target, callback), out injection);

    /// <summary>
    /// Attempts to inject a callback into the specified target method.
    /// </summary>
    /// <param name="target">The method into which the callback is injected.</param>
    /// <param name="callback">The method to invoke as a callback.</param>
    /// <param name="thisArg">The object instance to use as the first argument (e.g., <c>this</c>) when invoking the callback.</param>
    /// <param name="injection">When this method returns <c>true</c>, contains an object that removes the injection when disposed.</param>
    /// <returns><c>true</c> if the injection succeeds; otherwise, <c>false</c>.</returns>
    public bool TryInject(MethodBase target, MethodInfo callback, object? thisArg, [NotNullWhen(true)] out IDisposable? injection)
        => TryInject(new(target, callback, thisArg), out injection);

    private bool TryInject(CallbackInfo callback, [NotNullWhen(true)] out IDisposable? injection)
    {
        if (CanInjectInto(callback.Target))
        {
            injection = InjectUnsafe(callback);
            return true;
        }
        injection = null;
        return false;
    }

    /// <inheritdoc cref="Inject(MethodBase, MethodInfo, object?)"/>
    public IDisposable Inject(MethodBase target, Delegate callback)
        => Inject(new(target, callback));

    /// <inheritdoc cref="Inject(MethodBase, MethodInfo, object?)"/>
    public IDisposable Inject(MethodBase target, MethodInfo callback)
        => Inject(new(target, callback));

    /// <summary>
    /// Injects a callback into the specified target method.
    /// </summary>
    /// <param name="target">The method to inject into.</param>
    /// <param name="callback">The method to invoke as a callback.</param>
    /// <param name="thisArg">The object instance to use as the first argument (e.g., <c>this</c>) when invoking the callback.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the injection when disposed.</returns>
    public IDisposable Inject(MethodBase target, MethodInfo callback, object? thisArg)
        => Inject(new(target, callback, thisArg));

    private IDisposable Inject(CallbackInfo callback)
    {
        if (!CanInjectInto(callback.Target))
            ArgumentException.Throw(nameof(callback), "The provided method cannot be injected into.");

        return InjectUnsafe(callback);
    }

    private IDisposable InjectUnsafe(CallbackInfo callback)
    {
        if (!RequiresCallbackTrampoline(callback))
            return CreateHook(callback.Target, callback.Callback, callback.FirstArgument);

        IDisposable hook = Unsafe.As<IDisposable>(RuntimeHelpers.GetUninitializedObject(CreateHookType(callback, out Action<IDisposable> ctor)));
        ctor(hook);
        return hook;
    }

    /// <summary>
    /// Creates a hook object that represents an injection into the specified target method.
    /// </summary>
    /// <param name="target">The method into which the callback is injected.</param>
    /// <param name="callback">The method to invoke as a callback.</param>
    /// <param name="thisArg">The object instance to use as the first argument (e.g., <c>this</c>) when invoking the callback.</param>
    /// <returns>An <see cref="IDisposable"/> representing the hook. Disposing it removes the injection.</returns>
    protected abstract IDisposable CreateHook(MethodBase target, MethodInfo callback, object? thisArg = null);

    /// <summary>
    /// Determines whether the specified callback requires a trampoline method.
    /// </summary>
    /// <param name="callback">The information describing the callback.</param>
    /// <returns><c>true</c> if a trampoline is required; otherwise, <c>false</c>.</returns>
    protected virtual bool RequiresCallbackTrampoline(CallbackInfo callback)
    {
        if (!callback.Target.IsStatic || !callback.Callback.IsStatic)
            return true;

        if (callback.GetTargetReturnType() == typeof(void) && callback.GetCallbackReturnType() != typeof(void))
            return true;

        ReadOnlySpan<ParameterInfo> parameters = callback.GetTargetParameters();
        ReadOnlySpan<CallbackArgument> args = callback.GetCallbackArguments();
        if (args.Length != parameters.Length || args is not ([] or [{ Type: CallbackArgumentType.Argument, Index: 0 }, ..]))
            return true;

        for (int i = 1; i < args.Length; i++)
        {
            ref readonly CallbackArgument arg = ref args[i];
            if (arg.Type != CallbackArgumentType.Argument || arg.Index != args[i - 1].Index + 1)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Defines the trampoline method body for the specified callback.
    /// </summary>
    /// <param name="builder">The type builder used to define the method.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="callback">The information describing the callback.</param>
    /// <param name="extraParameterCount">
    /// When this method returns, contains the number of additional parameters introduced by the trampoline.
    /// </param>
    /// <returns>A <see cref="MethodBuilder"/> used to emit the trampoline body.</returns>
    protected virtual MethodBuilder DefineCallbackTrampoline(TypeBuilder builder, string name, CallbackInfo callback, out int extraParameterCount)
    {
        extraParameterCount = 0;
        MethodBase target = callback.Target;
        MethodAttributes attributes = target.Attributes & ~(MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Virtual);
        return builder.DefineMethod(name, attributes, target.CallingConvention, callback.GetTargetReturnType(), target.GetParameterTypes());
    }

    /// <summary>
    /// Emits IL to load a delegate representing the specified target method onto the evaluation stack.
    /// </summary>
    /// <param name="il">The <see cref="ILGenerator"/> used to emit IL.</param>
    /// <param name="target">The target method for which a delegate is being created.</param>
    /// <param name="delegateType">The delegate type to construct.</param>
    /// <param name="ctor">A managed "constructor" action for the dynamically generated hook type.</param>
    /// <param name="dispose">
    /// A <see cref="MethodBuilder"/> representing the dispose method of the generated hook type.
    /// Implementations may emit cleanup logic into this method for any resources or fields
    /// created during delegate construction.
    /// </param>
    protected abstract void LoadDelegate(ILGenerator il, MethodBase target, Type delegateType, ref Action<IDisposable> ctor, MethodBuilder dispose);

    private Type CreateHookType(CallbackInfo callback, out Action<IDisposable> ctor)
    {
        // If this were taken out of context, we would need to manually add all the required `IgnoresAccessChecksToAttribute`s
        // to allow the generated class to call both the callback and the method being injected into.
        // However, our global dynamic assembly already has all access checks suppressed, making this unnecessary.
        using IDisposable context = AssemblyHelper.GetDynamicAssembly(out AssemblyBuilder assemblyBuilder, out ModuleBuilder moduleBuilder);
        MethodBase from = callback.Target;
        MethodInfo to = callback.Callback;
        ctor = _ => { };

        // file sealed class Hook : IDisposable
        // {
        //     private static object? s_thisArg;
        //
        //     private IDisposable? _injection;
        //
        //     private static <{to.Name}>k__Trampoline(TArg0 arg0, TArg1 arg1, ..., TArgN argN)
        //     {
        //         to(s_thisArg, arg0, arg1, ..., argN);
        //     }
        //
        //     public void Dispose()
        //     {
        //         _injection?.Dispose();
        //         _injection = null;
        //         s_thisArg = null;
        //     }
        // }
        (string declaringTypeNamespace, string declaringTypeName) = from.DeclaringType is Type t ? (t.Namespace, t.Name) : ("", "<Module>");
        string name = $"{declaringTypeNamespace}.<{declaringTypeName}>{Guid.NewGuid():N}__Hook";
        TypeBuilder hookBuilder = moduleBuilder.DefineType(name, TypeAttributes.Sealed | TypeAttributes.Class, typeof(object), [typeof(IDisposable)]);
        FieldBuilder injection = hookBuilder.DefineField("_injection", typeof(IDisposable), FieldAttributes.Private);
        MethodAttributes disposeAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
        MethodBuilder dispose = hookBuilder.DefineMethod(nameof(IDisposable.Dispose), disposeAttributes, typeof(void), []);
        MethodBuilder callbackTrampoline = CreateCallbackTrampoline($"<{to.Name}>k__Trampoline", callback, ref ctor, dispose);
        ILGenerator disposeIl = dispose.GetILGenerator();
        Label notDisposable = disposeIl.DefineLabel();
        disposeIl.Emit(OpCodes.Ldarg_0);
        disposeIl.Emit(OpCodes.Ldarg_0);
        disposeIl.Emit(OpCodes.Ldfld, injection);
        disposeIl.Emit(OpCodes.Dup);
        disposeIl.Emit(OpCodes.Brfalse_S, notDisposable);
        disposeIl.Emit(OpCodes.Callvirt, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!);
        disposeIl.Emit(OpCodes.Ldnull);
        disposeIl.MarkLabel(notDisposable);
        disposeIl.Emit(OpCodes.Stfld, injection);
        disposeIl.Emit(OpCodes.Ret);

        ctor += x => x.GetType().GetField(injection.Name, (BindingFlags)(-1)).SetValue(x, CreateHook(from, x.GetType().GetMethod(callbackTrampoline.Name, (BindingFlags)(-1))));
        return hookBuilder.CreateTypeInfo();
    }

    private MethodBuilder CreateCallbackTrampoline(string name, CallbackInfo callback, ref Action<IDisposable> ctor, MethodBuilder dispose)
    {
        TypeBuilder builder = (TypeBuilder)dispose.DeclaringType;
        MethodBuilder callbackTrampoline = DefineCallbackTrampoline(builder, name, callback, out int offset);

        FieldBuilder? thisArg = null;
        if (callback.FirstArgument is object firstArg)
        {
            thisArg = builder.DefineField("s_thisArg", firstArg.GetType(), FieldAttributes.Private | FieldAttributes.Static);
            dispose.GetILGenerator().Emit(OpCodes.Ldnull);
            dispose.GetILGenerator().Emit(OpCodes.Stsfld, thisArg);
            ctor += x => x.GetType().GetField(thisArg.Name, (BindingFlags)(-1)).SetValue(null, firstArg);
        }

        ILGenerator il = callbackTrampoline.GetILGenerator();
        ReadOnlySpan<ParameterInfo> parameters = callback.GetCallbackParameters();
        ReadOnlySpan<CallbackArgument> arguments = callback.GetCallbackArguments();
        for (int i = 0; i < arguments.Length; i++)
        {
            ref readonly CallbackArgument argument = ref arguments[i];
            switch (argument.Type)
            {
                case CallbackArgumentType.Argument:
                    il.Emit(OpCodes.Ldarg, argument.Index + offset);
                    break;

                case CallbackArgumentType.Delegate:
                    LoadDelegate(il, callback.Target, callback.GetTargetDelegateType(), ref ctor, dispose);
                    break;

                case CallbackArgumentType.BoundObject when thisArg is null:
                    il.Emit(OpCodes.Ldnull);
                    break;

                case CallbackArgumentType.BoundObject:
                    il.Emit(parameters[i].ParameterType.IsByRef ? OpCodes.Ldsflda : OpCodes.Ldsfld, thisArg);
                    break;
            }
        }

        il.Emit(callback.Callback.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, callback.Callback);
        if (callback.GetTargetReturnType() == typeof(void) && callback.GetCallbackReturnType() != typeof(void))
            il.Emit(OpCodes.Pop);

        il.Emit(OpCodes.Ret);
        return callbackTrampoline;
    }
}
