using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using HotAvalonia.Collections;
using HotAvalonia.Reflection.Inject;
using HotAvalonia.Xaml;

namespace HotAvalonia;

/// <summary>
/// Manages the lifecycle and state of Avalonia controls.
/// </summary>
[DebuggerDisplay($"{{{nameof(Document)},nq}}")]
internal sealed class AvaloniaControlManager : IDisposable
{
    /// <summary>
    /// The document associated with controls managed by this instance.
    /// </summary>
    private readonly CompiledXamlDocument _document;

    /// <summary>
    /// The set of weak references to the controls managed by this instance.
    /// </summary>
    private readonly WeakSet<object> _controls;

    /// <summary>
    /// The injection instance responsible for injecting
    /// a callback into the control's populate method.
    /// </summary>
    private readonly IDisposable? _populateInjection;

    /// <summary>
    /// The most recent version of the document associated
    /// with controls managed by this instance, if any.
    /// </summary>
    private CompiledXamlDocument? _recompiledDocument;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaControlManager"/> class.
    /// </summary>
    /// <param name="document">The document associated with controls managed by this instance.</param>
    public AvaloniaControlManager(CompiledXamlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _controls = new();

        if (!document.TryOverridePopulate((s, c) => OnPopulate(document._populate, s, c), out _populateInjection))
            CallbackInjector.TryInject(document.PopulateMethod, OnPopulate, out _populateInjection);
    }

    /// <summary>
    /// Gets the document associated with controls managed by this instance.
    /// </summary>
    public CompiledXamlDocument Document => _recompiledDocument ?? _document;

    /// <inheritdoc/>
    public void Dispose()
        => _populateInjection?.Dispose();

    /// <summary>
    /// Asynchronously recompiles the controls associated with this manager without reloading them.
    /// </summary>
    /// <param name="xaml">The XAML markup to recompile the controls from.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task RecompileAsync(string xaml, CancellationToken cancellationToken = default)
        => Dispatcher.UIThread.InvokeAsync(() => Recompile(xaml, cancellationToken), DispatcherPriority.Render, cancellationToken).GetTask();

    /// <inheritdoc cref="ReloadAsync(string, CancellationToken)"/>
    public Task ReloadAsync(CancellationToken cancellationToken = default)
        => Dispatcher.UIThread.InvokeAsync(() => Reload(cancellationToken), DispatcherPriority.Render, cancellationToken).GetTask();

    /// <summary>
    /// Asynchronously reloads the controls associated with this manager.
    /// </summary>
    /// <param name="xaml">The XAML markup to reload the controls from.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task ReloadAsync(string xaml, CancellationToken cancellationToken = default)
        => Dispatcher.UIThread.InvokeAsync(() => Reload(xaml, cancellationToken), DispatcherPriority.Render, cancellationToken).GetTask();

    private void Recompile(string xaml, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CompiledXamlDocument compiledXaml = XamlCompiler.Compile(xaml, _document._uri, _document._rootType.Assembly);
        _recompiledDocument = new(compiledXaml._uri, compiledXaml._build, compiledXaml._populate, compiledXaml._populateOverride, _document._refresh);
    }

    /// <summary>
    /// Asynchronously reloads the controls from a host-compiled (Mac-produced) populate assembly,
    /// bypassing the on-device XAML compiler (which relies on Reflection.Emit, unavailable on iOS).
    /// </summary>
    /// <param name="assemblyBytes">The raw bytes of the host-compiled populate assembly.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task ReloadFromAssemblyAsync(byte[] assemblyBytes, CancellationToken cancellationToken = default)
        => Dispatcher.UIThread.InvokeAsync(() => ReloadFromAssembly(assemblyBytes, cancellationToken), DispatcherPriority.Render, cancellationToken).GetTask();

    private void ReloadFromAssembly(byte[] assemblyBytes, CancellationToken cancellationToken)
    {
        RecompileFromAssembly(assemblyBytes, cancellationToken);
        Reload(cancellationToken);
    }

    private void RecompileFromAssembly(byte[] assemblyBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Assembly.Load(byte[]) is supported under the iOS Mono interpreter (read-only load; no Reflection.Emit).
        Assembly assembly = Assembly.Load(assemblyBytes);
        if (!HostCompiledXamlLoader.TryFindResourceMethods(assembly, _document._rootType, out MethodBase build, out MethodInfo populate))
            throw new InvalidOperationException($"Host-compiled assembly '{assembly.GetName().Name}' has no build/populate pair for '{_document._rootType}'.");

        // Reuse the original document's populate-override field (the live device type's
        // !XamlIlPopulateOverride) and refresh delegate; only the build/populate logic is swapped.
        _recompiledDocument = new(_document._uri, build, populate, _document._populateOverride, _document._refresh);
    }

    private void Reload(string xaml, CancellationToken cancellationToken)
    {
        Recompile(xaml, cancellationToken);
        Reload(cancellationToken);
    }

    private void Reload(CancellationToken cancellationToken)
    {
        CompiledXamlDocument document = _recompiledDocument ?? _document;
        foreach (object control in _controls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            document.Reload(serviceProvider: null, control);
        }
    }

    private void OnPopulate(Action<IServiceProvider?, object> populate, IServiceProvider? provider, object control)
    {
        _controls.Add(control);
        if (_recompiledDocument is not null)
        {
            _recompiledDocument.Populate(provider, control);
        }
        else
        {
            populate(provider, control);
        }
    }

    /// <summary>
    /// Finds the closest descendants of the current instance within the provided collection.
    /// </summary>
    /// <param name="controls">The array of control managers to search.</param>
    /// <returns>The closest descendants of the current instance found in the provided collection.</returns>
    internal IEnumerable<AvaloniaControlManager> FindClosestDescendants(AvaloniaControlManager[] controls)
        => _controls.SelectMany(x => FindClosestDescendants(x, controls)).Distinct();

    private static IEnumerable<AvaloniaControlManager> FindClosestDescendants(object control, AvaloniaControlManager[] controls)
    {
        IEnumerable<object> children = control switch
        {
            ILogical logical => logical.LogicalChildren,
            Application { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime app } => app.Windows,
            Application { ApplicationLifetime: ISingleViewApplicationLifetime { MainView: { } view } } => [view],
            _ => [],
        };
        return children.SelectMany(x => FindSelfOrClosestDescendants(x, controls));
    }

    private static IEnumerable<AvaloniaControlManager> FindSelfOrClosestDescendants(object control, AvaloniaControlManager[] controls)
    {
        foreach (AvaloniaControlManager self in controls)
        {
            if (self._controls.Contains(control))
                return [self];
        }
        return FindClosestDescendants(control, controls);
    }
}
