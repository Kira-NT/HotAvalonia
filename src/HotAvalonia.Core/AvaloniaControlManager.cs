using System.Reflection;
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
    /// The dynamically compiled populate method associated with the control.
    /// </summary>
    private MethodInfo? _dynamicPopulate;

    /// <summary>
    /// The <see cref="IInjection"/> instance responsible for injecting
    /// a callback into the control's populate method.
    /// </summary>
    private readonly IInjection? _populateInjection;

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
    public CompiledXamlDocument Document => _document;

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

        using IEnumerator<object> controls = _controls.GetEnumerator();
        object? firstControl = controls.MoveNext() ? controls.Current : null;

        _document.Load(xaml, firstControl, out MethodInfo? newDynamicPopulate);
        _dynamicPopulate = newDynamicPopulate ?? _dynamicPopulate;
        if (_dynamicPopulate is null)
            return;

        while (controls.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();

            _document.Populate(serviceProvider: null, controls.Current, _dynamicPopulate);
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
        if (_dynamicPopulate is null)
            return false;

        _document.Populate(provider, control, _dynamicPopulate);
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
        // At this point, we have three different fallbacks at our disposal:
        //  - First, we try to perform an injection via MonoMod. It's great and reliable;
        //    however, it doesn't support architectures other than x86/x86_64 (at least at
        //    the time of writing), and it requires explicit support for every single new
        //    .NET release.
        //  - Therefore, in case the code is run on arm64 or via a .NET runtime that MonoMod
        //    doesn't currently support, we fall back to my homebrewed injection technique,
        //    which works consistently across different runtimes and architectures. However,
        //    it requires JIT not to optimize the methods we are injecting into, which is
        //    naturally achieved whenever an app is compiled using the Debug configuration
        //    and then run with a debugger attached to it.
        //  - Finally, in case this whole endeavor is run on arm64 via .NET 42 using the
        //    Release configuration, rendering `CallbackInjector` unusable, we fall back to
        //    undocumented `!XamlIlPopulateOverride` fields. These fields are only generated
        //    for controls that have their `x:Class` property set, leaving things like styles
        //    and resource dictionaries unreloadable. However, partially working hot reload
        //    is still better than no hot reload at all, right?

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
