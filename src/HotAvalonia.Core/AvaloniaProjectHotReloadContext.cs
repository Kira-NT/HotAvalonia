using System.ComponentModel;
using System.Diagnostics;
using HotAvalonia.Helpers;
using HotAvalonia.IO;
using HotAvalonia.Logging;
using HotAvalonia.Xaml;

namespace HotAvalonia;

/// <summary>
/// Represents a hot reload context for a single Avalonia project.
/// </summary>
[DebuggerDisplay($"{{{nameof(Path)},nq}}")]
internal sealed class AvaloniaProjectHotReloadContext : IHotReloadContext, ISupportInitialize
{
    private readonly AvaloniaSolutionHotReloadContext _solution;

    private readonly Dictionary<string, AvaloniaControlManager> _controls;

    private readonly FileWatcher _watcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaProjectHotReloadContext"/> class.
    /// </summary>
    /// <param name="solution">The solution this project belongs to.</param>
    /// <param name="path">The root directory of this project.</param>
    /// <param name="documents">The list of XAML documents that belong to the project.</param>
    public AvaloniaProjectHotReloadContext(AvaloniaSolutionHotReloadContext solution, string path, IEnumerable<CompiledXamlDocument> documents)
    {
        IFileSystem fileSystem = solution.Config.FileSystem;
        path = fileSystem.GetFullPath(path);

        _solution = solution;
        _controls = documents.ToDictionary
        (
            x => fileSystem.GetFullPath(fileSystem.ResolvePathFromUri(path, x.Uri)),
            x => new AvaloniaControlManager(x),
            fileSystem.PathComparer
        );
        _watcher = new(path, fileSystem, _controls.Keys);
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
    }

    /// <summary>
    /// Gets the solution this project belongs to.
    /// </summary>
    public AvaloniaSolutionHotReloadContext Solution => _solution;

    /// <summary>
    /// Gets the collection of controls that belong to this project.
    /// </summary>
    public IReadOnlyCollection<AvaloniaControlManager> Controls => _controls.Values;

    /// <summary>
    /// Gets the hot reload configuration applied to this project.
    /// </summary>
    public AvaloniaHotReloadConfig Config => _solution.Config;

    /// <summary>
    /// Gets the root directory of this project.
    /// </summary>
    public string Path => _watcher.DirectoryName;

    public bool IsHotReloadEnabled => _solution.IsHotReloadEnabled;

    public void BeginInit() { }

    public void EndInit()
    {
        if (Config.SkipInitialPatching)
            return;

        _ = RunWithDefaultTimeoutAsync(ct => Task.WhenAll(_controls.Select(x => PatchAsync(x.Value, x.Key, ct))));
    }

    public void TriggerHotReload()
        => _solution.TriggerHotReload();

    public void EnableHotReload()
        => _solution.EnableHotReload();

    public void DisableHotReload()
        => _solution.DisableHotReload();

    public void Dispose()
    {
        _watcher.Changed -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnError;
        _watcher.Dispose();

        foreach ((_, AvaloniaControlManager control) in _controls)
            control.Dispose();

        _controls.Clear();
    }

    private void OnChanged(object sender, FileSystemEventArgs args)
    {
        if (!IsHotReloadEnabled)
            return;

        _ = RunWithDefaultTimeoutAsync(cancellationToken => ReloadAsync(args.FullPath, cancellationToken));
    }

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

    private void OnError(object sender, ErrorEventArgs args)
        => Logger.LogError(sender, "An unexpected error occurred while monitoring file changes: {Exception}", args.GetException());

    private async Task ReloadAsync(string path, CancellationToken cancellationToken)
    {
        IFileSystem fileSystem = Config.FileSystem;
        string fullPath = fileSystem.GetFullPath(path);
        if (!_controls.TryGetValue(fullPath, out AvaloniaControlManager? control))
            return;

        try
        {
            if (!await fileSystem.FileExistsAsync(fullPath, cancellationToken).ConfigureAwait(false))
                return;

            Logger.LogInfo(this, "Reloading '{Uri}'...", control.Document.Uri);
            string xaml = await fileSystem.ReadAllTextAsync(fullPath, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
            string patchedXaml = Config.XamlPatcher.Patch(xaml);
            await control.ReloadAsync(patchedXaml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError(this, "Failed to reload '{Uri}': {Exception}", control.Document.Uri, e);
        }
    }

    private async Task PatchAsync(AvaloniaControlManager controlManager, string path, CancellationToken cancellationToken)
    {
        try
        {
            IFileSystem fileSystem = Config.FileSystem;
            if (!await fileSystem.FileExistsAsync(path, cancellationToken).ConfigureAwait(false))
                return;

            XamlPatcher xamlPatcher = Config.XamlPatcher;
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

    private async Task RunWithDefaultTimeoutAsync(Func<CancellationToken, Task> function)
    {
        TimeSpan timeout = Config.Timeout;
        using CancellationTokenSource? cancellationTokenSource = timeout <= TimeSpan.Zero ? null : new(timeout);
        await function(cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
    }
}
