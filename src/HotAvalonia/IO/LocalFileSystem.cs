#pragma warning disable RS0030 // Do not use banned APIs

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for interacting with the local file system.
/// </summary>
internal sealed class LocalFileSystem : IFileSystem
{
    /// <inheritdoc/>
    public StringComparer PathComparer { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.CurrentCultureIgnoreCase
            : StringComparer.CurrentCulture;

    /// <inheritdoc cref="PathComparer"/>
    public StringComparison PathComparison { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.CurrentCultureIgnoreCase
            : StringComparison.CurrentCulture;

    /// <inheritdoc/>
    public char DirectorySeparatorChar => Path.DirectorySeparatorChar;

    /// <inheritdoc/>
    public char AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

    /// <inheritdoc/>
    public char VolumeSeparatorChar => Path.VolumeSeparatorChar;

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher() => new LocalFileSystemWatcher(this);

    /// <inheritdoc/>
    ValueTask<IFileSystemWatcher> IFileSystem.CreateFileSystemWatcherAsync(CancellationToken cancellationToken) => new(CreateFileSystemWatcher());

    /// <inheritdoc/>
    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

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
    public bool FileExists(string? path) => File.Exists(path);

    /// <inheritdoc/>
    ValueTask<bool> IFileSystem.FileExistsAsync(string? path, CancellationToken cancellationToken) => new(FileExists(path));

    /// <inheritdoc/>
    public bool DirectoryExists(string? path) => Directory.Exists(path);

    /// <inheritdoc/>
    ValueTask<bool> IFileSystem.DirectoryExistsAsync(string? path, CancellationToken cancellationToken) => new(DirectoryExists(path));

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => Directory.EnumerateFiles(path, searchPattern, searchOption);

    /// <inheritdoc/>
    async IAsyncEnumerable<string> IFileSystem.EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await default(ValueTask);
        foreach (string filePath in EnumerateFiles(path, searchPattern, searchOption))
            yield return filePath;
    }

    /// <inheritdoc/>
    public Stream OpenRead(string path) => File.OpenRead(path);

    /// <inheritdoc/>
    Task<Stream> IFileSystem.OpenReadAsync(string path, CancellationToken cancellationToken) => Task.FromResult(OpenRead(path));

    /// <inheritdoc/>
    void IDisposable.Dispose() { }

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync() => default;
}

/// <summary>
/// Listens to the local file system change notifications and raises
/// events when a directory, or file in a directory, changes.
/// </summary>
file sealed class LocalFileSystemWatcher : FileSystemWatcher, IFileSystemWatcher
{
    /// <inheritdoc/>
    public IFileSystem FileSystem { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this system.</param>
    public LocalFileSystemWatcher(LocalFileSystem fileSystem) => FileSystem = fileSystem;
}

#pragma warning restore RS0030 // Do not use banned APIs
