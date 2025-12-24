namespace HotAvalonia;

/// <summary>
/// Represents a context for managing hot reload functionality within an application.
/// </summary>
public interface IHotReloadContext : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether hot reload is currently enabled.
    /// </summary>
    bool IsHotReloadEnabled { get; }

    /// <summary>
    /// Enables the hot reload functionality.
    /// </summary>
    void EnableHotReload();

    /// <summary>
    /// Disables the hot reload functionality.
    /// </summary>
    void DisableHotReload();
}
