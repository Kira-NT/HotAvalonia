using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotAvalonia.Helpers;

namespace HotAvalonia.IO;

#pragma warning disable RS0030 // Do not use banned APIs

/// <summary>
/// Provides functionality for interacting with the local file system.
/// </summary>
internal sealed class LocalFileSystem : IFileSystem, IFileSystemPathOperations
{
    /// <summary>
    /// The singleton instance of the <see cref="LocalFileSystem"/> class.
    /// </summary>
    public static readonly LocalFileSystem Instance = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileSystem"/> class.
    /// </summary>
    private LocalFileSystem() { }

    /// <inheritdoc/>
    public string Name
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => "@local";
    }

    /// <inheritdoc/>
    public string CurrentDirectory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Environment.CurrentDirectory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Environment.CurrentDirectory = value;
    }

    /// <inheritdoc/>
    public string TempDirectory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Path.GetTempPath();
    }

    /// <inheritdoc/>
    public StringComparison PathComparison { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.CurrentCultureIgnoreCase
            : StringComparison.CurrentCulture;

    /// <inheritdoc/>
    public StringComparer PathComparer { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.CurrentCultureIgnoreCase
            : StringComparer.CurrentCulture;

    /// <inheritdoc/>
    public char DirectorySeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Path.DirectorySeparatorChar;
    }

    /// <inheritdoc/>
    public char AltDirectorySeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Path.AltDirectorySeparatorChar;
    }

    /// <inheritdoc/>
    public char VolumeSeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Path.VolumeSeparatorChar;
    }

    /// <inheritdoc/>
    public char PathSeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Path.PathSeparator;
    }

    /// <inheritdoc/>
    public char[] InvalidPathChars => Path.GetInvalidPathChars();

    /// <inheritdoc/>
    public char[] InvalidFileNameChars => Path.GetInvalidFileNameChars();

    /// <inheritdoc/>
    public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
        => File.Copy(sourceFileName, destFileName, overwrite);

    /// <inheritdoc/>
    public ValueTask CopyFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        CopyFile(sourceFileName, destFileName, overwrite);
        return default;
    }

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return new(this, path);
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => new(CreateDirectory(path));

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectorySymbolicLink(string path, string pathToTarget)
    {
#if NET6_0_OR_GREATER
        Directory.CreateSymbolicLink(path, pathToTarget);
        return new(this, path);
#else
        throw new NotImplementedException();
#endif
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectorySymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default)
        => new(CreateDirectorySymbolicLink(path, pathToTarget));

    /// <inheritdoc/>
    public FileSystemFileInfo CreateFileSymbolicLink(string path, string pathToTarget)
    {
#if NET6_0_OR_GREATER
        File.CreateSymbolicLink(path, pathToTarget);
        return new(this, path);
#else
        throw new NotImplementedException();
#endif
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo> CreateFileSymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default)
        => new(CreateFileSymbolicLink(path, pathToTarget));

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher()
        => new LocalFileSystemWatcher(this);

    /// <inheritdoc/>
    public ValueTask<IFileSystemWatcher> CreateFileSystemWatcherAsync(CancellationToken cancellationToken = default)
        => new(CreateFileSystemWatcher());

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateTempSubdirectory(string? prefix = null)
    {
#if NET7_0_OR_GREATER
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory(prefix);
        return new(this, tempDir.FullName);
#else
        string tempDirName = Path.Combine(Path.GetTempPath(), prefix + Path.ChangeExtension(Path.GetRandomFileName(), ".tmp"));
        return CreateDirectory(tempDirName);
#endif
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateTempSubdirectoryAsync(string? prefix = null, CancellationToken cancellationToken = default)
        => new(CreateTempSubdirectory(prefix));

    /// <inheritdoc/>
    public void DeleteDirectory(string path, bool recursive = false)
        => Directory.Delete(path, recursive);

    /// <inheritdoc/>
    public ValueTask DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        DeleteDirectory(path, recursive);
        return default;
    }

    /// <inheritdoc/>
    public void DeleteFile(string path)
        => File.Delete(path);

    /// <inheritdoc/>
    public ValueTask DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        DeleteFile(path);
        return default;
    }

    /// <inheritdoc/>
    public bool DirectoryExists([NotNullWhen(true)] string? path)
        => path is { Length: > 0 } && Directory.Exists(path);

    /// <inheritdoc/>
    public ValueTask<bool> DirectoryExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
        => new(DirectoryExists(path));

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        => Directory.EnumerateDirectories(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateDirectoriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => EnumerateDirectories(path, searchPattern, searchOption).ToAsyncEnumerable();

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.EnumerateFiles(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => EnumerateFiles(path, searchPattern, searchOption).ToAsyncEnumerable();

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        => Directory.EnumerateFileSystemEntries(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFileSystemEntriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => EnumerateFileSystemEntries(path, searchPattern, searchOption).ToAsyncEnumerable();

    /// <inheritdoc/>
    public bool FileExists([NotNullWhen(true)] string? path)
        => path is { Length: > 0 } && File.Exists(path);

    /// <inheritdoc/>
    public ValueTask<bool> FileExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
        => new(FileExists(path));

    /// <inheritdoc/>
    public DateTime GetDirectoryCreationTimeUtc(string path)
        => Directory.GetCreationTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetDirectoryCreationTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetDirectoryLastAccessTimeUtc(string path)
        => Directory.GetLastAccessTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetDirectoryLastAccessTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetDirectoryLastWriteTimeUtc(string path)
        => Directory.GetLastWriteTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetDirectoryLastWriteTimeUtc(path));

    /// <inheritdoc/>
    public FileAttributes GetFileAttributes(string path)
        => File.GetAttributes(path);

    /// <inheritdoc/>
    public ValueTask<FileAttributes> GetFileAttributesAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileAttributes(path));

    /// <inheritdoc/>
    public DateTime GetFileCreationTimeUtc(string path)
        => File.GetCreationTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileCreationTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetFileLastAccessTimeUtc(string path)
        => File.GetLastAccessTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileLastAccessTimeUtc(path));

    /// <inheritdoc/>
    public DateTime GetFileLastWriteTimeUtc(string path)
        => File.GetLastWriteTimeUtc(path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => new(GetFileLastWriteTimeUtc(path));

    /// <inheritdoc/>
    public string[] GetLogicalDrives()
        => Directory.GetLogicalDrives();

    /// <inheritdoc/>
    public Task<string[]> GetLogicalDrivesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetLogicalDrives());

    /// <inheritdoc/>
    public string GetTempFileName()
        => Path.GetTempFileName();

    /// <inheritdoc/>
    public ValueTask<string> GetTempFileNameAsync(CancellationToken cancellationToken = default)
        => new(GetTempFileName());

    /// <inheritdoc/>
    public void MoveDirectory(string sourceDirName, string destDirName)
        => Directory.Move(sourceDirName, destDirName);

    /// <inheritdoc/>
    public ValueTask MoveDirectoryAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken = default)
    {
        MoveDirectory(sourceDirName, destDirName);
        return default;
    }

    /// <inheritdoc/>
    public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
