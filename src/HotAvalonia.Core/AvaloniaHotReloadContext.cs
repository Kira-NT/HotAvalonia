using System.ComponentModel;
using System.Reflection;
using HotAvalonia.IO;

namespace HotAvalonia;

/// <summary>
/// Provides methods to create hot reload contexts for Avalonia applications.
/// </summary>
public static class AvaloniaHotReloadContext
{
    /// <inheritdoc cref="Create(AvaloniaProjectLocator)"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use 'Create(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext Create()
        => Create(AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="Create(AvaloniaHotReloadConfig)"/>
    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use 'Create(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext Create(AvaloniaProjectLocator projectLocator)
        => Create(AvaloniaHotReloadConfig.Default with { ProjectLocator = projectLocator });

    /// <summary>
    /// Creates a hot reload context for the current environment.
    /// </summary>
    /// <remarks>
    /// This method is opinionated and represents the "best" way to create
    /// a hot reload context for the current environment.
    /// However, the specific details of what constitutes "best" are subject to change.
    /// </remarks>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>A hot reload context for the current environment.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IHotReloadContext Create(AvaloniaHotReloadConfig config)
    {
        IHotReloadContext appDomainContext = FromAppDomain(config);
        IHotReloadContext assetContext = ForAssets(config);
        return HotReloadContext.Combine([appDomainContext, assetContext]);
    }

    /// <inheritdoc cref="CreateLite(AvaloniaProjectLocator)"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use 'CreateLite(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext CreateLite()
        => CreateLite(AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="CreateLite(AvaloniaHotReloadConfig)"/>
    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use 'CreateLite(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext CreateLite(AvaloniaProjectLocator projectLocator)
        => CreateLite(AvaloniaHotReloadConfig.Default with { ProjectLocator = projectLocator });

    /// <summary>
    /// Creates a lightweight hot reload context for the current environment.
    /// </summary>
    /// <remarks>
    /// This method is opinionated and represents the "best" lightweight way to create
    /// a hot reload context for the current environment. However, the specific details
    /// of what constitutes "best" are subject to change.
    /// </remarks>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>A lightweight hot reload context for the current environment.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IHotReloadContext CreateLite(AvaloniaHotReloadConfig config)
        => FromAppDomain(config);

    /// <inheritdoc cref="ForAssets(IServiceProvider)"/>
    [Obsolete("Use 'ForAssets(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext ForAssets()
        => ForAssets(AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="ForAssets(IServiceProvider, AvaloniaProjectLocator)"/>
    [Obsolete("Use 'ForAssets(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext ForAssets(IServiceProvider serviceProvider)
        => ForAssets(AvaloniaHotReloadConfig.Default with { ServiceProvider = serviceProvider });

    /// <inheritdoc cref="ForAssets(AvaloniaHotReloadConfig)"/>
    /// <param name="serviceProvider">The service provider defining <c>IAssetLoader</c>.</param>
    /// <param name="projectLocator">The project locator used to find source directories of assets.</param>
    [Obsolete("Use 'ForAssets(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext ForAssets(IServiceProvider serviceProvider, AvaloniaProjectLocator projectLocator)
        => ForAssets(AvaloniaHotReloadConfig.Default with { ServiceProvider = serviceProvider, ProjectLocator = projectLocator });

    /// <summary>
    /// Creates a hot reload context for Avalonia assets.
    /// </summary>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>A hot reload context for Avalonia assets.</returns>
    public static IHotReloadContext ForAssets(AvaloniaHotReloadConfig config)
        => new AvaloniaAssetsHotReloadContext(config);

    /// <summary>
    /// Creates a hot reload context for all assemblies within the current <see cref="AppDomain"/>.
    /// </summary>
    /// <returns>A hot reload context for the current application domain.</returns>
    /// <inheritdoc cref="FromAppDomain(AppDomain)"/>
    [Obsolete("Use 'FromAppDomain(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAppDomain()
        => FromAppDomain(AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="FromAppDomain(AppDomain, AvaloniaProjectLocator)"/>
    [Obsolete("Use 'FromAppDomain(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAppDomain(AppDomain appDomain)
        => FromAppDomain(AvaloniaHotReloadConfig.Default with { AppDomain = appDomain });

    /// <inheritdoc cref="FromAppDomain(AvaloniaHotReloadConfig)"/>
    /// <param name="appDomain">The <see cref="AppDomain"/> to create the hot reload context from.</param>
    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    [Obsolete("Use 'FromAppDomain(AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAppDomain(AppDomain appDomain, AvaloniaProjectLocator projectLocator)
        => FromAppDomain(AvaloniaHotReloadConfig.Default with { AppDomain = appDomain, ProjectLocator = projectLocator });

    /// <summary>
    /// Creates a hot reload context for all assemblies within the <see cref="AvaloniaHotReloadConfig.AppDomain"/>.
    /// </summary>
    /// <remarks>
    /// This context will include all currently loaded assemblies and any of those that are loaded
    /// in the future, automatically determining if they contain Avalonia controls and if their
    /// source project directories can be located.
    /// </remarks>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>A hot reload context for the specified application domain.</returns>
    public static IHotReloadContext FromAppDomain(AvaloniaHotReloadConfig config)
        => AvaloniaSolutionHotReloadContext.FromAppDomain(config);

    /// <inheritdoc cref="FromAssembly(Assembly, string)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly)
        => FromAssembly(assembly, AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="FromAssembly(Assembly, string, IFileSystem)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly, string rootPath)
        => FromAssembly(assembly, rootPath, AvaloniaHotReloadConfig.Default);

    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    /// <inheritdoc cref="FromAssembly(Assembly, string)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly, AvaloniaProjectLocator projectLocator)
        => FromAssembly(assembly, AvaloniaHotReloadConfig.Default with { ProjectLocator = projectLocator });

    /// <inheritdoc cref="FromAssembly(Assembly, string, AvaloniaHotReloadConfig)"/>
    public static IHotReloadContext FromAssembly(Assembly assembly, AvaloniaHotReloadConfig config)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return AvaloniaSolutionHotReloadContext.FromAssembly(assembly, config);
    }

    /// <inheritdoc cref="FromAssembly(Assembly, string, AvaloniaHotReloadConfig)"/>
    /// <param name="fileSystem">The file system where <paramref name="assembly"/> resides.</param>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly, string rootPath, IFileSystem fileSystem)
        => FromAssembly(assembly, rootPath, AvaloniaHotReloadConfig.Default with { FileSystem = fileSystem });

    /// <summary>
    /// Creates a hot reload context from the specified assembly, representing a single Avalonia project.
    /// </summary>
    /// <param name="assembly">The assembly to create the hot reload context from.</param>
    /// <param name="rootPath">
    /// The root path associated with the specified assembly,
    /// which is the directory containing its source code.
    /// </param>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>A hot reload context for the specified assembly.</returns>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly, string rootPath, AvaloniaHotReloadConfig config)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        config.ProjectLocator.AddHint(assembly, rootPath);
        return FromAssembly(assembly, config);
    }

    /// <inheritdoc cref="FromControl(object, string)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(object control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return FromControl(control.GetType());
    }

    /// <inheritdoc cref="FromControl(Type, string)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(Type controlType)
    {
        ArgumentNullException.ThrowIfNull(controlType);
        return FromAssembly(controlType.Assembly);
    }

    /// <summary>
    /// Creates a hot reload context for the assembly containing the specified control.
    /// </summary>
    /// <param name="control">The control to create the hot reload context from.</param>
    /// <param name="controlPath">The path to the control's XAML file.</param>
    /// <returns>A hot reload context for the specified control.</returns>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(object control, string controlPath)
    {
        ArgumentNullException.ThrowIfNull(control);

        return FromControl(control.GetType(), controlPath);
    }

    /// <inheritdoc cref="FromControl(Type, string, IFileSystem)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(Type controlType, string controlPath)
        => FromControl(controlType, controlPath, AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="FromControl(Type, string, AvaloniaHotReloadConfig)"/>
    /// <param name="fileSystem">The file system where <paramref name="controlPath"/> can be found.</param>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(Type controlType, string controlPath, IFileSystem fileSystem)
        => FromControl(controlType, controlPath, AvaloniaHotReloadConfig.Default with { FileSystem = fileSystem });

    /// <summary>
    /// Creates a hot reload context for the assembly containing the specified control.
    /// </summary>
    /// <param name="controlType">The type of the control to create the hot reload context from.</param>
    /// <param name="controlPath">The path to the control's XAML file.</param>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>A hot reload context for the specified control type.</returns>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(Type controlType, string controlPath, AvaloniaHotReloadConfig config)
    {
        ArgumentNullException.ThrowIfNull(controlType);
        ArgumentNullException.ThrowIfNull(controlPath);
        ArgumentNullException.ThrowIfNull(config);

        config.ProjectLocator.AddHint(controlType, controlPath);
        return FromAssembly(controlType.Assembly, config);
    }
}
