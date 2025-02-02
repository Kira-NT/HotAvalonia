namespace HotAvalonia.IO;

/// <summary>
/// Listens to the local file system change notifications and raises
/// events when a directory, or file in a directory, changes.
/// </summary>
internal sealed class LocalFileSystemWatcher : FileSystemWatcher, IFileSystemWatcher
{
    /// <inheritdoc/>
    public IFileSystem FileSystem { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this system.</param>
    public LocalFileSystemWatcher(LocalFileSystem fileSystem) => FileSystem = fileSystem;
}
