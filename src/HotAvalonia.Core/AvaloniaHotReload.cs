using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Reactive;
using Avalonia.Threading;
using HotAvalonia.Helpers;
using HotAvalonia.Logging;

namespace HotAvalonia;

/// <summary>
/// Provides functionality to enable or disable hot reload for Avalonia applications.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AvaloniaHotReload
{
    /// <summary>
    /// The hot reload contexts mapped to their respective applications.
    /// </summary>
    private static readonly ConditionalWeakTable<Application, IHotReloadContext> s_contexts = new();

    /// <summary>
    /// Enables hot reload for the application managed by the provided <see cref="AppBuilder"/>
    /// and automatically registers a default hotkey to trigger a manual hot reload event
    /// (usually <c>Alt+F5</c>).
    /// </summary>
    /// <inheritdoc cref="Enable(AppBuilder, Func{IHotReloadContext}, KeyGesture?)"/>
    public static void Enable(AppBuilder builder, Func<IHotReloadContext> contextFactory)
        => Enable(builder, contextFactory, HotReloadFeatures.Hotkey);

    /// <summary>
    /// Enables hot reload for the application managed by the provided <see cref="AppBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="AppBuilder"/> to configure.</param>
    /// <param name="contextFactory">
    /// A factory function that creates the hot reload context for
    /// the application managed by <paramref name="builder"/>.
    /// </param>
    /// <param name="gesture">
    /// A gesture representing the hotkey that should trigger a hot reload event,
    /// or <c>null</c> if no hotkey should be registered.
    /// </param>
    public static void Enable(AppBuilder builder, Func<IHotReloadContext> contextFactory, KeyGesture? gesture)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(contextFactory);

        if (builder.Instance is not null)
        {
            Enable(builder.Instance, contextFactory);
            return;
        }

        string appFactoryFieldName = "_appFactory";
        FieldInfo appFactoryField = typeof(AppBuilder).GetInstanceField(appFactoryFieldName, typeof(Func<Application>))
            ?? typeof(AppBuilder).GetInstanceFields().FirstOrDefault(x => x.FieldType == typeof(Func<Application>))
            ?? throw new MissingFieldException(typeof(AppBuilder).FullName, appFactoryFieldName);

        if (appFactoryField.GetValue(builder) is not Func<Application> appFactory)
            throw new InvalidOperationException("Could not enable hot reload: The 'AppBuilder' instance has not been properly initialized.");

        // If the factory function has already been replaced with our own, there's nothing left to do.
        if (appFactory.Method.Module.Assembly == typeof(AvaloniaHotReload).Assembly)
            return;

        appFactoryField.SetValue(builder, () =>
        {
            IHotReloadContext context = CreateHotReloadContext(contextFactory);
            Application app = appFactory();
            s_contexts.Add(app, context);
            ConfigureHotReloadHotkey(app, gesture);
            return app;
        });
    }

    /// <summary>
    /// Enables hot reload for the provided application and automatically registers
    /// a default hotkey to trigger a manual hot reload event (usually <c>Alt+F5</c>).
    /// </summary>
    /// <inheritdoc cref="Enable(Application, Func{IHotReloadContext}, KeyGesture?)"/>
    public static void Enable(Application app, Func<IHotReloadContext> contextFactory)
        => Enable(app, contextFactory, HotReloadFeatures.Hotkey);

    /// <summary>
    /// Enables hot reload for the provided application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance to enable hot reload for.</param>
    /// <param name="contextFactory">
    /// A factory function that creates the hot reload context for the given application.
    /// </param>
    /// <param name="gesture">
    /// A gesture representing the hotkey that should trigger a hot reload event,
    /// or <c>null</c> if no hotkey should be registered.
    /// </param>
    public static void Enable(Application app, Func<IHotReloadContext> contextFactory, KeyGesture? gesture)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(contextFactory);

        if (s_contexts.TryGetValue(app, out IHotReloadContext? context))
        {
            context.EnableHotReload();
        }
        else
        {
            context = CreateHotReloadContext(contextFactory);
            s_contexts.Add(app, context);
            ConfigureHotReloadHotkey(app, gesture);
        }
    }

    /// <summary>
    /// Enables hot reload for the provided application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance to enable hot reload for.</param>
    public static void Enable(Application app)
        => GetHotReloadContext(app).EnableHotReload();

    /// <summary>
    /// Disables hot reload for the provided application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance to disable hot reload for.</param>
    public static void Disable(Application app)
        => GetHotReloadContext(app).DisableHotReload();

    /// <summary>
    /// Triggers a hot reload event for the provided application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance to trigger a hot reload event for.</param>
    public static void Trigger(Application app)
        => GetHotReloadContext(app).TriggerHotReload();

    /// <summary>
    /// Retrieves the hot reload context associated with the specified application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance for which to retrieve the hot reload context.</param>
    /// <returns>The <see cref="IHotReloadContext"/> associated with the specified application.</returns>
    private static IHotReloadContext GetHotReloadContext(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!s_contexts.TryGetValue(app, out IHotReloadContext? context))
            throw new InvalidOperationException("No hot reload context was registered for the provided application.");

        return context;
    }

    /// <summary>
    /// Creates and initializes a new hot reload context using the specified factory method.
    /// </summary>
    /// <param name="contextFactory">A factory function that produces an <see cref="IHotReloadContext"/> instance.</param>
    /// <returns>A fully initialized <see cref="IHotReloadContext"/> instance.</returns>
    private static IHotReloadContext CreateHotReloadContext(Func<IHotReloadContext> contextFactory)
    {
        IHotReloadContext context = contextFactory();
        ISupportInitialize? initContext = context as ISupportInitialize;

        initContext?.BeginInit();
        context.EnableHotReload();

        // Since Avalonia doesn't provide anything similar to an "AppStarted" event
        // of some sort (at least as far as I'm aware), we use this small "hack":
        // actions posted to the UI thread are processed only after both the framework
        // and the app are fully initialized, which is exactly what we need.
        if (initContext is not null)
            Dispatcher.UIThread.Post(initContext.EndInit);

        return context;
    }

    /// <summary>
    /// Configures a hotkey to trigger a hot reload event for the provided application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance to register a hotkey for.</param>
    /// <param name="gesture">
    /// A gesture representing the hotkey that should trigger a hot reload event,
    /// or <c>null</c> if no hotkey should be registered.
    /// </param>
    private static void ConfigureHotReloadHotkey(Application app, KeyGesture? gesture) => Dispatcher.UIThread.Post(() =>
    {
        if (gesture is null)
            return;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        IInputManager? inputManager = app.GetType().GetProperty("InputManager", flags)?.GetValue(app) as IInputManager;
        object? preProcess = inputManager?.GetType().GetProperty("PreProcess", flags)?.GetValue(inputManager);
        if (preProcess is not IObservable<RawInputEventArgs> onInput)
        {
            Logger.LogWarning("Failed to register a hot reload hotkey: unable to retrieve the application's input manager.");
            return;
        }

        // Avalonia appears to have erased public members of RawKeyEventArgs from the metadata, so the compiler cannot see them.
        // As a result, we need to use reflection to access what otherwise would have been a bunch of public properties.
        Type t = typeof(RawKeyEventArgs);
        Action<RawInputEventArgs, bool>? setHandled = t.GetProperty("Handled", flags)?.SetMethod?.CreateDelegate<Action<RawInputEventArgs, bool>>();
        Func<RawKeyEventArgs, RawKeyEventType>? getType = t.GetProperty("Type", flags)?.GetMethod?.CreateDelegate<Func<RawKeyEventArgs, RawKeyEventType>>();
        Func<RawKeyEventArgs, Key>? getKey = t.GetProperty("Key", flags)?.GetMethod?.CreateDelegate<Func<RawKeyEventArgs, Key>>();
        Func<RawKeyEventArgs, RawInputModifiers>? getModifiers = t.GetProperty("Modifiers", flags)?.GetMethod?.CreateDelegate<Func<RawKeyEventArgs, RawInputModifiers>>();
        if (setHandled is null || getType is null || getKey is null || getModifiers is null)
        {
            Logger.LogWarning("Failed to register a hot reload hotkey.");
            return;
        }

        Key expectedKey = PatchNumPadKey(gesture.Key);
        KeyModifiers expectedModifiers = gesture.KeyModifiers;
        onInput.Subscribe(new AnonymousObserver<RawInputEventArgs>(args =>
        {
            if (args is not RawKeyEventArgs keyArgs || getType(keyArgs) != RawKeyEventType.KeyUp)
                return;

            Key key = PatchNumPadKey(getKey(keyArgs));
            KeyModifiers modifiers = (KeyModifiers)(getModifiers(keyArgs) & RawInputModifiers.KeyboardMask);
            if ((key, modifiers) != (expectedKey, expectedModifiers))
                return;

            Trigger(app);
            setHandled(keyArgs, true);
        }));

        static Key PatchNumPadKey(Key key) => key switch
        {
            Key.Add => Key.OemPlus,
            Key.Subtract => Key.OemMinus,
            Key.Decimal => Key.OemPeriod,
            _ => key,
        };
    });
}
