using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HotAvalonia.Assets;
using HotAvalonia.DependencyInjection;
using HotAvalonia.Helpers;
using HotAvalonia.IO;
using HotAvalonia.Xaml;

namespace HotAvalonia;

/// <summary>
/// Provides methods to create hot reload contexts for Avalonia applications.
/// </summary>
public static class AvaloniaHotReloadContext
{
    /// <inheritdoc cref="Create(AvaloniaProjectLocator)"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IHotReloadContext Create()
        => Create(new AvaloniaProjectLocator());

    /// <summary>
    /// Creates a hot reload context for the current environment.
    /// </summary>
    /// <remarks>
    /// This method is opinionated and represents the "best" way to create
    /// a hot reload context for the current environment.
    /// However, the specific details of what constitutes "best" are subject to change.
    /// </remarks>
    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    /// <returns>A hot reload context for the current environment.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IHotReloadContext Create(AvaloniaProjectLocator projectLocator)
    {
        IHotReloadContext appDomainContext = FromAppDomain(AppDomain.CurrentDomain, projectLocator);
        IHotReloadContext assetContext = ForAssets(AvaloniaServiceProvider.Current, projectLocator);
        return HotReloadContext.Combine([appDomainContext, assetContext]);
    }

    /// <inheritdoc cref="CreateLite(AvaloniaProjectLocator)"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IHotReloadContext CreateLite()
        => CreateLite(new AvaloniaProjectLocator());

    /// <summary>
    /// Creates a lightweight hot reload context for the current environment.
    /// </summary>
    /// <remarks>
    /// This method is opinionated and represents the "best" lightweight way to create
    /// a hot reload context for the current environment. However, the specific details
    /// of what constitutes "best" are subject to change.
    /// </remarks>
    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    /// <returns>A lightweight hot reload context for the current environment.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IHotReloadContext CreateLite(AvaloniaProjectLocator projectLocator)
    {
        IHotReloadContext appDomainContext = FromAppDomain(AppDomain.CurrentDomain, projectLocator);
        return appDomainContext;
    }

    /// <inheritdoc cref="ForAssets(IServiceProvider)"/>
    public static IHotReloadContext ForAssets()
        => ForAssets(AvaloniaServiceProvider.Current);

    /// <inheritdoc cref="ForAssets(IServiceProvider, AvaloniaProjectLocator)"/>
    public static IHotReloadContext ForAssets(IServiceProvider serviceProvider)
        => ForAssets(serviceProvider, new AvaloniaProjectLocator());

    /// <summary>
    /// Creates a hot reload context for Avalonia assets.
    /// </summary>
    /// <param name="serviceProvider">The service provider defining <c>IAssetLoader</c>.</param>
    /// <param name="projectLocator">The project locator used to find source directories of assets.</param>
    /// <returns>A hot reload context for Avalonia assets.</returns>
    public static IHotReloadContext ForAssets(IServiceProvider serviceProvider, AvaloniaProjectLocator projectLocator)
    {
        _ = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _ = projectLocator ?? throw new ArgumentNullException(nameof(projectLocator));

        return new AvaloniaAssetsHotReloadContext(serviceProvider, projectLocator);
    }

    /// <summary>
    /// Creates a hot reload context for all assemblies within the current <see cref="AppDomain"/>.
    /// </summary>
    /// <returns>A hot reload context for the current application domain.</returns>
    /// <inheritdoc cref="FromAppDomain(AppDomain)"/>
    public static IHotReloadContext FromAppDomain()
        => FromAppDomain(AppDomain.CurrentDomain);

    /// <inheritdoc cref="FromAppDomain(AppDomain, AvaloniaProjectLocator)"/>
    public static IHotReloadContext FromAppDomain(AppDomain appDomain)
        => FromAppDomain(appDomain, new AvaloniaProjectLocator());

