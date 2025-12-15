using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using Avalonia.Platform;
using HotAvalonia.Helpers;
using HotAvalonia.IO;

namespace HotAvalonia.Assets;

/// <summary>
/// Provides a way to load dynamic assets.
/// </summary>
internal class DynamicAssetLoader
{
    /// <summary>
    /// The type of the dynamic asset loader.
    /// </summary>
    private static readonly Type s_type = DynamicAssetLoaderBuilder.CreateDynamicAssetLoaderType();

    /// <summary>
    /// The fallback asset loader used when dynamic asset loading fails.
    /// </summary>
    protected readonly IAssetLoader _assetLoader;

    /// <summary>
    /// The project locator used to find source directories of assets.
    /// </summary>
    protected readonly AvaloniaProjectLocator _projectLocator;

    /// <summary>
    /// The file system accessor.
    /// </summary>
    protected readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicAssetLoader"/> class.
    /// </summary>
    /// <param name="fallbackAssetLoader">
    /// The fallback <see cref="IAssetLoader"/> to use when dynamic asset loading fails.
    /// </param>
    /// <param name="projectLocator">
    /// The project locator used to find source directories of assets.
    /// </param>
    protected DynamicAssetLoader(IAssetLoader fallbackAssetLoader, AvaloniaProjectLocator projectLocator)
    {
        ArgumentNullException.ThrowIfNull(fallbackAssetLoader);
        ArgumentNullException.ThrowIfNull(projectLocator);

        _assetLoader = fallbackAssetLoader;
        _projectLocator = projectLocator;
        _fileSystem = new CachingFileSystem(projectLocator.FileSystem);
    }

    /// <inheritdoc cref="Create(IAssetLoader, AvaloniaProjectLocator)"/>
    public static IAssetLoader Create(IAssetLoader fallbackAssetLoader)
        => Create(fallbackAssetLoader, new());

    /// <summary>
    /// Creates a new instance of the <see cref="DynamicAssetLoader"/> class.
    /// </summary>
    /// <param name="fallbackAssetLoader">
    /// The fallback <see cref="IAssetLoader"/> to use when dynamic asset loading fails.
    /// </param>
    /// <param name="projectLocator">
    /// The project locator used to find source directories of assets.
    /// </param>
    public static IAssetLoader Create(IAssetLoader fallbackAssetLoader, AvaloniaProjectLocator projectLocator)
        => (IAssetLoader)Activator.CreateInstance(s_type, fallbackAssetLoader, projectLocator);

    /// <summary>
    /// Gets the fallback asset loader that is used when dynamic asset loading fails.
    /// </summary>
    public IAssetLoader FallbackAssetLoader => _assetLoader;

    /// <inheritdoc cref="IAssetLoader.Exists(Uri, Uri?)"/>
    public bool Exists(Uri uri, Uri? baseUri = null)
    {
        if (_assetLoader.Exists(uri, baseUri))
            return true;

        if (TryGetAssetInfo(uri, baseUri, out AssetInfo? asset))
            return _fileSystem.FileExists(asset.Path);

        return false;
    }

    /// <inheritdoc cref="IAssetLoader.GetAssets(Uri, Uri?)"/>
    public IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
    {
        IEnumerable<Uri> assets = _assetLoader.GetAssets(uri, baseUri);

        if (TryGetAssetInfo(uri, baseUri, out AssetInfo? asset) && _fileSystem.DirectoryExists(asset.Path))
        {
            Uri assemblyUri = new UriBuilder(asset.Uri.Scheme, asset.Uri.Host).Uri;
            Uri projectUri = asset.Project;
            IEnumerable<Uri> fileAssets = _fileSystem
                .EnumerateFiles(asset.Path, "*", SearchOption.AllDirectories)
                .Select(x => projectUri.MakeRelativeUri(new(x)))
                .Select(x => new Uri(assemblyUri, x));

            assets = assets.Concat(fileAssets).Distinct();
        }

        return assets;
    }

    /// <inheritdoc cref="IAssetLoader.Open(Uri, Uri?)"/>
    public Stream Open(Uri uri, Uri? baseUri = null)
        => OpenAndGetAssembly(uri, baseUri).stream;

    /// <inheritdoc cref="IAssetLoader.OpenAndGetAssembly(Uri, Uri?)"/>
    public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
    {
        if (!TryGetAssetInfo(uri, baseUri, out AssetInfo? asset) || !_fileSystem.FileExists(asset.Path))
            return _assetLoader.OpenAndGetAssembly(uri, baseUri);

        return (_fileSystem.OpenRead(asset.Path), asset.Assembly);
    }

