namespace HotAvalonia.IO;

/// <summary>
/// Provides contextual information about a file system.
/// </summary>
public interface IFileSystemContext
{
    /// <summary>
    /// Gets the <see cref="StringComparison"/> mode used for path comparisons.
    /// </summary>
    StringComparison PathComparison { get; }

    /// <summary>
    /// Gets the <see cref="StringComparer"/> used to compare file or directory paths.
    /// </summary>
    StringComparer PathComparer { get; }

    /// <summary>
    /// Gets the fully qualified path of the current working directory.
    /// </summary>
    string CurrentDirectory { get; }

    /// <summary>
    /// Gets a platform-specific character used to separate directory levels
    /// in a path string that reflects a hierarchical file system organization.
    /// </summary>
    char DirectorySeparator { get; }

    /// <summary>
    /// Gets a platform-specific alternate character used to separate directory levels
    /// in a path string that reflects a hierarchical file system organization.
    /// </summary>
    char AltDirectorySeparator { get; }

    /// <summary>
    /// Gets a platform-specific volume separator character.
    /// </summary>
    char VolumeSeparator { get; }

    /// <summary>
    /// Gets a platform-specific separator character used to separate path strings in environment variables.
    /// </summary>
    char PathSeparator { get; }

    /// <summary>
    /// Gets an array containing the characters that are not allowed in path names.
    /// </summary>
    char[] InvalidPathChars { get; }

    /// <summary>
    /// Gets an array containing the characters that are not allowed in file names.
    /// </summary>
    char[] InvalidFileNameChars { get; }
}
