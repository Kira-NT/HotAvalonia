using System.ComponentModel;

namespace HotAvalonia;

/// <summary>
/// An <see cref="IHotReloadContext"/> implementation that defers all operations
/// to an underlying hot reload context that is created lazily.
/// </summary>
internal sealed class LazyHotReloadContext : IHotReloadContext, ISupportInitialize
{
    /// <summary>
    /// The underlying hot reload context.
    /// </summary>
    private readonly Lazy<IHotReloadContext> _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyHotReloadContext"/> class.
    /// </summary>
    /// <param name="context">A <see cref="Lazy{T}"/> that provides the underlying <see cref="IHotReloadContext"/>.</param>
    public LazyHotReloadContext(Lazy<IHotReloadContext> context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled
        => _context is { IsValueCreated: true } ctx && ctx.Value.IsHotReloadEnabled;

    /// <inheritdoc/>
    public void BeginInit()
        => (_context.Value as ISupportInitialize)?.BeginInit();

    /// <inheritdoc/>
    public void EndInit()
        => (_context.Value as ISupportInitialize)?.EndInit();

    /// <inheritdoc/>
    public void TriggerHotReload()
        => _context.Value.TriggerHotReload();

    /// <inheritdoc/>
    public void EnableHotReload()
        => _context.Value.EnableHotReload();

    /// <inheritdoc/>
    public void DisableHotReload()
    {
        if (_context is { IsValueCreated: true } context)
            context.Value.DisableHotReload();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_context is { IsValueCreated: true } context)
            context.Value.Dispose();
    }
}