    /// <summary>
    /// Creates a hot reload context for all assemblies within the specified <see cref="AppDomain"/>.
    /// </summary>
    /// <remarks>
    /// This context will include all currently loaded assemblies and any of those that are loaded
    /// in the future, automatically determining if they contain Avalonia controls and if their
    /// source project directories can be located.
    /// </remarks>
    /// <param name="appDomain">The <see cref="AppDomain"/> to create the hot reload context from.</param>
    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    /// <returns>A hot reload context for the specified application domain.</returns>
    public static IHotReloadContext FromAppDomain(AppDomain appDomain, AvaloniaProjectLocator projectLocator)
    {
        _ = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
        _ = projectLocator ?? throw new ArgumentNullException(nameof(projectLocator));

        return HotReloadContext.FromAppDomain(appDomain, (_, asm) => FromUnverifiedAssembly(asm, projectLocator));
    }

    /// <summary>
    /// Creates a hot reload context from the specified assembly, if it contains Avalonia controls;
    /// otherwise, returns <c>null</c>.
    /// </summary>
    /// <param name="assembly">The assembly to create the hot reload context from.</param>
    /// <param name="projectLocator">The project locator used to find the source directory of the assembly.</param>
    /// <returns>
    /// A hot reload context for the specified assembly, or <c>null</c> if the assembly
    /// does not contain Avalonia controls or if its source project cannot be located.
    /// </returns>
    private static IHotReloadContext? FromUnverifiedAssembly(Assembly assembly, AvaloniaProjectLocator projectLocator)
    {
        CompiledXamlDocument[] documents = XamlScanner.GetDocuments(assembly).ToArray();
        if (documents.Length == 0)
            return null;

        if (!projectLocator.TryGetDirectoryName(assembly, documents, out string? rootPath))
        {
            LoggingHelper.LogInfo("Found an assembly containing Avalonia controls ({AssemblyName}). However, its source project location could not be determined. Skipping.", assembly.GetName().Name);
            return null;
        }

        if (!projectLocator.FileSystem.DirectoryExists(rootPath))
        {
            LoggingHelper.LogInfo("Found an assembly containing Avalonia controls ({AssemblyName}) with its source project located at {ProjectLocation}. However, the project could not be found on the local system. Skipping.", assembly.GetName().Name, rootPath);
            return null;
        }

        LoggingHelper.LogInfo("Found an assembly containing Avalonia controls ({AssemblyName}) with its source project located at {ProjectLocation}.", assembly.GetName().Name, rootPath);
        return new AvaloniaProjectHotReloadContext(rootPath, projectLocator.FileSystem, documents);
    }

    /// <inheritdoc cref="FromAssembly(Assembly, string)"/>
    public static IHotReloadContext FromAssembly(Assembly assembly)
        => FromAssembly(assembly, new AvaloniaProjectLocator());

    /// <inheritdoc cref="FromAssembly(Assembly, string, IFileSystem)"/>
    public static IHotReloadContext FromAssembly(Assembly assembly, string rootPath)
        => FromAssembly(assembly, rootPath, FileSystem.Current);

    /// <param name="projectLocator">The project locator used to find source directories of assemblies.</param>
    /// <inheritdoc cref="FromAssembly(Assembly, string)"/>
    public static IHotReloadContext FromAssembly(Assembly assembly, AvaloniaProjectLocator projectLocator)
    {
        _ = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _ = projectLocator ?? throw new ArgumentNullException(nameof(projectLocator));

        CompiledXamlDocument[] documents = XamlScanner.GetDocuments(assembly).ToArray();
        string rootPath = projectLocator.GetDirectoryName(assembly, documents);
        return new AvaloniaProjectHotReloadContext(rootPath, projectLocator.FileSystem, documents);
    }

