namespace HotAvalonia.Fody.MSBuild;

/// <summary>
/// Represents an MSBuild file and provides basic file-related properties.
/// </summary>
public class MSBuildFile
{
    /// <summary>
    /// The full path of the file.
    /// </summary>
    private readonly string _fullPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildFile"/> class.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    public MSBuildFile(string path)
    {
        _fullPath = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFullPath(path);
    }

    /// <summary>
    /// Gets a value indicating whether the file exists at the specified path.
    /// </summary>
    public bool Exists => !string.IsNullOrEmpty(_fullPath) && File.Exists(_fullPath);

    /// <summary>
    /// Gets the full path of the file.
    /// </summary>
    public string FullPath => _fullPath;

    /// <summary>
    /// Gets the file name extracted from the full path.
    /// </summary>
    public string FileName => string.IsNullOrEmpty(_fullPath) ? string.Empty : Path.GetFileName(_fullPath);

    /// <summary>
    /// Gets the directory name where the file is located.
    /// </summary>
    public string DirectoryName => string.IsNullOrEmpty(_fullPath) ? string.Empty : Path.GetDirectoryName(_fullPath);

    /// <summary>
    /// Gets the content of the file.
    /// </summary>
    /// <remarks>
    /// If the file does not exist, an empty string is returned.
    /// </remarks>
    public string Content => field ??= Exists ? File.ReadAllText(_fullPath) : string.Empty;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MSBuildFile file && file._fullPath == _fullPath;

    /// <inheritdoc/>
    public override int GetHashCode() => _fullPath.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => _fullPath;
}
