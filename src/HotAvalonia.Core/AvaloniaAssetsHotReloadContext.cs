using Avalonia.Controls;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HotAvalonia.Assets;
using HotAvalonia.Logging;

namespace HotAvalonia;

/// <summary>
/// Manages the hot reload context for Avalonia assets.
/// </summary>
internal sealed class AvaloniaAssetsHotReloadContext : IHotReloadContext
{
    /// <summary>
    /// The asset manager.
    /// </summary>
    private readonly AvaloniaAssetManager _assetManager;

    /// <summary>
    /// The project locator used to find source directories of assets.
    /// </summary>
    private readonly AvaloniaProjectLocator _projectLocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaAssetsHotReloadContext"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider for resolving dependencies required by the asset manager.
    /// </param>
    /// <param name="projectLocator">
    /// The project locator used to find source directories of assets.
    /// </param>
    public AvaloniaAssetsHotReloadContext(IServiceProvider serviceProvider, AvaloniaProjectLocator projectLocator)
    {
        _assetManager = new AvaloniaAssetManager(serviceProvider);
        _projectLocator = projectLocator;
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled => _assetManager.AssetLoader is DynamicAssetLoader;

    /// <inheritdoc/>
    public void EnableHotReload()
    {
        Logger.LogInfo(this, "Enabling hot reload for assets...");
        IAssetLoader? currentAssetLoader = _assetManager.AssetLoader;
        if (currentAssetLoader is null or DynamicAssetLoader)
            return;

        _assetManager.AssetLoader = DynamicAssetLoader.Create(currentAssetLoader, _projectLocator);
        _assetManager.IconTypeConverter = DynamicAssetTypeConverter<WindowIcon, IconTypeConverter>.Create(_projectLocator);
        _assetManager.BitmapTypeConverter = DynamicAssetTypeConverter<Bitmap, BitmapTypeConverter>.Create(_projectLocator);
    }

    /// <inheritdoc/>
    public void DisableHotReload()
    {
        Logger.LogInfo(this, "Disabling hot reload for assets...");
        IAssetLoader? currentAssetLoader = _assetManager.AssetLoader;
        if (currentAssetLoader is not DynamicAssetLoader dynamicAssetLoader)
            return;

        _assetManager.AssetLoader = dynamicAssetLoader.FallbackAssetLoader;
        _assetManager.IconTypeConverter = new();
        _assetManager.BitmapTypeConverter = new();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisableHotReload();

        _assetManager.Dispose();
    }
}
