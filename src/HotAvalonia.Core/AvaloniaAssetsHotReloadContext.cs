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
    /// The hot reload configuration.
    /// </summary>
    private readonly AvaloniaHotReloadConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaAssetsHotReloadContext"/> class.
    /// </summary>
    /// <param name="config">The hot reload configuration to use.</param>
    public AvaloniaAssetsHotReloadContext(AvaloniaHotReloadConfig config)
    {
        _assetManager = new AvaloniaAssetManager(config.ServiceProvider);
        _config = config;
    }

    /// <inheritdoc/>
    public bool IsHotReloadEnabled => _assetManager.AssetLoader is DynamicAssetLoader;

    /// <inheritdoc/>
    void IHotReloadContext.TriggerHotReload() { }

    /// <inheritdoc/>
    public void EnableHotReload()
    {
        Logger.LogInfo(this, "Enabling hot reload for assets...");
        IAssetLoader? currentAssetLoader = _assetManager.AssetLoader;
        if (currentAssetLoader is null or DynamicAssetLoader)
            return;

        AvaloniaProjectLocator projectLocator = _config.ProjectLocator;
        _assetManager.AssetLoader = DynamicAssetLoader.Create(currentAssetLoader, projectLocator);
        _assetManager.IconTypeConverter = DynamicAssetTypeConverter<WindowIcon, IconTypeConverter>.Create(projectLocator);
        _assetManager.BitmapTypeConverter = DynamicAssetTypeConverter<Bitmap, BitmapTypeConverter>.Create(projectLocator);
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
