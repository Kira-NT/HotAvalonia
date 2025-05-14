namespace HotAvalonia.IO;

/// <summary>
/// Provides the base class for both <see cref="FileSystemFileInfo"/> and <see cref="FileSystemDirectoryInfo"/> objects.
/// </summary>
public abstract class FileSystemEntryInfo
{
    /// <summary>
    /// The file system this file or directory belongs to.
    /// </summary>
    protected readonly IFileSystem _fileSystem;

    /// <summary>
    /// The path originally specified by the user, whether relative or absolute.
    /// </summary>
    protected string _originalPath;

    /// <summary>
    /// Represents the fully qualified path of the directory or file.
    /// </summary>
    protected string _fullPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemEntryInfo"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system <paramref name="path"/> belongs to.</param>
    /// <param name="path">A string specifying the path on which to create the <see cref="FileSystemEntryInfo"/>.</param>
    private protected FileSystemEntryInfo(IFileSystem fileSystem, string path)
    {
        _fileSystem = fileSystem;
        _originalPath = path;
        _fullPath = fileSystem.GetFullPath(path);
    }

    /// <summary>
    /// Gets the file system this file or directory belongs to.
    /// </summary>
    public IFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Gets the full path of the directory or file.
    /// </summary>
    public string FullName => _fullPath;

    /// <summary>
    /// For files, gets the name of the file.
    /// For directories, gets the name of the last directory in the hierarchy if a hierarchy exists;
    /// otherwise, gets the full name of the directory.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the extension part of the file name, including the leading dot ".", or an empty string if no extension is present.
    /// </summary>
    public string Extension
    {
        get
        {
            string path = _fullPath;
            ReadOnlySpan<char> separators = ['.', _fileSystem.DirectorySeparator, _fileSystem.AltDirectorySeparator, _fileSystem.VolumeSeparator];
            int i = path.AsSpan().LastIndexOfAny(separators);
            return (uint)i < path.Length && path[i] == '.' ? path.Substring(i) : string.Empty;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the file or directory exists.
    /// </summary>
    public abstract bool Exists { get; }

    /// <summary>
    /// Gets or sets the attributes for the current file or directory.
    /// </summary>
    public FileAttributes Attributes
    {
        get => _fileSystem.GetFileAttributes(_fullPath);
        set => _fileSystem.SetFileAttributes(_fullPath, value);
    }

    /// <summary>
    /// Gets or sets the creation time of the current file or directory.
    /// </summary>
    public DateTime CreationTime
    {
        get => CreationTimeUtc.ToLocalTime();
        set => CreationTimeUtc = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets or sets the creation time, in coordinated universal time (UTC), of the current file or directory.
    /// </summary>
    public abstract DateTime CreationTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the time the current file or directory was last accessed.
    /// </summary>
    public DateTime LastAccessTime
    {
        get => LastAccessTimeUtc.ToLocalTime();
        set => LastAccessTimeUtc = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets or sets the time, in coordinated universal time (UTC), that the current file or directory was last accessed.
    /// </summary>
    public abstract DateTime LastAccessTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the time when the current file or directory was last written to.
    /// </summary>
    public DateTime LastWriteTime
    {
        get => LastWriteTimeUtc.ToLocalTime();
        set => LastWriteTimeUtc = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets or sets the time, in coordinated universal time (UTC), when the current file or directory was last written to.
    /// </summary>
    public abstract DateTime LastWriteTimeUtc { get; set; }

    /// <summary>
    /// Gets the target path of the link located in <see cref="FullName"/>, or <c>null</c> if this instance doesn't represent a link.
    /// </summary>
    public string? LinkTarget => ResolveLinkTarget(returnFinalTarget: false)?.FullName;

    /// <summary>
    /// Creates a symbolic link located in <see cref="FullName"/> that points to the specified <paramref name="pathToTarget"/>.
    /// </summary>
    /// <param name="pathToTarget">The path of the symbolic link target.</param>
    public abstract void CreateAsSymbolicLink(string pathToTarget);

    /// <summary>
    /// Gets the target of the specified link.
    /// </summary>
    /// <param name="returnFinalTarget"><c>true</c> to follow links to the final target; <c>false</c> to return the immediate next link.</param>
    /// <returns>A <see cref="FileSystemEntryInfo"/> instance if the link exists, independently if the target exists or not; <c>null</c> if this file or directory is not a link.</returns>
    public abstract FileSystemEntryInfo? ResolveLinkTarget(bool returnFinalTarget);

    /// <summary>
    /// Refreshes the state of the object.
    /// </summary>
    public virtual void Refresh() { }

    /// <summary>
    /// Deletes a file or directory.
    /// </summary>
    public abstract void Delete();

    /// <summary>
    /// Returns the original path.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="FullName"/> or <see cref="Name"/> properties for the full path or file/directory name.
    /// </remarks>
    /// <returns>A string with the original path.</returns>
    public override string ToString() => _originalPath;

    /// <inheritdoc/>
    public override int GetHashCode() => _fileSystem.PathComparer.GetHashCode(_fullPath);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is FileSystemFileInfo info
            && GetType() == info.GetType()
            && _fileSystem == info._fileSystem
            && _fileSystem.PathComparer.Equals(_fullPath, info._fullPath);
}
