using System.Linq.Expressions;
using System.Reflection;

namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Describes the relationship between a method and
/// a callback injected into its execution pipeline.
/// </summary>
internal sealed class CallbackInfo
{
    private static readonly object s_noArg = new();

    private readonly MethodBase _target;

    private readonly MethodInfo _callback;

    private readonly object? _thisArg;

    private readonly ParameterInfo[] _targetParameters;

    private readonly ParameterInfo[] _callbackParameters;

    private readonly CallbackArgument[] _callbackArguments;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackInfo"/> class.
    /// </summary>
    /// <param name="target">The method into which the callback is injected.</param>
    /// <param name="callback">The delegate representing the callback method.</param>
    public CallbackInfo(MethodBase target, Delegate callback)
        : this(target, Deconstruct(callback, out object? thisArg), thisArg)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackInfo"/> class.
    /// </summary>
    /// <param name="target">The method into which the callback is injected.</param>
    /// <param name="callback">The callback method.</param>
    public CallbackInfo(MethodBase target, MethodInfo callback)
        : this(target, callback, s_noArg)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackInfo"/> class.
    /// </summary>
    /// <param name="target">The method into which the callback is injected.</param>
    /// <param name="callback">The callback method.</param>
    /// <param name="thisArg">The object instance to bind to the callback invocation.</param>
    public CallbackInfo(MethodBase target, MethodInfo callback, object? thisArg)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(callback);
        if (!callback.IsStatic && (thisArg is null || thisArg == s_noArg))
            ArgumentNullException.Throw(nameof(thisArg));
        if (target is MethodInfo { ReturnType: Type t } && t != typeof(void) && !IsCompatible(t, callback.ReturnType))
            ArgumentException.Throw(nameof(callback), "The return type of the method is not compatible with the target method's return type.");

