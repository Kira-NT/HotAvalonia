using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HotAvalonia.DependencyInjection;
using HotAvalonia.Logging;
using HotAvalonia.Reflection.Inject;

namespace HotAvalonia;

/// <summary>
/// Manages the lifecycle and state of Avalonia assets.
/// </summary>
internal sealed class AvaloniaAssetManager : IDisposable
{
    /// <summary>
    /// The service provider used to resolve dependencies required by the asset manager.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The <see cref="IInjection"/> instances responsible for injecting callbacks,
    /// required to enable custom functionality for dynamic asset management.
    /// </summary>
    private readonly IInjection[] _injections;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaAssetManager"/> class.
    /// </summary>
    public AvaloniaAssetManager() : this(AvaloniaServiceProvider.Current)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaAssetManager"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public AvaloniaAssetManager(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
        IconTypeConverter = new();
        BitmapTypeConverter = new();
        if (!TryInjectAssetCallbacks(out _injections))
            Logger.LogWarning("Failed to subscribe to asset loading events. Icons and images won't be reloaded upon file changes.");
    }

    /// <summary>
    /// Gets or sets the asset loader used for resolving and loading assets.
    /// </summary>
    public IAssetLoader? AssetLoader
    {
        get => _serviceProvider.GetService(typeof(IAssetLoader)) as IAssetLoader;
        set
        {
            if (_serviceProvider is AvaloniaServiceProvider mutableServiceProvider)
            {
                mutableServiceProvider.SetService(typeof(IAssetLoader), _ => value);
            }
            else
            {
                InvalidOperationException.Throw();
            }
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="TypeConverter"/> used for loading icons.
    /// </summary>
    public IconTypeConverter IconTypeConverter
    {
        get => field;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="TypeConverter"/> used for loading images.
    /// </summary>
    public BitmapTypeConverter BitmapTypeConverter
    {
        get => field;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>
    /// Disposes of the resources used by the <see cref="AvaloniaAssetManager"/>.
    /// </summary>
    public void Dispose()
    {
        foreach (IInjection injection in _injections)
            injection.Dispose();
    }

    /// <summary>
    /// Attempts to load an icon.
    /// </summary>
    /// <inheritdoc cref="TryLoadAsset"/>
    private bool TryLoadIcon(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value,
        [NotNullWhen(true), CallbackResult] out object? result
    )
        => TryLoadAsset(IconTypeConverter, context, culture, value, out result);

    /// <summary>
    /// Attempts to load an image.
    /// </summary>
    /// <inheritdoc cref="TryLoadAsset"/>
    private bool TryLoadBitmap(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value,
        [NotNullWhen(true), CallbackResult] out object? result
    )
        => TryLoadAsset(BitmapTypeConverter, context, culture, value, out result);

    /// <summary>
    /// Attempts to load an asset.
    /// </summary>
    /// <param name="context">An optional context providing information about the conversion environment.</param>
    /// <param name="culture">The culture to use for the conversion.</param>
    /// <param name="value">The value representing the asset source (e.g., a file path).</param>
    /// <param name="result">
    /// When this method returns, contains the loaded asset if the method succeeds,
    /// or <c>null</c> if the method fails.
    /// </param>
    /// <returns>
    /// <c>true</c> if the asset was successfully loaded;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool TryLoadAsset(
        TypeConverter converter,
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value,
        [NotNullWhen(true)] out object? result)
    {
        if (converter.CanConvertFrom(context, value?.GetType() ?? typeof(object)))
        {
            result = converter.ConvertFrom(context, culture, value!);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Attempts to bind a value to the specified property
    /// of the given <see cref="AvaloniaObject"/>.
    /// </summary>
    /// <param name="obj">The target <see cref="AvaloniaObject"/> to which the value will be bound.</param>
    /// <param name="property">The <see cref="AvaloniaProperty"/> representing the property to bind.</param>
    /// <param name="value">The value representing the asset to bind.</param>
    private static void TryBindAsset(AvaloniaObject obj, AvaloniaProperty property, object? value)
    {
        if (value is IObservable<object> observableValue)
            obj.Bind(property, observableValue);
    }

    /// <summary>
    /// Attempts to inject asset-related callbacks for various Avalonia control properties.
    /// </summary>
    /// <param name="injections">
    /// When this method returns, contains an array of <see cref="IInjection"/> instances
    /// for each successful callback injection.
    /// </param>
    /// <returns>
    /// <c>true</c> if the callback injections were successful;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool TryInjectAssetCallbacks(out IInjection[] injections)
    {
        if (!CallbackInjector.SupportsOptimizedMethods)
        {
            injections = [];
            return false;
        }

        Type[] converterParameters = [typeof(ITypeDescriptorContext), typeof(CultureInfo), typeof(object)];
        MethodInfo toIcon = typeof(IconTypeConverter).GetMethod(nameof(IconTypeConverter.ConvertFrom), converterParameters)!;
        MethodInfo toBitmap = typeof(BitmapTypeConverter).GetMethod(nameof(BitmapTypeConverter.ConvertFrom), converterParameters)!;

        injections =
        [
            // Ideally, we would inject into something like
            // `Avalonia.PropertyStore.EffectiveValue`1.SetLocalValueAndRaise`
            // to catch all these scenarios, including custom ones, automatically.
            // However, generic injections are quite flaky to say the least,
            // so it's better to avoid them if we want predictable results.
            //
            // Perhaps we could add automatic detection of similar cases via reflection?
            ..new (Type Type, string Name, AvaloniaProperty Property)[]
            {
                (typeof(Window), nameof(Window.Icon), Window.IconProperty),
                (typeof(Image), nameof(Image.Source), Image.SourceProperty),
                (typeof(ImageBrush), nameof(ImageBrush.Source), ImageBrush.SourceProperty),
                (typeof(CroppedBitmap), nameof(CroppedBitmap.Source), CroppedBitmap.SourceProperty),
                (typeof(ImageDrawing), nameof(ImageDrawing.ImageSource), ImageDrawing.ImageSourceProperty),
            }.Select(static x => CallbackInjector.Inject(
                x.Type.GetProperty(x.Name).SetMethod,
                ([Caller] AvaloniaObject obj, object value) => TryBindAsset(obj, x.Property, value))
            ),

            CallbackInjector.Inject(toIcon, TryLoadIcon),
            CallbackInjector.Inject(toBitmap, TryLoadBitmap),
        ];
        return true;
    }
}
