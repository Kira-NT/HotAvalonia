using System.ComponentModel;
using HotAvalonia.Helpers;
using HotAvalonia.IO;
using HotAvalonia.Logging;
using HotAvalonia.Xaml;

namespace HotAvalonia;

/// <summary>
/// Manages the hot reload context for Avalonia controls.
/// </summary>
internal sealed class AvaloniaProjectHotReloadContext : IHotReloadContext, ISupportInitialize
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
        ArgumentNullException.ThrowIfNull(rootPath);
        ArgumentNullException.ThrowIfNull(fileSystem);
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
            Logger.LogError(this, "Failed to pre-patch all available documents: {Exception}", e);
        }
    }

    /// <inheritdoc/>
    public void EnableHotReload()
    {
        Logger.LogInfo(this, "Enabling hot reload for '{Location}'...", _watcher.DirectoryName);
        _enabled = true;
    }

    /// <inheritdoc/>
    public void DisableHotReload()
    {
        Logger.LogInfo(this, "Disabling hot reload for '{Location}'...", _watcher.DirectoryName);
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

            Logger.LogInfo(this, "Reloading '{Uri}'...", controlManager.Document.Uri);
            string xaml = await fileSystem.ReadAllTextAsync(path, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
            string patchedXaml = _xamlPatcher.Patch(xaml);
            await controlManager.ReloadAsync(patchedXaml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError(this, "Failed to reload '{Uri}': {Exception}", controlManager.Document.Uri, e);
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

        Logger.LogInfo(this, "'{Uri}' has been moved from '{OldLocation}' to '{Location}'.", controlManager.Document.Uri, oldFullPath, newFullPath);
        _controls.Remove(oldFullPath);
        _controls[newFullPath] = controlManager;
    }

    /// <summary>
    /// Handles errors that occur during file monitoring.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event arguments containing the error details.</param>
    private void OnError(object sender, ErrorEventArgs args)
        => Logger.LogError(sender, "An unexpected error occurred while monitoring file changes: {Exception}", args.GetException());

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

            Logger.LogInfo(this, "Patching '{Uri}'...", controlManager.Document.Uri);
            string patchedXaml = _xamlPatcher.Patch(xaml);
            await controlManager.ReloadAsync(patchedXaml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError(this, "Failed to patch '{Uri}': {Exception}", controlManager.Document.Uri, e);
        }
    }
}
