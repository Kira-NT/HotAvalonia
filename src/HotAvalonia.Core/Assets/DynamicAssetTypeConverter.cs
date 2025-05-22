using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml;
using HotAvalonia.Helpers;
using HotAvalonia.Reflection.Emit;

namespace HotAvalonia.Assets;

/// <summary>
/// Provides a way to dynamically load assets of type <typeparamref name="TAsset"/>.
/// </summary>
/// <typeparam name="TAsset">The type of the asset to be dynamically loaded.</typeparam>
internal sealed class DynamicAssetTypeConverter<TAsset> : TypeConverter where TAsset : notnull
{
    /// <summary>
    /// The project locator used to find source directories of assets.
    /// </summary>
    private readonly AvaloniaProjectLocator _projectLocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicAssetTypeConverter{TAsset}"/> class.
    /// </summary>
    /// <param name="projectLocator">The project locator used to find source directories of assets.</param>
    public DynamicAssetTypeConverter(AvaloniaProjectLocator projectLocator)
    {
        _projectLocator = projectLocator ?? throw new ArgumentNullException(nameof(projectLocator));
    }

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string);

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string path)
            return base.ConvertFrom(context, culture, value);

        IUriContext? uriContext = (context as IServiceProvider)?.GetService(typeof(IUriContext)) as IUriContext;
        Uri uri = new(path, path.StartsWith("/") ? UriKind.Relative : UriKind.RelativeOrAbsolute);
        Uri? baseUri = uriContext?.BaseUri;

        return DynamicAsset<TAsset>.Create(uri, baseUri, _projectLocator);
    }
}

/// <inheritdoc cref="DynamicAssetTypeConverter{TAsset}"/>
/// <typeparam name="TAssetTypeConverter">
/// The specific type of the <see cref="TypeConverter"/> to wrap
/// the <see cref="DynamicAssetTypeConverter{TAsset}"/> with.
/// </typeparam>
internal static class DynamicAssetTypeConverter<TAsset, TAssetTypeConverter>
    where TAsset : notnull
    where TAssetTypeConverter : TypeConverter
{
    /// <summary>
    /// The type of the dynamic asset type converter.
    /// </summary>
    private static readonly Type s_type = DynamicAssetTypeConverter.CreateDynamicAssetConverterType(typeof(TAsset), typeof(TAssetTypeConverter));

    /// <summary>
    /// Creates a new instance of the <typeparamref name="TAssetTypeConverter"/> class.
    /// </summary>
    /// <param name="projectLocator">The project locator used to find source directories of assets.</param>
    /// <returns>A new instance of the <typeparamref name="TAssetTypeConverter"/> class.</returns>
    public static TAssetTypeConverter Create(AvaloniaProjectLocator projectLocator)
        => (TAssetTypeConverter)Activator.CreateInstance(s_type, projectLocator);
}

/// <summary>
/// Provides functionality to dynamically generate custom asset type converters at runtime.
/// </summary>
internal static class DynamicAssetTypeConverter
{
    /// <summary>
    /// Creates a type that serves as a custom asset type converter.
    /// </summary>
    /// <param name="assetType">The type of the asset to be dynamically loaded.</param>
    /// <param name="assetConverterType">The base type of the asset type converter.</param>
    /// <returns>
    /// A <see cref="Type"/> representing the generated asset type converter,
    /// which wraps the <see cref="DynamicAssetTypeConverter{TAsset}"/> logic.
    /// </returns>
    public static Type CreateDynamicAssetConverterType(Type assetType, Type assetConverterType)
    {
        const MethodAttributes MethodOverride = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual;

        _ = assetType ?? throw new ArgumentNullException(nameof(assetType));
        _ = assetConverterType ?? throw new ArgumentNullException(nameof(assetConverterType));

        string fullName = $"{assetConverterType.FullName}$Dynamic";
        if (DynamicAssembly.Shared.GetType(fullName, throwOnError: false) is Type existingType)
            return existingType;

        DynamicAssembly.Shared.AllowAccessTo(assetType);
        DynamicAssembly.Shared.AllowAccessTo(assetConverterType);

        // public sealed class {TAssetTypeConverter}$Dynamic : TAssetTypeConverter
        // {
        using DynamicTypeBuilder typeBuilder = DynamicAssembly.Shared.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Sealed);
        typeBuilder.SetParent(assetConverterType);

        //     private readonly DynamicAssetTypeConverter<TAsset> _converter;
        Type dynamicConverterType = typeof(DynamicAssetTypeConverter<>).MakeGenericType(assetType);
        FieldBuilder converterFieldBuilder = typeBuilder.DefineField("_converter", dynamicConverterType, FieldAttributes.Private | FieldAttributes.InitOnly);

