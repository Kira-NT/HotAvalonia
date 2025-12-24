using System.Reflection;

namespace HotAvalonia;

/// <summary>
/// Provides factory methods and utilities for working with <see cref="IHotReloadContext"/> instances.
/// </summary>
public static class HotReloadContext
{
    /// <summary>
    /// Creates a new <see cref="IHotReloadContext"/> for the specified <see cref="AppDomain"/>
    /// using the provided context factory.
    /// </summary>
    /// <param name="appDomain">The <see cref="AppDomain"/> to create the context for.</param>
    /// <param name="contextFactory">
    /// The factory function to create <see cref="IHotReloadContext"/> instances for
    /// the assemblies that have been loaded into the execution context of
    /// the specified application domain.
    /// </param>
    /// <returns>A new <see cref="IHotReloadContext"/> for the specified <see cref="AppDomain"/>.</returns>
    public static IHotReloadContext FromAppDomain(
        AppDomain appDomain,
        Func<IHotReloadContext, AppDomain, Assembly, IHotReloadContext?> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(appDomain);
        ArgumentNullException.ThrowIfNull(contextFactory);

        return new AppDomainHotReloadContext(appDomain, contextFactory);
    }

    /// <inheritdoc cref="FromAppDomain(AppDomain, Func{IHotReloadContext, AppDomain, Assembly, IHotReloadContext?})"/>
    public static IHotReloadContext FromAppDomain(
        AppDomain appDomain,
        Func<AppDomain, Assembly, IHotReloadContext?> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(appDomain);
        ArgumentNullException.ThrowIfNull(contextFactory);

        return new AppDomainHotReloadContext(appDomain, (_, dom, asm) => contextFactory(dom, asm));
    }

    /// <inheritdoc cref="FromAppDomain(AppDomain, Func{IHotReloadContext, AppDomain, Assembly, IHotReloadContext?})"/>
    public static IHotReloadContext FromAppDomain(
        AppDomain appDomain,
        Func<Assembly, IHotReloadContext?> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(appDomain);
        ArgumentNullException.ThrowIfNull(contextFactory);

        return new AppDomainHotReloadContext(appDomain, (_, _, asm) => contextFactory(asm));
    }

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
