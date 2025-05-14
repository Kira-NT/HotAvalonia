namespace HotAvalonia.IO;

/// <summary>
/// Exposes instance methods for creating, moving, and enumerating through directories and subdirectories.
/// This class cannot be inherited.
/// </summary>
public sealed class FileSystemDirectoryInfo : FileSystemEntryInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemDirectoryInfo"/> class on the specified path.
    /// </summary>
    /// <param name="fileSystem">The file system <paramref name="path"/> belongs to.</param>
    /// <param name="path">A string specifying the path on which to create the <see cref="FileSystemDirectoryInfo"/>.</param>
    public FileSystemDirectoryInfo(IFileSystem fileSystem, string path) : base(fileSystem, path)
    {
    }

    /// <inheritdoc/>
    public override string Name
    {
        get
        {
            if (_fileSystem.IsRoot(_fullPath.AsSpan()))
                return _fullPath;

            return _fileSystem.GetFileName(_fileSystem.TrimEndingDirectorySeparator(_fullPath.AsSpan())).ToString();
        }
    }

    /// <summary>
    /// Gets the parent directory of a specified subdirectory.
    /// </summary>
    public FileSystemDirectoryInfo? Parent
    {
        get
        {
            ReadOnlySpan<char> parentName = _fileSystem.GetDirectoryName(_fileSystem.TrimEndingDirectorySeparator(_fullPath.AsSpan()));
            return parentName.IsEmpty ? null : new(_fileSystem, parentName.ToString());
        }
    }

    /// <summary>
    /// Gets the root portion of the directory.
    /// </summary>
    public FileSystemDirectoryInfo Root => new(_fileSystem, _fileSystem.GetPathRoot(_fullPath)!);

    /// <inheritdoc/>
    public override bool Exists => _fileSystem.DirectoryExists(_fullPath);

    /// <inheritdoc/>
    public override DateTime CreationTimeUtc
    {
        get => _fileSystem.GetDirectoryCreationTimeUtc(_fullPath);
        set => _fileSystem.SetDirectoryCreationTimeUtc(_fullPath, value);
    }

    /// <inheritdoc/>
    public override DateTime LastAccessTimeUtc
    {
        get => _fileSystem.GetDirectoryLastAccessTimeUtc(_fullPath);
        set => _fileSystem.SetDirectoryLastAccessTimeUtc(_fullPath, value);
    }

    /// <inheritdoc/>
    public override DateTime LastWriteTimeUtc
    {
        get => _fileSystem.GetDirectoryLastWriteTimeUtc(_fullPath);
        set => _fileSystem.SetDirectoryLastWriteTimeUtc(_fullPath, value);
    }

    /// <summary>
    /// Creates a directory.
    /// </summary>
    public void Create()
    {
        _fileSystem.CreateDirectory(_fullPath);
        Refresh();
    }

    /// <inheritdoc/>
    public override void CreateAsSymbolicLink(string pathToTarget)
    {
        _fileSystem.CreateDirectorySymbolicLink(_fullPath, pathToTarget);
        Refresh();
    }

    /// <summary>
    /// Creates a subdirectory or subdirectories on the specified path.
    /// The specified path should be relative to this instance of the <see cref="FileSystemDirectoryInfo"/> class.
    /// </summary>
    /// <param name="path">The specified path.</param>
    /// <returns>The last directory specified in <paramref name="path"/>.</returns>
    public FileSystemDirectoryInfo CreateSubdirectory(string path)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));

        if (path.Length == 0)
            throw new ArgumentException("Path cannot be the empty string.", nameof(path));

        if (_fileSystem.IsPathRooted(path))
            throw new ArgumentException("Path cannot be a drive name.", nameof(path));

        string newPath = _fileSystem.GetFullPath(_fileSystem.Combine(_fullPath, path));
        ReadOnlySpan<char> trimmedNewPath = _fileSystem.TrimEndingDirectorySeparator(newPath.AsSpan());
        ReadOnlySpan<char> trimmedCurrentPath = _fileSystem.TrimEndingDirectorySeparator(_fullPath.AsSpan());
        if (!trimmedNewPath.StartsWith(trimmedCurrentPath, _fileSystem.PathComparison) || trimmedNewPath.Length != trimmedCurrentPath.Length && !_fileSystem.IsDirectorySeparator(newPath[trimmedCurrentPath.Length]))
            throw new ArgumentException($"The directory specified, '{path}', is not a subdirectory of '{_fullPath}'.", nameof(path));

        return _fileSystem.CreateDirectory(newPath);
    }

    /// <inheritdoc/>
    public override FileSystemEntryInfo? ResolveLinkTarget(bool returnFinalTarget)
        => _fileSystem.ResolveDirectoryLinkTarget(_fullPath, returnFinalTarget);

    /// <summary>
    /// Moves a DirectoryInfo instance and its contents to a new path.
    /// </summary>
    /// <param name="destDirName">The name and path to which to move this directory.</param>
    public void MoveTo(string destDirName)
    {
        _ = destDirName ?? throw new ArgumentNullException(nameof(destDirName));

        string destination = _fileSystem.GetFullPath(destDirName);
        _fileSystem.MoveDirectory(_fullPath, destination);

        _originalPath = destDirName;
        _fullPath = _fileSystem.EnsureTrailingSeparator(destination);
        Refresh();
    }

    /// <inheritdoc/>
    public override void Delete() => Delete(recursive: false);

    /// <summary>
    /// Deletes this directory and, if indicated, any subdirectories and files in the directory.
    /// </summary>
    /// <param name="recursive"><c>true</c> to remove directories, subdirectories, and files in path; otherwise, <c>false</c>.</param>
    public void Delete(bool recursive)
    {
        _fileSystem.DeleteDirectory(_fullPath, recursive);
        Refresh();
    }

    /// <summary>
    /// Returns the subdirectories of the current directory.
    /// </summary>
    /// <returns>An array of <see cref="FileSystemDirectoryInfo"/> objects.</returns>
    public FileSystemDirectoryInfo[] GetDirectories()
        => GetDirectories("*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an array of directories in the current <see cref="FileSystemDirectoryInfo"/> matching the given search criteria.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories.</param>
    /// <returns>An array of type <see cref="FileSystemDirectoryInfo"/> matching <paramref name="searchPattern"/>.</returns>
    public FileSystemDirectoryInfo[] GetDirectories(string searchPattern)
        => GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an array of directories in the current <see cref="FileSystemDirectoryInfo"/> matching
    /// the given search criteria and using a value to determine whether to search subdirectories.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
    /// <returns>An array of type <see cref="FileSystemDirectoryInfo"/> matching <paramref name="searchPattern"/>.</returns>
    public FileSystemDirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
        => EnumerateDirectories(searchPattern, searchOption).ToArray();

    /// <summary>
    /// Returns a file list from the current directory.
    /// </summary>
    /// <returns>An array of type <see cref="FileSystemFileInfo"/>.</returns>
    public FileSystemFileInfo[] GetFiles()
        => GetFiles("*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns a file list from the current directory matching the given search pattern.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of files.</param>
    /// <returns>An array of type <see cref="FileSystemFileInfo"/>.</returns>
    public FileSystemFileInfo[] GetFiles(string searchPattern)
        => GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns a file list from the current directory matching the given search pattern and
    /// using a value to determine whether to search subdirectories.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of files.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
    /// <returns>An array of type <see cref="FileSystemFileInfo"/>.</returns>
    public FileSystemFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        => EnumerateFiles(searchPattern, searchOption).ToArray();

    /// <summary>
    /// Returns an array of strongly typed <see cref="FileSystemEntryInfo"/> entries
    /// representing all the files and subdirectories in a directory.
    /// </summary>
    /// <returns>An array of strongly typed <see cref="FileSystemEntryInfo"/> entries.</returns>
    public FileSystemEntryInfo[] GetFileSystemInfos()
        => GetFileSystemInfos("*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Retrieves an array of strongly typed <see cref="FileSystemEntryInfo"/> objects representing
    /// the files and subdirectories that match the specified search criteria.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories and files.</param>
    /// <returns>An array of strongly typed <see cref="FileSystemEntryInfo"/> objects matching the search criteria.</returns>
    public FileSystemEntryInfo[] GetFileSystemInfos(string searchPattern)
        => GetFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Retrieves an array of <see cref="FileSystemEntryInfo"/> objects that represent
    /// the files and subdirectories matching the specified search criteria.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories and files.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
    /// <returns>An array of file system entries that match the search criteria.</returns>
    public FileSystemEntryInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption)
        => EnumerateFileSystemInfos(searchPattern, searchOption).ToArray();

    /// <summary>
    /// Returns an enumerable collection of directory information in the current directory.
    /// </summary>
    /// <returns>An enumerable collection of directories in the current directory.</returns>
    public IEnumerable<FileSystemDirectoryInfo> EnumerateDirectories()
        => EnumerateDirectories("*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an enumerable collection of directory information that matches a specified search pattern.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories.</param>
    /// <returns>An enumerable collection of directories that matches <paramref name="searchPattern"/>.</returns>
    public IEnumerable<FileSystemDirectoryInfo> EnumerateDirectories(string searchPattern)
        => EnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an enumerable collection of directory information that matches a specified search pattern and search subdirectory option.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
    /// <returns>An enumerable collection of directories that matches <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
    public IEnumerable<FileSystemDirectoryInfo> EnumerateDirectories(string searchPattern, SearchOption searchOption)
        => _fileSystem.EnumerateDirectories(_fullPath, searchPattern, searchOption).Select(x => new FileSystemDirectoryInfo(_fileSystem, x));

    /// <summary>
    /// Returns an enumerable collection of file information in the current directory.
    /// </summary>
    /// <returns>An enumerable collection of the files in the current directory.</returns>
    public IEnumerable<FileSystemFileInfo> EnumerateFiles()
        => EnumerateFiles("*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an enumerable collection of file information that matches a search pattern.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of files.</param>
    /// <returns>An enumerable collection of files that matches <paramref name="searchPattern"/>.</returns>
    public IEnumerable<FileSystemFileInfo> EnumerateFiles(string searchPattern)
        => EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an enumerable collection of file information that matches a specified search pattern and search subdirectory option.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of files.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
    /// <returns>An enumerable collection of files that matches <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
    public IEnumerable<FileSystemFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
        => _fileSystem.EnumerateFiles(_fullPath, searchPattern, searchOption).Select(x => new FileSystemFileInfo(_fileSystem, x));

    /// <summary>
    /// Returns an enumerable collection of file system information in the current directory.
    /// </summary>
    /// <returns>An enumerable collection of file system information in the current directory.</returns>
    public IEnumerable<FileSystemEntryInfo> EnumerateFileSystemInfos()
        => EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an enumerable collection of file system information that matches a specified search pattern.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories.</param>
    /// <returns>An enumerable collection of file system information objects that matches <paramref name="searchPattern"/>.</returns>
    public IEnumerable<FileSystemEntryInfo> EnumerateFileSystemInfos(string searchPattern)
        => EnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an enumerable collection of file system information that matches a specified search pattern and search subdirectory option.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of directories.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
    /// <returns>An enumerable collection of file system information objects that matches <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
    public IEnumerable<FileSystemEntryInfo> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
    {
        IEnumerable<FileSystemEntryInfo> files = EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly);
        IEnumerable<FileSystemDirectoryInfo> directories = EnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        if (searchOption is SearchOption.TopDirectoryOnly)
            return files.Concat(directories);

        IEnumerable<FileSystemEntryInfo> nestedEntries = directories.SelectMany(x => new FileSystemEntryInfo[] { x }.Concat(x.EnumerateFileSystemInfos(searchPattern, searchOption)));
        return files.Concat(nestedEntries);
    }
}
