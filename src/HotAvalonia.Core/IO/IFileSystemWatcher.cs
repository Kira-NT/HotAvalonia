namespace HotAvalonia.IO;

/// <summary>
/// Listens to the file system change notifications and raises
/// events when a directory, or file in a directory, changes.
/// </summary>
public interface IFileSystemWatcher : IDisposable
{
    /// <summary>
    /// Gets the file system associated with this instance.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// Gets or sets the path of the directory to watch.
    /// </summary>
    string Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the component is enabled.
    /// </summary>
    bool EnableRaisingEvents { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether subdirectories
    /// within the specified path should be monitored.
    /// </summary>
    bool IncludeSubdirectories { get; set; }

    /// <summary>
    /// Gets or sets the filter string used to determine
    /// what files are monitored in a directory.
    /// </summary>
    string Filter { get; set; }

    /// <summary>
    /// Gets or sets the type of changes to watch for.
    /// </summary>
    NotifyFilters NotifyFilter { get; set; }

    /// <summary>
    /// Occurs when a file or directory in the specified <see cref="Path"/> is created.
    /// </summary>
    event FileSystemEventHandler Created;

    /// <summary>
    /// Occurs when a file or directory in the specified <see cref="Path"/> is deleted.
    /// </summary>
    event FileSystemEventHandler Deleted;

    /// <summary>
    /// Occurs when a file or directory in the specified <see cref="Path"/> is changed.
    /// </summary>
    event FileSystemEventHandler Changed;

    /// <summary>
    /// Occurs when a file or directory in the specified <see cref="Path"/> is renamed.
    /// </summary>
    event RenamedEventHandler Renamed;

    /// <summary>
    /// Occurs when this instance is unable to continue monitoring changes.
    /// </summary>
    event ErrorEventHandler Error;
}