        //     public {TAssetTypeConverter}$Dynamic(AvaloniaProjectLocator projectLocator)
        //     {
        //          _converter = new(projectLocator);
        //     }
        ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            CallingConventions.Standard | CallingConventions.HasThis,
            [typeof(AvaloniaProjectLocator)]
        );
        ILGenerator ctorIl = ctorBuilder.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, assetConverterType.GetInstanceConstructor()!);
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Ldarg_1);
        ctorIl.Emit(OpCodes.Newobj, dynamicConverterType.GetInstanceConstructor(typeof(AvaloniaProjectLocator))!);
        ctorIl.Emit(OpCodes.Stfld, converterFieldBuilder);
        ctorIl.Emit(OpCodes.Ret);

        //     public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        //         => _converter.CanConvertFrom(context, sourceType) || base.CanConvertFrom(context, sourceType);
        MethodBuilder canConvertBuilder = typeBuilder.DefineMethod(
            nameof(TypeConverter.CanConvertFrom), MethodOverride,
            typeof(bool), [typeof(ITypeDescriptorContext), typeof(Type)]
        );
        ILGenerator canConvertIl = canConvertBuilder.GetILGenerator();
        Label canConvertEnd = canConvertIl.DefineLabel();

        canConvertIl.Emit(OpCodes.Ldarg_0);
        canConvertIl.Emit(OpCodes.Ldfld, converterFieldBuilder);
        canConvertIl.Emit(OpCodes.Ldarg_1);
        canConvertIl.Emit(OpCodes.Ldarg_2);
        canConvertIl.Emit(OpCodes.Call, dynamicConverterType.GetMethod(nameof(TypeConverter.CanConvertFrom), [typeof(ITypeDescriptorContext), typeof(Type)])!);
        canConvertIl.Emit(OpCodes.Dup);
        canConvertIl.Emit(OpCodes.Brtrue_S, canConvertEnd);
        canConvertIl.Emit(OpCodes.Pop);

        canConvertIl.Emit(OpCodes.Ldarg_0);
        canConvertIl.Emit(OpCodes.Ldarg_1);
        canConvertIl.Emit(OpCodes.Ldarg_2);
        canConvertIl.Emit(OpCodes.Call, assetConverterType.GetMethod(nameof(TypeConverter.CanConvertFrom), [typeof(ITypeDescriptorContext), typeof(Type)])!);

        canConvertIl.MarkLabel(canConvertEnd);
        canConvertIl.Emit(OpCodes.Ret);

        //     public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        //     {
        //         if (value is null || !_converter.CanConvertFrom(context, value.GetType()))
        //             return base.ConvertFrom(context, culture, value!);
        //
        //         return _converter.ConvertFrom(context, culture, value);
        //     }
        MethodBuilder convertBuilder = typeBuilder.DefineMethod(
            nameof(TypeConverter.ConvertFrom), MethodOverride,
            typeof(object), [typeof(ITypeDescriptorContext), typeof(CultureInfo), typeof(object)]
        );
        ILGenerator convertIl = convertBuilder.GetILGenerator();
        Label convertStart = convertIl.DefineLabel();
        Label convertEnd = convertIl.DefineLabel();

        convertIl.Emit(OpCodes.Ldarg_3);
        convertIl.Emit(OpCodes.Brfalse_S, convertStart);

        convertIl.Emit(OpCodes.Ldarg_0);
        convertIl.Emit(OpCodes.Ldfld, converterFieldBuilder);
        convertIl.Emit(OpCodes.Ldarg_1);
        convertIl.Emit(OpCodes.Ldarg_3);
        convertIl.Emit(OpCodes.Call, typeof(object).GetMethod(nameof(GetType))!);
        convertIl.Emit(OpCodes.Call, dynamicConverterType.GetMethod(nameof(TypeConverter.CanConvertFrom), [typeof(ITypeDescriptorContext), typeof(Type)])!);
        convertIl.Emit(OpCodes.Brfalse_S, convertStart);

        convertIl.Emit(OpCodes.Ldarg_0);
        convertIl.Emit(OpCodes.Ldfld, converterFieldBuilder);
        convertIl.Emit(OpCodes.Ldarg_1);
        convertIl.Emit(OpCodes.Ldarg_2);
        convertIl.Emit(OpCodes.Ldarg_3);
        convertIl.Emit(OpCodes.Call, dynamicConverterType.GetMethod(nameof(TypeConverter.ConvertFrom), [typeof(ITypeDescriptorContext), typeof(CultureInfo), typeof(object)])!);
        convertIl.Emit(OpCodes.Br_S, convertEnd);

        convertIl.MarkLabel(convertStart);
        convertIl.Emit(OpCodes.Ldarg_0);
        convertIl.Emit(OpCodes.Ldarg_1);
        convertIl.Emit(OpCodes.Ldarg_2);
        convertIl.Emit(OpCodes.Ldarg_3);
        convertIl.Emit(OpCodes.Call, assetConverterType.GetMethod(nameof(TypeConverter.ConvertFrom), [typeof(ITypeDescriptorContext), typeof(CultureInfo), typeof(object)])!);

        convertIl.MarkLabel(convertEnd);
        convertIl.Emit(OpCodes.Ret);

        // }
        return typeBuilder.CreateTypeInfo();
    }
}