    /// <summary>
    /// Attempts to retrieve asset information for the given URI.
    /// </summary>
    /// <param name="uri">The URI of the asset to resolve.</param>
    /// <param name="baseUri">An optional base URI for resolving relative URIs.</param>
    /// <param name="assetInfo">
    /// When this method returns, contains the resolved <see cref="AssetInfo"/>
    /// if the URI is valid; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the asset information was successfully retrieved;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool TryGetAssetInfo(Uri uri, Uri? baseUri, [NotNullWhen(true)] out AssetInfo? assetInfo)
    {
        assetInfo = null;
        if (uri is null || !uri.IsAbsoluteUri && baseUri is null)
            return false;

        Uri absoluteUri = uri.AsAbsoluteUri(baseUri);
        if (absoluteUri.Scheme != UriHelper.AvaloniaResourceScheme)
            return false;

        Assembly? assembly = _assetLoader.GetAssembly(absoluteUri);
        if (assembly is null)
            return false;

        if (!_projectLocator.TryGetDirectoryName(assembly, out string? rootPath))
            return false;

        assetInfo = ResolveAssetInfo(absoluteUri, assembly, rootPath);
        return true;
    }

    /// <summary>
    /// Resolves an asset from the given URI.
    /// </summary>
    /// <param name="uri">The URI of the asset.</param>
    /// <param name="assembly">The assembly associated with the asset.</param>
    /// <param name="project">The path of the project root containing the asset.</param>
    /// <returns>A resolved <see cref="AssetInfo"/> instance.</returns>
    private AssetInfo ResolveAssetInfo(Uri uri, Assembly assembly, string project)
    {
        project = _fileSystem.GetFullPath(project);
        char projectEnd = project.Length > 0 ? project[project.Length - 1] : _fileSystem.DirectorySeparatorChar;
        if (projectEnd != _fileSystem.DirectorySeparatorChar && projectEnd != _fileSystem.AltDirectorySeparatorChar)
            project += _fileSystem.DirectorySeparatorChar;

        return new(uri, assembly, new(project), _fileSystem.ResolvePathFromUri(project, uri));
    }
}

/// <summary>
/// Provides a way to generate a subclass of <see cref="DynamicAssetLoader"/>
/// that properly implements the <see cref="IAssetLoader"/> interface.
/// </summary>
file static class DynamicAssetLoaderBuilder
{
    /// <summary>
    /// Creates a <see cref="DynamicAssetLoader"/> that implements <see cref="IAssetLoader"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="Type"/> representing the generated dynamic asset loader.
    /// </returns>
    public static Type CreateDynamicAssetLoaderType()
    {
        const MethodAttributes VirtualMethod = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;

        using IDisposable context = AssemblyHelper.GetDynamicAssembly(out AssemblyBuilder assemblyBuilder, out ModuleBuilder moduleBuilder);
        Type parentType = typeof(DynamicAssetLoader);
        FieldInfo fallbackAssetLoader = parentType.GetInstanceFields().First(x => typeof(IAssetLoader).IsAssignableFrom(x.FieldType));

        string fullName = $"{parentType.FullName}Impl";
        Type? existingType = assemblyBuilder.GetType(fullName, throwOnError: false);
        if (existingType is not null)
            return existingType;

        // public sealed class DynamicAssetLoaderImpl : DynamicAssetLoader, IAssetLoader
        // {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
        typeBuilder.SetParent(parentType);
        typeBuilder.AddInterfaceImplementation(typeof(IAssetLoader));

        //     public DynamicAssetLoaderImpl(IAssetLoader fallbackAssetLoader, AvaloniaProjectLocator projectLocator)
        //         : base(fallbackAssetLoader, projectLocator)
        //     {
        //     }
        Type[] ctorParameterTypes = [typeof(IAssetLoader), typeof(AvaloniaProjectLocator)];
        ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            CallingConventions.Standard | CallingConventions.HasThis,
            ctorParameterTypes
        );
        ILGenerator ctorIl = ctorBuilder.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Ldarg_1);
        ctorIl.Emit(OpCodes.Ldarg_2);
        ctorIl.Emit(OpCodes.Call, parentType.GetInstanceConstructor(ctorParameterTypes)!);
        ctorIl.Emit(OpCodes.Ret);

        //     public IAssetLoader.<...>(...)
        //     {
        //         _assetLoader.<...>(...);
        //     }
        MethodInfo[] virtualMethods = typeof(IAssetLoader).GetMethods();
        foreach (MethodInfo virtualMethod in virtualMethods)
        {
            Type[] parameterTypes = virtualMethod.GetParameterTypes();
            MethodInfo? parentMethod = parentType.GetInstanceMethod(virtualMethod.Name, parameterTypes);

            MethodBuilder virtualMethodBuilder = typeBuilder.DefineMethod(
                virtualMethod.Name, VirtualMethod,
                virtualMethod.ReturnType, parameterTypes
            );
            ILGenerator virtualIl = virtualMethodBuilder.GetILGenerator();
            virtualIl.Emit(OpCodes.Ldarg_0);
            if (parentMethod is null)
                virtualIl.Emit(OpCodes.Ldfld, fallbackAssetLoader);
            for (int i = 1; i <= parameterTypes.Length; i++)
                virtualIl.EmitLdarg(i);
            virtualIl.EmitCall(parentMethod ?? virtualMethod);
            virtualIl.Emit(OpCodes.Ret);
        }

        // }
        return typeBuilder.CreateTypeInfo();
    }
}
