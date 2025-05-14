using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotAvalonia.Helpers;

namespace HotAvalonia.IO;

/// <summary>
/// Represents an empty, read-only file system.
/// </summary>
internal sealed class EmptyFileSystem : IFileSystem
{
    /// <summary>
    /// The singleton instance of the <see cref="EmptyFileSystem"/> class.
    /// </summary>
    public static readonly EmptyFileSystem Instance = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyFileSystem"/> class.
    /// </summary>
    private EmptyFileSystem() { }

    /// <inheritdoc/>
    public string Name
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => "@empty";
    }

    /// <inheritdoc/>
    public string CurrentDirectory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => "/";

        set
        {
            _ = value ?? throw new ArgumentNullException(nameof(CurrentDirectory));
            if (this.GetFullPath(value) != "/")
                ThrowDirectoryNotFoundException(value);
        }
    }

    /// <inheritdoc/>
    public string TempDirectory => CurrentDirectory;

    /// <inheritdoc/>
    public StringComparison PathComparison
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => StringComparison.Ordinal;
    }

    /// <inheritdoc/>
    public StringComparer PathComparer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => StringComparer.Ordinal;
    }

    /// <inheritdoc/>
    public char DirectorySeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => '/';
    }

    /// <inheritdoc/>
    public char AltDirectorySeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => '/';
    }

    /// <inheritdoc/>
    public char VolumeSeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => '/';
    }

    /// <inheritdoc/>
    public char PathSeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ':';
    }

    /// <inheritdoc/>
    public char[] InvalidFileNameChars
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ['/'];
    }

    /// <inheritdoc/>
    public char[] InvalidPathChars
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => [];
    }

    /// <inheritdoc/>
    public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
        => ThrowFileNotFoundException(sourceFileName);

    /// <inheritdoc/>
    public ValueTask CopyFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        CopyFile(sourceFileName, destFileName, overwrite);
        return default;
    }

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectory(string path)
    {
        if (DirectoryExists(path))
            return new(this, path);

        ThrowUnauthorizedAccessException(path);
        return default;
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => new(CreateDirectory(path));

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectorySymbolicLink(string path, string pathToTarget)
    {
        ThrowUnauthorizedAccessException(path);
        return default;
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectorySymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default)
        => new(CreateDirectorySymbolicLink(path, pathToTarget));

    /// <inheritdoc/>
    public FileSystemFileInfo CreateFileSymbolicLink(string path, string pathToTarget)
    {
        ThrowUnauthorizedAccessException(path);
        return default;
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo> CreateFileSymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default)
        => new(CreateFileSymbolicLink(path, pathToTarget));

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher()
        => new EmptyFileSystemWatcher(this);

    /// <inheritdoc/>
    public ValueTask<IFileSystemWatcher> CreateFileSystemWatcherAsync(CancellationToken cancellationToken = default)
        => new(CreateFileSystemWatcher());

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateTempSubdirectory(string? prefix = null)
    {
        ThrowUnauthorizedAccessException(TempDirectory);
        return default;
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateTempSubdirectoryAsync(string? prefix = null, CancellationToken cancellationToken = default)
        => new(CreateTempSubdirectory(prefix));

    /// <inheritdoc/>
    public void DeleteDirectory(string path, bool recursive = false)
    {
        if (DirectoryExists(path))
            ThrowUnauthorizedAccessException(path);
    }

    /// <inheritdoc/>
    public ValueTask DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        DeleteDirectory(path, recursive);
        return default;
    }

    /// <inheritdoc/>
    public void DeleteFile(string path) { }

    /// <inheritdoc/>
    public ValueTask DeleteFileAsync(string path, CancellationToken cancellationToken = default) => default;

    /// <inheritdoc/>
    public bool DirectoryExists([NotNullWhen(true)] string? path)
        => path is { Length: > 0 } && this.GetFullPath(path) == "/";

    /// <inheritdoc/>
    public ValueTask<bool> DirectoryExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
        => new(DirectoryExists(path));

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        => EnumerateFileSystemEntries(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateDirectoriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => EnumerateDirectories(path, searchPattern, searchOption).ToAsyncEnumerable();

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        => EnumerateFileSystemEntries(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => EnumerateFiles(path, searchPattern, searchOption).ToAsyncEnumerable();

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
    {
        if (!DirectoryExists(path))
            ThrowDirectoryNotFoundException(path);

        return [];
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFileSystemEntriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => EnumerateFileSystemEntries(path, searchPattern, searchOption).ToAsyncEnumerable();

    /// <inheritdoc/>
    public bool FileExists([NotNullWhen(true)] string? path)
        => false;

    /// <inheritdoc/>
    public ValueTask<bool> FileExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
        => new(FileExists(path));

    /// <inheritdoc/>
    public DateTime GetDirectoryCreationTimeUtc(string path)
        => FileSystem.s_missingFileSystemEntryTimestampUtc;

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetDirectoryCreationTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetDirectoryLastAccessTimeUtc(string path)
        => FileSystem.s_missingFileSystemEntryTimestampUtc;

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetDirectoryLastAccessTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetDirectoryLastWriteTimeUtc(string path)
        => FileSystem.s_missingFileSystemEntryTimestampUtc;

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetDirectoryLastWriteTimeUtc(path));

    /// <inheritdoc/>
    public FileAttributes GetFileAttributes(string path)
    {
        ThrowFileNotFoundException(path);
        return default;
    }

    /// <inheritdoc/>
    public ValueTask<FileAttributes> GetFileAttributesAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileAttributes(path));

    /// <inheritdoc/>
    public DateTime GetFileCreationTimeUtc(string path)
        => FileSystem.s_missingFileSystemEntryTimestampUtc;

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileCreationTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetFileLastAccessTimeUtc(string path)
        => FileSystem.s_missingFileSystemEntryTimestampUtc;

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileLastAccessTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetFileLastWriteTimeUtc(string path)
        => FileSystem.s_missingFileSystemEntryTimestampUtc;

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileLastWriteTimeUtc(path));

    /// <inheritdoc/>
    public string[] GetLogicalDrives()
        => [CurrentDirectory];

    /// <inheritdoc/>
    public Task<string[]> GetLogicalDrivesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetLogicalDrives());

    /// <inheritdoc/>
    public string GetTempFileName()
    {
        ThrowUnauthorizedAccessException(TempDirectory);
        return default;
    }

    /// <inheritdoc/>
    public ValueTask<string> GetTempFileNameAsync(CancellationToken cancellationToken = default)
        => new(GetTempFileName());

    /// <inheritdoc/>
    public void MoveDirectory(string sourceDirName, string destDirName)
        => ThrowDirectoryNotFoundExceptionOrUnauthorizedAccessException(sourceDirName);

    /// <inheritdoc/>
    public ValueTask MoveDirectoryAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken = default)
    {
        MoveDirectory(sourceDirName, destDirName);
        return default;
    }

    /// <inheritdoc/>
    public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
        => ThrowFileNotFoundException(sourceFileName);

    /// <inheritdoc/>
    public ValueTask MoveFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        MoveFile(sourceFileName, destFileName);
        return default;
    }

    /// <inheritdoc/>
    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
    {
        ThrowFileNotFoundException(path);
        return default;
    }

    /// <inheritdoc/>
    public Task<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken = default)
        => Task.FromResult(Open(path, mode, access, share));

    /// <inheritdoc/>
    public void ReplaceFile(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false)
        => ThrowFileNotFoundException(sourceFileName);

    /// <inheritdoc/>
    public ValueTask ReplaceFileAsync(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false, CancellationToken cancellationToken = default)
    {
        ReplaceFile(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        return default;
    }

    /// <inheritdoc/>
    public FileSystemDirectoryInfo? ResolveDirectoryLinkTarget(string linkPath, bool returnFinalTarget)
    {
        if (!DirectoryExists(linkPath))
            ThrowDirectoryNotFoundException(linkPath);

        return null;
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo?> ResolveDirectoryLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default)
        => new(ResolveDirectoryLinkTarget(linkPath, returnFinalTarget));

    /// <inheritdoc/>
    public FileSystemFileInfo? ResolveFileLinkTarget(string linkPath, bool returnFinalTarget)
    {
        ThrowFileNotFoundException(linkPath);
        return null;
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo?> ResolveFileLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default)
        => new(ResolveFileLinkTarget(linkPath, returnFinalTarget));

    /// <inheritdoc/>
    public void SetDirectoryCreationTimeUtc(string path, DateTime creationTimeUtc)
        => ThrowDirectoryNotFoundExceptionOrUnauthorizedAccessException(path);

    /// <inheritdoc/>
    public ValueTask SetDirectoryCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default)
    {
        SetDirectoryCreationTimeUtc(path, creationTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetDirectoryLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        => ThrowDirectoryNotFoundExceptionOrUnauthorizedAccessException(path);

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default)
    {
        SetDirectoryLastAccessTimeUtc(path, lastAccessTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetDirectoryLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        => ThrowDirectoryNotFoundExceptionOrUnauthorizedAccessException(path);

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default)
    {
        SetDirectoryLastWriteTimeUtc(path, lastWriteTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileAttributes(string path, FileAttributes fileAttributes)
        => ThrowFileNotFoundException(path);

    /// <inheritdoc/>
    public ValueTask SetFileAttributesAsync(string path, FileAttributes fileAttributes, CancellationToken cancellationToken = default)
    {
        SetFileAttributes(path, fileAttributes);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileCreationTimeUtc(string path, DateTime creationTimeUtc)
        => ThrowFileNotFoundException(path);

    /// <inheritdoc/>
    public ValueTask SetFileCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default)
    {
        SetFileCreationTimeUtc(path, creationTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        => ThrowFileNotFoundException(path);

    /// <inheritdoc/>
    public ValueTask SetFileLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default)
    {
        SetFileLastAccessTimeUtc(path, lastAccessTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        => ThrowFileNotFoundException(path);

    /// <inheritdoc/>
    public ValueTask SetFileLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default)
    {
        SetFileLastWriteTimeUtc(path, lastWriteTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void Dispose() { }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;

    /// <inheritdoc/>
    public override string ToString() => this.ToFormattedString();

    /// <summary>
    /// Throws a <see cref="DirectoryNotFoundException"/> if the specified path does not exist as a directory;
    /// otherwise throws an <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    /// <param name="path">The directory path to validate access for.</param>
    [DoesNotReturn]
    private void ThrowDirectoryNotFoundExceptionOrUnauthorizedAccessException(string path)
    {
        if (DirectoryExists(path))
        {
            ThrowUnauthorizedAccessException(path);
        }
        else
        {
            ThrowDirectoryNotFoundException(path);
        }
    }

    /// <summary>
    /// Throws an <see cref="UnauthorizedAccessException"/> for the specified path.
    /// </summary>
    /// <param name="path">The path that access is denied to.</param>
    [DoesNotReturn]
    private static void ThrowUnauthorizedAccessException(string path)
        => throw new UnauthorizedAccessException($"Access to the path '{path}' is denied.");

    /// <summary>
    /// Throws a <see cref="DirectoryNotFoundException"/> for the specified path.
    /// </summary>
    /// <param name="path">The path that could not be found.</param>
    [DoesNotReturn]
    private static void ThrowDirectoryNotFoundException(string path)
        => throw new DirectoryNotFoundException($"Could not find a part of the path '{path}'.");

    /// <summary>
    /// Throws a <see cref="FileNotFoundException"/> for the specified path.
    /// </summary>
    /// <param name="path">The file path that could not be found.</param>
    [DoesNotReturn]
    private static void ThrowFileNotFoundException(string path)
        => throw new FileNotFoundException(null, path);
}
