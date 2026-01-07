using System.ComponentModel;
using System.Reflection;
using HotAvalonia.Helpers;
using HotAvalonia.IO;
using HotAvalonia.Logging;
using HotAvalonia.Xaml;

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
    public static IHotReloadContext Create(AvaloniaHotReloadConfig config) => CreateRooted(config, static config =>
    {
        IHotReloadContext appDomainContext = FromAppDomain(config);
        IHotReloadContext assetContext = ForAssets(config);
        return HotReloadContext.Combine([appDomainContext, assetContext]);
    });

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
        => CreateRooted(config, static config => new AvaloniaAssetsHotReloadContext(config));

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
        => CreateRooted(config, static config => HotReloadContext.FromAppDomain(config.AppDomain, (ctx, _, asm) => FromUnverifiedAssembly(ctx, asm, config)));

    /// <summary>
    /// Creates a hot reload context from the specified assembly, if it contains Avalonia controls;
    /// otherwise, returns <c>null</c>.
    /// </summary>
    /// <param name="context">The parent hot reload context, if any.</param>
    /// <param name="assembly">The assembly to create the hot reload context from.</param>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>
    /// A hot reload context for the specified assembly, or <c>null</c> if the assembly
    /// does not contain Avalonia controls or if its source project cannot be located.
    /// </returns>
    private static IHotReloadContext? FromUnverifiedAssembly(IHotReloadContext context, Assembly assembly, AvaloniaHotReloadConfig config)
    {
        CompiledXamlDocument[] documents = XamlScanner.GetDocuments(assembly).ToArray();
        if (documents.Length == 0)
            return null;

        AvaloniaProjectLocator projectLocator = config.ProjectLocator;
        string? assemblyName = assembly.GetName().Name;
        if (!projectLocator.TryGetDirectoryName(assembly, documents, out string? rootPath))
        {
            // At runtime, there is no reliable way to determine whether a given library comes from
            // a NuGet package or a referenced project. This means we cannot simply emit a warning,
            // as that could spam users' logs with information about libraries they do not own.
            //
            // On the other hand, emitting a potential warning as a mere informational message is
            // also problematic, because it can obscure debugging when something actually goes wrong.
            //
            // So here's a compromise: if the inspected assembly shares a prefix with the entry assembly
            // (e.g., "Foo.Core" and "Foo.Desktop"), emit a warning, as this most likely indicates
            // a referenced project. Otherwise, emit an informational event. This simple heuristic
            // should cover the most common cases and is good enough for a log message.
            LogLevel logLevel = assemblyName?.Split('.')[0] == Assembly.GetEntryAssembly()?.GetName().Name?.Split('.')[0] ? LogLevel.Error : LogLevel.Information;
            Logger.Log(logLevel, context, "Failed to create a hot reload context for '{Assembly}': sources not found.", assemblyName);
            return null;
        }

        if (!projectLocator.FileSystem.DirectoryExists(rootPath))
        {
            // Same as above.
            LogLevel logLevel = assemblyName?.Split('.')[0] == Assembly.GetEntryAssembly()?.GetName().Name?.Split('.')[0] ? LogLevel.Error : LogLevel.Information;
            Logger.Log(logLevel, context, "Failed to create a hot reload context for '{Assembly}': '{Location}' not found.", assemblyName, rootPath);
            return null;
        }

        Logger.LogInfo(context, "Loading new hot reload context for '{Assembly}' from '{Location}'...", assemblyName, rootPath);
        return new AvaloniaProjectHotReloadContext(rootPath, documents, config);
    }

    /// <inheritdoc cref="FromAssembly(Assembly, string)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly)
        => FromAssembly(assembly, AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="FromAssembly(Assembly, string, IFileSystem)"/>
    [Obsolete("Use 'FromAssembly(Assembly, string, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly, string rootPath)
        => FromAssembly(assembly, rootPath, AvaloniaHotReloadConfig.Default);

    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    /// <inheritdoc cref="FromAssembly(Assembly, string)"/>
    [Obsolete("Use 'FromAssembly(Assembly, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromAssembly(Assembly assembly, AvaloniaProjectLocator projectLocator)
        => FromAssembly(assembly, AvaloniaHotReloadConfig.Default with { ProjectLocator = projectLocator });

    /// <inheritdoc cref="FromAssembly(Assembly, string, AvaloniaHotReloadConfig)"/>
    public static IHotReloadContext FromAssembly(Assembly assembly, AvaloniaHotReloadConfig config) => CreateRooted(config, config =>
    {
        ArgumentNullException.ThrowIfNull(assembly);

        CompiledXamlDocument[] documents = XamlScanner.GetDocuments(assembly).ToArray();
        string rootPath = config.ProjectLocator.GetDirectoryName(assembly, documents);
        return new AvaloniaProjectHotReloadContext(rootPath, documents, config);
    });

    /// <inheritdoc cref="FromAssembly(Assembly, string, AvaloniaHotReloadConfig)"/>
    /// <param name="fileSystem">The file system where <paramref name="assembly"/> resides.</param>
    [Obsolete("Use 'FromAssembly(Assembly, string, AvaloniaHotReloadConfig)' instead.")]
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
    public static IHotReloadContext FromAssembly(Assembly assembly, string rootPath, AvaloniaHotReloadConfig config) => CreateRooted(config, config =>
    {
        ArgumentNullException.ThrowIfNull(assembly);

        IEnumerable<CompiledXamlDocument> documents = XamlScanner.GetDocuments(assembly);
        return new AvaloniaProjectHotReloadContext(rootPath, documents, config);
    });

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
    [Obsolete("Use 'FromControl(Type, string, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(object control, string controlPath)
    {
        ArgumentNullException.ThrowIfNull(control);

        return FromControl(control.GetType(), controlPath);
    }

    /// <inheritdoc cref="FromControl(Type, string, IFileSystem)"/>
    [Obsolete("Use 'FromControl(Type, string, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(Type controlType, string controlPath)
        => FromControl(controlType, controlPath, AvaloniaHotReloadConfig.Default);

    /// <inheritdoc cref="FromControl(Type, string, AvaloniaHotReloadConfig)"/>
    /// <param name="fileSystem">The file system where <paramref name="controlPath"/> can be found.</param>
    [Obsolete("Use 'FromControl(Type, string, AvaloniaHotReloadConfig)' instead.")]
    public static IHotReloadContext FromControl(Type controlType, string controlPath, IFileSystem fileSystem)
        => FromControl(controlType, controlPath, AvaloniaHotReloadConfig.Default with { FileSystem = fileSystem });

    /// <summary>
    /// Creates a hot reload context for the assembly containing the specified control.
    /// </summary>
    /// <param name="controlType">The type of the control to create the hot reload context from.</param>
    /// <param name="controlPath">The path to the control's XAML file.</param>
    /// <param name="config">The hot reload configuration to use.</param>
    /// <returns>A hot reload context for the specified control type.</returns>
    public static IHotReloadContext FromControl(Type controlType, string controlPath, AvaloniaHotReloadConfig config)
    {
        ArgumentNullException.ThrowIfNull(controlType);
        ArgumentNullException.ThrowIfNull(controlPath);
        ArgumentNullException.ThrowIfNull(config);

        IFileSystem fileSystem = config.FileSystem;
        if (!fileSystem.FileExists(controlPath))
            throw new FileNotFoundException(null, controlPath);

        controlPath = fileSystem.GetFullPath(controlPath);
        if (!XamlScanner.TryExtractDocumentUri(controlType, out string? controlUri))
            ArgumentException.Throw(nameof(controlType), "The provided control is not a valid user-defined Avalonia control. Could not determine its URI.");

        string rootPath = UriHelper.ResolveHostPath(controlUri, controlPath);
        return FromAssembly(controlType.Assembly, rootPath, config);
    }

    /// <summary>
    /// Creates a new hot reload context and assigns it as the root context if it has not been configured yet.
    /// </summary>
    /// <param name="config">The hot reload configuration that stores the root context.</param>
    /// <param name="factory">The factory function used to create a new hot reload context.</param>
    /// <returns>A newly created hot reload context.</returns>
    private static IHotReloadContext CreateRooted(AvaloniaHotReloadConfig config, Func<AvaloniaHotReloadConfig, IHotReloadContext> factory)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Root is not null)
            return factory(config);

        config = config with { Root = HotReloadContext.Lazy(() => factory(config)) };
        return config.Root;
    }
}
