using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace HotAvalonia.IO;

/// <summary>
/// Provides a caching layer for file system operations.
/// </summary>
internal sealed class CachingFileSystem : IFileSystem, IFileSystemPathOperations
{
    /// <summary>
    /// The original file system wrapped by this instance.
    /// </summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Indicates whether the underlying file system should be left open after this instance is disposed.
    /// </summary>
    private readonly bool _leaveOpen;

    /// <summary>
    /// A cache to store file entries keyed by their file names.
    /// </summary>
    private readonly ConcurrentDictionary<string, Entry> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingFileSystem"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system to wrap.</param>
    /// <param name="leaveOpen"><c>true</c> to leave the underlying file system open after this object is disposed; otherwise, <c>false</c>.</param>
    public CachingFileSystem(IFileSystem fileSystem, bool leaveOpen = false)
    {
        _fileSystem = fileSystem;
        _leaveOpen = leaveOpen;
        _cache = new(fileSystem.PathComparer);
    }

    /// <inheritdoc/>
    public string Name => _fileSystem.Name;

    /// <inheritdoc/>
    public string CurrentDirectory
    {
        get => _fileSystem.CurrentDirectory;
        set => _fileSystem.CurrentDirectory = value;
    }

    /// <inheritdoc/>
    public string TempDirectory => _fileSystem.TempDirectory;

    /// <inheritdoc/>
    public StringComparison PathComparison => _fileSystem.PathComparison;

    /// <inheritdoc/>
    public StringComparer PathComparer => _fileSystem.PathComparer;

    /// <inheritdoc/>
    public char DirectorySeparator => _fileSystem.DirectorySeparator;

    /// <inheritdoc/>
    public char AltDirectorySeparator => _fileSystem.AltDirectorySeparator;

    /// <inheritdoc/>
    public char VolumeSeparator => _fileSystem.VolumeSeparator;

    /// <inheritdoc/>
    public char PathSeparator => _fileSystem.PathSeparator;

    /// <inheritdoc/>
    public char[] InvalidFileNameChars => _fileSystem.InvalidFileNameChars;

    /// <inheritdoc/>
    public char[] InvalidPathChars => _fileSystem.InvalidPathChars;

