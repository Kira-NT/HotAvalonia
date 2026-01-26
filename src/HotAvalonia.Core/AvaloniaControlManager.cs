using System.Diagnostics;
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

        Action<IServiceProvider?, object> populate = document.Populate;
        if (!document.TryOverridePopulate((s, c) => OnPopulate(populate, s, c), out _populateInjection))
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
        CompiledXamlDocument compiledXaml = XamlCompiler.Compile(xaml, _document.Uri, _document.RootType.Assembly);
        _recompiledDocument = new(compiledXaml.Uri, compiledXaml.BuildMethod, compiledXaml.PopulateMethod, _document);
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