        _target = target;
        _callback = callback;
        _thisArg = thisArg;
        _targetParameters = target.IsStatic ? target.GetParameters() : [new ThisParameterInfo(target), .. target.GetParameters()];
        _callbackParameters = callback.IsStatic ? callback.GetParameters() : [new ThisParameterInfo(callback), .. callback.GetParameters()];
        _callbackArguments = ResolveArguments(_targetParameters, _callbackParameters, thisArg);
    }

    /// <summary>
    /// Gets the method into which the callback is injected.
    /// </summary>
    public MethodBase Target => _target;

    /// <summary>
    /// Gets the callback method.
    /// </summary>
    public MethodInfo Callback => _callback;

    /// <summary>
    /// Gets the object instance bound to the callback invocation, if any.
    /// </summary>
    public object? FirstArgument => _thisArg == s_noArg ? null : _thisArg;

    /// <summary>
    /// Gets the return type of the target method.
    /// </summary>
    public Type GetTargetReturnType() => _target is MethodInfo m ? m.ReturnType : typeof(void);

    /// <summary>
    /// Gets the return type of the callback method.
    /// </summary>
    public Type GetCallbackReturnType() => _callback.ReturnType;

    /// <summary>
    /// Gets the parameters of the target method.
    /// </summary>
    /// <remarks>
    /// The returned collection includes all parameters, including the implicit
    /// <c>this</c> argument for instance methods. As a result, the returned
    /// sequence might differ from one produced by <see cref="MethodBase.GetParameters"/>.
    /// </remarks>
    public ReadOnlySpan<ParameterInfo> GetTargetParameters() => _targetParameters;

    /// <summary>
    /// Gets the parameters of the callback method.
    /// </summary>
    /// <remarks>
    /// The returned collection includes all parameters, including the implicit
    /// <c>this</c> argument for instance methods. As a result, the returned
    /// sequence might differ from one produced by <see cref="MethodBase.GetParameters"/>.
    /// </remarks>
    public ReadOnlySpan<ParameterInfo> GetCallbackParameters() => _callbackParameters;

    /// <summary>
    /// Gets the ordered mapping of values supplied to the callback method.
    /// </summary>
    public ReadOnlySpan<CallbackArgument> GetCallbackArguments() => _callbackArguments;

    /// <summary>
    /// Gets a delegate type that represents the signature of the target method.
    /// </summary>
    public Type GetTargetDelegateType()
    {
        CallbackArgument[] args = _callbackArguments;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Type == CallbackArgumentType.Delegate)
                return _callbackParameters[i].ParameterType;
        }
        return Expression.GetDelegateType(Array.ConvertAll(_targetParameters, x => x.ParameterType));
    }

    private static MethodInfo Deconstruct(Delegate callback, out object? thisArg)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (callback.GetInvocationList().Length == 1)
        {
            thisArg = callback.Target;
            return callback.Method;
        }
        else
        {
            thisArg = callback;
            return callback.GetType().GetMethod(nameof(Action.Invoke))!;
        }
    }

    private static CallbackArgument[] ResolveArguments(ReadOnlySpan<ParameterInfo> targetParameters, ReadOnlySpan<ParameterInfo> callbackParameters, object? thisArg)
    {
        CallbackArgument[] arguments = new CallbackArgument[callbackParameters.Length];
        Span<CallbackArgument> remainingArguments = arguments;

        int boundObjectIndex = -1;
        bool hasThis = thisArg is not null && thisArg != s_noArg;
        switch (callbackParameters.Length - targetParameters.Length)
        {
            case 2 when IsCompatible(typeof(Delegate), callbackParameters[1].ParameterType):
                arguments[boundObjectIndex = 0] = new(CallbackArgumentType.BoundObject);
                arguments[1] = new(CallbackArgumentType.Delegate);
                remainingArguments = remainingArguments[2..];
                break;

            case 1 when hasThis || !IsCompatible(typeof(Delegate), callbackParameters[0].ParameterType):
                arguments[boundObjectIndex = 0] = new(CallbackArgumentType.BoundObject);
                remainingArguments = remainingArguments[1..];
                break;

            case 1 when IsCompatible(typeof(Delegate), callbackParameters[0].ParameterType):
                arguments[0] = new(CallbackArgumentType.Delegate);
                remainingArguments = remainingArguments[1..];
                break;

            case not 0:
                ArgumentException.Throw("callback", "Unable to map the method's parameters to the target method's parameters.");
                break;
        }

        if ((uint)boundObjectIndex < (uint)callbackParameters.Length && (thisArg == s_noArg || !IsCompatible(callbackParameters[boundObjectIndex].ParameterType, thisArg?.GetType() ?? typeof(object))))
            ArgumentException.Throw("thisArg", "The provided argument is not compatible with the callback method's first parameter.");

        for (int i = remainingArguments.Length - 1; i >= 0; i--)
            remainingArguments[i] = new(index: i);

        return arguments;
    }

    private static bool IsCompatible(Type left, Type right)
    {
        if (left == right)
            return true;

        left = left.IsEnum ? left.GetEnumUnderlyingType() : left;
        right = right.IsEnum ? right.GetEnumUnderlyingType() : right;

        left = left.IsPointer || left.IsByRef ? typeof(nint) : left;
        right = right.IsPointer || right.IsByRef ? typeof(nint) : right;

        return left.IsValueType == right.IsValueType && (left.IsAssignableFrom(right) || right.IsAssignableFrom(left));
    }
}

file sealed class ThisParameterInfo : ParameterInfo
{
    public ThisParameterInfo(MethodBase method)
    {
        Type type = method.DeclaringType!;
        NameImpl = "@this";
        ClassImpl = type.IsValueType ? type.MakeByRefType() : type;
        DefaultValueImpl = DBNull.Value;
        MemberImpl = method;
        PositionImpl = -1;
    }

    public override bool HasDefaultValue => false;

    public override object? DefaultValue => DefaultValueImpl;

    public override object? RawDefaultValue => DefaultValueImpl;
}
