using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Threading;
using HotAvalonia.Helpers;

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
    /// Enables hot reload for the application managed by the provided <see cref="AppBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="AppBuilder"/> to configure.</param>
    /// <param name="contextFactory">
    /// A factory function that creates the hot reload context for
    /// the application managed by <paramref name="builder"/>.
    /// </param>
    public static void Enable(AppBuilder builder, Func<IHotReloadContext> contextFactory)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));
        _ = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

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
            return app;
        });
    }

    /// <summary>
    /// Enables hot reload for the provided application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance to enable hot reload for.</param>
    /// <param name="contextFactory">
    /// A factory function that creates the hot reload context for the given application.
    /// </param>
    public static void Enable(Application app, Func<IHotReloadContext> contextFactory)
    {
        _ = app ?? throw new ArgumentNullException(nameof(app));
        _ = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        if (s_contexts.TryGetValue(app, out IHotReloadContext? context))
        {
            context.EnableHotReload();
        }
        else
        {
            context = CreateHotReloadContext(contextFactory);
            s_contexts.Add(app, context);
        }
    }

    /// <summary>
    /// Disables hot reload for the provided application.
    /// </summary>
    /// <param name="app">The <see cref="Application"/> instance to disable hot reload for.</param>
    public static void Disable(Application app)
    {
        _ = app ?? throw new ArgumentNullException(nameof(app));

        if (s_contexts.TryGetValue(app, out IHotReloadContext? context))
            context.DisableHotReload();
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
}
