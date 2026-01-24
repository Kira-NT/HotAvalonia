namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Specifies the source of a callback method argument.
/// </summary>
internal enum CallbackArgumentType
{
    /// <summary>
    /// The argument value is taken from a parameter of the target method.
    /// </summary>
    Argument,

    /// <summary>
    /// The argument value is a delegate that can be used
    /// to invoke the original target method.
    /// </summary>
    Delegate,

    /// <summary>
    /// The argument value is an object instance bound to the callback invocation.
    /// </summary>
    BoundObject,
}
