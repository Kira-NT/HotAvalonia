using System.Diagnostics;
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

    /// <inheritdoc cref="ReloadAsync(string, CancellationToken)"/>
    public Task ReloadAsync(CancellationToken cancellationToken = default)
        => Dispatcher.UIThread.InvokeAsync(() => UnsafeReload(cancellationToken), DispatcherPriority.Render, cancellationToken).GetTask();

    /// <summary>
    /// Asynchronously reloads the controls associated with this manager.
    /// </summary>
    /// <param name="xaml">The XAML markup to reload the control from.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task ReloadAsync(string xaml, CancellationToken cancellationToken = default)
        => Dispatcher.UIThread.InvokeAsync(() => UnsafeReloadAsync(xaml, cancellationToken), DispatcherPriority.Render, cancellationToken).GetTask().Unwrap();

    /// <inheritdoc cref="ReloadAsync(string, CancellationToken)"/>
    private async Task UnsafeReloadAsync(string xaml, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();

        CompiledXamlDocument compiledXaml = XamlCompiler.Compile(xaml, _document.Uri, _document.RootType.Assembly);
        _recompiledDocument = new(compiledXaml.Uri, compiledXaml.BuildMethod, compiledXaml.PopulateMethod, _document);

        UnsafeReload(cancellationToken);
    }

    /// <summary>
    /// Reloads the controls associated with this manager.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    private void UnsafeReload(CancellationToken cancellationToken)
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
        if (_recompiledDocument is null)
        {
            populate(provider, control);
        }
        else
        {
            _recompiledDocument.Populate(provider, control);
        }
    }
}
