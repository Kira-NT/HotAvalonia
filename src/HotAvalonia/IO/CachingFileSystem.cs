using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace HotAvalonia.IO;

/// <summary>
/// Provides a caching layer for file system operations.
/// </summary>
internal sealed class CachingFileSystem : IFileSystem
{
    /// <summary>
    /// The original file system wrapped by this instance.
    /// </summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// A cache to store file entries keyed by their file names.
    /// </summary>
    private readonly ConcurrentDictionary<string, Entry> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingFileSystem"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system to wrap.</param>
    /// <param name="fileNameComparer">
    /// An optional equality comparer used for file name comparisons.
    /// Defaults to the system's default comparer if <c>null</c>.
    /// </param>
    public CachingFileSystem(IFileSystem fileSystem, IEqualityComparer<string>? fileNameComparer = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _cache = new(fileNameComparer ?? fileSystem.PathComparer);
    }

    /// <inheritdoc/>
    public StringComparer PathComparer => _fileSystem.PathComparer;

    /// <inheritdoc/>
    public StringComparison PathComparison => _fileSystem.PathComparison;

    /// <inheritdoc/>
    public char DirectorySeparatorChar => _fileSystem.DirectorySeparatorChar;

    /// <inheritdoc/>
    public char AltDirectorySeparatorChar => _fileSystem.AltDirectorySeparatorChar;

    /// <inheritdoc/>
    public char VolumeSeparatorChar => _fileSystem.VolumeSeparatorChar;

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher()
        => _fileSystem.CreateFileSystemWatcher();

    /// <inheritdoc/>
    public bool DirectoryExists([NotNullWhen(true)] string? path)
        => _fileSystem.DirectoryExists(path);

    /// <inheritdoc/>
    public ValueTask<bool> DirectoryExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
        => _fileSystem.DirectoryExistsAsync(path, cancellationToken);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        => _fileSystem.EnumerateFiles(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => _fileSystem.EnumerateFilesAsync(path, searchPattern, searchOption, cancellationToken);

    /// <inheritdoc/>
    public bool FileExists([NotNullWhen(true)] string? path)
    {
        if (path is null)
            return false;

        return _cache.ContainsKey(path) || _fileSystem.FileExists(path);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> FileExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
    {
        if (path is null)
            return false;

        if (_cache.ContainsKey(path))
            return true;

        return await _fileSystem.FileExistsAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public string GetFullPath(string path)
        => _fileSystem.GetFullPath(path);

    /// <inheritdoc/>
    public string GetDirectoryName(string path)
        => _fileSystem.GetDirectoryName(path);

    /// <inheritdoc/>
    public string GetFileName(string path)
        => _fileSystem.GetFileName(path);

    /// <inheritdoc/>
    public string Combine(string path1, string path2)
        => _fileSystem.Combine(path1, path2);

    /// <inheritdoc/>
    public string ChangeExtension(string path, string? extension)
        => _fileSystem.ChangeExtension(path, extension);

    /// <inheritdoc/>
    public DateTime GetLastWriteTimeUtc(string path)
        => _fileSystem.GetLastWriteTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => _fileSystem.GetLastWriteTimeUtcAsync(path, cancellationToken);

    /// <inheritdoc/>
    public Stream OpenRead(string path)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));

        if (!_fileSystem.FileExists(path))
            throw new FileNotFoundException(path);

        return _cache.GetOrAdd(path, x => new(x, _fileSystem)).OpenRead();
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));

        if (!await _fileSystem.FileExistsAsync(path, cancellationToken).ConfigureAwait(false))
            throw new FileNotFoundException(path);

        return await _cache.GetOrAdd(path, x => new(x, _fileSystem)).OpenReadAsync(cancellationToken).ConfigureAwait(false);
    }

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
        /// The name of the file represented by this entry.
        /// </summary>
        private readonly string _fileName;

        /// <summary>
        /// The last known write time of the file, used for cache validation.
        /// </summary>
        private long _lastWriteTime;

        /// <summary>
        /// The cached data of the file, if available.
        /// </summary>
        private byte[]? _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class.
        /// </summary>
        /// <param name="fileName">The name of the file represented by this entry.</param>
        /// <param name="fileSystem">The file system where <paramref name="fileName"/> can be found.</param>
        public Entry(string fileName, IFileSystem fileSystem)
        {
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _lastWriteTime = DateTime.MinValue.Ticks;
        }

        /// <summary>
        /// Gets the name of the file represented by this entry.
        /// </summary>
        public string FileName => _fileName;

        /// <summary>
        /// Opens a stream to read the cached data of the file.
        /// </summary>
        /// <returns>A <see cref="Stream"/> for reading the file's cached data.</returns>
        public Stream OpenRead()
        {
            byte[]? data = _data;
            DateTime lastWriteTime = new(Interlocked.Read(ref _lastWriteTime));
            DateTime currentLastWriteTime = _fileSystem.GetLastWriteTimeUtc(_fileName);

            if (data is null || currentLastWriteTime != lastWriteTime && _fileSystem.FileExists(_fileName))
            {
                data = _fileSystem.ReadAllBytes(_fileName);
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
        public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            byte[]? data = _data;
            DateTime lastWriteTime = new(Interlocked.Read(ref _lastWriteTime));
            DateTime currentLastWriteTime = await _fileSystem.GetLastWriteTimeUtcAsync(_fileName, cancellationToken).ConfigureAwait(false);

            if (data is null || currentLastWriteTime != lastWriteTime && await _fileSystem.FileExistsAsync(_fileName, cancellationToken).ConfigureAwait(false))
            {
                data = await _fileSystem.ReadAllBytesAsync(_fileName, cancellationToken).ConfigureAwait(false);
                Interlocked.Exchange(ref _data, data);
                Interlocked.Exchange(ref _lastWriteTime, currentLastWriteTime.Ticks);
            }

            return new MemoryStream(data);
        }
    }
}
