using System.ComponentModel;
using Avalonia;
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
    /// The hot reload configuration.
    /// </summary>
    private readonly AvaloniaHotReloadConfig _config;

    /// <summary>
    /// Indicates whether hot reload is currently enabled.
    /// </summary>
    private bool _enabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaProjectHotReloadContext"/> class.
    /// </summary>
    /// <param name="rootPath">The root directory of the Avalonia project to watch.</param>
    /// <param name="documents">The list of XAML documents to manage.</param>
    /// <param name="config">The hot reload configuration to use.</param>
    public AvaloniaProjectHotReloadContext(string rootPath, IEnumerable<CompiledXamlDocument> documents, AvaloniaHotReloadConfig config)
    {
        IFileSystem fileSystem = config.FileSystem;
        rootPath = fileSystem.GetFullPath(rootPath);
        _controls = documents.ToDictionary
        (
            x => fileSystem.GetFullPath(fileSystem.ResolvePathFromUri(rootPath, x.Uri)),
            x => new AvaloniaControlManager(x),
            fileSystem.PathComparer
        );

        _config = config;
        _watcher = new(rootPath, fileSystem, _controls.Keys);
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled => _enabled;

    /// <inheritdoc/>
    public void BeginInit() { }

    /// <inheritdoc/>
    public void EndInit()
    {
        if (_config.SkipInitialPatching)
            return;

        _ = RunWithDefaultTimeoutAsync(ct => Task.WhenAll(_controls.Select(x => PatchAsync(x.Value, x.Key, ct))));
    }

    /// <inheritdoc/>
    public void TriggerHotReload()
        => _ = RunWithDefaultTimeoutAsync(ReloadAsync);

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
    private void OnChanged(object sender, FileSystemEventArgs args)
    {
        if (!_enabled)
            return;

        _ = RunWithDefaultTimeoutAsync(cancellationToken => ReloadAsync(args.FullPath, cancellationToken));
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
    /// Asynchronously reloads all registered controls.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        foreach ((_, AvaloniaControlManager control) in _controls)
        {
            // The simplest and most effective way to reload all controls within the current
            // context is to reload the top-level `Application` instance that contains them.
            if (!typeof(Application).IsAssignableFrom(control.Document.RootType))
                continue;

            try
            {
                Logger.LogInfo(this, "Reloading '{Uri}'...", control.Document.Uri);
                await control.ReloadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogError(this, "Failed to reload '{Uri}': {Exception}", control.Document.Uri, e);
            }
        }
    }

    /// <summary>
    /// Asynchronously reloads a control associated with the provided file path.
    /// </summary>
    /// <param name="path">The file path associated with the control that needs to be reloaded.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ReloadAsync(string path, CancellationToken cancellationToken)
    {
        IFileSystem fileSystem = _config.FileSystem;
        string fullPath = fileSystem.GetFullPath(path);
        if (!_controls.TryGetValue(fullPath, out AvaloniaControlManager? control))
            return;

        try
        {
            if (!await fileSystem.FileExistsAsync(fullPath, cancellationToken).ConfigureAwait(false))
                return;

            Logger.LogInfo(this, "Reloading '{Uri}'...", control.Document.Uri);
            string xaml = await fileSystem.ReadAllTextAsync(fullPath, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
            string patchedXaml = _config.XamlPatcher.Patch(xaml);
            await control.ReloadAsync(patchedXaml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError(this, "Failed to reload '{Uri}': {Exception}", control.Document.Uri, e);
        }
    }

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
            IFileSystem fileSystem = _config.FileSystem;
            if (!await fileSystem.FileExistsAsync(path, cancellationToken).ConfigureAwait(false))
                return;

            XamlPatcher xamlPatcher = _config.XamlPatcher;
            string xaml = await fileSystem.ReadAllTextAsync(path, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
            if (!xamlPatcher.RequiresPatching(xaml))
                return;

            Logger.LogInfo(this, "Patching '{Uri}'...", controlManager.Document.Uri);
            string patchedXaml = xamlPatcher.Patch(xaml);
            await controlManager.ReloadAsync(patchedXaml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError(this, "Failed to patch '{Uri}': {Exception}", controlManager.Document.Uri, e);
        }
    }

    /// <summary>
    /// Executes the specified asynchronous operation using a cancellation token
    /// that is automatically canceled after the configured default timeout.
    /// </summary>
    /// <param name="function">The function to execute.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunWithDefaultTimeoutAsync(Func<CancellationToken, Task> function)
    {
        TimeSpan timeout = _config.Timeout;
        using CancellationTokenSource? cancellationTokenSource = timeout <= TimeSpan.Zero ? null : new(timeout);
        await function(cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
    }
}
