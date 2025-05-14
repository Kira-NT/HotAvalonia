using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotAvalonia.IO;

/// <summary>
/// Provides a set of operations for manipulating and querying path strings
/// in a manner consistent with platform-specific behavior.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IFileSystemPathOperations
{
    /// <summary>
    /// Changes the extension of a path string.
    /// </summary>
    /// <param name="path">The path information to modify.</param>
    /// <param name="extension">
    /// The new extension (with or without a leading period).
    /// Specify <c>null</c> to remove an existing extension from path.
    /// </param>
    /// <returns>The modified path information.</returns>
    [return: NotNullIfNotNull(nameof(path))]
    string? ChangeExtension(string? path, string? extension);

    /// <summary>
    /// Combines a span of strings into a path.
    /// </summary>
    /// <param name="paths">A span of parts of the path.</param>
    /// <returns>The combined paths.</returns>
    string Combine(params scoped ReadOnlySpan<string> paths);

    /// <summary>
    /// Combines an array of strings into a path.
    /// </summary>
    /// <param name="paths">An array of parts of the path.</param>
    /// <returns>The combined paths.</returns>
    string Combine(params string[] paths);

    /// <inheritdoc cref="Combine(string, string, string, string)"/>
    string Combine(string path1, string path2);

    /// <inheritdoc cref="Combine(string, string, string, string)"/>
    string Combine(string path1, string path2, string path3);

    /// <summary>
    /// Combines given strings into a path.
    /// </summary>
    /// <param name="path1">The first path to combine.</param>
    /// <param name="path2">The second path to combine.</param>
    /// <param name="path3">The third path to combine.</param>
    /// <param name="path4">The fourth path to combine.</param>
    /// <returns>The combined paths.</returns>
    string Combine(string path1, string path2, string path3, string path4);

    /// <summary>
    /// Returns a value that indicates whether the path ends in a directory separator.
    /// </summary>
    /// <param name="path">The path to analyze.</param>
    /// <returns><c>true</c> if the path ends in a directory separator; otherwise, <c>false</c>.</returns>
    bool EndsInDirectorySeparator(ReadOnlySpan<char> path);

    /// <inheritdoc cref="EndsInDirectorySeparator(ReadOnlySpan{char})"/>
    bool EndsInDirectorySeparator([NotNullWhen(true)] string? path);

    /// <summary>
    /// Returns the directory information for the specified path.
    /// </summary>
    /// <param name="path">The path of a file or directory.</param>
    /// <returns>
    /// Directory information for path, or <c>null</c> if path denotes a root directory or is <c>null</c>.
    /// Returns <see cref="string.Empty"/> if path does not contain directory information.
    /// </returns>
    string? GetDirectoryName(string? path);

    /// <summary>
    /// Returns the directory information for the specified path represented by a character span.
    /// </summary>
    /// <param name="path">The path to retrieve the directory information from.</param>
    /// <returns>
    /// Directory information for path, or an empty span if path is an empty span or a root.
    /// </returns>
    ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path);

    /// <summary>
    /// Returns the extension of a file path that is represented by a read-only character span.
    /// </summary>
    /// <param name="path">The file path from which to get the extension.</param>
    /// <returns>
    /// The extension of the specified path (including the period, "."),
    /// or <see cref="ReadOnlySpan{char}.Empty"/> if path does not have extension information.
    /// </returns>
    ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path);

    /// <summary>
    /// Returns the extension (including the period ".") of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to get the extension.</param>
    /// <returns>
    /// The extension of the specified path (including the period ".").
    /// If path is <c>null</c>, returns <c>null</c>; otherwise, if path does not have
    /// extension information, returns <see cref="ReadOnlySpan{char}.Empty"/>.
    /// </returns>
    [return: NotNullIfNotNull(nameof(path))]
    string? GetExtension(string? path);

    /// <summary>
    /// Returns the file name and extension of a file path that is represented by a read-only character span.
    /// </summary>
    /// <param name="path">A read-only span that contains the path from which to obtain the file name and extension.</param>
    /// <returns>The characters after the last directory separator character in path.</returns>
    ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path);

    /// <summary>
    /// Returns the file name and extension of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to obtain the file name and extension.</param>
    /// <returns>
    /// The characters after the last directory separator character in path.
    /// If path is <c>null</c>, this method returns <c>null</c>.
    /// </returns>
    [return: NotNullIfNotNull(nameof(path))]
    string? GetFileName(string? path);

    /// <summary>
    /// Returns the file name without the extension of a file path that is represented by a read-only character span.
    /// </summary>
    /// <param name="path">A read-only span that contains the path from which to obtain the file name without the extension.</param>
    /// <returns>
    /// The characters in the read-only span returned by <see cref="GetFileName(ReadOnlySpan{char})"/>,
    /// minus the last period (.) and all characters following it.
    /// </returns>
    ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path);

    /// <summary>
    /// Returns the file name of the specified path string without the extension.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>
    /// The string returned by <see cref="GetFileName(string?)"/>,
    /// minus the last period (.) and all characters following it.
    /// </returns>
    [return: NotNullIfNotNull(nameof(path))]
    string? GetFileNameWithoutExtension(string? path);

    /// <summary>
    /// Returns the absolute path for the specified path string.
    /// </summary>
    /// <param name="path">The file or directory for which to obtain absolute path information.</param>
    /// <returns>The fully qualified location of <paramref name="path"/>.</returns>
    string GetFullPath(string path);

    /// <summary>
    /// Returns an absolute path from a relative path and a fully qualified base path.
    /// </summary>
    /// <param name="path">A relative path to concatenate to <paramref name="basePath"/>.</param>
    /// <param name="basePath">The beginning of a fully qualified path.</param>
    /// <returns>The absolute path.</returns>
    string GetFullPath(string path, string basePath);

    /// <summary>
    /// Gets the root directory information from the path contained in the specified string.
    /// </summary>
    /// <param name="path">A string containing the path from which to obtain root directory information.</param>
    /// <returns>
    /// The root directory of <paramref name="path"/> if it is rooted.
    /// <see cref="string.Empty"/> if <paramref name="path"/> does not contain root directory information.
    /// <c>null</c> if <paramref name="path"/> is <c>null</c> or is effectively empty.
    /// </returns>
    string? GetPathRoot(string? path);

    /// <summary>
    /// Gets the root directory information from the path contained in the specified character span.
    /// </summary>
    /// <param name="path">A read-only span of characters containing the path from which to obtain root directory information.</param>
    /// <returns>A read-only span of characters containing the root directory of <paramref name="path"/>.</returns>
    ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path);

    /// <summary>
    /// Returns a random directory name or file name.
    /// </summary>
    /// <returns>A random directory name or file name.</returns>
    string GetRandomFileName();

    /// <summary>
    /// Returns a relative path from one path to another.
    /// </summary>
    /// <param name="relativeTo">
    /// The source path the result should be relative to.
    /// This path is always considered to be a directory.
    /// </param>
    /// <param name="path">The destination path.</param>
    /// <returns>The relative path, or <paramref name="path"/> if the paths don't share the same root.</returns>
    string GetRelativePath(string relativeTo, string path);

    /// <summary>
    /// Determines whether the provided path includes a file name extension.
    /// </summary>
    /// <param name="path">The path to search for an extension.</param>
    /// <returns>
    /// <c>true</c> if the characters that follow the last directory separator character or volume separator
    /// in the path include a period (".") followed by one or more characters; otherwise, <c>false</c>.
    /// </returns>
    bool HasExtension(ReadOnlySpan<char> path);

    /// <inheritdoc cref="HasExtension(ReadOnlySpan{char})"/>
    bool HasExtension([NotNullWhen(true)] string? path);

    /// <summary>
    /// Returns a value that indicates whether the provided file path is fixed to a specific drive.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <returns>
    /// <c>true</c> if the path is fixed to a specific drive;
    /// <c>false</c> if the path is relative to the current drive or working directory.
    /// </returns>
    bool IsPathFullyQualified(ReadOnlySpan<char> path);

    /// <inheritdoc cref="IsPathFullyQualified(ReadOnlySpan{char})"/>
    bool IsPathFullyQualified([NotNullWhen(true)] string? path);

    /// <summary>
    /// Returns a value indicating whether the specified path contains a root.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><c>true</c> if <paramref name="path"/> contains a root; otherwise, <c>false</c>.</returns>
    bool IsPathRooted(ReadOnlySpan<char> path);

    /// <inheritdoc cref="IsPathRooted(ReadOnlySpan{char})"/>
    bool IsPathRooted([NotNullWhen(true)] string? path);

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    string Join(string? path1, string? path2);

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    string Join(string? path1, string? path2, string? path3);

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    string Join(string? path1, string? path2, string? path3, string? path4);

    /// <inheritdoc cref="Join(ReadOnlySpan{string?})"/>
    string Join(params string?[] paths);

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2);

    /// <inheritdoc cref="Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3);

    /// <summary>
    /// Concatenates provided paths into a single path.
    /// </summary>
    /// <param name="path1">The first path to join.</param>
    /// <param name="path2">The second path to join.</param>
    /// <param name="path3">The third path to join.</param>
    /// <param name="path4">The fourth path to join.</param>
    /// <returns>The concatenated path.</returns>
    string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4);

    /// <summary>
    /// Concatenates provided paths into a single path.
    /// </summary>
    /// <param name="paths">The paths to join.</param>
    /// <returns>The concatenated path.</returns>
    string Join(params scoped ReadOnlySpan<string?> paths);

    /// <summary>
    /// Trims one trailing directory separator beyond the root of the specified path.
    /// </summary>
    /// <param name="path">The path to trim.</param>
    /// <returns>The <paramref name="path"/> without any trailing directory separators.</returns>
    ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path);

    /// <inheritdoc cref="TrimEndingDirectorySeparator(ReadOnlySpan{char})"/>
    [return: NotNullIfNotNull(nameof(path))]
    string? TrimEndingDirectorySeparator(string? path);

    /// <inheritdoc cref="TryJoin(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, Span{char}, out int)"/>
    bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten);

    /// <summary>
    /// Attempts to concatenate provided path components to a single preallocated character span, and
    /// returns a value that indicates whether the operation succeeded.
    /// </summary>
    /// <param name="path1">A character span that contains the first path to join.</param>
    /// <param name="path2">A character span that contains the second path to join.</param>
    /// <param name="path3">A character span that contains the third path to join.</param>
    /// <param name="destination">A character span to hold the concatenated path.</param>
    /// <param name="charsWritten">
    /// When the method returns, a value that indicates the number of characters
    /// written to the <paramref name="destination"/>.
    /// </param>
    /// <returns><c>true</c> if the concatenation operation is successful; otherwise, <c>false</c>.</returns>
    bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten);
}