    /// <summary>
    /// Creates a hot reload context from the specified assembly, representing a single Avalonia project.
    /// </summary>
    /// <param name="assembly">The assembly to create the hot reload context from.</param>
    /// <param name="rootPath">
    /// The root path associated with the specified assembly,
    /// which is the directory containing its source code.
    /// </param>
    /// <param name="fileSystem">The file system where <paramref name="assembly"/> resides.</param>
    /// <returns>A hot reload context for the specified assembly.</returns>
    public static IHotReloadContext FromAssembly(Assembly assembly, string rootPath, IFileSystem fileSystem)
    {
        _ = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        IEnumerable<CompiledXamlDocument> documents = XamlScanner.GetDocuments(assembly);
        return new AvaloniaProjectHotReloadContext(rootPath, fileSystem, documents);
    }

    /// <inheritdoc cref="FromControl(object, string)"/>
    public static IHotReloadContext FromControl(object control)
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        return FromControl(control.GetType());
    }

    /// <inheritdoc cref="FromControl(Type, string)"/>
    public static IHotReloadContext FromControl(Type controlType)
    {
        _ = controlType ?? throw new ArgumentNullException(nameof(controlType));

        return FromAssembly(controlType.Assembly);
    }

    /// <summary>
    /// Creates a hot reload context for the assembly containing the specified control.
    /// </summary>
    /// <param name="control">The control to create the hot reload context from.</param>
    /// <param name="controlPath">The path to the control's XAML file.</param>
    /// <returns>A hot reload context for the specified control.</returns>
    public static IHotReloadContext FromControl(object control, string controlPath)
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        return FromControl(control.GetType(), controlPath);
    }

    /// <inheritdoc cref="FromControl(Type, string, IFileSystem)"/>
    public static IHotReloadContext FromControl(Type controlType, string controlPath)
        => FromControl(controlType, controlPath, FileSystem.Current);

    /// <summary>
    /// Creates a hot reload context for the assembly containing the specified control.
    /// </summary>
    /// <param name="controlType">The type of the control to create the hot reload context from.</param>
    /// <param name="controlPath">The path to the control's XAML file.</param>
    /// <param name="fileSystem">The file system where <paramref name="controlPath"/> can be found.</param>
    /// <returns>A hot reload context for the specified control type.</returns>
    public static IHotReloadContext FromControl(Type controlType, string controlPath, IFileSystem fileSystem)
    {
        _ = controlType ?? throw new ArgumentNullException(nameof(controlType));
        _ = controlPath ?? throw new ArgumentNullException(nameof(controlPath));
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = fileSystem.FileExists(controlPath) ? controlPath : throw new FileNotFoundException(controlPath);

        controlPath = fileSystem.GetFullPath(controlPath);
        if (!XamlScanner.TryExtractDocumentUri(controlType, out string? controlUri))
            throw new ArgumentException("The provided control is not a valid user-defined Avalonia control. Could not determine its URI.", nameof(controlType));

        string rootPath = UriHelper.ResolveHostPath(controlUri, controlPath);
        return FromAssembly(controlType.Assembly, rootPath, fileSystem);
    }
}

