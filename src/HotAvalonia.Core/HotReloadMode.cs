namespace HotAvalonia;

/// <summary>
/// Specifies how extensively the application and its components should be reloaded when a change is detected.
/// </summary>
public enum HotReloadMode
{
    /// <summary>
    /// Reloads only the control that was directly modified.
    /// </summary>
    /// <remarks>
    /// This mode provides the fastest reload times but does not account
    /// for dependencies or consumers of the modified control.
    /// </remarks>
    Minimal,

    /// <summary>
    /// Reloads the modified control and all controls that reference it.
    /// </summary>
    /// <remarks>
    /// This mode offers a balance between reload performance and correctness
    /// by ensuring that dependent controls are updated.
    /// </remarks>
    Balanced,

    /// <summary>
    /// Reloads the entire application for every detected change.
    /// </summary>
    /// <remarks>
    /// This mode provides the strongest consistency guarantees but
    /// incurs the highest performance cost.
    /// </remarks>
    Aggressive,
}
