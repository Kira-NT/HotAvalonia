namespace HotAvalonia.IO;

/// <summary>
/// Provides properties and instance methods for the creation, copying, deletion, moving,
/// and opening of files, and aids in the creation of <see cref="Stream"/> objects.
/// This class cannot be inherited.
/// </summary>
public sealed class FileSystemFileInfo : FileSystemEntryInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemFileInfo"/> class,
    /// which acts as a wrapper for a file path.
    /// </summary>
    /// <param name="fileSystem">The file system <paramref name="fileName"/> belongs to.</param>
    /// <param name="fileName">The fully qualified name of the new file, or the relative file name.</param>
    public FileSystemFileInfo(IFileSystem fileSystem, string fileName) : base(fileSystem, fileName)
    {
    }

    /// <inheritdoc/>
    public override string Name => _fileSystem.GetFileName(_fullPath);

    /// <summary>
    /// Gets a string representing the directory's full path.
    /// </summary>
    public string? DirectoryName => _fileSystem.GetDirectoryName(_fullPath);

    /// <summary>
    /// Gets an instance of the parent directory.
    /// </summary>
    public FileSystemDirectoryInfo? Directory => DirectoryName is string dirName ? new(_fileSystem, dirName) : null;

    /// <inheritdoc/>
    public override bool Exists => _fileSystem.FileExists(_fullPath);

    /// <summary>
    /// Gets or sets a value that determines if the current file is read only.
    /// </summary>
    public bool IsReadOnly
    {
        get => (Attributes & FileAttributes.ReadOnly) != 0;

        set
        {
            if (value)
            {
                Attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }

    /// <summary>
    /// Gets the size, in bytes, of the current file.
    /// </summary>
    public long Length
    {
        get
        {
            using Stream stream = OpenRead();
            return stream.Length;
        }
    }

    /// <inheritdoc/>
    public override DateTime CreationTimeUtc
    {
        get => _fileSystem.GetFileCreationTimeUtc(_fullPath);
        set => _fileSystem.SetFileCreationTimeUtc(_fullPath, value);
    }

    /// <inheritdoc/>
    public override DateTime LastAccessTimeUtc
    {
        get => _fileSystem.GetFileLastAccessTimeUtc(_fullPath);
        set => _fileSystem.SetFileLastAccessTimeUtc(_fullPath, value);
    }

    /// <inheritdoc/>
    public override DateTime LastWriteTimeUtc
    {
        get => _fileSystem.GetFileLastWriteTimeUtc(_fullPath);
        set => _fileSystem.SetFileLastWriteTimeUtc(_fullPath, value);
    }

    /// <summary>
    /// Opens a file in the specified mode.
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode in which to open the file.</param>
    /// <returns>A file opened in the specified mode, with read/write access and unshared.</returns>
    public Stream Open(FileMode mode)
        => _fileSystem.Open(_fullPath, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None);

    /// <summary>
    /// Opens a file in the specified mode with read, write, or read/write access.
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode in which to open the file.</param>
    /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with Read, Write, or ReadWrite file access.</param>
    /// <returns>A <see cref="Stream"/> object opened in the specified mode and access, and unshared.</returns>
    public Stream Open(FileMode mode, FileAccess access)
        => _fileSystem.Open(_fullPath, mode, access, FileShare.None);

    /// <summary>
    /// Opens a file in the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode in which to open the file.</param>
    /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with Read, Write, or ReadWrite file access.</param>
    /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other <see cref="Stream"/> objects have to this file.</param>
    /// <returns>A <see cref="Stream"/> object opened with the specified mode, access, and sharing options.</returns>
    public Stream Open(FileMode mode, FileAccess access, FileShare share)
        => _fileSystem.Open(_fullPath, mode, access, share);

    /// <summary>
    /// Creates a read-only <see cref="Stream"/>.
    /// </summary>
    /// <returns>A new read-only <see cref="Stream"/> object.</returns>
    public Stream OpenRead()
        => _fileSystem.Open(_fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <summary>
    /// Creates a write-only <see cref="Stream"/>.
    /// </summary>
    /// <returns>A write-only unshared <see cref="Stream"/> object for a new or existing file.</returns>
    public Stream OpenWrite()
        => _fileSystem.Open(_fullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

    /// <summary>
    /// Creates a <see cref="StreamReader"/> with UTF-8 encoding that reads from an existing text file.
    /// </summary>
    /// <returns>A new <see cref="StreamReader"/> with UTF-8 encoding.</returns>
    public StreamReader OpenText() => _fileSystem.OpenText(_fullPath);

    /// <summary>
    /// Creates a <see cref="StreamWriter"/> that appends text to the file represented by this instance.
    /// </summary>
    /// <returns>A new <see cref="StreamWriter"/>.</returns>
    public StreamWriter AppendText() => _fileSystem.AppendText(_fullPath);

    /// <summary>
    /// Creates a file.
    /// </summary>
    /// <returns>A new file.</returns>
    public Stream Create() => _fileSystem.Create(_fullPath);

    /// <inheritdoc/>
    public override void CreateAsSymbolicLink(string pathToTarget)
    {
        _fileSystem.CreateFileSymbolicLink(_fullPath, pathToTarget);
        Refresh();
    }

    /// <summary>
    /// Creates a <see cref="StreamWriter"/> that writes a new text file.
    /// </summary>
    /// <returns>A new <see cref="StreamWriter"/>.</returns>
    public StreamWriter CreateText() => _fileSystem.CreateText(_fullPath);

    /// <inheritdoc/>
    public override FileSystemEntryInfo? ResolveLinkTarget(bool returnFinalTarget)
        => _fileSystem.ResolveFileLinkTarget(_fullPath, returnFinalTarget);

    /// <summary>
    /// Replaces the contents of a specified file with the file described by the current instance,
    /// deleting the original file and creating a backup of the replaced file.
    /// </summary>
    /// <param name="destinationFileName">The name of a file to replace with the current file.</param>
    /// <param name="destinationBackupFileName">The name of a file with which to create a backup of the file described by the <paramref name="destinationFileName"/> parameter.</param>
    /// <returns>A <see cref="FileSystemFileInfo"/> object that encapsulates information about the file described by the <paramref name="destinationFileName"/> parameter.</returns>
    public FileSystemFileInfo Replace(string destinationFileName, string? destinationBackupFileName)
        => Replace(destinationFileName, destinationBackupFileName, ignoreMetadataErrors: false);

    /// <summary>
    /// Replaces the contents of a specified file with the file described by the current instance,
    /// deleting the original file, and creating a backup of the replaced file.
    /// Also specifies whether to ignore merge errors.
    /// </summary>
    /// <param name="destinationFileName">The name of a file to replace with the current file.</param>
    /// <param name="destinationBackupFileName">The name of a file with which to create a backup of the file described by the <paramref name="destinationFileName"/> parameter.</param>
    /// <param name="ignoreMetadataErrors"><c>true</c> to ignore merge errors (such as attributes and ACLs) from the replaced file to the replacement file; otherwise <c>false</c>.</param>
    /// <returns>A <see cref="FileSystemFileInfo"/> object that encapsulates information about the file described by the <paramref name="destinationFileName"/> parameter.</returns>
    public FileSystemFileInfo Replace(string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors)
    {
        _fileSystem.ReplaceFile(_fullPath, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        return new(_fileSystem, destinationFileName);
    }

    /// <summary>
    /// Copies an existing file to a new file, disallowing the overwriting of an existing file.
    /// </summary>
    /// <param name="destFileName">The name of the new file to copy to.</param>
    /// <returns>A new file with a fully qualified path.</returns>
    public FileSystemFileInfo CopyTo(string destFileName) => CopyTo(destFileName, overwrite: false);

    /// <summary>
    /// Copies an existing file to a new file, allowing the overwriting of an existing file.
    /// </summary>
    /// <param name="destFileName">The name of the new file to copy to.</param>
    /// <param name="overwrite"><c>true</c> to allow an existing file to be overwritten; otherwise, <c>false</c>.</param>
    /// <returns>A new file, or an overwrite of an existing file if <paramref name="overwrite"/> is <c>true</c>.</returns>
    public FileSystemFileInfo CopyTo(string destFileName, bool overwrite)
    {
        _ = destFileName ?? throw new ArgumentNullException(nameof(destFileName));

        string fullDestFileName = _fileSystem.GetFullPath(destFileName);
        _fileSystem.CopyFile(_fullPath, fullDestFileName, overwrite);
        return new(_fileSystem, fullDestFileName);
    }

    /// <summary>
    /// Moves a specified file to a new location, providing the option to specify a new file name.
    /// </summary>
    /// <param name="destFileName">The path to move the file to, which can specify a different file name.</param>
    public void MoveTo(string destFileName) => MoveTo(destFileName, overwrite: false);

    /// <summary>
    /// Moves a specified file to a new location, providing the options to specify a new file name and
    /// to overwrite the destination file if it already exists.
    /// </summary>
    /// <param name="destFileName">The path to move the file to, which can specify a different file name.</param>
    /// <param name="overwrite"><c>true</c> to overwrite the destination file if it already exists; <c>false</c> otherwise.</param>
    public void MoveTo(string destFileName, bool overwrite)
    {
        _ = destFileName ?? throw new ArgumentNullException(nameof(destFileName));

        string fullDestFileName = _fileSystem.GetFullPath(destFileName);
        _fileSystem.MoveFile(_fullPath, fullDestFileName, overwrite);

        _fullPath = fullDestFileName;
        _originalPath = destFileName;
        Refresh();
    }

    /// <summary>
    /// Permanently deletes a file.
    /// </summary>
    public override void Delete()
    {
        _fileSystem.DeleteFile(_fullPath);
        Refresh();
    }
}
