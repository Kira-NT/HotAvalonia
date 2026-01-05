using System.ComponentModel;
using System.Reflection;

namespace HotAvalonia;

/// <summary>
/// A hot reload context that operates within an <see cref="AppDomain"/> and manages
/// automatically created hot reload contexts for dynamically loaded assemblies.
/// </summary>
internal sealed class AppDomainHotReloadContext : IHotReloadContext, ISupportInitializeNotification
{
    /// <summary>
    /// The <see cref="AppDomain"/> associated with this hot reload context.
    /// </summary>
    private readonly AppDomain _appDomain;

    /// <summary>
    /// The factory function for creating <see cref="IHotReloadContext"/> instances
    /// for dynamically loaded assemblies.
    /// </summary>
    private readonly Func<IHotReloadContext, AppDomain, Assembly, IHotReloadContext?> _contextFactory;

    /// <summary>
    /// The hot reload context responsible for managing dynamically loaded assemblies.
    /// </summary>
    private IHotReloadContext _context;

    /// <summary>
    /// An object used to synchronize access to the <see cref="_context"/>.
    /// </summary>
    private readonly object _lock;

    /// <summary>
    /// Indicates whether the initialization process has begun.
    /// </summary>
    private bool _isInitializing;

    /// <summary>
    /// Indicates whether the initialization process has completed.
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDomainHotReloadContext"/> class.
    /// </summary>
    /// <param name="appDomain">The <see cref="AppDomain"/> to manage.</param>
    /// <param name="contextFactory">
    /// The factory function to create <see cref="IHotReloadContext"/> instances for
    /// dynamically loaded assemblies.
    /// </param>
    public AppDomainHotReloadContext(AppDomain appDomain, Func<IHotReloadContext, AppDomain, Assembly, IHotReloadContext?> contextFactory)
    {
        _appDomain = appDomain;
        _contextFactory = contextFactory;
        _lock = new();
        _isInitializing = false;
        _isInitialized = false;
        _context = new CombinedHotReloadContext([]);
        _appDomain.AssemblyLoad += OnAssemblyLoad;

        foreach (Assembly assembly in appDomain.GetAssemblies())
            OnAssemblyLoad(appDomain, new(assembly));
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled
    {
        get
        {
            lock (_lock)
                return _context.IsHotReloadEnabled;
        }
    }

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public event EventHandler? Initialized;

    /// <inheritdoc/>
    public void BeginInit()
    {
        _isInitializing = true;
        lock (_lock)
            (_context as ISupportInitialize)?.BeginInit();
    }

    /// <inheritdoc/>
    public void EndInit()
    {
        lock (_lock)
            (_context as ISupportInitialize)?.EndInit();

        _isInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void TriggerHotReload()
    {
        lock (_lock)
            _context.TriggerHotReload();
    }

    /// <inheritdoc/>
    public void EnableHotReload()
    {
        lock (_lock)
            _context.EnableHotReload();
    }

    /// <inheritdoc/>
    public void DisableHotReload()
    {
        lock (_lock)
            _context.DisableHotReload();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _appDomain.AssemblyLoad -= OnAssemblyLoad;
        _context.Dispose();
    }

    /// <summary>
    /// Handles the <see cref="AppDomain.AssemblyLoad"/> event, creating and combining hot reload contexts
    /// for newly loaded assemblies.
    /// </summary>
    /// <param name="sender">The source of the event, typically an <see cref="AppDomain"/>.</param>
    /// <param name="eventArgs">The event data containing the loaded assembly.</param>
    private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs eventArgs)
    {
        AppDomain appDomain = sender as AppDomain ?? _appDomain;
        Assembly? assembly = eventArgs?.LoadedAssembly;
        if (assembly is null)
            return;

        IHotReloadContext? assemblyContext = _contextFactory(this, appDomain, assembly);
        if (assemblyContext is null)
            return;

        lock (_lock)
        {
            if (_isInitializing)
                (assemblyContext as ISupportInitialize)?.BeginInit();

            if (_context.IsHotReloadEnabled)
                assemblyContext.EnableHotReload();

            if (_isInitialized)
                (assemblyContext as ISupportInitialize)?.EndInit();

            _context = _context.Combine(assemblyContext);
        }
    }
}