/// <summary>
/// Manages the hot reload context for Avalonia controls.
/// </summary>
file sealed class AvaloniaProjectHotReloadContext : IHotReloadContext, ISupportInitialize
{
    /// <summary>
    /// The Avalonia control managers, mapped by their respective file paths.
    /// </summary>
    private readonly Dictionary<string, AvaloniaControlManager> _controls;

    /// <summary>
    /// The file watcher responsible for observing changes in Avalonia control files.
    /// </summary>
    private readonly FileWatcher _watcher;

    /// <summary>
    /// The XAML patcher to be applied to the contents of updated files.
    /// </summary>
    private readonly XamlPatcher _xamlPatcher;

    /// <summary>
    /// Indicates whether hot reload is currently enabled.
    /// </summary>
    private bool _enabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaProjectHotReloadContext"/> class.
    /// </summary>
    /// <param name="rootPath">The root directory of the Avalonia project to watch.</param>
    /// <param name="fileSystem">The file system where <paramref name="rootPath"/> can be found.</param>
    /// <param name="documents">The list of XAML documents to manage.</param>
    /// <param name="xamlPatcher">An optional XAML patcher to be applied to the contents of updated files.</param>
    public AvaloniaProjectHotReloadContext(string rootPath, IFileSystem fileSystem, IEnumerable<CompiledXamlDocument> documents, XamlPatcher? xamlPatcher = null)
    {
        _ = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = fileSystem.DirectoryExists(rootPath) ? rootPath : throw new DirectoryNotFoundException(rootPath);

        rootPath = fileSystem.GetFullPath(rootPath);
        _controls = documents.ToDictionary
        (
            x => fileSystem.GetFullPath(fileSystem.ResolvePathFromUri(rootPath, x.Uri)),
            x => new AvaloniaControlManager(x),
            fileSystem.PathComparer
        );

        _watcher = new(rootPath, fileSystem, _controls.Keys);
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;

        _xamlPatcher = xamlPatcher ?? XamlPatcher.Default;
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled => _enabled;

    /// <inheritdoc/>
    public void BeginInit() { }

    /// <inheritdoc/>
    public async void EndInit()
    {
        if (HotReloadFeatures.SkipInitialPatching)
            return;

        try
        {
            // Since this is an `async void` method, ensure that it does not hang indefinitely.
            using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(10));
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            await Task.Run(() => PatchAllAsync(cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LoggingHelper.LogError("Failed to pre-patch all available documents: {Exception}", e);
        }
    }

    /// <inheritdoc/>
    public void EnableHotReload()
    {
        LoggingHelper.LogInfo("Enabling hot reload for the project located at {ProjectLocation}...", _watcher.DirectoryName);
        _enabled = true;
    }

    /// <inheritdoc/>
    public void DisableHotReload()
    {
        LoggingHelper.LogInfo("Disabling hot reload for the project located at {ProjectLocation}...", _watcher.DirectoryName);
        _enabled = false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisableHotReload();

        _watcher.Changed -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnError;
        _watcher.Dispose();

        foreach (AvaloniaControlManager control in _controls.Values)
            control.Dispose();

        _controls.Clear();
    }

    /// <summary>
    /// Handles the file changes by attempting to reload the corresponding Avalonia control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event arguments containing details of the changed file.</param>
    private async void OnChanged(object sender, FileSystemEventArgs args)
    {
        if (!_enabled)
            return;

        IFileSystem fileSystem = _watcher.FileSystem;
        string path = fileSystem.GetFullPath(args.FullPath);
        if (!_controls.TryGetValue(path, out AvaloniaControlManager? controlManager))
            return;

        try
        {
            // Since this is an `async void` method, ensure that it does not hang indefinitely.
            using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(10));
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            if (!await fileSystem.FileExistsAsync(path, cancellationToken).ConfigureAwait(false))
                return;

            LoggingHelper.LogInfo("Reloading {ControlUri}...", controlManager.Document.Uri);
            string xaml = await fileSystem.ReadAllTextAsync(path, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
            string patchedXaml = _xamlPatcher.Patch(xaml);
            await controlManager.ReloadAsync(patchedXaml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LoggingHelper.LogError("Failed to reload {ControlUri}: {Exception}", controlManager.Document.Uri, e);
        }
    }

    /// <summary>
    /// Handles the renamed files by updating their corresponding <see cref="AvaloniaControlManager"/> entries.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event arguments containing details of the renamed file.</param>
    private void OnRenamed(object sender, RenamedEventArgs args)
    {
        string newFullPath = args.FullPath;
        string oldFullPath = args.OldFullPath;
        if (!_controls.TryGetValue(oldFullPath, out AvaloniaControlManager? controlManager))
            return;

        LoggingHelper.LogInfo("{ControlUri} has been moved from {OldControlLocation} to {ControlLocation}.", controlManager.Document.Uri, oldFullPath, newFullPath);
        _controls.Remove(oldFullPath);
        _controls[newFullPath] = controlManager;
    }

    /// <summary>
    /// Handles errors that occur during file monitoring.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event arguments containing the error details.</param>
    private void OnError(object sender, ErrorEventArgs args)
        => LoggingHelper.LogError(sender, "An unexpected error occurred while monitoring file changes: {Exception}", args.GetException());

    /// <summary>
    /// Applies patches asynchronously to all registered controls.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private Task PatchAllAsync(CancellationToken cancellationToken)
        => Task.WhenAll(_controls.Select(x => PatchAsync(x.Value, x.Key, cancellationToken)));

    /// <summary>
    /// Asynchronously applies a patch to a specified control if required.
    /// </summary>
    /// <param name="controlManager">The control manager responsible for reloading the patched XAML.</param>
    /// <param name="path">The file path to the XAML document.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task PatchAsync(AvaloniaControlManager controlManager, string path, CancellationToken cancellationToken)
    {
        try
        {
            IFileSystem fileSystem = _watcher.FileSystem;
            if (!await fileSystem.FileExistsAsync(path, cancellationToken).ConfigureAwait(false))
                return;

            string xaml = await fileSystem.ReadAllTextAsync(path, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
            if (!_xamlPatcher.RequiresPatching(xaml))
                return;

            LoggingHelper.LogInfo("Patching {ControlUri}...", controlManager.Document.Uri);
            string patchedXaml = _xamlPatcher.Patch(xaml);
            await controlManager.ReloadAsync(patchedXaml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LoggingHelper.LogError("Failed to patch {ControlUri}: {Exception}", controlManager.Document.Uri, e);
        }
    }
}

/// <summary>
/// Manages the hot reload context for Avalonia assets.
/// </summary>
file sealed class AvaloniaAssetsHotReloadContext : IHotReloadContext
{
    /// <summary>
    /// The asset manager.
    /// </summary>
    private readonly AvaloniaAssetManager _assetManager;

    /// <summary>
    /// The project locator used to find source directories of assets.
    /// </summary>
    private readonly AvaloniaProjectLocator _projectLocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaAssetsHotReloadContext"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider for resolving dependencies required by the asset manager.
    /// </param>
    /// <param name="projectLocator">
    /// The project locator used to find source directories of assets.
    /// </param>
    public AvaloniaAssetsHotReloadContext(IServiceProvider serviceProvider, AvaloniaProjectLocator projectLocator)
    {
        _assetManager = new AvaloniaAssetManager(serviceProvider);
        _projectLocator = projectLocator;
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled => _assetManager.AssetLoader is DynamicAssetLoader;

    /// <inheritdoc/>
    public void EnableHotReload()
    {
        LoggingHelper.LogInfo("Enabling hot reload for assets...");
        IAssetLoader? currentAssetLoader = _assetManager.AssetLoader;
        if (currentAssetLoader is null or DynamicAssetLoader)
            return;

        _assetManager.AssetLoader = DynamicAssetLoader.Create(currentAssetLoader, _projectLocator);
        _assetManager.IconTypeConverter = DynamicAssetTypeConverter<WindowIcon, IconTypeConverter>.Create(_projectLocator);
        _assetManager.BitmapTypeConverter = DynamicAssetTypeConverter<Bitmap, BitmapTypeConverter>.Create(_projectLocator);
    }

    /// <inheritdoc/>
    public void DisableHotReload()
    {
        LoggingHelper.LogInfo("Disabling hot reload for assets...");
        IAssetLoader? currentAssetLoader = _assetManager.AssetLoader;
        if (currentAssetLoader is not DynamicAssetLoader dynamicAssetLoader)
            return;

        _assetManager.AssetLoader = dynamicAssetLoader.FallbackAssetLoader;
        _assetManager.IconTypeConverter = new();
        _assetManager.BitmapTypeConverter = new();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisableHotReload();

        _assetManager.Dispose();
    }
}
