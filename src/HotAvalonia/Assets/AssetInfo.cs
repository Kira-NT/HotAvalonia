using System.Reflection;

namespace HotAvalonia.Assets;

/// <summary>
/// Represents metadata about an Avalonia asset.
/// </summary>
internal class AssetInfo
{
    /// <summary>
    /// Gets the URI of the asset.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets the assembly associated with the asset.
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// Gets the URI of the project root containing the asset.
    /// </summary>
    public Uri Project { get; }

    /// <summary>
    /// Gets the path of the asset.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetInfo"/> class.
    /// </summary>
    /// <param name="uri">The URI of the asset.</param>
    /// <param name="assembly">The assembly associated with the asset.</param>
    /// <param name="project">The URI of the project root containing the asset.</param>
    /// <param name="path">The path of the asset.</param>
    public AssetInfo(Uri uri, Assembly assembly, Uri project, string path)
    {
        Uri = uri;
        Assembly = assembly;
        Project = project;
        Path = path;
    }
}
