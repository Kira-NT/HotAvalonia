using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HotAvalonia.DependencyInjection;
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
    /// The injection instances responsible for injecting callbacks,
    /// required to enable custom functionality for dynamic asset management.
    /// </summary>
    private readonly List<IDisposable> _injections;

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

        IconTypeConverter = new();
        BitmapTypeConverter = new();
        _serviceProvider = serviceProvider;
        _injections = InjectAssetCallbacks();
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
        foreach (IDisposable injection in _injections)
            injection.Dispose();
    }

    private object? LoadIcon(
        Func<TypeConverter, ITypeDescriptorContext?, CultureInfo?, object?, object?> convertFrom,
        TypeConverter typeConverter,
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value)
    {
        if (IconTypeConverter.CanConvertFrom(context, value?.GetType() ?? typeof(object)))
            return IconTypeConverter.ConvertFrom(context, culture, value!);

        return convertFrom(typeConverter, context, culture, value);
    }

    private object? LoadBitmap(
        Func<TypeConverter, ITypeDescriptorContext?, CultureInfo?, object?, object?> convertFrom,
        TypeConverter typeConverter,
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value)
    {
        if (BitmapTypeConverter.CanConvertFrom(context, value?.GetType() ?? typeof(object)))
            return BitmapTypeConverter.ConvertFrom(context, culture, value!);

        return convertFrom(typeConverter, context, culture, value);
    }

    private static void BindAsset(AvaloniaProperty property, Action<AvaloniaObject, object?> setValue, AvaloniaObject obj, object? value)
    {
        if (value is IObservable<object> observableValue)
            obj.Bind(property, observableValue);

        setValue(obj, value);
    }

    private List<IDisposable> InjectAssetCallbacks()
    {
        List<IDisposable> injections = new(7);
        Type[] converterParameters = [typeof(ITypeDescriptorContext), typeof(CultureInfo), typeof(object)];

        MethodInfo toIcon = typeof(IconTypeConverter).GetMethod(nameof(IconTypeConverter.ConvertFrom), converterParameters)!;
        if (CallbackInjector.TryInject(toIcon, LoadIcon, out IDisposable? toIconInjection))
            injections.Add(toIconInjection);

        MethodInfo toBitmap = typeof(BitmapTypeConverter).GetMethod(nameof(BitmapTypeConverter.ConvertFrom), converterParameters)!;
        if (CallbackInjector.TryInject(toBitmap, LoadBitmap, out IDisposable? toBitmapInjection))
            injections.Add(toBitmapInjection);

        // Ideally, we would inject into something like
        // `Avalonia.PropertyStore.EffectiveValue`1.SetLocalValueAndRaise`
        // to catch all these scenarios, including custom ones, automatically.
        // However, generic injections are quite flaky to say the least,
        // so it's better to avoid them if we want predictable results.
        //
        // Perhaps we could add automatic detection of similar cases via reflection?
        MethodInfo bindAsset = ((Delegate)BindAsset).Method;
        ReadOnlySpan<(Type, string, AvaloniaProperty)> properties = [
            (typeof(Window), nameof(Window.Icon), Window.IconProperty),
            (typeof(Image), nameof(Image.Source), Image.SourceProperty),
            (typeof(ImageBrush), nameof(ImageBrush.Source), ImageBrush.SourceProperty),
            (typeof(CroppedBitmap), nameof(CroppedBitmap.Source), CroppedBitmap.SourceProperty),
            (typeof(ImageDrawing), nameof(ImageDrawing.ImageSource), ImageDrawing.ImageSourceProperty),
        ];
        foreach ((Type type, string propertyName, AvaloniaProperty avaloniaProperty) in properties)
        {
            MethodInfo setMethod = type.GetProperty(propertyName)!.SetMethod!;
            Delegate onSet = bindAsset.CreateDelegate(typeof(Action<Action<AvaloniaObject, object?>, AvaloniaObject, object?>), avaloniaProperty);
            if (CallbackInjector.TryInject(setMethod, onSet, out IDisposable? setInjection))
                injections.Add(setInjection);
        }

        return injections;
    }
}
