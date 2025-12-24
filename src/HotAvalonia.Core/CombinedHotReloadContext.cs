using System.Collections;
using System.ComponentModel;

namespace HotAvalonia;

/// <summary>
/// A combined hot reload context that manages multiple <see cref="IHotReloadContext"/> instances.
/// </summary>
internal sealed class CombinedHotReloadContext : IHotReloadContext, ISupportInitialize, IEnumerable<IHotReloadContext>
{
    /// <summary>
    /// The <see cref="IHotReloadContext"/> instances to be managed.
    /// </summary>
    private readonly IHotReloadContext[] _contexts;

    /// <summary>
    /// Initializes a new instance of the <see cref="CombinedHotReloadContext"/> class.
    /// </summary>
    /// <param name="contexts">The <see cref="IHotReloadContext"/> instances to be managed.</param>
    public CombinedHotReloadContext(IHotReloadContext[] contexts)
    {
        _contexts = contexts;
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled
        => _contexts.Length != 0 && _contexts.All(static x => x.IsHotReloadEnabled);

    /// <inheritdoc/>
    public void BeginInit()
    {
        foreach (IHotReloadContext context in _contexts)
            (context as ISupportInitialize)?.BeginInit();
    }

    /// <inheritdoc/>
    public void EndInit()
    {
        foreach (IHotReloadContext context in _contexts)
            (context as ISupportInitialize)?.EndInit();
    }

    /// <inheritdoc/>
    public void EnableHotReload()
    {
        foreach (IHotReloadContext context in _contexts)
            context.EnableHotReload();
    }

    /// <inheritdoc/>
    public void DisableHotReload()
    {
        foreach (IHotReloadContext context in _contexts)
            context.DisableHotReload();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (IHotReloadContext context in _contexts)
            context.Dispose();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public IEnumerator<IHotReloadContext> GetEnumerator()
        => _contexts.AsEnumerable().GetEnumerator();
}
