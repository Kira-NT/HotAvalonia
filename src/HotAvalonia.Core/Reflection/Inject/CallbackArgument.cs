namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Represents a single argument passed to a callback method, describing
/// how its value is sourced from the target method invocation.
/// </summary>
internal readonly struct CallbackArgument
{
    /// <summary>
    /// Specifies the kind of value supplied for this callback argument.
    /// </summary>
    public readonly CallbackArgumentType Type;

    /// <summary>
    /// Gets the zero-based index of the target method argument
    /// from which the value is taken.
    /// </summary>
    public readonly int Index;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackArgument"/> structure.
    /// </summary>
    /// <param name="type">The kind of callback argument.</param>
    public CallbackArgument(CallbackArgumentType type)
    {
        Type = type;
        Index = -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackArgument"/> structure.
    /// </summary>
    /// <param name="index">The zero-based index of the target method argument.</param>
    public CallbackArgument(int index)
    {
        Type = CallbackArgumentType.Argument;
        Index = index;
    }
}
