using System.Diagnostics.CodeAnalysis;

namespace HotAvalonia.IO;

#pragma warning disable RS0030 // Do not use banned APIs

/// <summary>
/// Represents an empty, read-only file system.
/// </summary>
internal sealed class EmptyFileSystem : IFileSystem
{
    /// <inheritdoc/>
    public StringComparer PathComparer => StringComparer.CurrentCulture;

    /// <inheritdoc/>
    public StringComparison PathComparison => StringComparison.CurrentCulture;

    /// <inheritdoc/>
    public char DirectorySeparatorChar => Path.DirectorySeparatorChar;

    /// <inheritdoc/>
    public char AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

    /// <inheritdoc/>
    public char VolumeSeparatorChar => Path.VolumeSeparatorChar;

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher() => new EmptyFileSystemWatcher(this);

    /// <inheritdoc/>
    ValueTask<IFileSystemWatcher> IFileSystem.CreateFileSystemWatcherAsync(CancellationToken cancellationToken) => new(CreateFileSystemWatcher());

    /// <inheritdoc/>
    public DateTime GetLastWriteTimeUtc(string path) => DateTime.MinValue;

    /// <inheritdoc/>
    ValueTask<DateTime> IFileSystem.GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken) => new(GetLastWriteTimeUtc(path));

    /// <inheritdoc/>
    public string GetFullPath(string path) => Path.GetFullPath(path);

    /// <inheritdoc/>
    public string? GetDirectoryName(string? path) => Path.GetDirectoryName(path);

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetFileName(string? path) => Path.GetFileName(path);

    /// <inheritdoc/>
    public string Combine(string path1, string path2) => Path.Combine(path1, path2);

    /// <inheritdoc/>
    public string ChangeExtension(string path, string? extension) => Path.ChangeExtension(path, extension);

    /// <inheritdoc/>
    public bool FileExists(string? path) => false;

    /// <inheritdoc/>
    ValueTask<bool> IFileSystem.FileExistsAsync(string? path, CancellationToken cancellationToken) => new(FileExists(path));

    /// <inheritdoc/>
    public bool DirectoryExists(string? path) => false;

    /// <inheritdoc/>
    ValueTask<bool> IFileSystem.DirectoryExistsAsync(string? path, CancellationToken cancellationToken) => new(DirectoryExists(path));

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => throw new DirectoryNotFoundException($"Could not find a part of the path '{path}'.");

    /// <inheritdoc/>
    IAsyncEnumerable<string> IFileSystem.EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken) => throw new DirectoryNotFoundException($"Could not find a part of the path '{path}'.");

    /// <inheritdoc/>
    public Stream OpenRead(string path) => throw new FileNotFoundException($"Could not find a part of the path '{path}'.", path);

    /// <inheritdoc/>
    Task<Stream> IFileSystem.OpenReadAsync(string path, CancellationToken cancellationToken) => Task.FromException<Stream>(new FileNotFoundException($"Could not find a part of the path '{path}'.", path));

    /// <inheritdoc/>
    void IDisposable.Dispose() { }

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync() => default;
}

#pragma warning restore RS0030 // Do not use banned APIs
