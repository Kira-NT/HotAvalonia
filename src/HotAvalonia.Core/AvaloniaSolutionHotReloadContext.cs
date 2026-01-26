using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia;
using HotAvalonia.Logging;
using HotAvalonia.Xaml;

namespace HotAvalonia;

/// <summary>
/// Represents a hot reload context for multiple Avalonia projects within a single solution.
/// </summary>
internal sealed class AvaloniaSolutionHotReloadContext : IHotReloadContext, ISupportInitialize
{
    private readonly AvaloniaHotReloadConfig _config;

    private readonly AppDomain? _appDomain;

    private readonly object _lock;

    private AvaloniaProjectHotReloadContext[] _projects;

    private State _state;

    private AvaloniaSolutionHotReloadContext(AvaloniaHotReloadConfig config)
    {
        _config = config;
        _projects = [];
        _lock = new();
        _state = State.None;
        _appDomain = null;
    }

    private AvaloniaSolutionHotReloadContext(AppDomain appDomain, AvaloniaHotReloadConfig config) : this(config)
    {
        _appDomain = appDomain;
        _appDomain.AssemblyLoad += OnAssemblyLoad;
        foreach (Assembly assembly in appDomain.GetAssemblies())
            OnAssemblyLoad(appDomain, new(assembly));
    }

    /// <summary>
    /// Creates a hot reload context for all assemblies loaded into the application domain
    /// provided by the <paramref name="config"/>.
    /// </summary>
    /// <param name="config">The hot reload configuration to apply to the solution.</param>
    /// <returns>A new <see cref="AvaloniaSolutionHotReloadContext"/> instance representing the entire solution.</returns>
    public static AvaloniaSolutionHotReloadContext FromAppDomain(AvaloniaHotReloadConfig config)
        => new(config.AppDomain, config);

    /// <summary>
    /// Creates a hot reload context for a single project represented by the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly that represents the project to be hot reloaded.</param>
    /// <param name="config">The hot reload configuration to apply to the solution.</param>
    /// <returns>A new <see cref="AvaloniaSolutionHotReloadContext"/> instance representing the specified project.</returns>
    public static AvaloniaSolutionHotReloadContext FromAssembly(Assembly assembly, AvaloniaHotReloadConfig config)
    {
        AvaloniaSolutionHotReloadContext solution = new(config);
        solution.OnAssemblyLoad(solution, new(assembly));
        return solution;
    }

    /// <summary>
    /// Gets the collection of project-level hot reload contexts that belong to this solution.
    /// </summary>
    public IReadOnlyList<AvaloniaProjectHotReloadContext> Projects
    {
        get
        {
            lock (_lock)
                return _projects;
        }
    }

    /// <summary>
    /// Gets the hot reload configuration applied to this solution.
    /// </summary>
    public AvaloniaHotReloadConfig Config => _config;

    public bool IsHotReloadEnabled => (_state & State.HotReloadEnabled) != 0;

    public void BeginInit()
    {
        AvaloniaProjectHotReloadContext[] projects;
        lock (_lock)
        {
            projects = _projects;
            _state |= State.Initializing;
        }
        foreach (AvaloniaProjectHotReloadContext project in projects)
            project.BeginInit();
    }

    public void EndInit()
    {
        AvaloniaProjectHotReloadContext[] projects;
        lock (_lock)
        {
            projects = _projects;
            _state |= State.Initialized;
        }
        foreach (AvaloniaProjectHotReloadContext project in projects)
            project.EndInit();
    }

    public void TriggerHotReload()
        => _ = ReloadAsync();

    public void EnableHotReload()
    {
        _state |= State.HotReloadEnabled;
        Logger.LogInfo(this, "Enabling hot reload for Avalonia projects...");
    }

    public void DisableHotReload()
    {
        _state &= ~State.HotReloadEnabled;
        Logger.LogInfo(this, "Disabling hot reload for Avalonia projects...");
    }

    public void Dispose()
    {
        DisableHotReload();
        _appDomain?.AssemblyLoad -= OnAssemblyLoad;
        foreach (AvaloniaProjectHotReloadContext project in Projects)
            project.Dispose();
    }

    private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs eventArgs)
    {
        Assembly? assembly = eventArgs?.LoadedAssembly;
        if (assembly is null || !TryLoadAvaloniaProject(assembly, out AvaloniaProjectHotReloadContext? project))
            return;

        State state;
        lock (_lock)
        {
            AvaloniaProjectHotReloadContext[] projects = _projects;
            Array.Resize(ref projects, projects.Length + 1);
            projects[^1] = project;

            state = _state;
            _projects = projects;
        }

        if ((state & State.Initializing) != 0)
            project.BeginInit();

        if ((state & State.Initialized) != 0)
            project.EndInit();
    }

    private bool TryLoadAvaloniaProject(Assembly assembly, [NotNullWhen(true)] out AvaloniaProjectHotReloadContext? project)
    {
        project = null;
        CompiledXamlDocument[] documents = XamlScanner.GetDocuments(assembly).ToArray();
        if (documents.Length == 0)
            return false;

        AvaloniaProjectLocator projectLocator = _config.ProjectLocator;
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
            Logger.Log(logLevel, this, "Failed to create a hot reload context for '{Assembly}': sources not found.", assemblyName);
            return false;
        }

        if (!projectLocator.FileSystem.DirectoryExists(rootPath))
        {
            // Same as above.
            LogLevel logLevel = assemblyName?.Split('.')[0] == Assembly.GetEntryAssembly()?.GetName().Name?.Split('.')[0] ? LogLevel.Error : LogLevel.Information;
            Logger.Log(logLevel, this, "Failed to create a hot reload context for '{Assembly}': '{Location}' not found.", assemblyName, rootPath);
            return false;
        }

        Logger.LogInfo(this, "Loading new hot reload context for '{Assembly}' from '{Location}'...", assemblyName, rootPath);
        project = new AvaloniaProjectHotReloadContext(this, rootPath, documents);
        return true;
    }

    private async Task ReloadAsync()
    {
        TimeSpan timeout = _config.Timeout;
        using CancellationTokenSource? cancellationTokenSource = timeout <= TimeSpan.Zero ? null : new(timeout);
        CancellationToken cancellationToken = cancellationTokenSource?.Token ?? default;

        AvaloniaControlManager[] controls = Projects.SelectMany(static x => x.Controls).ToArray();
        AvaloniaControlManager[] apps = controls.Where(static x => typeof(Application).IsAssignableFrom(x.Document.RootType)).ToArray();
        IEnumerable<AvaloniaControlManager> descendants = apps.SelectMany(x => x.FindClosestDescendants(controls));
        await Task.WhenAll(apps.Concat(descendants).Select(x => ReloadAsync(x, cancellationToken))).ConfigureAwait(false);
    }

    private async Task ReloadAsync(AvaloniaControlManager control, CancellationToken cancellationToken)
    {
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

    [Flags]
    private enum State
    {
        None = 0,
        Initializing = 1 << 0,
        Initialized = 1 << 1,
        HotReloadEnabled = 1 << 2,
    }
}
