using Avalonia.Threading;
using HotAvalonia.Collections;
using HotAvalonia.Helpers;
using HotAvalonia.Reflection.Inject;
using HotAvalonia.Xaml;

namespace HotAvalonia;

/// <summary>
/// Manages the lifecycle and state of Avalonia controls.
/// </summary>
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
    /// The <see cref="IInjection"/> instance responsible for injecting
    /// a callback into the control's populate method.
    /// </summary>
    private readonly IInjection? _populateInjection;

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
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _controls = new();

        if (!TryInjectPopulateCallback(document, OnPopulate, out _populateInjection))
            LoggingHelper.Log("Failed to subscribe to the 'Populate' event of {ControlUri}. The control won't be reloaded upon file changes.", document.Uri);
    }

    /// <summary>
    /// Gets the document associated with controls managed by this instance.
    /// </summary>
    public CompiledXamlDocument Document => _recompiledDocument ?? _document;

    /// <inheritdoc/>
    public void Dispose()
        => _populateInjection?.Dispose();

    /// <summary>
    /// Reloads the controls associated with this manager asynchronously.
    /// </summary>
    /// <param name="xaml">The XAML markup to reload the control from.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task ReloadAsync(string xaml, CancellationToken cancellationToken = default)
        => Dispatcher.UIThread.InvokeAsync(() => UnsafeReloadAsync(xaml, cancellationToken), DispatcherPriority.Render);

    /// <inheritdoc cref="ReloadAsync(string, CancellationToken)"/>
    private async Task UnsafeReloadAsync(string xaml, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();

        CompiledXamlDocument compiledXaml = XamlCompiler.Compile(xaml, _document.Uri, _document.RootType.Assembly);
        _recompiledDocument = new(compiledXaml.Uri, compiledXaml.BuildMethod, compiledXaml.PopulateMethod, _document);

        foreach (object control in _controls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _recompiledDocument.Populate(serviceProvider: null, control);
        }
    }

    /// <summary>
    /// Handles the population of a control.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="control">The control to be populated.</param>
    /// <returns><c>true</c> if the control was populated successfully; otherwise, <c>false</c>.</returns>
    private bool OnPopulate(IServiceProvider? provider, object control)
    {
        _controls.Add(control);
        if (_recompiledDocument is null)
            return false;

        _recompiledDocument.Populate(provider, control);
        return true;
    }

    /// <summary>
    /// Attempts to inject a callback into the populate method of the given control.
    /// </summary>
    /// <param name="document">The document associated with controls managed by this instance.</param>
    /// <param name="onPopulate">The callback to invoke when a control is populated.</param>
    /// <param name="injection">
    /// When this method returns, contains the <see cref="IInjection"/> instance if the injection was successful;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the injection was successful;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool TryInjectPopulateCallback(
        CompiledXamlDocument document,
        Func<IServiceProvider?, object, bool> onPopulate,
        out IInjection? injection)
    {
        // At this point, we have two different fallbacks at our disposal:
        //  - First, we try to perform an injection via MonoMod. It's great
        //    and reliable; however, it doesn't support architectures other
        //    than AMD64 (at least at the time of writing), and it requires
        //    explicit support for every single new .NET release.
        //  - In case this whole endeavor is run on a non-AMD64 device,
        //    rendering `CallbackInjector` unusable, we fall back to
        //    undocumented `!XamlIlPopulateOverride` fields.

        if (CallbackInjector.IsSupported)
        {
            injection = CallbackInjector.Inject(document.PopulateMethod, onPopulate);
            return true;
        }

        void PopulateOverride(IServiceProvider? provider, object control)
        {
            if (!onPopulate(provider, control))
                document.Populate(provider, control);
        }
        return document.TryOverridePopulate(PopulateOverride, out injection);
    }
}
