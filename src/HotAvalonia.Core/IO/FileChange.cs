using System.IO;

namespace HotAvalonia.IO;

/// <summary>
/// Describes a file system change.
/// </summary>
/// <remarks>
/// iOS ships <c>System.IO.FileSystem.Watcher</c> as a throw-stub: <see cref="FileSystemEventArgs"/>'s
/// constructor and <c>FullPath</c> getter both throw <see cref="PlatformNotSupportedException"/>, so the
/// BCL event-arg types are unusable there. This is the platform-safe substitute carried through
/// HotAvalonia's watcher abstraction. The <see cref="WatcherChangeTypes"/> enum itself is safe to use —
/// only the classes in that assembly throw.
/// </remarks>
public class FileChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileChangeEventArgs"/> class.
    /// </summary>
    /// <param name="changeType">The kind of change detected.</param>
    /// <param name="fullPath">The fully qualified path of the affected file or directory.</param>
    public FileChangeEventArgs(WatcherChangeTypes changeType, string fullPath)
    {
        ChangeType = changeType;
        FullPath = fullPath;
    }

    /// <summary>
    /// Gets the kind of change that occurred.
    /// </summary>
    public WatcherChangeTypes ChangeType { get; }

    /// <summary>
    /// Gets the fully qualified path of the affected file or directory.
    /// </summary>
    public string FullPath { get; }
}

/// <summary>
/// Describes a file system rename.
/// </summary>
public sealed class FileRenameEventArgs : FileChangeEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileRenameEventArgs"/> class.
    /// </summary>
    /// <param name="changeType">The kind of change detected.</param>
    /// <param name="fullPath">The fully qualified path of the affected file or directory.</param>
    /// <param name="oldFullPath">The previous fully qualified path of the affected file or directory.</param>
    public FileRenameEventArgs(WatcherChangeTypes changeType, string fullPath, string oldFullPath)
        : base(changeType, fullPath)
    {
        OldFullPath = oldFullPath;
    }

    /// <summary>
    /// Gets the previous fully qualified path of the affected file or directory.
    /// </summary>
    public string OldFullPath { get; }
}

/// <summary>
/// Represents the method that handles a <see cref="FileChangeEventArgs"/>.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The change data.</param>
public delegate void FileChangeEventHandler(object sender, FileChangeEventArgs e);

/// <summary>
/// Represents the method that handles a <see cref="FileRenameEventArgs"/>.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The rename data.</param>
public delegate void FileRenameEventHandler(object sender, FileRenameEventArgs e);

/// <summary>
/// Represents the method that handles a file watcher error.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="error">The error that occurred.</param>
public delegate void FileWatcherErrorEventHandler(object sender, Exception error);