/// <summary>
/// Provides extension methods for <see cref="IFileSystem"/> and <see cref="IFileSystemContext"/>
/// to perform common file system path operations.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class FileSystemPathOperations
{
    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.ChangeExtension(string?, string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? ChangeExtension(this IFileSystem fs, string? path, string? extension)
        => fs is IFileSystemPathOperations ops ? ops.ChangeExtension(path, extension) : ((IFileSystemContext)fs).ChangeExtension(path, extension);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(ReadOnlySpan{string})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Combine(this IFileSystem fs, params scoped ReadOnlySpan<string> paths)
        => fs is IFileSystemPathOperations ops ? ops.Combine(paths) : ((IFileSystemContext)fs).Combine(paths);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Combine(this IFileSystem fs, params string[] paths)
        => fs is IFileSystemPathOperations ops ? ops.Combine(paths) : ((IFileSystemContext)fs).Combine(paths);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string, string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Combine(this IFileSystem fs, string path1, string path2)
        => fs is IFileSystemPathOperations ops ? ops.Combine(path1, path2) : ((IFileSystemContext)fs).Combine(path1, path2);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string, string, string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Combine(this IFileSystem fs, string path1, string path2, string path3)
        => fs is IFileSystemPathOperations ops ? ops.Combine(path1, path2, path3) : ((IFileSystemContext)fs).Combine(path1, path2, path3);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string, string, string, string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Combine(this IFileSystem fs, string path1, string path2, string path3, string path4)
        => fs is IFileSystemPathOperations ops ? ops.Combine(path1, path2, path3, path4) : ((IFileSystemContext)fs).Combine(path1, path2, path3, path4);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.EndsInDirectorySeparator(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsInDirectorySeparator(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.EndsInDirectorySeparator(path) : ((IFileSystemContext)fs).EndsInDirectorySeparator(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.EndsInDirectorySeparator(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsInDirectorySeparator(this IFileSystem fs, [NotNullWhen(true)] string? path)
        => fs is IFileSystemPathOperations ops ? ops.EndsInDirectorySeparator(path) : ((IFileSystemContext)fs).EndsInDirectorySeparator(path);

    /// <summary>
    /// Ensures that the specified path ends with the directory separator character.
    /// If the path already ends with a directory separator, it is returned unchanged.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file or directory path to process.</param>
    /// <returns>The original path if it already ends with a directory separator; otherwise, the path with the separator appended.</returns>
    public static string EnsureTrailingSeparator(this IFileSystem fs, string path)
        => fs.EndsInDirectorySeparator(path.AsSpan()) ? path : (path + $"{fs.DirectorySeparator}");

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetDirectoryName(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetDirectoryName(this IFileSystem fs, string? path)
        => fs is IFileSystemPathOperations ops ? ops.GetDirectoryName(path) : ((IFileSystemContext)fs).GetDirectoryName(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetDirectoryName(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetDirectoryName(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.GetDirectoryName(path) : ((IFileSystemContext)fs).GetDirectoryName(path);

    /// <summary>
    /// Returns the volume information, root information, or both for the specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path of a file or directory.</param>
    /// <returns>A string that contains the volume information, root information, or both for the specified path.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDirectoryRoot(this IFileSystem fs, string path)
        => fs is IFileSystemPathOperations ops ? ops.GetPathRoot(ops.GetFullPath(path))! : ((IFileSystemContext)fs).GetPathRoot(((IFileSystemContext)fs).GetFullPath(path))!;

    /// <summary>
    /// Retrieves the parent directory of the specified path, including both absolute and relative paths.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path for which to retrieve the parent directory.</param>
    /// <returns>The parent directory, or <c>null</c> if path is the root directory.</returns>
    public static FileSystemDirectoryInfo? GetParentDirectory(this IFileSystem fs, string path)
        => (fs is IFileSystemPathOperations ops ? ops.GetDirectoryName(ops.GetFullPath(path)) : ((IFileSystemContext)fs).GetDirectoryName(((IFileSystemContext)fs).GetFullPath(path))) is string s ? new(fs, s) : null;

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetExtension(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetExtension(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.GetExtension(path) : ((IFileSystemContext)fs).GetExtension(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetExtension(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetExtension(this IFileSystem fs, string? path)
        => fs is IFileSystemPathOperations ops ? ops.GetExtension(path) : ((IFileSystemContext)fs).GetExtension(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileName(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetFileName(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.GetFileName(path) : ((IFileSystemContext)fs).GetFileName(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileName(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileName(this IFileSystem fs, string? path)
        => fs is IFileSystemPathOperations ops ? ops.GetFileName(path) : ((IFileSystemContext)fs).GetFileName(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileNameWithoutExtension(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetFileNameWithoutExtension(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.GetFileNameWithoutExtension(path) : ((IFileSystemContext)fs).GetFileNameWithoutExtension(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileNameWithoutExtension(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileNameWithoutExtension(this IFileSystem fs, string? path)
        => fs is IFileSystemPathOperations ops ? ops.GetFileNameWithoutExtension(path) : ((IFileSystemContext)fs).GetFileNameWithoutExtension(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFullPath(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFullPath(this IFileSystem fs, string path)
        => fs is IFileSystemPathOperations ops ? ops.GetFullPath(path) : ((IFileSystemContext)fs).GetFullPath(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFullPath(string, string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFullPath(this IFileSystem fs, string path, string basePath)
        => fs is IFileSystemPathOperations ops ? ops.GetFullPath(path, basePath) : ((IFileSystemContext)fs).GetFullPath(path, basePath);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetPathRoot(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetPathRoot(this IFileSystem fs, string? path)
        => fs is IFileSystemPathOperations ops ? ops.GetPathRoot(path) : ((IFileSystemContext)fs).GetPathRoot(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetPathRoot(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetPathRoot(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.GetPathRoot(path) : ((IFileSystemContext)fs).GetPathRoot(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetRandomFileName()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRandomFileName(this IFileSystem fs)
        => fs is IFileSystemPathOperations ops ? ops.GetRandomFileName() : ((IFileSystemContext)fs).GetRandomFileName();

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetRelativePath(string, string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRelativePath(this IFileSystem fs, string relativeTo, string path)
        => fs is IFileSystemPathOperations ops ? ops.GetRelativePath(relativeTo, path) : ((IFileSystemContext)fs).GetRelativePath(relativeTo, path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.HasExtension(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasExtension(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.HasExtension(path) : ((IFileSystemContext)fs).HasExtension(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.HasExtension(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasExtension(this IFileSystem fs, [NotNullWhen(true)] string? path)
        => fs is IFileSystemPathOperations ops ? ops.HasExtension(path) : ((IFileSystemContext)fs).HasExtension(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathFullyQualified(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPathFullyQualified(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.IsPathFullyQualified(path) : ((IFileSystemContext)fs).IsPathFullyQualified(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathFullyQualified(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPathFullyQualified(this IFileSystem fs, [NotNullWhen(true)] string? path)
        => fs is IFileSystemPathOperations ops ? ops.IsPathFullyQualified(path) : ((IFileSystemContext)fs).IsPathFullyQualified(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathRooted(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPathRooted(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.IsPathRooted(path) : ((IFileSystemContext)fs).IsPathRooted(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathRooted(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPathRooted(this IFileSystem fs, [NotNullWhen(true)] string? path)
        => fs is IFileSystemPathOperations ops ? ops.IsPathRooted(path) : ((IFileSystemContext)fs).IsPathRooted(path);

    /// <summary>
    /// Determines whether the specified path is a root path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to evaluate.</param>
    /// <returns><c>true</c> if the path is a root path; otherwise, <c>false</c>.</returns>
    public static bool IsRoot(this IFileSystem fs, [NotNullWhen(true)] string? path)
        => path is { Length: > 0 } && path.Length == fs.GetPathRoot(path.AsSpan()).Length;

    /// <inheritdoc cref="IsRoot(IFileSystem, string?)"/>
    public static bool IsRoot(this IFileSystem fs, ReadOnlySpan<char> path)
        => path.Length != 0 && path.Length == fs.GetPathRoot(path).Length;

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?, string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, string? path1, string? path2)
        => fs is IFileSystemPathOperations ops ? ops.Join(path1, path2) : ((IFileSystemContext)fs).Join(path1, path2);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?, string?, string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, string? path1, string? path2, string? path3)
        => fs is IFileSystemPathOperations ops ? ops.Join(path1, path2, path3) : ((IFileSystemContext)fs).Join(path1, path2, path3);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?, string?, string?, string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, string? path1, string? path2, string? path3, string? path4)
        => fs is IFileSystemPathOperations ops ? ops.Join(path1, path2, path3, path4) : ((IFileSystemContext)fs).Join(path1, path2, path3, path4);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, params string?[] paths)
        => fs is IFileSystemPathOperations ops ? ops.Join(paths) : ((IFileSystemContext)fs).Join(paths);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
        => fs is IFileSystemPathOperations ops ? ops.Join(path1, path2) : ((IFileSystemContext)fs).Join(path1, path2);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
        => fs is IFileSystemPathOperations ops ? ops.Join(path1, path2, path3) : ((IFileSystemContext)fs).Join(path1, path2, path3);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
        => fs is IFileSystemPathOperations ops ? ops.Join(path1, path2, path3, path4) : ((IFileSystemContext)fs).Join(path1, path2, path3, path4);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{string?})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Join(this IFileSystem fs, params scoped ReadOnlySpan<string?> paths)
        => fs is IFileSystemPathOperations ops ? ops.Join(paths) : ((IFileSystemContext)fs).Join(paths);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TrimEndingDirectorySeparator(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> TrimEndingDirectorySeparator(this IFileSystem fs, ReadOnlySpan<char> path)
        => fs is IFileSystemPathOperations ops ? ops.TrimEndingDirectorySeparator(path) : ((IFileSystemContext)fs).TrimEndingDirectorySeparator(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TrimEndingDirectorySeparator(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? TrimEndingDirectorySeparator(this IFileSystem fs, string? path)
        => fs is IFileSystemPathOperations ops ? ops.TrimEndingDirectorySeparator(path) : ((IFileSystemContext)fs).TrimEndingDirectorySeparator(path);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TryJoin(ReadOnlySpan{char}, ReadOnlySpan{char}, Span{char}, out int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryJoin(this IFileSystem fs, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten)
        => fs is IFileSystemPathOperations ops ? ops.TryJoin(path1, path2, destination, out charsWritten) : ((IFileSystemContext)fs).TryJoin(path1, path2, destination, out charsWritten);

    /// <param name="fs">The file system.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TryJoin(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, Span{char}, out int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryJoin(this IFileSystem fs, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten)
        => fs is IFileSystemPathOperations ops ? ops.TryJoin(path1, path2, path3, destination, out charsWritten) : ((IFileSystemContext)fs).TryJoin(path1, path2, path3, destination, out charsWritten);

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.ChangeExtension(string?, string?)"/>
    [OverloadResolutionPriority(-1)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? ChangeExtension(this IFileSystemContext context, string? path, string? extension)
    {
        if (path is not { Length: > 0 })
            return path;

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        ReadOnlySpan<char> subpath = path;
        int i = subpath.LastIndexOfAny('.', sep, altSep);
        if (i >= 0 && path[i] == '.')
            subpath = subpath.Slice(0, i);

        if (extension is null)
            return path.Substring(0, subpath.Length);

        int resultLength = subpath.Length + extension.Length + (extension.StartsWith(".") ? 0 : 1);
        Span<char> result = resultLength < 256 ? stackalloc char[resultLength] : new char[256];
        subpath.CopyTo(result);
        result[subpath.Length] = '.';
        extension.AsSpan().CopyTo(result.Slice(result.Length - extension.Length, extension.Length));
        return result.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(ReadOnlySpan{string})"/>
    [OverloadResolutionPriority(-1)]
    public static string Combine(this IFileSystemContext context, params scoped ReadOnlySpan<string> paths)
    {
        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        int maxLength = 0;
        int firstPathIndex = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            ReadOnlySpan<char> path = paths[i];
            if (path.Length == 0)
                continue;

            if (context.IsPathRooted(path))
            {
                firstPathIndex = i;
                maxLength = path.Length;
            }
            else
            {
                maxLength += path.Length;
            }

            char c = path[path.Length - 1];
            if (c != sep & c != altSep)
                maxLength++;
        }

        Span<char> result = maxLength < 256 ? stackalloc char[maxLength] : new char[maxLength];
        int currentLength = 0;
        for (int i = firstPathIndex; i < paths.Length; i++)
        {
            ReadOnlySpan<char> path = paths[i];
            if (path.Length == 0)
                continue;

            if (currentLength > 0 && result[currentLength - 1] != sep && result[currentLength - 1] != altSep)
                result[currentLength++] = sep;

            path.CopyTo(result.Slice(currentLength));
            currentLength += path.Length;
        }

        return result.Slice(0, currentLength).ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string[])"/>
    [OverloadResolutionPriority(-1)]
    public static string Combine(this IFileSystemContext context, params string[] paths)
        => context.Combine(paths.AsSpan());

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string, string)"/>
    [OverloadResolutionPriority(-1)]
    public static string Combine(this IFileSystemContext context, string path1, string path2)
    {
        if (path1 is not { Length: > 0 })
            return path2;

        if (path2 is not { Length: > 0 })
            return path1;

        if (context.IsPathRooted(path2.AsSpan()))
            return path2;

        return context.Join(path1.AsSpan(), path2.AsSpan());
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string, string, string)"/>
    [OverloadResolutionPriority(-1)]
    public static string Combine(this IFileSystemContext context, string path1, string path2, string path3)
    {
        if (path1 is not { Length: > 0 })
            return context.Combine(path2, path3);

        if (path2 is not { Length: > 0 })
            return context.Combine(path1, path3);

        if (path3 is not { Length: > 0 })
            return context.Combine(path1, path2);

        if (context.IsPathRooted(path3.AsSpan()))
            return path3;

        if (context.IsPathRooted(path2.AsSpan()))
            return context.Combine(path2, path3);

        return context.Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan());
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Combine(string, string, string, string)"/>
    [OverloadResolutionPriority(-1)]
    public static string Combine(this IFileSystemContext context, string path1, string path2, string path3, string path4)
    {
        if (path1 is not { Length: > 0 })
            return context.Combine(path2, path3, path4);

        if (path2 is not { Length: > 0 })
            return context.Combine(path1, path3, path4);

        if (path3 is not { Length: > 0 })
            return context.Combine(path1, path2, path4);

        if (path4 is not { Length: > 0 })
            return context.Combine(path1, path2, path3);

        if (context.IsPathRooted(path4.AsSpan()))
            return path4;

        if (context.IsPathRooted(path3.AsSpan()))
            return context.Combine(path3, path4);

        if (context.IsPathRooted(path2.AsSpan()))
            return context.Combine(path2, path3, path4);

        return context.Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan());
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.EndsInDirectorySeparator(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static bool EndsInDirectorySeparator(this IFileSystemContext context, ReadOnlySpan<char> path)
    {
        if (path.Length == 0)
            return false;

        char c = path[path.Length - 1];
        return c == context.DirectorySeparator || c == context.AltDirectorySeparator;
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.EndsInDirectorySeparator(string?)"/>
    [OverloadResolutionPriority(-1)]
    public static bool EndsInDirectorySeparator(this IFileSystemContext context, [NotNullWhen(true)] string? path)
    {
        if (path is not { Length: > 0 })
            return false;

        char c = path[path.Length - 1];
        return c == context.DirectorySeparator || c == context.AltDirectorySeparator;
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="EnsureTrailingSeparator(IFileSystem, string)"/>
    [OverloadResolutionPriority(-1)]
    public static string EnsureTrailingSeparator(this IFileSystemContext context, string path)
        => context.EndsInDirectorySeparator(path.AsSpan()) ? path : (path + $"{context.DirectorySeparator}");

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetDirectoryName(string?)"/>
    [OverloadResolutionPriority(-1)]
    public static string? GetDirectoryName(this IFileSystemContext context, string? path)
    {
        if (path is not { Length: > 0 })
            return null;

        ReadOnlySpan<char> directoryName = context.GetDirectoryName(path.AsSpan());
        if (directoryName.Length == 0)
            return context.GetPathRoot(path.AsSpan()).Length == 0 ? string.Empty : null;

        return directoryName.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetDirectoryName(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static ReadOnlySpan<char> GetDirectoryName(this IFileSystemContext context, ReadOnlySpan<char> path)
    {
        int rootLength = context.GetPathRoot(path).Length;
        if (path.Length <= rootLength)
            return default;

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        int i = Math.Max(path.LastIndexOfAny(sep, altSep), rootLength);
        while (i > rootLength && (path[i - 1] == sep || path[i - 1] == altSep))
            i--;

        return path.Slice(0, i);
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="GetDirectoryRoot(IFileSystem, string)"/>
    [OverloadResolutionPriority(-1)]
    public static string GetDirectoryRoot(this IFileSystemContext context, string path)
        => context.GetPathRoot(context.GetFullPath(path))!;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetExtension(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static ReadOnlySpan<char> GetExtension(this IFileSystemContext context, ReadOnlySpan<char> path)
    {
        int i = path.LastIndexOfAny('.', context.DirectorySeparator, context.AltDirectorySeparator);
        if ((uint)i >= path.Length - 1 || path[i] != '.')
            return default;

        return path.Slice(i);
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetExtension(string?)"/>
    [OverloadResolutionPriority(-1)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetExtension(this IFileSystemContext context, string? path)
    {
        if (path is null)
            return null;

        return context.GetExtension(path.AsSpan()).ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileName(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static ReadOnlySpan<char> GetFileName(this IFileSystemContext context, ReadOnlySpan<char> path)
    {
        int root = context.GetPathRoot(path).Length;
        int i = path.LastIndexOfAny(context.DirectorySeparator, context.AltDirectorySeparator);
        return path.Slice(i < root ? root : (i + 1));
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileName(string?)"/>
    [OverloadResolutionPriority(-1)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileName(this IFileSystemContext context, string? path)
    {
        if (path is null)
            return null;

        ReadOnlySpan<char> result = context.GetFileName(path.AsSpan());
        return path.Length == result.Length ? path : result.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileNameWithoutExtension(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static ReadOnlySpan<char> GetFileNameWithoutExtension(this IFileSystemContext context, ReadOnlySpan<char> path)
    {
        ReadOnlySpan<char> fileName = context.GetFileName(path);
        int i = fileName.LastIndexOf('.');
        return i < 0 ? fileName : fileName.Slice(0, i);
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFileNameWithoutExtension(string?)"/>
    [OverloadResolutionPriority(-1)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileNameWithoutExtension(this IFileSystemContext context, string? path)
    {
        if (path is null)
            return null;

        ReadOnlySpan<char> result = context.GetFileNameWithoutExtension(path.AsSpan());
        return path.Length == result.Length ? path : result.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFullPath(string)"/>
    [OverloadResolutionPriority(-1)]
    public static string GetFullPath(this IFileSystemContext context, string path)
    {
        _ = path ?? throw new ArgumentNullException(path);
        if (path.Length == 0)
            throw new ArgumentException("The path is empty.", nameof(path));

        if (context.GetPathRoot(path.AsSpan()).Length == 0)
            path = context.Combine(context.CurrentDirectory, path);

        string result = context.RemoveRelativeSegments(path);
        return result.Length == 0 ? context.GetPathRoot(path)! : result;
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetFullPath(string, string)"/>
    [OverloadResolutionPriority(-1)]
    public static string GetFullPath(this IFileSystemContext context, string path, string basePath)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));
        if (path.Length == 0)
            throw new ArgumentException("The path is empty.", nameof(path));

        _ = basePath ?? throw new ArgumentNullException(nameof(basePath));
        if (!context.IsPathFullyQualified(basePath.AsSpan()))
            throw new ArgumentException("Basepath argument is not fully qualified.", nameof(basePath));

        if (context.GetPathRoot(path.AsSpan()).Length == 0)
            path = context.Combine(basePath, path);

        string result = context.RemoveRelativeSegments(path);
        return result.Length == 0 ? context.GetPathRoot(path)! : result;
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetPathRoot(string?)"/>
    [OverloadResolutionPriority(-1)]
    public static string? GetPathRoot(this IFileSystemContext context, string? path)
    {
        if (path is not { Length: > 0 })
            return null;

        ReadOnlySpan<char> root = context.GetPathRoot(path.AsSpan());
        return root.Length == 0 ? string.Empty : root.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetPathRoot(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static ReadOnlySpan<char> GetPathRoot(this IFileSystemContext context, ReadOnlySpan<char> path)
    {
        if (path.Length == 0)
            return path;

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        if (path[0] == sep | path[0] == altSep)
            return path.Slice(0, 1);

        char vol = context.VolumeSeparator;
        if (sep != altSep && path.Length > 1 && path[1] == vol && char.IsLetter(path[0]))
            return path.Slice(0, path.Length > 2 && (path[2] == sep || path[2] == altSep) ? 3 : 2);

        return default;
    }

#pragma warning disable
    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetRandomFileName()"/>
    [OverloadResolutionPriority(-1)]
    public static string GetRandomFileName(this IFileSystemContext context) => Path.GetRandomFileName();
#pragma warning restore

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.GetRelativePath(string, string)"/>
    [OverloadResolutionPriority(-1)]
    public static string GetRelativePath(this IFileSystemContext context, string relativeTo, string path)
    {
        _ = relativeTo ?? throw new ArgumentNullException(nameof(relativeTo));
        _ = path ?? throw new ArgumentNullException(nameof(path));

        relativeTo = context.GetFullPath(relativeTo);
        path = context.GetFullPath(path);
        int commonPathLength = context.GetCommonPath([relativeTo, path]).Length;
        if (commonPathLength == 0 || commonPathLength > relativeTo.Length || commonPathLength > path.Length)
            return path;

        ReadOnlySpan<char> remainingRelativeTo = relativeTo.AsSpan(commonPathLength);
        if (context.EndsInDirectorySeparator(remainingRelativeTo))
            remainingRelativeTo = remainingRelativeTo.Slice(0, remainingRelativeTo.Length - 1);

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        ReadOnlySpan<char> remainingPath = path.AsSpan(commonPathLength);
        if (remainingPath.Length > 0 && (remainingPath[0] == sep || remainingPath[0] == altSep))
            remainingPath = remainingPath.Slice(1);

        if (remainingPath.Length == 0 && context.GetPathRoot(path.AsSpan()).Length != path.Length)
            return ".";

        int relativeDepth = 0;
        while (remainingRelativeTo.Length > 0)
        {
            relativeDepth++;
            int nextSeparator = remainingRelativeTo.IndexOfAny(sep, altSep);
            if (nextSeparator < 0)
                break;

            remainingRelativeTo = remainingRelativeTo.Slice(nextSeparator + 1);
        }

        if (relativeDepth == 0)
            return remainingPath.ToString();

        int relativePathLength = remainingPath.Length + 3 * relativeDepth;
        Span<char> relativePath = relativePathLength < 256 ? stackalloc char[relativePathLength] : new char[relativePathLength];
        remainingPath.CopyTo(relativePath.Slice(3 * relativeDepth));
        for (int i = 0, j = 0; i < relativeDepth; i++)
        {
            relativePath[j++] = '.';
            relativePath[j++] = '.';
            relativePath[j++] = sep;
        }

        return relativePath.Slice(0, relativePath.Length - (remainingPath.Length == 0 && relativePath.Length > 1 ? 1 : 0)).ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.HasExtension(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static bool HasExtension(this IFileSystemContext context, ReadOnlySpan<char> path)
        => context.GetExtension(path).Length != 0;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.HasExtension(string?)"/>
    [OverloadResolutionPriority(-1)]
    public static bool HasExtension(this IFileSystemContext context, [NotNullWhen(true)] string? path)
        => context.GetExtension(path.AsSpan()).Length != 0;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathFullyQualified(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static bool IsPathFullyQualified(this IFileSystemContext context, ReadOnlySpan<char> path)
    {
        int rootLength = context.GetPathRoot(path).Length;
        if (rootLength == 0)
            return false;

        int i = rootLength - 1;
        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        return (path[i] == sep || path[i] == altSep) && (sep == altSep || i != 0);
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathFullyQualified(string?)"/>
    [OverloadResolutionPriority(-1)]
    public static bool IsPathFullyQualified(this IFileSystemContext context, [NotNullWhen(true)] string? path)
        => context.IsPathFullyQualified(path.AsSpan());

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathRooted(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static bool IsPathRooted(this IFileSystemContext context, ReadOnlySpan<char> path)
        => context.GetPathRoot(path).Length != 0;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.IsPathRooted(string?)"/>
    [OverloadResolutionPriority(-1)]
    public static bool IsPathRooted(this IFileSystemContext context, [NotNullWhen(true)] string? path)
        => context.GetPathRoot(path.AsSpan()).Length != 0;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IsRoot(IFileSystem, string?)"/>
    [OverloadResolutionPriority(-1)]
    public static bool IsRoot(this IFileSystemContext context, [NotNullWhen(true)] string? path)
        => path is { Length: > 0 } && path.Length == context.GetPathRoot(path.AsSpan()).Length;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IsRoot(IFileSystem, ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static bool IsRoot(this IFileSystemContext context, ReadOnlySpan<char> path)
        => path.Length != 0 && path.Length == context.GetPathRoot(path).Length;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?, string?)"/>
    [OverloadResolutionPriority(-1)]
    public static string Join(this IFileSystemContext context, string? path1, string? path2)
    {
        if (path1 is not { Length: > 0 })
            return path2 ?? string.Empty;

        if (path2 is not { Length: > 0 })
            return path1;

        return context.Join(path1.AsSpan(), path2.AsSpan());
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?, string?, string?)"/>
    [OverloadResolutionPriority(-1)]
    public static string Join(this IFileSystemContext context, string? path1, string? path2, string? path3)
    {
        if (path1 is not { Length: > 0 })
            return context.Join(path2, path3);

        if (path2 is not { Length: > 0 })
            return context.Join(path1, path3);

        if (path3 is not { Length: > 0 })
            return context.Join(path1, path2);

        return context.Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan());
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?, string?, string?, string?)"/>
    [OverloadResolutionPriority(-1)]
    public static string Join(this IFileSystemContext context, string? path1, string? path2, string? path3, string? path4)
    {
        if (path1 is not { Length: > 0 })
            return context.Join(path2, path3, path4);

        if (path2 is not { Length: > 0 })
            return context.Join(path1, path3, path4);

        if (path3 is not { Length: > 0 })
            return context.Join(path1, path2, path4);

        if (path4 is not { Length: > 0 })
            return context.Join(path1, path2, path3);

        return context.Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan());
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(string?[])"/>
    [OverloadResolutionPriority(-2)]
    public static string Join(this IFileSystemContext context, params string?[] paths)
        => context.Join((ReadOnlySpan<string?>)paths);

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static string Join(this IFileSystemContext context, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
    {
        if (path1.Length == 0)
            return path2.ToString();

        if (path2.Length == 0)
            return path1.ToString();

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        char l = path1[path1.Length - 1];
        char r = path2[0];
        bool hasSeparator = l == sep | l == altSep | r == sep | r == altSep;

        int length = path1.Length + path2.Length + (hasSeparator ? 0 : 1);
        Span<char> result = length < 256 ? stackalloc char[length] : new char[256];
        path1.CopyTo(result);
        result[path1.Length] = sep;
        path2.CopyTo(result.Slice(length - path2.Length));
        return result.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static string Join(this IFileSystemContext context, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
    {
        if (path1.Length == 0)
            return context.Join(path2, path3);

        if (path2.Length == 0)
            return context.Join(path1, path3);

        if (path3.Length == 0)
            return context.Join(path1, path2);

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;

        char l1 = path1[path1.Length - 1];
        char r1 = path2[0];
        int addSep1 = l1 == sep | l1 == altSep | r1 == sep | r1 == altSep ? 0 : 1;

        char l2 = path2[path1.Length - 1];
        char r2 = path3[0];
        int addSep2 = l2 == sep | l2 == altSep | r2 == sep | r2 == altSep ? 0 : 1;

        int length = path1.Length + path2.Length + path3.Length + addSep1 + addSep2;
        Span<char> result = length < 256 ? stackalloc char[length] : new char[256];
        path1.CopyTo(result);
        result[path1.Length] = sep;
        path2.CopyTo(result.Slice(path1.Length + addSep1));
        result[path1.Length + addSep1 + path2.Length] = sep;
        path3.CopyTo(result.Slice(length - path3.Length));
        return result.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static string Join(this IFileSystemContext context, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
    {
        if (path1.Length == 0)
            return context.Join(path2, path3, path4);

        if (path2.Length == 0)
            return context.Join(path1, path3, path4);

        if (path3.Length == 0)
            return context.Join(path1, path2, path4);

        if (path4.Length == 0)
            return context.Join(path1, path2, path3);

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;

        char l1 = path1[path1.Length - 1];
        char r1 = path2[0];
        int addSep1 = l1 == sep | l1 == altSep | r1 == sep | r1 == altSep ? 0 : 1;

        char l2 = path2[path1.Length - 1];
        char r2 = path3[0];
        int addSep2 = l2 == sep | l2 == altSep | r2 == sep | r2 == altSep ? 0 : 1;

        char l3 = path3[path1.Length - 1];
        char r3 = path4[0];
        int addSep3 = l2 == sep | l2 == altSep | r2 == sep | r2 == altSep ? 0 : 1;

        int length = path1.Length + path2.Length + path3.Length + path4.Length + addSep1 + addSep2 + addSep3;
        Span<char> result = length < 256 ? stackalloc char[length] : new char[256];
        path1.CopyTo(result);
        result[path1.Length] = sep;
        path2.CopyTo(result.Slice(path1.Length + addSep1));
        result[path1.Length + addSep1 + path2.Length] = sep;
        path3.CopyTo(result.Slice(path1.Length + addSep1 + path2.Length + addSep2));
        result[path1.Length + addSep1 + path2.Length + addSep2 + path3.Length] = sep;
        path4.CopyTo(result.Slice(length - path4.Length));
        return result.ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.Join(ReadOnlySpan{string?})"/>
    [OverloadResolutionPriority(-1)]
    public static string Join(this IFileSystemContext context, params scoped ReadOnlySpan<string?> paths)
    {
        if (paths.Length == 0)
            return string.Empty;

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        int maxLength = paths.Length - 1;
        foreach (string? path in paths)
            maxLength += path?.Length ?? 0;

        int length = 0;
        Span<char> result = maxLength < 256 ? stackalloc char[maxLength] : new char[maxLength];
        for (int i = 0; i < paths.Length; i++)
        {
            string? path = paths[i];
            if (path is not { Length: > 0 })
                continue;

            if (length != 0)
            {
                char l = result[length - 1];
                char r = path[0];
                if (l != sep & l != altSep & r != sep & r != altSep)
                    result[length++] = sep;
            }

            path.AsSpan().CopyTo(result.Slice(length));
            length += path.Length;
        }
        return result.Slice(0, length).ToString();
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TrimEndingDirectorySeparator(ReadOnlySpan{char})"/>
    [OverloadResolutionPriority(-1)]
    public static ReadOnlySpan<char> TrimEndingDirectorySeparator(this IFileSystemContext context, ReadOnlySpan<char> path)
        => context.EndsInDirectorySeparator(path) && context.GetPathRoot(path).Length != path.Length ? path.Slice(0, path.Length - 1) : path;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TrimEndingDirectorySeparator(string?)"/>
    [OverloadResolutionPriority(-1)]
    [return: NotNullIfNotNull(nameof(path))]
    public static string? TrimEndingDirectorySeparator(this IFileSystemContext context, string? path)
        => context.EndsInDirectorySeparator(path) && context.GetPathRoot(path.AsSpan()).Length != path.Length ? path.Substring(0, path.Length - 1) : path;

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TryJoin(ReadOnlySpan{char}, ReadOnlySpan{char}, Span{char}, out int)"/>
    [OverloadResolutionPriority(-1)]
    public static bool TryJoin(this IFileSystemContext context, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten)
    {
        if (destination.Length < path1.Length + path2.Length)
        {
            charsWritten = 0;
            return false;
        }

        if (path1.Length == 0)
        {
            charsWritten = path2.Length;
            return path2.TryCopyTo(destination);
        }

        if (path2.Length == 0)
        {
            charsWritten = path1.Length;
            return path1.TryCopyTo(destination);
        }

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        char l = path1[path1.Length - 1];
        char r = path2[0];
        int addSep = l == sep | l == altSep | r == sep | r == altSep ? 0 : 1;
        charsWritten = path1.Length + path2.Length + addSep;
        if (destination.Length < charsWritten)
        {
            charsWritten = 0;
            return false;
        }

        path1.CopyTo(destination);
        destination[path1.Length] = sep;
        path2.CopyTo(destination.Slice(path1.Length + addSep));
        return true;
    }

    /// <param name="context">The file system context.</param>
    /// <inheritdoc cref="IFileSystemPathOperations.TryJoin(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char}, Span{char}, out int)"/>
    [OverloadResolutionPriority(-1)]
    public static bool TryJoin(this IFileSystemContext context, ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;
        if (path1.Length == 0 && path2.Length == 0 && path3.Length == 0)
            return true;

        if (path1.Length == 0)
            return context.TryJoin(path2, path3, destination, out charsWritten);

        if (path2.Length == 0)
            return context.TryJoin(path1, path3, destination, out charsWritten);

        if (path3.Length == 0)
            return context.TryJoin(path1, path2, destination, out charsWritten);

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;

        char l1 = path1[path1.Length - 1];
        char r1 = path2[0];
        int addSep1 = l1 == sep | l1 == altSep | r1 == sep | r1 == altSep ? 0 : 1;

        char l2 = path2[path1.Length - 1];
        char r2 = path3[0];
        int addSep2 = l2 == sep | l2 == altSep | r2 == sep | r2 == altSep ? 0 : 1;

        int length = path1.Length + path2.Length + path3.Length + addSep1 + addSep2;
        if (destination.Length < length)
            return false;

        context.TryJoin(path1, path2, destination, out charsWritten);
        destination[charsWritten] = sep;
        path3.CopyTo(destination.Slice(charsWritten + addSep2));
        charsWritten = length;
        return true;
    }

    /// <summary>
    /// Returns the longest common prefix path shared by two specified paths.
    /// </summary>
    /// <param name="context">The file system context.</param>
    /// <param name="path1">The first file path to compare.</param>
    /// <param name="path2">The second file path to compare.</param>
    /// <returns>The common path shared by two provided paths.</returns>
    public static string GetCommonPath(this IFileSystemContext context, string path1, string path2)
        => context.GetCommonPath([path1, path2]).ToString();

    /// <inheritdoc cref="GetCommonPath(IFileSystemContext, ReadOnlySpan{string})"/>
    public static string GetCommonPath(this IFileSystemContext context, params string[] paths)
        => context.GetCommonPath(paths.AsSpan()).ToString();

    /// <summary>
    /// Returns the longest common prefix path from the specified set of paths, using the file system's path comparison rules.
    /// </summary>
    /// <param name="context">The file system context.</param>
    /// <param name="paths">The paths to compare.</param>
    /// <returns>The common path shared by all provided paths.</returns>
    public static ReadOnlySpan<char> GetCommonPath(this IFileSystemContext context, params ReadOnlySpan<string> paths)
    {
        if (paths.Length == 0)
            return default;

        StringComparison comparison = context.PathComparison;
        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;

        ReadOnlySpan<char> firstPath = paths[0];
        ReadOnlySpan<char> commonPath = default;
        while (commonPath.Length < firstPath.Length)
        {
            int hasSeparator = 1;
            ReadOnlySpan<char> firstSegment = firstPath.Slice(commonPath.Length);
            int firstSeparator = firstSegment.IndexOfAny(sep, altSep);
            if (firstSeparator >= 0)
                firstSegment = firstSegment.Slice(0, firstSeparator);
            else
                hasSeparator = 0;

            for (int i = 1; i < paths.Length; i++)
            {
                ReadOnlySpan<char> secondPath = paths[i];
                ReadOnlySpan<char> secondSegment = secondPath.Slice(Math.Min(secondPath.Length, commonPath.Length));
                int secondSeparator = secondSegment.IndexOfAny(sep, altSep);
                if (secondSeparator >= 0)
                    secondSegment = secondSegment.Slice(0, secondSeparator);
                else
                    hasSeparator = 0;

                if (!firstSegment.Equals(secondSegment, comparison))
                    return commonPath;
            }

            commonPath = firstPath.Slice(0, commonPath.Length + firstSegment.Length + hasSeparator);
            if (hasSeparator == 0)
                break;
        }
        return commonPath;
    }

    /// <summary>
    /// Normalizes directory separators in the specified path using the platform's default directory separator character.
    /// </summary>
    /// <param name="context">The file system context.</param>
    /// <param name="path">The path to normalize.</param>
    /// <returns>
    /// A new path string where all directory-separator-like characters have been replaced with the platform-specific
    /// directory separator character, or <c>null</c> if <paramref name="path"/> is <c>null</c>.
    /// </returns>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? NormalizeDirectorySeparators(this IFileSystemContext context, string? path)
        => context.NormalizeDirectorySeparators(path, context.DirectorySeparator);

    /// <summary>
    /// Normalizes directory separators in the specified path using the provided directory separator character.
    /// </summary>
    /// <param name="context">The file system context.</param>
    /// <param name="path">The path to normalize.</param>
    /// <param name="directorySeparatorChar">The character to use as the normalized directory separator.</param>
    /// <returns>
    /// A new path string where all directory-separator-like characters have been replaced with
    /// <paramref name="directorySeparatorChar"/>, or <c>null</c> if <paramref name="path"/> is <c>null</c>.
    /// </returns>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? NormalizeDirectorySeparators(this IFileSystemContext context, string? path, char directorySeparatorChar)
    {
        if (path is not { Length: > 0 })
            return path;

        bool isNormalized = true;
        bool supportsUnc = context.DirectorySeparator != context.AltDirectorySeparator;
        bool acceptUnc = supportsUnc;
        for (int i = 0; i < path.Length; i++)
        {
            char c = path[i];
            if (context.IsLikeDirectorySeparator(c) && (c != directorySeparatorChar || !acceptUnc && i + 1 < path.Length && context.IsLikeDirectorySeparator(path[i + 1])))
            {
                isNormalized = false;
                break;
            }
            acceptUnc = false;
        }
        if (isNormalized)
            return path;

        int maxLength = path.Length;
        int length = 0;
        Span<char> result = maxLength < 256 ? stackalloc char[maxLength] : new char[maxLength];

        if (supportsUnc && context.IsLikeDirectorySeparator(path[0]))
            result[length++] = directorySeparatorChar;

        for (int i = length; i < path.Length; i++)
        {
            char c = path[i];
            if (context.IsLikeDirectorySeparator(c))
            {
                if (i + 1 < path.Length && context.IsLikeDirectorySeparator(path[i + 1]))
                    continue;

                c = directorySeparatorChar;
            }
            result[length++] = c;
        }

        return result.Slice(0, length).ToString();
    }

    /// <summary>
    /// Determines whether the specified character is considered a directory separator
    /// according to the file system context.
    /// </summary>
    /// <param name="context">The file system context.</param>
    /// <param name="c">The character to evaluate.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="c"/> matches either <see cref="IFileSystemContext.DirectorySeparator"/> or
    /// <see cref="IFileSystemContext.AltDirectorySeparator"/>; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDirectorySeparator(this IFileSystemContext context, char c)
        => c == context.DirectorySeparator || c == context.AltDirectorySeparator;

    /// <summary>
    /// Determines whether the specified character is considered like a directory separator,
    /// including common cross-platform separator characters ('/' and '\\').
    /// </summary>
    /// <param name="context">The file system context.</param>
    /// <param name="c">The character to evaluate.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="c"/> is '/' or '\\', or matches either the platform-specific <see cref="IFileSystemContext.DirectorySeparator"/> or
    /// <see cref="IFileSystemContext.AltDirectorySeparator"/>; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsLikeDirectorySeparator(this IFileSystemContext context, char c)
        => c == '/' | c == '\\' || c == context.DirectorySeparator || c == context.AltDirectorySeparator;

    /// <summary>
    /// Removes relative path segments from the specified path.
    /// </summary>
    /// <param name="context">The file system context.</param>
    /// <param name="path">The path to clean up.</param>
    /// <returns>The path with relative segments removed.</returns>
    private static string RemoveRelativeSegments(this IFileSystemContext context, string path)
    {
        StringBuilder builder = new(path.Length);
        bool flippedSeparator = false;

        char sep = context.DirectorySeparator;
        char altSep = context.AltDirectorySeparator;
        int rootLength = context.GetPathRoot(path.AsSpan()).Length;
        int skip = rootLength;
        if (path[skip - 1] == sep | path[skip - 1] == altSep)
            skip--;

        if (skip > 0)
            builder.Append(path, 0, skip);

        for (int i = skip; i < path.Length; i++)
        {
            char c = path[i];

            if ((c == sep || c == altSep) && i + 1 < path.Length)
            {
                if (path[i + 1] == sep || path[i + 1] == altSep)
                    continue;

                if ((i + 2 == path.Length || path[i + 2] == sep || path[i + 2] == altSep) && path[i + 1] == '.')
                {
                    i++;
                    continue;
                }

                if (i + 2 < path.Length && (i + 3 == path.Length || path[i + 3] == sep || path[i + 3] == altSep) && path[i + 1] == '.' && path[i + 2] == '.')
                {
                    int s;
                    for (s = builder.Length - 1; s >= skip; s--)
                    {
                        if (builder[s] == sep || builder[s] == altSep)
                        {
                            builder.Length = (i + 3 >= path.Length && s == skip) ? s + 1 : s;
                            break;
                        }
                    }

                    if (s < skip)
                        builder.Length = skip;

                    i += 2;
                    continue;
                }
            }

            if (c != sep && c == altSep)
            {
                c = sep;
                flippedSeparator = true;
            }

            builder.Append(c);
        }

        if (!flippedSeparator && builder.Length == path.Length)
            return path;

        if (skip != rootLength && builder.Length < rootLength)
            builder.Append(path[rootLength - 1]);

        return builder.ToString();
    }
}
