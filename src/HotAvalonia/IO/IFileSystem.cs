using System.Diagnostics.CodeAnalysis;

namespace HotAvalonia.IO;

/// <summary>
/// Represents an abstraction for interacting with a file system.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gets the <see cref="StringComparer"/> used to compare file or directory paths.
    /// </summary>
    StringComparer PathComparer { get; }

    /// <summary>
    /// Gets the <see cref="StringComparison"/> mode used for path comparisons.
    /// </summary>
    StringComparison PathComparison { get; }

    /// <summary>
    /// Gets a platform-specific character used to separate directory levels
    /// in a path string that reflects a hierarchical file system organization.
    /// </summary>
    char DirectorySeparatorChar { get; }

    /// <summary>
    /// Gets a platform-specific alternate character used to separate directory levels
    /// in a path string that reflects a hierarchical file system organization.
    /// </summary>
    char AltDirectorySeparatorChar { get; }

    /// <summary>
    /// Gets a platform-specific volume separator character.
    /// </summary>
    char VolumeSeparatorChar { get; }

    /// <summary>
    /// Creates a new instance of <see cref="IFileSystemWatcher"/> to monitor changes in the file system.
    /// </summary>
    /// <returns>
    /// A new <see cref="IFileSystemWatcher"/> instance.
    /// </returns>
    IFileSystemWatcher CreateFileSystemWatcher();

    /// <summary>
    /// Returns the absolute path for the specified path string.
    /// </summary>
    /// <param name="path">The file or directory for which to obtain absolute path information.</param>
    /// <returns>
    /// The fully qualified location of <paramref name="path"/>, such as <c>"/foo.txt"</c>.
    /// </returns>
    string GetFullPath(string path);

    /// <summary>
    /// Returns the directory information for the specified path.
    /// </summary>
    /// <param name="path">The path of a file or directory.</param>
    /// <returns>
    /// The directory information for <paramref name="path"/>.
    /// </returns>
    string GetDirectoryName(string path);

    /// <summary>
    /// Returns the file name and extension of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to obtain the file name and extension.</param>
    /// <returns>
    /// The characters after the last directory separator character in <paramref name="path"/>.
    /// </returns>
    string GetFileName(string path);

    /// <summary>
    /// Changes the extension of a path string.
    /// </summary>
    /// <param name="path">The path information to modify.</param>
    /// <param name="extension">
    /// The new extension (with or without a leading period).
    /// Specify <c>null</c> to remove an existing extension from <paramref name="path"/>.
    /// </param>
    /// <returns>The modified path information.</returns>
    string ChangeExtension(string path, string? extension);

    /// <summary>
    /// Combines two strings into a path.
    /// </summary>
    /// <param name="path1">The first path to combine.</param>
    /// <param name="path2">The second path to combine.</param>
    /// <returns>The combined paths.</returns>
    string Combine(string path1, string path2);

    /// <summary>
    /// Gets the date and time, in Coordinated Universal Time (UTC),
    /// that the specified file or directory was last written to.
    /// </summary>
    /// <param name="path">The file or directory for which to obtain write date and time information.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file or directory was last written to.
    /// This value is expressed in UTC time.
    /// </returns>
    DateTime GetLastWriteTimeUtc(string path);

    /// <summary>
    /// Asynchronously gets the date and time, in Coordinated Universal Time (UTC),
    /// that the specified file or directory was last written to.
    /// </summary>
    /// <param name="path">The file or directory for which to obtain write date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file or directory was last written to.
    /// This value is expressed in UTC time.
    /// </returns>
    ValueTask<DateTime> GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an enumerable collection of full file names that match a search pattern
    /// in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>
    /// An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern and search option.
    /// </returns>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Returns an async enumerable collection of full file names that match a search pattern
    /// in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// An async enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern and search option.
    /// </returns>
    IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an existing file for reading.
    /// </summary>
    /// <param name="path">The file to be opened for reading.</param>
    /// <returns>A read-only <see cref="Stream"/> on the specified path.</returns>
    Stream OpenRead(string path);

    /// <summary>
    /// Asynchronously opens an existing file for reading.
    /// </summary>
    /// <param name="path">The file to be opened for reading.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A read-only <see cref="Stream"/> on the specified path.</returns>
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The file to check.</param>
    /// <returns>
    /// <c>true</c> if the caller has the required permissions and path contains the name of an existing file;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool FileExists([NotNullWhen(true)] string? path);

    /// <summary>
    /// Asynchronously determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The file to check.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// <c>true</c> if the caller has the required permissions and path contains the name of an existing file;
    /// otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> FileExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">The directory to check.</param>
    /// <returns>
    /// <c>true</c> if the caller has the required permissions and path contains the name of an existing directory;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool DirectoryExists([NotNullWhen(true)] string? path);

    /// <summary>
    /// Asynchronously determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">The directory to check.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// <c>true</c> if the caller has the required permissions and path contains the name of an existing directory;
    /// otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> DirectoryExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default);
}
