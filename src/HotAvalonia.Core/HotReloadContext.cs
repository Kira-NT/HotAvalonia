using System.ComponentModel;

namespace HotAvalonia;

/// <summary>
/// Provides factory methods and utilities for working with <see cref="IHotReloadContext"/> instances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HotReloadContext
{
    /// <inheritdoc cref="Combine(IHotReloadContext, IEnumerable&lt;IHotReloadContext&gt;)"/>
    public static IHotReloadContext Combine(this IHotReloadContext context, params IHotReloadContext[] contexts)
        => Combine(context, (IEnumerable<IHotReloadContext>)contexts);

    /// <summary>
    /// Combines the specified <see cref="IHotReloadContext"/> with a collection
    /// of additional contexts into a single <see cref="IHotReloadContext"/>.
    /// </summary>
    /// <param name="context">The base <see cref="IHotReloadContext"/>.</param>
    /// <param name="contexts">A collection of additional contexts to combine with the base one.</param>
    /// <returns>A combined <see cref="IHotReloadContext"/>.</returns>
    public static IHotReloadContext Combine(this IHotReloadContext context, IEnumerable<IHotReloadContext> contexts)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contexts);

        return Combine(contexts.Concat([context]));
    }

    /// <summary>
    /// Combines a collection of <see cref="IHotReloadContext"/> instances into
    /// a single <see cref="IHotReloadContext"/>.
    /// </summary>
    /// <param name="contexts">A collection of contexts to combine.</param>
    /// <returns>A combined <see cref="IHotReloadContext"/>.</returns>
    public static IHotReloadContext Combine(this IEnumerable<IHotReloadContext> contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        IHotReloadContext[] contextArray = contexts
            .SelectMany(static x => x is CombinedHotReloadContext c ? c.AsEnumerable() : [x])
            .Where(static x => x is not null)
            .ToArray();

        return new CombinedHotReloadContext(contextArray);
    }
}