    /// <inheritdoc/>
    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));

        string fullPath = _fileSystem.GetFullPath(path);
        try
        {
            return _cache.GetOrAdd(fullPath, new Entry(_fileSystem, fullPath)).Open(mode, access, share);
        }
        catch
        {
            _cache.TryRemove(fullPath, out _);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken = default)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));

        string fullPath = _fileSystem.GetFullPath(path);
        try
        {
            return await _cache.GetOrAdd(fullPath, new Entry(_fileSystem, fullPath)).OpenAsync(mode, access, share, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _cache.TryRemove(fullPath, out _);
            throw;
        }
    }

    /// <inheritdoc/>
    public bool FileExists([NotNullWhen(true)] string? path)
    {
        if (path is not { Length: > 0 })
            return false;

        if (_fileSystem.FileExists(path))
            return true;

        _cache.TryRemove(_fileSystem.GetFullPath(path), out _);
        return false;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> FileExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
    {
        if (path is not { Length: > 0 })
            return false;

        if (await _fileSystem.FileExistsAsync(path, cancellationToken).ConfigureAwait(false))
            return true;

        _cache.TryRemove(_fileSystem.GetFullPath(path), out _);
        return false;
    }

    /// <inheritdoc/>
    public void DeleteFile(string path)
    {
        _fileSystem.DeleteFile(path);
        _cache.TryRemove(_fileSystem.GetFullPath(path), out _);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await _fileSystem.DeleteFileAsync(path, cancellationToken).ConfigureAwait(false);
        _cache.TryRemove(_fileSystem.GetFullPath(path), out _);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_leaveOpen)
            _fileSystem.Dispose();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _leaveOpen ? default : _fileSystem.DisposeAsync();

    /// <inheritdoc/>
    public override string ToString() => this.ToFormattedString();

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? ChangeExtension(string? path, string? extension) => _fileSystem.ChangeExtension(path, extension);

    /// <inheritdoc/>
    public string Combine(params scoped ReadOnlySpan<string> paths) => _fileSystem.Combine(paths);

    /// <inheritdoc/>
    public string Combine(params string[] paths) => _fileSystem.Combine(paths);

    /// <inheritdoc/>
    public string Combine(string path1, string path2) => _fileSystem.Combine(path1, path2);

    /// <inheritdoc/>
    public string Combine(string path1, string path2, string path3) => _fileSystem.Combine(path1, path2, path3);

    /// <inheritdoc/>
    public string Combine(string path1, string path2, string path3, string path4) => _fileSystem.Combine(path1, path2, path3, path4);

    /// <inheritdoc/>
    public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) => _fileSystem.CopyFile(sourceFileName, destFileName, overwrite);

    /// <inheritdoc/>
    public ValueTask CopyFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default) => _fileSystem.CopyFileAsync(sourceFileName, destFileName, overwrite, cancellationToken);

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectory(string path) => _fileSystem.CreateDirectory(path);

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.CreateDirectoryAsync(path, cancellationToken);

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectorySymbolicLink(string path, string pathToTarget) => _fileSystem.CreateDirectorySymbolicLink(path, pathToTarget);

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectorySymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default) => _fileSystem.CreateDirectorySymbolicLinkAsync(path, pathToTarget, cancellationToken);

    /// <inheritdoc/>
    public FileSystemFileInfo CreateFileSymbolicLink(string path, string pathToTarget) => _fileSystem.CreateFileSymbolicLink(path, pathToTarget);

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo> CreateFileSymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default) => _fileSystem.CreateFileSymbolicLinkAsync(path, pathToTarget, cancellationToken);

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher() => _fileSystem.CreateFileSystemWatcher();

    /// <inheritdoc/>
    public ValueTask<IFileSystemWatcher> CreateFileSystemWatcherAsync(CancellationToken cancellationToken = default) => _fileSystem.CreateFileSystemWatcherAsync(cancellationToken);

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateTempSubdirectory(string? prefix = null) => _fileSystem.CreateTempSubdirectory(prefix);

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateTempSubdirectoryAsync(string? prefix = null, CancellationToken cancellationToken = default) => _fileSystem.CreateTempSubdirectoryAsync(prefix, cancellationToken);

    /// <inheritdoc/>
    public void DeleteDirectory(string path, bool recursive = false) => _fileSystem.DeleteDirectory(path, recursive);

    /// <inheritdoc/>
    public ValueTask DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default) => _fileSystem.DeleteDirectoryAsync(path, recursive, cancellationToken);

    /// <inheritdoc/>
    public bool DirectoryExists([NotNullWhen(true)] string? path) => _fileSystem.DirectoryExists(path);

    /// <inheritdoc/>
    public ValueTask<bool> DirectoryExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default) => _fileSystem.DirectoryExistsAsync(path, cancellationToken);

    /// <inheritdoc/>
    public bool EndsInDirectorySeparator(ReadOnlySpan<char> path) => _fileSystem.EndsInDirectorySeparator(path);

    /// <inheritdoc/>
    public bool EndsInDirectorySeparator([NotNullWhen(true)] string? path) => _fileSystem.EndsInDirectorySeparator(path);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption) => _fileSystem.EnumerateDirectories(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateDirectoriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default) => _fileSystem.EnumerateDirectoriesAsync(path, searchPattern, searchOption, cancellationToken);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => _fileSystem.EnumerateFiles(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default) => _fileSystem.EnumerateFilesAsync(path, searchPattern, searchOption, cancellationToken);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => _fileSystem.EnumerateFileSystemEntries(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFileSystemEntriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default) => _fileSystem.EnumerateFileSystemEntriesAsync(path, searchPattern, searchOption, cancellationToken);

    /// <inheritdoc/>
    public DateTime GetDirectoryCreationTimeUtc(string path) => _fileSystem.GetDirectoryCreationTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.GetDirectoryCreationTimeUtcAsync(path, cancellationToken);

    /// <inheritdoc/>
    public DateTime GetDirectoryLastAccessTimeUtc(string path) => _fileSystem.GetDirectoryLastAccessTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.GetDirectoryLastAccessTimeUtcAsync(path, cancellationToken);

    /// <inheritdoc/>
    public DateTime GetDirectoryLastWriteTimeUtc(string path) => _fileSystem.GetDirectoryLastWriteTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.GetDirectoryLastWriteTimeUtcAsync(path, cancellationToken);

    /// <inheritdoc/>
    public string? GetDirectoryName(string? path) => _fileSystem.GetDirectoryName(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path) => _fileSystem.GetDirectoryName(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path) => _fileSystem.GetExtension(path);

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetExtension(string? path) => _fileSystem.GetExtension(path);

    /// <inheritdoc/>
    public FileAttributes GetFileAttributes(string path) => _fileSystem.GetFileAttributes(path);

    /// <inheritdoc/>
    public ValueTask<FileAttributes> GetFileAttributesAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.GetFileAttributesAsync(path, cancellationToken);

    /// <inheritdoc/>
    public DateTime GetFileCreationTimeUtc(string path) => _fileSystem.GetFileCreationTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.GetFileCreationTimeUtcAsync(path, cancellationToken);

    /// <inheritdoc/>
    public DateTime GetFileLastAccessTimeUtc(string path) => _fileSystem.GetFileLastAccessTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.GetFileLastAccessTimeUtcAsync(path, cancellationToken);

    /// <inheritdoc/>
    public DateTime GetFileLastWriteTimeUtc(string path) => _fileSystem.GetFileLastWriteTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default) => _fileSystem.GetFileLastWriteTimeUtcAsync(path, cancellationToken);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path) => _fileSystem.GetFileName(path);

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetFileName(string? path) => _fileSystem.GetFileName(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path) => _fileSystem.GetFileNameWithoutExtension(path);

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetFileNameWithoutExtension(string? path) => _fileSystem.GetFileNameWithoutExtension(path);

    /// <inheritdoc/>
    public string GetFullPath(string path) => _fileSystem.GetFullPath(path);

    /// <inheritdoc/>
    public string GetFullPath(string path, string basePath) => _fileSystem.GetFullPath(path, basePath);

    /// <inheritdoc/>
    public string[] GetLogicalDrives() => _fileSystem.GetLogicalDrives();

    /// <inheritdoc/>
    public Task<string[]> GetLogicalDrivesAsync(CancellationToken cancellationToken = default) => _fileSystem.GetLogicalDrivesAsync(cancellationToken);

    /// <inheritdoc/>
    public string? GetPathRoot(string? path) => _fileSystem.GetPathRoot(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path) => _fileSystem.GetPathRoot(path);

    /// <inheritdoc/>
    public string GetRandomFileName() => _fileSystem.GetRandomFileName();

    /// <inheritdoc/>
    public string GetRelativePath(string relativeTo, string path) => _fileSystem.GetRelativePath(relativeTo, path);

    /// <inheritdoc/>
    public string GetTempFileName() => _fileSystem.GetTempFileName();

    /// <inheritdoc/>
    public ValueTask<string> GetTempFileNameAsync(CancellationToken cancellationToken = default) => _fileSystem.GetTempFileNameAsync(cancellationToken);

    /// <inheritdoc/>
    public bool HasExtension(ReadOnlySpan<char> path) => _fileSystem.HasExtension(path);

    /// <inheritdoc/>
    public bool HasExtension([NotNullWhen(true)] string? path) => _fileSystem.HasExtension(path);

    /// <inheritdoc/>
    public bool IsPathFullyQualified(ReadOnlySpan<char> path) => _fileSystem.IsPathFullyQualified(path);

    /// <inheritdoc/>
    public bool IsPathFullyQualified([NotNullWhen(true)] string? path) => _fileSystem.IsPathFullyQualified(path);

    /// <inheritdoc/>
    public bool IsPathRooted(ReadOnlySpan<char> path) => _fileSystem.IsPathRooted(path);

    /// <inheritdoc/>
    public bool IsPathRooted([NotNullWhen(true)] string? path) => _fileSystem.IsPathRooted(path);

    /// <inheritdoc/>
    public string Join(string? path1, string? path2) => _fileSystem.Join(path1, path2);

    /// <inheritdoc/>
    public string Join(string? path1, string? path2, string? path3) => _fileSystem.Join(path1, path2, path3);

    /// <inheritdoc/>
    public string Join(string? path1, string? path2, string? path3, string? path4) => _fileSystem.Join(path1, path2, path3, path4);

    /// <inheritdoc/>
    public string Join(params string?[] paths) => _fileSystem.Join(paths);

    /// <inheritdoc/>
    public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2) => _fileSystem.Join(path1, path2);

    /// <inheritdoc/>
    public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3) => _fileSystem.Join(path1, path2, path3);

    /// <inheritdoc/>
    public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4) => _fileSystem.Join(path1, path2, path3, path4);

    /// <inheritdoc/>
    public string Join(params scoped ReadOnlySpan<string?> paths) => _fileSystem.Join(paths);

    /// <inheritdoc/>
    public void MoveDirectory(string sourceDirName, string destDirName) => _fileSystem.MoveDirectory(sourceDirName, destDirName);

    /// <inheritdoc/>
    public ValueTask MoveDirectoryAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken = default) => _fileSystem.MoveDirectoryAsync(sourceDirName, destDirName, cancellationToken);

    /// <inheritdoc/>
    public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false) => _fileSystem.MoveFile(sourceFileName, destFileName, overwrite);

    /// <inheritdoc/>
    public ValueTask MoveFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default) => _fileSystem.MoveFileAsync(sourceFileName, destFileName, overwrite, cancellationToken);

    /// <inheritdoc/>
    public void ReplaceFile(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false) => _fileSystem.ReplaceFile(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);

    /// <inheritdoc/>
    public ValueTask ReplaceFileAsync(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false, CancellationToken cancellationToken = default) => _fileSystem.ReplaceFileAsync(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors, cancellationToken);

    /// <inheritdoc/>
    public FileSystemDirectoryInfo? ResolveDirectoryLinkTarget(string linkPath, bool returnFinalTarget) => _fileSystem.ResolveDirectoryLinkTarget(linkPath, returnFinalTarget);

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo?> ResolveDirectoryLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default) => _fileSystem.ResolveDirectoryLinkTargetAsync(linkPath, returnFinalTarget, cancellationToken);

    /// <inheritdoc/>
    public FileSystemFileInfo? ResolveFileLinkTarget(string linkPath, bool returnFinalTarget) => _fileSystem.ResolveFileLinkTarget(linkPath, returnFinalTarget);

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo?> ResolveFileLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default) => _fileSystem.ResolveFileLinkTargetAsync(linkPath, returnFinalTarget, cancellationToken);

    /// <inheritdoc/>
    public void SetDirectoryCreationTimeUtc(string path, DateTime creationTimeUtc) => _fileSystem.SetDirectoryCreationTimeUtc(path, creationTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetDirectoryCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default) => _fileSystem.SetDirectoryCreationTimeUtcAsync(path, creationTimeUtc, cancellationToken);

    /// <inheritdoc/>
    public void SetDirectoryLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => _fileSystem.SetDirectoryLastAccessTimeUtc(path, lastAccessTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default) => _fileSystem.SetDirectoryLastAccessTimeUtcAsync(path, lastAccessTimeUtc, cancellationToken);

    /// <inheritdoc/>
    public void SetDirectoryLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => _fileSystem.SetDirectoryLastWriteTimeUtc(path, lastWriteTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default) => _fileSystem.SetDirectoryLastWriteTimeUtcAsync(path, lastWriteTimeUtc, cancellationToken);

    /// <inheritdoc/>
    public void SetFileAttributes(string path, FileAttributes fileAttributes) => _fileSystem.SetFileAttributes(path, fileAttributes);

    /// <inheritdoc/>
    public ValueTask SetFileAttributesAsync(string path, FileAttributes fileAttributes, CancellationToken cancellationToken = default) => _fileSystem.SetFileAttributesAsync(path, fileAttributes, cancellationToken);

    /// <inheritdoc/>
    public void SetFileCreationTimeUtc(string path, DateTime creationTimeUtc) => _fileSystem.SetFileCreationTimeUtc(path, creationTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetFileCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default) => _fileSystem.SetFileCreationTimeUtcAsync(path, creationTimeUtc, cancellationToken);

    /// <inheritdoc/>
    public void SetFileLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => _fileSystem.SetFileLastAccessTimeUtc(path, lastAccessTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetFileLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default) => _fileSystem.SetFileLastAccessTimeUtcAsync(path, lastAccessTimeUtc, cancellationToken);

    /// <inheritdoc/>
    public void SetFileLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => _fileSystem.SetFileLastWriteTimeUtc(path, lastWriteTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetFileLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default) => _fileSystem.SetFileLastWriteTimeUtcAsync(path, lastWriteTimeUtc, cancellationToken);

    /// <inheritdoc/>
    public ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) => _fileSystem.TrimEndingDirectorySeparator(path);

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? TrimEndingDirectorySeparator(string? path) => _fileSystem.TrimEndingDirectorySeparator(path);

    /// <inheritdoc/>
    public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten) => _fileSystem.TryJoin(path1, path2, destination, out charsWritten);

    /// <inheritdoc/>
    public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten) => _fileSystem.TryJoin(path1, path2, path3, destination, out charsWritten);

    /// <summary>
    /// Represents a cached file entry.
    /// </summary>
    private sealed class Entry
    {
        /// <summary>
        /// The file system associated with this entry.
        /// </summary>
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// The full path of the file represented by this entry.
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// The cached data of the file, if available.
        /// </summary>
        private byte[]? _data;

        /// <summary>
        /// The last known write time of the file, used for cache validation.
        /// </summary>
        private long _lastWriteTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system where <paramref name="path"/> can be found.</param>
        /// <param name="path">The full path of the file represented by this entry.</param>
        public Entry(IFileSystem fileSystem, string path)
        {
            _fileSystem = fileSystem;
            _path = path;
            _data = null;
            _lastWriteTime = DateTime.MinValue.Ticks;
        }

        /// <summary>
        /// Gets the full path of the file represented by this entry.
        /// </summary>
        public string FullName => _path;

        /// <inheritdoc cref="IFileSystem.Open(string, FileMode, FileAccess, FileShare)"/>
        public Stream Open(FileMode mode, FileAccess access, FileShare share)
        {
            if ((mode, access, share & ~FileShare.Read) == (FileMode.Open, FileAccess.Read, FileShare.None))
                return OpenRead();

            Interlocked.Exchange(ref _data, null);
            return _fileSystem.Open(_path, mode, access, share);
        }

        /// <inheritdoc cref="IFileSystem.OpenAsync(string, FileMode, FileAccess, FileShare, CancellationToken)"/>
        public Task<Stream> OpenAsync(FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken)
        {
            if ((mode, access, share & ~FileShare.Read) == (FileMode.Open, FileAccess.Read, FileShare.None))
                return OpenReadAsync(cancellationToken);

            Interlocked.Exchange(ref _data, null);
            return _fileSystem.OpenAsync(_path, mode, access, share, cancellationToken);
        }

        /// <summary>
        /// Opens a stream to read the cached data of the file.
        /// </summary>
        /// <returns>A <see cref="Stream"/> for reading the file's cached data.</returns>
        private Stream OpenRead()
        {
            byte[]? data = _data;
            DateTime lastWriteTime = new(Interlocked.Read(ref _lastWriteTime), DateTimeKind.Utc);
            DateTime currentLastWriteTime = _fileSystem.GetFileLastWriteTimeUtc(_path);

            if (data is null || currentLastWriteTime != lastWriteTime && _fileSystem.FileExists(_path))
            {
                data = _fileSystem.ReadAllBytes(_path);
                Interlocked.Exchange(ref _data, data);
                Interlocked.Exchange(ref _lastWriteTime, currentLastWriteTime.Ticks);
            }

            return new MemoryStream(data);
        }

        /// <summary>
        /// Asynchronously opens a stream to read the cached data of the file.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Stream"/> for reading the file's cached data.</returns>
        private async Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            byte[]? data = _data;
            DateTime lastWriteTime = new(Interlocked.Read(ref _lastWriteTime), DateTimeKind.Utc);
            DateTime currentLastWriteTime = await _fileSystem.GetFileLastWriteTimeUtcAsync(_path, cancellationToken).ConfigureAwait(false);

            if (data is null || currentLastWriteTime != lastWriteTime && await _fileSystem.FileExistsAsync(_path, cancellationToken).ConfigureAwait(false))
            {
                data = await _fileSystem.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
                Interlocked.Exchange(ref _data, data);
                Interlocked.Exchange(ref _lastWriteTime, currentLastWriteTime.Ticks);
            }

            return new MemoryStream(data);
        }

        /// <inheritdoc/>
        public override string ToString() => _path;
    }
}
