using System.ComponentModel;
using HotAvalonia.DependencyInjection;
using HotAvalonia.IO;
using HotAvalonia.Xaml;

namespace HotAvalonia;

/// <summary>
/// Represents configuration options for enabling and controlling hot reload
/// in Avalonia applications.
/// </summary>
public sealed record class AvaloniaHotReloadConfig
{
    /// <summary>
    /// Gets the default hot reload configuration for the current environment.
    /// </summary>
    public static AvaloniaHotReloadConfig Default => new()
    {
        XamlPatcher = XamlPatcher.Default,
        SkipInitialPatching = HotReloadFeatures.SkipInitialPatching,
        Mode = HotReloadFeatures.Mode,
        Timeout = HotReloadFeatures.Timeout,
    };

    /// <summary>
    /// Gets or sets the application domain that hot reload operations
    /// are performed in.
    /// </summary>
    public AppDomain AppDomain { get; init; } = AppDomain.CurrentDomain;

    /// <summary>
    /// Gets or sets the service provider used to resolve services required
    /// by XAML controls during hot reload.
    /// </summary>
    public IServiceProvider ServiceProvider { get; init; } = AvaloniaServiceProvider.Current;

    /// <summary>
    /// Gets or sets the XAML patcher used to modify XAML before it is compiled.
    /// </summary>
    public XamlPatcher XamlPatcher { get; init; } = XamlPatcher.Advanced;

    /// <summary>
    /// Gets or sets a value indicating whether the initial XAML patching phase should be skipped.
    /// </summary>
    public bool SkipInitialPatching { get; init; }

    /// <summary>
    /// Gets or sets the project locator used to discover source directories for loaded assemblies.
    /// </summary>
    public AvaloniaProjectLocator ProjectLocator { get; init; } = new();

    /// <summary>
    /// Gets or sets the file system that contains source files for hot-reloadable components.
    /// </summary>
    public IFileSystem FileSystem
    {
        get => ProjectLocator.FileSystem;
        init => ProjectLocator = new(value);
    }

    /// <summary>
    /// Gets or sets the hot reload mode.
    /// </summary>
    public HotReloadMode Mode { get; init; } = HotReloadMode.Minimal;

    /// <summary>
    /// Gets or sets the default timeout applied to hot reload–related operations.
    /// </summary>
    public TimeSpan Timeout { get; init; }

    // We need this method because record semantics are inaccessible from older
    // C# versions, and HotAvalonia.Extensions uses C# 8 as its baseline.
    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public AvaloniaHotReloadConfig With(
        AppDomain? appDomain = null,
        IServiceProvider? serviceProvider = null,
        XamlPatcher? xamlPatcher = null,
        bool? skipInitialPatching = null,
        AvaloniaProjectLocator? projectLocator = null,
        HotReloadMode? mode = null,
        TimeSpan? timeout = null
    ) => this with
    {
        AppDomain = appDomain ?? AppDomain,
        ServiceProvider = serviceProvider ?? ServiceProvider,
        XamlPatcher = xamlPatcher ?? XamlPatcher,
        SkipInitialPatching = skipInitialPatching ?? SkipInitialPatching,
        ProjectLocator = projectLocator ?? ProjectLocator,
        Mode = mode ?? Mode,
        Timeout = timeout ?? Timeout,
    };
}