#if NETCOREAPP3_0_OR_GREATER
        File.Move(sourceFileName, destFileName, overwrite);
#else
        if (overwrite)
            File.Delete(destFileName);

        File.Move(sourceFileName, destFileName);
#endif
    }

    /// <inheritdoc/>
    public ValueTask MoveFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        MoveFile(sourceFileName, destFileName, overwrite);
        return default;
    }

    /// <inheritdoc/>
    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        => File.Open(path, mode, access, share);

    /// <inheritdoc/>
    public Task<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken = default)
        => Task.FromResult(Open(path, mode, access, share));

    /// <inheritdoc/>
    public void ReplaceFile(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false)
        => File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);

    /// <inheritdoc/>
    public ValueTask ReplaceFileAsync(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false, CancellationToken cancellationToken = default)
    {
        ReplaceFile(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        return default;
    }

    /// <inheritdoc/>
    public FileSystemDirectoryInfo? ResolveDirectoryLinkTarget(string linkPath, bool returnFinalTarget)
    {
#if NET6_0_OR_GREATER
        FileSystemInfo? linkTarget = Directory.ResolveLinkTarget(linkPath, returnFinalTarget);
        return linkTarget is null ? null : new(this, linkTarget.FullName);
#else
        throw new NotImplementedException();
#endif
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo?> ResolveDirectoryLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default)
        => new(ResolveDirectoryLinkTarget(linkPath, returnFinalTarget));

    /// <inheritdoc/>
    public FileSystemFileInfo? ResolveFileLinkTarget(string linkPath, bool returnFinalTarget)
    {
#if NET6_0_OR_GREATER
        FileSystemInfo? linkTarget = File.ResolveLinkTarget(linkPath, returnFinalTarget);
        return linkTarget is null ? null : new(this, linkTarget.FullName);
#else
        throw new NotImplementedException();
#endif
    }

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo?> ResolveFileLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default)
        => new(ResolveFileLinkTarget(linkPath, returnFinalTarget));

    /// <inheritdoc/>
    public void SetDirectoryCreationTimeUtc(string path, DateTime creationTimeUtc)
        => Directory.SetCreationTimeUtc(path, creationTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetDirectoryCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default)
    {
        SetDirectoryCreationTimeUtc(path, creationTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetDirectoryLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        => Directory.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default)
    {
        SetDirectoryLastAccessTimeUtc(path, lastAccessTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetDirectoryLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        => Directory.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default)
    {
        SetDirectoryLastWriteTimeUtc(path, lastWriteTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileAttributes(string path, FileAttributes fileAttributes)
        => File.SetAttributes(path, fileAttributes);

    /// <inheritdoc/>
    public ValueTask SetFileAttributesAsync(string path, FileAttributes fileAttributes, CancellationToken cancellationToken = default)
    {
        SetFileAttributes(path, fileAttributes);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileCreationTimeUtc(string path, DateTime creationTimeUtc)
        => File.SetCreationTimeUtc(path, creationTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetFileCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default)
    {
        SetFileCreationTimeUtc(path, creationTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        => File.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetFileLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default)
    {
        SetFileLastAccessTimeUtc(path, lastAccessTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    public void SetFileLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        => File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

    /// <inheritdoc/>
    public ValueTask SetFileLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default)
    {
        SetFileLastWriteTimeUtc(path, lastWriteTimeUtc);
        return default;
    }

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? ChangeExtension(string? path, string? extension)
        => Path.ChangeExtension(path, extension);

    /// <inheritdoc/>
    public string Combine(params scoped ReadOnlySpan<string> paths)
#if NET9_0_OR_GREATER
        => Path.Combine(paths);
#else
        => paths.Length switch
        {
            0 => string.Empty,
            1 => paths[0],
            2 => Path.Combine(paths[0], paths[1]),
            3 => Path.Combine(paths[0], paths[1], paths[2]),
            4 => Path.Combine(paths[0], paths[1], paths[2], paths[3]),
            _ => Path.Combine(paths.ToArray()),
        };
#endif

    /// <inheritdoc/>
    public string Combine(params string[] paths)
        => Path.Combine(paths);

    /// <inheritdoc/>
    public string Combine(string path1, string path2)
        => Path.Combine(path1, path2);

    /// <inheritdoc/>
    public string Combine(string path1, string path2, string path3)
        => Path.Combine(path1, path2, path3);

    /// <inheritdoc/>
    public string Combine(string path1, string path2, string path3, string path4)
        => Path.Combine(path1, path2, path3, path4);

    /// <inheritdoc/>
    public bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
#if NETCOREAPP3_0_OR_GREATER
        => Path.EndsInDirectorySeparator(path);
#else
        => ((IFileSystemContext)this).EndsInDirectorySeparator(path);
#endif

    /// <inheritdoc/>
    public bool EndsInDirectorySeparator([NotNullWhen(true)] string? path)
#if NETCOREAPP3_0_OR_GREATER
        => Path.EndsInDirectorySeparator(path);
#else
        => ((IFileSystemContext)this).EndsInDirectorySeparator(path);
#endif

    /// <inheritdoc/>
    public string? GetDirectoryName(string? path)
        => Path.GetDirectoryName(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.GetDirectoryName(path);
#else
        => ((IFileSystemContext)this).GetDirectoryName(path);
#endif

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.GetExtension(path);
#else
        => ((IFileSystemContext)this).GetExtension(path);
#endif

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetExtension(string? path)
        => Path.GetExtension(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.GetFileName(path);
#else
        => ((IFileSystemContext)this).GetFileName(path);
#endif

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetFileName(string? path)
        => Path.GetFileName(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.GetFileNameWithoutExtension(path);
#else
        => ((IFileSystemContext)this).GetFileNameWithoutExtension(path);
#endif

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetFileNameWithoutExtension(string? path)
        => Path.GetFileNameWithoutExtension(path);

    /// <inheritdoc/>
    public string GetFullPath(string path)
        => Path.GetFullPath(path);

    /// <inheritdoc/>
    public string GetFullPath(string path, string basePath)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.GetFullPath(path, basePath);
#else
        => ((IFileSystemContext)this).GetFullPath(path, basePath);
#endif

    /// <inheritdoc/>
    public string? GetPathRoot(string? path)
        => Path.GetPathRoot(path);

    /// <inheritdoc/>
    public ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.GetPathRoot(path);
#else
        => Path.GetPathRoot(path.ToString());
#endif

    /// <inheritdoc/>
    public string GetRandomFileName()
        => Path.GetRandomFileName();

    /// <inheritdoc/>
    public string GetRelativePath(string relativeTo, string path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        => Path.GetRelativePath(relativeTo, path);
#else
        => ((IFileSystemContext)this).GetRelativePath(Path.GetFullPath(relativeTo), Path.GetFullPath(path));
#endif

    /// <inheritdoc/>
    public bool HasExtension(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.HasExtension(path);
#else
        => ((IFileSystemContext)this).HasExtension(path);
#endif

    /// <inheritdoc/>
    public bool HasExtension([NotNullWhen(true)] string? path)
        => Path.HasExtension(path);

    /// <inheritdoc/>
    public bool IsPathFullyQualified(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.IsPathFullyQualified(path);
#else
        => IsPathFullyQualified(path.ToString());
#endif

    /// <inheritdoc/>
    public bool IsPathFullyQualified([NotNullWhen(true)] string? path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => path is { Length: > 0 } && Path.IsPathFullyQualified(path);
#else
        => path is { Length: > 0 } && Path.IsPathRooted(path) && ((IFileSystemContext)this).IsPathFullyQualified(path);
#endif

    /// <inheritdoc/>
    public bool IsPathRooted(ReadOnlySpan<char> path)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.IsPathRooted(path);
#else
        => Path.IsPathRooted(path.ToString());
#endif

    /// <inheritdoc/>
    public bool IsPathRooted([NotNullWhen(true)] string? path)
        => Path.IsPathRooted(path);

    /// <inheritdoc/>
    public string Join(string? path1, string? path2)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.Join(path1, path2);
#else
        => ((IFileSystemContext)this).Join(path1, path2);
#endif

    /// <inheritdoc/>
    public string Join(string? path1, string? path2, string? path3)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.Join(path1, path2, path3);
#else
        => ((IFileSystemContext)this).Join(path1, path2, path3);
#endif

    /// <inheritdoc/>
    public string Join(string? path1, string? path2, string? path3, string? path4)
#if NETCOREAPP3_0_OR_GREATER
        => Path.Join(path1, path2, path3, path4);
#else
        => ((IFileSystemContext)this).Join(path1, path2, path3, path4);
#endif

    /// <inheritdoc/>
    public string Join(params string?[] paths)
#if NETCOREAPP3_0_OR_GREATER
        => Path.Join(paths);
#else
        => ((IFileSystemContext)this).Join(paths);
#endif

    /// <inheritdoc/>
    public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.Join(path1, path2);
#else
        => ((IFileSystemContext)this).Join(path1, path2);
#endif

    /// <inheritdoc/>
    public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.Join(path1, path2, path3);
#else
        => ((IFileSystemContext)this).Join(path1, path2, path3);
#endif

    /// <inheritdoc/>
    public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
#if NETCOREAPP3_0_OR_GREATER
        => Path.Join(path1, path2, path3, path4);
#else
        => ((IFileSystemContext)this).Join(path1, path2, path3, path4);
#endif

    /// <inheritdoc/>
    public string Join(params scoped ReadOnlySpan<string?> paths)
#if NET9_0_OR_GREATER
        => Path.Join(paths);
#else
        => ((IFileSystemContext)this).Join(paths);
#endif

    /// <inheritdoc/>
    public ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path)
#if NETCOREAPP3_0_OR_GREATER
        => Path.TrimEndingDirectorySeparator(path);
#else
        => ((IFileSystemContext)this).TrimEndingDirectorySeparator(path);
#endif

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? TrimEndingDirectorySeparator(string? path)
#if NETCOREAPP3_0_OR_GREATER
        => path is null ? null : Path.TrimEndingDirectorySeparator(path);
#else
        => ((IFileSystemContext)this).TrimEndingDirectorySeparator(path);
#endif

    /// <inheritdoc/>
    public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.TryJoin(path1, path2, destination, out charsWritten);
#else
        => ((IFileSystemContext)this).TryJoin(path1, path2, destination, out charsWritten);
#endif

    /// <inheritdoc/>
    public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        => Path.TryJoin(path1, path2, path3, destination, out charsWritten);
#else
        => ((IFileSystemContext)this).TryJoin(path1, path2, path3, destination, out charsWritten);
#endif

    /// <inheritdoc/>
    public void Dispose() { }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;

    /// <inheritdoc/>
    public override string ToString() => this.ToFormattedString();
}

#pragma warning restore RS0030 // Do not use banned APIs
