using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HotAvalonia.Helpers;
using HotAvalonia.Reflection.Emit;

namespace HotAvalonia.IO;

/// <summary>
/// Represents an abstraction for interacting with a file system.
/// </summary>
public interface IFileSystem : IFileSystemContext, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the name of this file system.
    /// </summary>
    /// <remarks>
    /// This property uniquely identifies the underlying file system and its state, so it can be safely
    /// used to determine effective equality of different <see cref="IFileSystem"/> instances.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets or sets the fully qualified path of the current working directory.
    /// </summary>
    new string CurrentDirectory { get; set; }

    /// <summary>
    /// Gets the path of the current user's temporary folder.
    /// </summary>
    string TempDirectory { get; }

    /// <summary>
    /// Creates a new instance of <see cref="IFileSystemWatcher"/> to monitor changes in the file system.
    /// </summary>
    /// <returns>
    /// A new <see cref="IFileSystemWatcher"/> instance.
    /// </returns>
    IFileSystemWatcher CreateFileSystemWatcher();

    /// <summary>
    /// Asynchronously creates a new instance of <see cref="IFileSystemWatcher"/> to monitor changes in the file system.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A new <see cref="IFileSystemWatcher"/> instance.
    /// </returns>
    ValueTask<IFileSystemWatcher> CreateFileSystemWatcherAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the names of the logical drives on this computer.
    /// </summary>
    /// <returns>An array of strings representing the logical drive names.</returns>
    string[] GetLogicalDrives();

    /// <summary>
    /// Asynchronously retrieves the names of the logical drives on this computer.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of strings representing the logical drive names.</returns>
    Task<string[]> GetLogicalDrivesAsync(CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Opens a <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <returns>
    /// A <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </returns>
    Stream Open(string path, FileMode mode, FileAccess access, FileShare share);

    /// <summary>
    /// Asynchronously opens a <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </returns>
    Task<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an existing file to a new file.
    /// </summary>
    /// <param name="sourceFileName">The file to copy.</param>
    /// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
    /// <param name="overwrite"><c>true</c> if the destination file should be replaced if it already exists; otherwise, <c>false</c>.</param>
    void CopyFile(string sourceFileName, string destFileName, bool overwrite = false);

    /// <summary>
    /// Asynchronously copies an existing file to a new file.
    /// </summary>
    /// <param name="sourceFileName">The file to copy.</param>
    /// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
    /// <param name="overwrite"><c>true</c> if the destination file should be replaced if it already exists; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask CopyFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a specified file to a new location, providing the options to specify a new file name and to replace the destination file if it already exists.
    /// </summary>
    /// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path.</param>
    /// <param name="destFileName">The new path and name for the file.</param>
    /// <param name="overwrite"><c>true</c> if the destination file should be replaced if it already exists; otherwise, <c>false</c>.</param>
    void MoveFile(string sourceFileName, string destFileName, bool overwrite = false);

    /// <summary>
    /// Asynchronously moves a specified file to a new location, providing the options to specify a new file name and to replace the destination file if it already exists.
    /// </summary>
    /// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path.</param>
    /// <param name="destFileName">The new path and name for the file.</param>
    /// <param name="overwrite"><c>true</c> if the destination file should be replaced if it already exists; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask MoveFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a directory and its contents to a new location.
    /// </summary>
    /// <param name="sourceDirName">The path of the directory to move.</param>
    /// <param name="destDirName">The path to the new location for <paramref name="sourceDirName"/>.</param>
    void MoveDirectory(string sourceDirName, string destDirName);

    /// <summary>
    /// Asynchronously moves a directory and its contents to a new location.
    /// </summary>
    /// <param name="sourceDirName">The path of the directory to move.</param>
    /// <param name="destDirName">The path to the new location for <paramref name="sourceDirName"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask MoveDirectoryAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the contents of a specified file with the contents of another file, deleting the original file and creating a backup of the replaced file.
    /// </summary>
    /// <param name="sourceFileName">The name of a file that replaces the file specified by <paramref name="destinationFileName"/>.</param>
    /// <param name="destinationFileName">The name of the file being replaced.</param>
    /// <param name="destinationBackupFileName">The name of the backup file.</param>
    /// <param name="ignoreMetadataErrors"><c>true</c> to ignore merge errors (such as attributes and access control lists (ACLs)) from the replaced file to the replacement file; otherwise, <c>false</c>.</param>
    void ReplaceFile(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false);

    /// <summary>
    /// Replaces the contents of a specified file with the contents of another file, deleting the original file and creating a backup of the replaced file.
    /// </summary>
    /// <param name="sourceFileName">The name of a file that replaces the file specified by <paramref name="destinationFileName"/>.</param>
    /// <param name="destinationFileName">The name of the file being replaced.</param>
    /// <param name="destinationBackupFileName">The name of the backup file.</param>
    /// <param name="ignoreMetadataErrors"><c>true</c> to ignore merge errors (such as attributes and access control lists (ACLs)) from the replaced file to the replacement file; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ReplaceFileAsync(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="path">The name of the file to be deleted.</param>
    void DeleteFile(string path);

    /// <summary>
    /// Asynchronously deletes the specified file.
    /// </summary>
    /// <param name="path">The name of the file to be deleted.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask DeleteFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory.
    /// </summary>
    /// <param name="path">The name of the directory to remove.</param>
    /// <param name="recursive"><c>true</c> to remove directories, subdirectories, and files in path; otherwise, <c>false</c>.</param>
    void DeleteDirectory(string path, bool recursive = false);

    /// <summary>
    /// Asynchronously deletes the specified directory and, if indicated, any subdirectories and files in the directory.
    /// </summary>
    /// <param name="path">The name of the directory to remove.</param>
    /// <param name="recursive"><c>true</c> to remove directories, subdirectories, and files in path; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a file symbolic link identified by <paramref name="path"/> that points to <paramref name="pathToTarget"/>.
    /// </summary>
    /// <param name="path">The path where the symbolic link should be created.</param>
    /// <param name="pathToTarget">The path of the target to which the symbolic link points.</param>
    /// <returns>A <see cref="FileSystemFileInfo"/> instance that wraps the newly created file symbolic link.</returns>
    FileSystemFileInfo CreateFileSymbolicLink(string path, string pathToTarget);

    /// <summary>
    /// Asynchronously creates a file symbolic link identified by <paramref name="path"/> that points to <paramref name="pathToTarget"/>.
    /// </summary>
    /// <param name="path">The path where the symbolic link should be created.</param>
    /// <param name="pathToTarget">The path of the target to which the symbolic link points.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="FileSystemFileInfo"/> instance that wraps the newly created file symbolic link.</returns>
    ValueTask<FileSystemFileInfo> CreateFileSymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory symbolic link identified by <paramref name="path"/> that points to <paramref name="pathToTarget"/>.
    /// </summary>
    /// <param name="path">The path where the symbolic link should be created.</param>
    /// <param name="pathToTarget">The target directory of the symbolic link.</param>
    /// <returns>A <see cref="FileSystemDirectoryInfo"/> instance that wraps the newly created directory symbolic link.</returns>
    FileSystemDirectoryInfo CreateDirectorySymbolicLink(string path, string pathToTarget);

    /// <summary>
    /// Asynchronously creates a directory symbolic link identified by <paramref name="path"/> that points to <paramref name="pathToTarget"/>.
    /// </summary>
    /// <param name="path">The path where the symbolic link should be created.</param>
    /// <param name="pathToTarget">The target directory of the symbolic link.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="FileSystemDirectoryInfo"/> instance that wraps the newly created directory symbolic link.</returns>
    ValueTask<FileSystemDirectoryInfo> CreateDirectorySymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the target of the specified file link.
    /// </summary>
    /// <param name="linkPath">The path of the file link.</param>
    /// <param name="returnFinalTarget"><c>true</c> to follow links to the final target; <c>false</c> to return the immediate next link.</param>
    /// <returns>A <see cref="FileSystemFileInfo"/> instance if <paramref name="linkPath"/> exists, independently of whether the target exists or not. <c>null</c> if <paramref name="linkPath"/> is not a link.</returns>
    FileSystemFileInfo? ResolveFileLinkTarget(string linkPath, bool returnFinalTarget);

    /// <summary>
    /// Asynchronously gets the target of the specified file link.
    /// </summary>
    /// <param name="linkPath">The path of the file link.</param>
    /// <param name="returnFinalTarget"><c>true</c> to follow links to the final target; <c>false</c> to return the immediate next link.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="FileSystemFileInfo"/> instance if <paramref name="linkPath"/> exists, independently of whether the target exists or not. <c>null</c> if <paramref name="linkPath"/> is not a link.</returns>
    ValueTask<FileSystemFileInfo?> ResolveFileLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the target of the specified directory link.
    /// </summary>
    /// <param name="linkPath">The path of the directory link.</param>
    /// <param name="returnFinalTarget"><c>true</c> to follow links to the final target; <c>false</c> to return the immediate next link.</param>
    /// <returns>A <see cref="FileSystemDirectoryInfo"/> instance if <paramref name="linkPath"/> exists, independently if the target exists or not. <c>null</c> if <paramref name="linkPath"/> is not a link.</returns>
    FileSystemDirectoryInfo? ResolveDirectoryLinkTarget(string linkPath, bool returnFinalTarget);

    /// <summary>
    /// Asynchronously gets the target of the specified directory link.
    /// </summary>
    /// <param name="linkPath">The path of the directory link.</param>
    /// <param name="returnFinalTarget"><c>true</c> to follow links to the final target; <c>false</c> to return the immediate next link.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="FileSystemDirectoryInfo"/> instance if <paramref name="linkPath"/> exists, independently if the target exists or not. <c>null</c> if <paramref name="linkPath"/> is not a link.</returns>
    ValueTask<FileSystemDirectoryInfo?> ResolveDirectoryLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates all directories and subdirectories in the specified path unless they already exist.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    /// <returns>An object that represents the directory at the specified path. This object is returned regardless of whether a directory at the specified path already exists.</returns>
    FileSystemDirectoryInfo CreateDirectory(string path);

    /// <summary>
    /// Asynchronously creates all directories and subdirectories in the specified path unless they already exist.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An object that represents the directory at the specified path. This object is returned regardless of whether a directory at the specified path already exists.</returns>
    ValueTask<FileSystemDirectoryInfo> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a uniquely named, zero-byte temporary file on disk and returns the full path of that file.
    /// </summary>
    /// <returns>The full path of the temporary file.</returns>
    string GetTempFileName();

    /// <summary>
    /// Asynchronously creates a uniquely named, zero-byte temporary file on disk and returns the full path of that file.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The full path of the temporary file.</returns>
    ValueTask<string> GetTempFileNameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a uniquely named, empty directory in the current user's temporary directory.
    /// </summary>
    /// <param name="prefix">An optional string to add to the beginning of the subdirectory name.</param>
    /// <returns>An object that represents the directory that was created.</returns>
    FileSystemDirectoryInfo CreateTempSubdirectory(string? prefix = null);

    /// <summary>
    /// Asynchronously creates a uniquely named, empty directory in the current user's temporary directory.
    /// </summary>
    /// <param name="prefix">An optional string to add to the beginning of the subdirectory name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An object that represents the directory that was created.</returns>
    ValueTask<FileSystemDirectoryInfo> CreateTempSubdirectoryAsync(string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an enumerable collection of directory full names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by path and that match the specified search pattern and search option.</returns>
    IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Returns an async enumerable collection of directory full names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of the full names (including paths) for the directories in the directory specified by path and that match the specified search pattern and search option.</returns>
    IAsyncEnumerable<string> EnumerateDirectoriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an enumerable collection of full file names that match a search pattern
    /// in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern and search option.</returns>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Returns an async enumerable collection of full file names that match a search pattern
    /// in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern and search option.</returns>
    IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an enumerable collection of file names and directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern and option.</returns>
    IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Returns an async enumerable collection of file names and directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern and option.</returns>
    IAsyncEnumerable<string> EnumerateFileSystemEntriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="FileAttributes"/> of the file on the path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The <see cref="FileAttributes"/> of the file on the path.</returns>
    FileAttributes GetFileAttributes(string path);

    /// <summary>
    /// Asynchronously gets the <see cref="FileAttributes"/> of the file on the path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="FileAttributes"/> of the file on the path.</returns>
    ValueTask<FileAttributes> GetFileAttributesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the specified <see cref="FileAttributes"/> of the file on the specified path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="fileAttributes">A bitwise combination of the enumeration values.</param>
    void SetFileAttributes(string path, FileAttributes fileAttributes);

    /// <summary>
    /// Asynchronously sets the specified <see cref="FileAttributes"/> of the file on the specified path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="fileAttributes">A bitwise combination of the enumeration values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetFileAttributesAsync(string path, FileAttributes fileAttributes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the creation date and time, in Coordinated Universal Time (UTC), of the specified file.
    /// </summary>
    /// <param name="path">The file for which to obtain creation date and time information.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the creation date and time for the specified file.
    /// This value is expressed in UTC time.
    /// </returns>
    DateTime GetFileCreationTimeUtc(string path);

    /// <summary>
    /// Asynchronously gets the creation date and time, in Coordinated Universal Time (UTC), of the specified file.
    /// </summary>
    /// <param name="path">The file for which to obtain creation date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="DateTime"/> structure set to the creation date and time for the specified file. This value is expressed in UTC time.</returns>
    ValueTask<DateTime> GetFileCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the creation date and time, in Coordinated Universal Time (UTC) format, of a directory.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <returns>A structure that is set to the creation date and time for the specified directory. This value is expressed in UTC time.</returns>
    DateTime GetDirectoryCreationTimeUtc(string path);

    /// <summary>
    /// Asynchronously gets the creation date and time, in Coordinated Universal Time (UTC) format, of a directory.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A structure that is set to the creation date and time for the specified directory. This value is expressed in UTC time.</returns>
    ValueTask<DateTime> GetDirectoryCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the date and time, in Coordinated Universal Time (UTC), that the file was created.
    /// </summary>
    /// <param name="path">The file for which to set the creation date and time information.</param>
    /// <param name="creationTimeUtc">The value to set for the creation date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
    void SetFileCreationTimeUtc(string path, DateTime creationTimeUtc);

    /// <summary>
    /// Asynchronously sets the date and time, in Coordinated Universal Time (UTC), that the file was created.
    /// </summary>
    /// <param name="path">The file for which to set the creation date and time information.</param>
    /// <param name="creationTimeUtc">The value to set for the creation date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetFileCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the creation date and time, in Coordinated Universal Time (UTC) format, for the specified directory.
    /// </summary>
    /// <param name="path">The directory for which to set the creation date and time information.</param>
    /// <param name="creationTimeUtc">The date and time the directory was created. This value is expressed in local time.</param>
    void SetDirectoryCreationTimeUtc(string path, DateTime creationTimeUtc);

    /// <summary>
    /// Asynchronously sets the creation date and time, in Coordinated Universal Time (UTC) format, for the specified directory.
    /// </summary>
    /// <param name="path">The directory for which to set the creation date and time information.</param>
    /// <param name="creationTimeUtc">The date and time the directory was created. This value is expressed in local time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetDirectoryCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the date and time, in Coordinated Universal Time (UTC), that the specified file was last accessed.
    /// </summary>
    /// <param name="path">The file for which to obtain access date and time information.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last accessed.
    /// This value is expressed in UTC time.
    /// </returns>
    DateTime GetFileLastAccessTimeUtc(string path);

    /// <summary>
    /// Asynchronously gets the date and time, in Coordinated Universal Time (UTC), that the specified file was last accessed.
    /// </summary>
    /// <param name="path">The file for which to obtain access date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last accessed.
    /// This value is expressed in UTC time.
    /// </returns>
    ValueTask<DateTime> GetFileLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the date and time, in Coordinated Universal Time (UTC) format, that the specified directory was last accessed.
    /// </summary>
    /// <param name="path">The directory for which to obtain access date and time information.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last accessed. This value is expressed in UTC time.</returns>
    DateTime GetDirectoryLastAccessTimeUtc(string path);

    /// <summary>
    /// Asynchronously returns the date and time, in Coordinated Universal Time (UTC) format, that the specified directory was last accessed.
    /// </summary>
    /// <param name="path">The directory for which to obtain access date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last accessed. This value is expressed in UTC time.</returns>
    ValueTask<DateTime> GetDirectoryLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the date and time, in Coordinated Universal Time (UTC), that the specified file was last accessed.
    /// </summary>
    /// <param name="path">The file for which to set the access date and time information.</param>
    /// <param name="lastAccessTimeUtc">A <see cref="DateTime"/> containing the value to set for the last access date and time of path. This value is expressed in UTC time.</param>
    void SetFileLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);

    /// <summary>
    /// Asynchronously sets the date and time, in Coordinated Universal Time (UTC), that the specified file was last accessed.
    /// </summary>
    /// <param name="path">The file for which to set the access date and time information.</param>
    /// <param name="lastAccessTimeUtc">A <see cref="DateTime"/> containing the value to set for the last access date and time of path. This value is expressed in UTC time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetFileLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the date and time, in Coordinated Universal Time (UTC) format, that the specified directory was last accessed.
    /// </summary>
    /// <param name="path">The directory for which to set the access date and time information.</param>
    /// <param name="lastAccessTimeUtc">An object that contains the value to set for the access date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
    void SetDirectoryLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);

    /// <summary>
    /// Asynchronously sets the date and time, in Coordinated Universal Time (UTC) format, that the specified directory was last accessed.
    /// </summary>
    /// <param name="path">The directory for which to set the access date and time information.</param>
    /// <param name="lastAccessTimeUtc">An object that contains the value to set for the access date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetDirectoryLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the date and time, in Coordinated Universal Time (UTC),
    /// that the specified file was last written to.
    /// </summary>
    /// <param name="path">The file for which to obtain write date and time information.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last written to.
    /// This value is expressed in UTC time.
    /// </returns>
    DateTime GetFileLastWriteTimeUtc(string path);

    /// <summary>
    /// Asynchronously gets the date and time, in Coordinated Universal Time (UTC),
    /// that the specified file was last written to.
    /// </summary>
    /// <param name="path">The file for which to obtain write date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last written to.
    /// This value is expressed in UTC time.
    /// </returns>
    ValueTask<DateTime> GetFileLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the date and time, in Coordinated Universal Time (UTC) format, that the specified directory was last written to.
    /// </summary>
    /// <param name="path">The directory for which to obtain modification date and time information.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last written to. This value is expressed in UTC time.</returns>
    DateTime GetDirectoryLastWriteTimeUtc(string path);

    /// <summary>
    /// Asynchronously returns the date and time, in Coordinated Universal Time (UTC) format, that the specified directory was last written to.
    /// </summary>
    /// <param name="path">The directory for which to obtain modification date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last written to. This value is expressed in UTC time.</returns>
    ValueTask<DateTime> GetDirectoryLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the date and time, in Coordinated Universal Time (UTC), that the specified file was last written to.
    /// </summary>
    /// <param name="path">The file for which to set the date and time information.</param>
    /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of path. This value is expressed in UTC time.</param>
    void SetFileLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);

    /// <summary>
    /// Asynchronously sets the date and time, in Coordinated Universal Time (UTC), that the specified file was last written to.
    /// </summary>
    /// <param name="path">The file for which to set the date and time information.</param>
    /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of path. This value is expressed in UTC time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetFileLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the date and time, in Coordinated Universal Time (UTC) format, that a directory was last written to.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="lastWriteTimeUtc">The date and time the directory was last written to. This value is expressed in UTC time.</param>
    void SetDirectoryLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);

    /// <summary>
    /// Sets the date and time, in Coordinated Universal Time (UTC) format, that a directory was last written to.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="lastWriteTimeUtc">The date and time the directory was last written to. This value is expressed in UTC time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetDirectoryLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides extension methods for working with <see cref="IFileSystem"/> instances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class FileSystemExtensions
{
    /// <summary>
    /// Returns a string that represents the provided file system.
    /// </summary>
    /// <param name="fs">The file system object to format as a string.</param>
    /// <returns>A string that represents the provided file system.</returns>
    internal static string ToFormattedString(this IFileSystem fs)
        => new StringBuilder(512)
            .Append(fs.GetType().Name).Append(' ').Append('{').Append(' ')
            .Append(nameof(IFileSystem.Name)).Append(' ').Append('=').Append(' ').Append(fs.Name).Append(',').Append(' ')
            .Append(nameof(IFileSystem.CurrentDirectory)).Append(' ').Append('=').Append(' ').Append(fs.CurrentDirectory).Append(',').Append(' ')
            .Append(nameof(IFileSystem.PathComparison)).Append(' ').Append('=').Append(' ').Append(fs.PathComparison.ToString()).Append(',').Append(' ')
            .Append(nameof(IFileSystem.DirectorySeparator)).Append(' ').Append('=').Append(' ').Append(fs.DirectorySeparator).Append(',').Append(' ')
            .Append(nameof(IFileSystem.AltDirectorySeparator)).Append(' ').Append('=').Append(' ').Append(fs.AltDirectorySeparator).Append(',').Append(' ')
            .Append(nameof(IFileSystem.VolumeSeparator)).Append(' ').Append('=').Append(' ').Append(fs.VolumeSeparator)
            .Append(' ').Append('}')
            .ToString();

    /// <summary>
    /// A factory function used to instantiate <see cref="FileSystemEventArgs"/> objects.
    /// </summary>
    private static readonly Func<WatcherChangeTypes, string, string, FileSystemEventArgs> s_fileSystemEventArgsFactory = CreateFileSystemEventArgsFactory();

    /// <summary>
    /// Creates a new instance of the <see cref="FileSystemEventArgs"/> class.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="changeType">One of the <see cref="WatcherChangeTypes"/> values, which represents the kind of change detected in the file system.</param>
    /// <param name="fullPath">The fully qualified path of the affected file or directory.</param>
    /// <returns>A new <see cref="FileSystemEventArgs"/> instance for the specified file system event.</returns>
    public static FileSystemEventArgs CreateFileSystemEventArgs(this IFileSystem fs, WatcherChangeTypes changeType, string fullPath)
        => s_fileSystemEventArgsFactory(changeType, fs.GetFileName(fullPath), fullPath);

    /// <summary>
    /// Creates a factory function used to instantiate <see cref="FileSystemEventArgs"/> objects.
    /// </summary>
    /// <returns>A factory function that creates <see cref="FileSystemEventArgs"/> objects.</returns>
    private static Func<WatcherChangeTypes, string, string, FileSystemEventArgs> CreateFileSystemEventArgsFactory()
    {
        // public static FileSystemEventArgs CreateFileSystemEventArgs(WatcherChangeTypes changeType, string name, string fullPath)
        // {
        //     FileSystemEventArgs args = new(changeType, Path.DirectorySeparatorChar.ToString(), name);
        //     args._fullPath = fullPath;
        //     return args;
        // }
        Type returnType = typeof(FileSystemEventArgs);
        Type[] parameterTypes = [typeof(WatcherChangeTypes), typeof(string), typeof(string)];

        ConstructorInfo ctor = returnType.GetInstanceConstructor(parameterTypes)!;
        FieldInfo fullPathField = returnType.GetInstanceField("_fullPath") ?? returnType.GetInstanceField("fullPath")!;

        using DynamicMethodBuilder method = DynamicAssembly.Shared.DefineMethod("CreateFileSystemEventArgs", returnType, parameterTypes, skipVisibility: true);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
#pragma warning disable RS0030 // Do not use banned APIs
        il.Emit(OpCodes.Ldstr, Path.DirectorySeparatorChar.ToString());
#pragma warning restore RS0030 // Do not use banned APIs
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, fullPathField);
        il.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<WatcherChangeTypes, string, string, FileSystemEventArgs>>();
    }

    /// <summary>
    /// A factory function used to instantiate <see cref="RenamedEventArgs"/> objects.
    /// </summary>
    private static readonly Func<WatcherChangeTypes, string, string, string, string, RenamedEventArgs> s_renamedEventArgsFactory = CreateRenamedEventArgsFactory();

    /// <summary>
    /// Creates a new instance of the <see cref="RenamedEventArgs"/> class.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="changeType">One of the <see cref="WatcherChangeTypes"/> values, which represents the kind of change detected in the file system.</param>
    /// <param name="fullPath">The fully qualified path of the affected file or directory.</param>
    /// <param name="oldFullPath">The previous fully qualified path of the affected file or directory.</param>
    /// <returns>A new <see cref="RenamedEventArgs"/> instance for the specified file system event.</returns>
    public static RenamedEventArgs CreateFileSystemEventArgs(this IFileSystem fs, WatcherChangeTypes changeType, string fullPath, string oldFullPath)
        => s_renamedEventArgsFactory(changeType, fs.GetFileName(fullPath), fullPath, fs.GetFileName(oldFullPath), oldFullPath);

    /// <summary>
    /// Creates a factory function used to instantiate <see cref="RenamedEventArgs"/> objects.
    /// </summary>
    /// <returns>A factory function that creates <see cref="RenamedEventArgs"/> objects.</returns>
    private static Func<WatcherChangeTypes, string, string, string, string, RenamedEventArgs> CreateRenamedEventArgsFactory()
    {
        // public static RenamedEventArgs CreateRenamedEventArgs(WatcherChangeTypes changeType, string name, string fullPath, string oldName, string oldFullPath)
        // {
        //     RenamedEventArgs args = new(changeType, Path.DirectorySeparatorChar.ToString(), name, oldName);
        //     args._fullPath = fullPath;
        //     args._oldFullPath = oldFullPath;
        //     return args;
        // }
        Type parentType = typeof(FileSystemEventArgs);
        Type returnType = typeof(RenamedEventArgs);
        Type[] parameterTypes = [typeof(WatcherChangeTypes), typeof(string), typeof(string), typeof(string), typeof(string)];

        ConstructorInfo ctor = returnType.GetInstanceConstructor([typeof(WatcherChangeTypes), typeof(string), typeof(string), typeof(string)])!;
        FieldInfo fullPathField = parentType.GetInstanceField("_fullPath") ?? parentType.GetInstanceField("fullPath")!;
        FieldInfo oldFullPathField = returnType.GetInstanceField("_oldFullPath") ?? returnType.GetInstanceField("oldFullPath")!;

        using DynamicMethodBuilder method = DynamicAssembly.Shared.DefineMethod("CreateRenamedEventArgs", returnType, parameterTypes, skipVisibility: true);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
#pragma warning disable RS0030 // Do not use banned APIs
        il.Emit(OpCodes.Ldstr, Path.DirectorySeparatorChar.ToString());
#pragma warning restore RS0030 // Do not use banned APIs
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, fullPathField);
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldarg_S, 4);
        il.Emit(OpCodes.Stfld, oldFullPathField);
        il.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<WatcherChangeTypes, string, string, string, string, RenamedEventArgs>>();
    }

    /// <summary>
    /// Creates a <see cref="FileSystemFileInfo"/> representing the specified file within the file system.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="fileName">The path to the file.</param>
    /// <returns>A <see cref="FileSystemFileInfo"/> for the specified file.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FileSystemFileInfo ToFileInfo(this IFileSystem fs, string fileName)
        => new(fs, fileName);

    /// <summary>
    /// Creates a <see cref="FileSystemDirectoryInfo"/> representing the specified directory within the file system.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to the directory.</param>
    /// <returns>A <see cref="FileSystemDirectoryInfo"/> for the specified directory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FileSystemDirectoryInfo ToDirectoryInfo(this IFileSystem fs, string path)
        => new(fs, path);

    /// <summary>
    /// Determines whether the specified file or directory exists.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the caller has the required permissions and <paramref name="path"/> contains the name of an existing file or directory; otherwise, <c>false</c>.</returns>
    public static bool Exists(this IFileSystem fs, [NotNullWhen(true)] string? path)
    {
        if (path is not { Length: > 0 })
            return false;

        bool directoryExists = fs.DirectoryExists(path);
        return directoryExists || !fs.EndsInDirectorySeparator(path) && fs.FileExists(path);
    }

    /// <summary>
    /// Asynchronously determines whether the specified file or directory exists.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to check.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the caller has the required permissions and <paramref name="path"/> contains the name of an existing file or directory; otherwise, <c>false</c>.</returns>
    public static async ValueTask<bool> ExistsAsync(this IFileSystem fs, [NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
    {
        if (path is not { Length: > 0 })
            return false;

        bool directoryExists = await fs.DirectoryExistsAsync(path, cancellationToken).ConfigureAwait(false);
        return directoryExists || !fs.EndsInDirectorySeparator(path) && await fs.FileExistsAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns an enumerable collection of directory full names in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
    public static IEnumerable<string> EnumerateDirectories(this IFileSystem fs, string path)
        => fs.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an async enumerable collection of directory full names in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
    public static IAsyncEnumerable<string> EnumerateDirectories(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.EnumerateDirectoriesAsync(path, "*", SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns an enumerable collection of directory full names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
    /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/> and that match the specified search pattern.</returns>
    public static IEnumerable<string> EnumerateDirectories(this IFileSystem fs, string path, string searchPattern)
        => fs.EnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an async enumerable collection of directory full names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="path"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/> and that match the specified search pattern.</returns>
    public static IAsyncEnumerable<string> EnumerateDirectories(this IFileSystem fs, string path, string searchPattern, CancellationToken cancellationToken = default)
        => fs.EnumerateDirectoriesAsync(path, searchPattern, SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns an enumerable collection of full file names in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <returns>An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/>.</returns>
    public static IEnumerable<string> EnumerateFiles(this IFileSystem fs, string path)
        => fs.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an async enumerable collection of full file names in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/>.</returns>
    public static IAsyncEnumerable<string> EnumerateFilesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.EnumerateFilesAsync(path, "*", SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns an enumerable collection of full file names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <returns>An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern.</returns>
    public static IEnumerable<string> EnumerateFiles(this IFileSystem fs, string path, string searchPattern)
        => fs.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an async enumerable collection of full file names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern.</returns>
    public static IAsyncEnumerable<string> EnumerateFilesAsync(this IFileSystem fs, string path, string searchPattern, CancellationToken cancellationToken = default)
        => fs.EnumerateFilesAsync(path, searchPattern, SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns an enumerable collection of file names and directory names in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <returns>An enumerable collection of file-system entries in the directory specified by <paramref name="path"/>.</returns>
    public static IEnumerable<string> EnumerateFileSystemEntries(this IFileSystem fs, string path)
        => fs.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an async enumerable collection of file names and directory names in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of file-system entries in the directory specified by <paramref name="path"/>.</returns>
    public static IAsyncEnumerable<string> EnumerateFileSystemEntriesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.EnumerateFileSystemEntriesAsync(path, "*", SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns an enumerable collection of file names and directory names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of file-system entries in <paramref name="path"/>.</param>
    /// <returns>An enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern.</returns>
    public static IEnumerable<string> EnumerateFileSystemEntries(this IFileSystem fs, string path, string searchPattern)
        => fs.EnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Returns an async enumerable collection of file names and directory names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of file-system entries in <paramref name="path"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern.</returns>
    public static IAsyncEnumerable<string> EnumerateFileSystemEntriesAsync(this IFileSystem fs, string path, string searchPattern, CancellationToken cancellationToken = default)
        => fs.EnumerateFileSystemEntriesAsync(path, searchPattern, SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns the names of subdirectories (including their paths) in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <returns>An array of the full names (including paths) of subdirectories in the specified path, or an empty array if no directories are found.</returns>
    public static string[] GetDirectories(this IFileSystem fs, string path)
        => fs.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Asynchronously returns the names of subdirectories (including their paths) in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of the full names (including paths) of subdirectories in the specified path, or an empty array if no directories are found.</returns>
    public static Task<string[]> GetDirectoriesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.GetDirectoriesAsync(path, "*", SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns the names of subdirectories (including their paths) that match the specified search pattern in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of subdirectories in <paramref name="path"/>.</param>
    /// <returns>An array of the full names (including paths) of the subdirectories that match the search pattern in the specified directory, or an empty array if no directories are found.</returns>
    public static string[] GetDirectories(this IFileSystem fs, string path, string searchPattern)
        => fs.GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Asynchronously returns the names of subdirectories (including their paths) that match the specified search pattern in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of subdirectories in <paramref name="path"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of the full names (including paths) of the subdirectories that match the search pattern in the specified directory, or an empty array if no directories are found.</returns>
    public static Task<string[]> GetDirectoriesAsync(this IFileSystem fs, string path, string searchPattern, CancellationToken cancellationToken = default)
        => fs.GetDirectoriesAsync(path, searchPattern, SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns the names of the subdirectories (including their paths) that match the specified search pattern in the specified directory, and optionally searches subdirectories.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of subdirectories in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An array of the full names (including paths) of the subdirectories that match the specified criteria, or an empty array if no directories are found.</returns>
    public static string[] GetDirectories(this IFileSystem fs, string path, string searchPattern, SearchOption searchOption)
        => fs.EnumerateDirectories(path, searchPattern, searchOption).ToArray();

    /// <summary>
    /// Asynchronously returns the names of the subdirectories (including their paths) that match the specified search pattern in the specified directory, and optionally searches subdirectories.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of subdirectories in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of the full names (including paths) of the subdirectories that match the specified criteria, or an empty array if no directories are found.</returns>
    public static Task<string[]> GetDirectoriesAsync(this IFileSystem fs, string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => fs.EnumerateDirectoriesAsync(path, searchPattern, searchOption, cancellationToken).ToArrayAsync();

    /// <summary>
    /// Returns the names of files (including their paths) in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory, or an empty array if no files are found.</returns>
    public static string[] GetFiles(this IFileSystem fs, string path)
        => fs.GetFiles(path, "*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Asynchronously returns the names of files (including their paths) in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory, or an empty array if no files are found.</returns>
    public static Task<string[]> GetFilesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.GetFilesAsync(path, "*", SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory that match the specified search pattern, or an empty array if no files are found.</returns>
    public static string[] GetFiles(this IFileSystem fs, string path, string searchPattern)
        => fs.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Asynchronously returns the names of files (including their paths) that match the specified search pattern in the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory that match the specified search pattern, or an empty array if no files are found.</returns>
    public static Task<string[]> GetFilesAsync(this IFileSystem fs, string path, string searchPattern, CancellationToken cancellationToken = default)
        => fs.GetFilesAsync(path, searchPattern, SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory, using a value to determine whether to search subdirectories.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory that match the specified search pattern and option, or an empty array if no files are found.</returns>
    public static string[] GetFiles(this IFileSystem fs, string path, string searchPattern, SearchOption searchOption)
        => fs.EnumerateFiles(path, searchPattern, searchOption).ToArray();

    /// <summary>
    /// Asynchronously returns the names of files (including their paths) that match the specified search pattern in the specified directory, using a value to determine whether to search subdirectories.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory that match the specified search pattern and option, or an empty array if no files are found.</returns>
    public static Task<string[]> GetFilesAsync(this IFileSystem fs, string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => fs.EnumerateFilesAsync(path, searchPattern, searchOption, cancellationToken).ToArrayAsync();

    /// <summary>
    /// Returns the names of all files and subdirectories in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <returns>An array of the names of files and subdirectories in the specified directory, or an empty array if no files or subdirectories are found.</returns>
    public static string[] GetFileSystemEntries(this IFileSystem fs, string path)
        => fs.GetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Asynchronously returns the names of all files and subdirectories in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of the names of files and subdirectories in the specified directory, or an empty array if no files or subdirectories are found.</returns>
    public static Task<string[]> GetFileSystemEntriesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.GetFileSystemEntriesAsync(path, "*", SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns an array of file names and directory names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files and directories in <paramref name="path"/>.</param>
    /// <returns>An array of file names and directory names that match the specified search criteria, or an empty array if no files or directories are found.</returns>
    public static string[] GetFileSystemEntries(this IFileSystem fs, string path, string searchPattern)
        => fs.GetFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Asynchronously returns an array of file names and directory names that match a search pattern in a specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files and directories in <paramref name="path"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of file names and directory names that match the specified search criteria, or an empty array if no files or directories are found.</returns>
    public static Task<string[]> GetFileSystemEntriesAsync(this IFileSystem fs, string path, string searchPattern, CancellationToken cancellationToken = default)
        => fs.GetFileSystemEntriesAsync(path, searchPattern, SearchOption.TopDirectoryOnly, cancellationToken);

    /// <summary>
    /// Returns an array of all the file names and directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files and directories in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An array of file the file names and directory names that match the specified search criteria, or an empty array if no files or directories are found.</returns>
    public static string[] GetFileSystemEntries(this IFileSystem fs, string path, string searchPattern, SearchOption searchOption)
        => fs.EnumerateFileSystemEntries(path, searchPattern, searchOption).ToArray();

    /// <summary>
    /// Asynchronously returns an array of all the file names and directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files and directories in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An array of file the file names and directory names that match the specified search criteria, or an empty array if no files or directories are found.</returns>
    public static Task<string[]> GetFileSystemEntriesAsync(this IFileSystem fs, string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => fs.EnumerateFileSystemEntriesAsync(path, searchPattern, searchOption, cancellationToken).ToArrayAsync();

    /// <summary>
    /// Gets the creation date and time of the specified file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to obtain creation date and time information.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the creation date and time for the specified file.
    /// This value is expressed in local time.
    /// </returns>
    public static DateTime GetFileCreationTime(this IFileSystem fs, string path)
        => fs.GetFileCreationTimeUtc(path).ToLocalTime();

    /// <summary>
    /// Asynchronously gets the creation date and time of the specified file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to obtain creation date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="DateTime"/> structure set to the creation date and time for the specified file. This value is expressed in local time.</returns>
    public static async ValueTask<DateTime> GetFileCreationTimeAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => (await fs.GetFileCreationTimeUtcAsync(path, cancellationToken).ConfigureAwait(false)).ToLocalTime();

    /// <summary>
    /// Gets the creation date and time of a directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path of the directory.</param>
    /// <returns>A structure that is set to the creation date and time for the specified directory. This value is expressed in local time.</returns>
    public static DateTime GetDirectoryCreationTime(this IFileSystem fs, string path)
        => fs.GetDirectoryCreationTimeUtc(path).ToLocalTime();

    /// <summary>
    /// Asynchronously gets the creation date and time of a directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A structure that is set to the creation date and time for the specified directory. This value is expressed in local time.</returns>
    public static async ValueTask<DateTime> GetDirectoryCreationTimeAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => (await fs.GetDirectoryCreationTimeUtcAsync(path, cancellationToken).ConfigureAwait(false)).ToLocalTime();

    /// <summary>
    /// Sets the date and time that the file was created.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to set the creation date and time information.</param>
    /// <param name="creationTime">The value to set for the creation date and time of <paramref name="path"/>. This value is expressed in local time.</param>
    public static void SetFileCreationTime(this IFileSystem fs, string path, DateTime creationTime)
        => fs.SetFileCreationTimeUtc(path, creationTime.ToUniversalTime());

    /// <summary>
    /// Asynchronously sets the date and time that the file was created.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to set the creation date and time information.</param>
    /// <param name="creationTime">The value to set for the creation date and time of <paramref name="path"/>. This value is expressed in local time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static ValueTask SetFileCreationTimeAsync(this IFileSystem fs, string path, DateTime creationTime, CancellationToken cancellationToken = default)
        => fs.SetFileCreationTimeUtcAsync(path, creationTime.ToUniversalTime(), cancellationToken);

    /// <summary>
    /// Sets the creation date and time for the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to set the creation date and time information.</param>
    /// <param name="creationTime">The date and time the directory was created. This value is expressed in local time.</param>
    public static void SetDirectoryCreationTime(this IFileSystem fs, string path, DateTime creationTime)
        => fs.SetDirectoryCreationTimeUtc(path, creationTime.ToUniversalTime());

    /// <summary>
    /// Asynchronously sets the creation date and time for the specified directory.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to set the creation date and time information.</param>
    /// <param name="creationTime">The date and time the directory was created. This value is expressed in local time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static ValueTask SetDirectoryCreationTimeAsync(this IFileSystem fs, string path, DateTime creationTime, CancellationToken cancellationToken = default)
        => fs.SetDirectoryCreationTimeUtcAsync(path, creationTime.ToUniversalTime(), cancellationToken);

    /// <summary>
    /// Gets the date and time that the specified file was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to obtain access date and time information.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last accessed.
    /// This value is expressed in local time.
    /// </returns>
    public static DateTime GetFileLastAccessTime(this IFileSystem fs, string path)
        => fs.GetFileLastAccessTimeUtc(path).ToLocalTime();

    /// <summary>
    /// Asynchronously gets the date and time that the specified file was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to obtain access date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last accessed.
    /// This value is expressed in local time.
    /// </returns>
    public static async ValueTask<DateTime> GetFileLastAccessTimeAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => (await fs.GetFileLastAccessTimeUtcAsync(path, cancellationToken).ConfigureAwait(false)).ToLocalTime();

    /// <summary>
    /// Returns the date and time that the specified directory was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to obtain access date and time information.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last accessed. This value is expressed in local time.</returns>
    public static DateTime GetDirectoryLastAccessTime(this IFileSystem fs, string path)
        => fs.GetDirectoryLastAccessTimeUtc(path).ToLocalTime();

    /// <summary>
    /// Asynchronously returns the date and time that the specified directory was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to obtain access date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last accessed. This value is expressed in local time.</returns>
    public static async ValueTask<DateTime> GetDirectoryLastAccessTimeAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => (await fs.GetDirectoryLastAccessTimeUtcAsync(path, cancellationToken).ConfigureAwait(false)).ToLocalTime();

    /// <summary>
    /// Sets the date and time that the specified file was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to set the access date and time information.</param>
    /// <param name="lastAccessTime">A <see cref="DateTime"/> containing the value to set for the last access date and time of path. This value is expressed in local time.</param>
    public static void SetFileLastAccessTime(this IFileSystem fs, string path, DateTime lastAccessTime)
        => fs.SetFileLastAccessTimeUtc(path, lastAccessTime.ToUniversalTime());

    /// <summary>
    /// Asynchronously sets the date and time that the specified file was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to set the access date and time information.</param>
    /// <param name="lastAccessTime">A <see cref="DateTime"/> containing the value to set for the last access date and time of path. This value is expressed in local time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static ValueTask SetFileLastAccessTimeAsync(this IFileSystem fs, string path, DateTime lastAccessTime, CancellationToken cancellationToken = default)
        => fs.SetFileLastAccessTimeUtcAsync(path, lastAccessTime.ToUniversalTime(), cancellationToken);

    /// <summary>
    /// Sets the date and time that the specified directory was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to set the access date and time information.</param>
    /// <param name="lastAccessTime">An object that contains the value to set for the access date and time of <paramref name="path"/>. This value is expressed in local time.</param>
    public static void SetDirectoryLastAccessTime(this IFileSystem fs, string path, DateTime lastAccessTime)
        => fs.SetDirectoryLastAccessTimeUtc(path, lastAccessTime.ToUniversalTime());

    /// <summary>
    /// Asynchronously sets the date and time that the specified directory was last accessed.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to set the access date and time information.</param>
    /// <param name="lastAccessTime">An object that contains the value to set for the access date and time of <paramref name="path"/>. This value is expressed in local time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static ValueTask SetDirectoryLastAccessTimeAsync(this IFileSystem fs, string path, DateTime lastAccessTime, CancellationToken cancellationToken = default)
        => fs.SetDirectoryLastAccessTimeUtcAsync(path, lastAccessTime.ToUniversalTime(), cancellationToken);

    /// <summary>
    /// Gets the date and time
    /// that the specified file was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to obtain write date and time information.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last written to.
    /// This value is expressed in local time.
    /// </returns>
    public static DateTime GetFileLastWriteTime(this IFileSystem fs, string path)
        => fs.GetFileLastWriteTimeUtc(path).ToLocalTime();

    /// <summary>
    /// Asynchronously gets the date and time
    /// that the specified file was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to obtain write date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="DateTime"/> structure set to the date and time that the specified file was last written to.
    /// This value is expressed in local time.
    /// </returns>
    public static async ValueTask<DateTime> GetFileLastWriteTimeAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => (await fs.GetFileLastWriteTimeUtcAsync(path, cancellationToken).ConfigureAwait(false)).ToLocalTime();

    /// <summary>
    /// Returns the date and time that the specified directory was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to obtain modification date and time information.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last written to. This value is expressed in local time.</returns>
    public static DateTime GetDirectoryLastWriteTime(this IFileSystem fs, string path)
        => fs.GetDirectoryLastWriteTimeUtc(path).ToLocalTime();

    /// <summary>
    /// Asynchronously returns the date and time that the specified directory was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The directory for which to obtain modification date and time information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A structure that is set to the date and time the specified directory was last written to. This value is expressed in local time.</returns>
    public static async ValueTask<DateTime> GetDirectoryLastWriteTimeAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => (await fs.GetDirectoryLastWriteTimeUtcAsync(path, cancellationToken).ConfigureAwait(false)).ToLocalTime();

    /// <summary>
    /// Sets the date and time that the specified file was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to set the date and time information.</param>
    /// <param name="lastWriteTime">A <see cref="DateTime"/> containing the value to set for the last write date and time of path. This value is expressed in local time.</param>
    public static void SetFileLastWriteTime(this IFileSystem fs, string path, DateTime lastWriteTime)
        => fs.SetFileLastWriteTimeUtc(path, lastWriteTime.ToUniversalTime());

    /// <summary>
    /// Asynchronously sets the date and time that the specified file was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file for which to set the date and time information.</param>
    /// <param name="lastWriteTime">A <see cref="DateTime"/> containing the value to set for the last write date and time of path. This value is expressed in local time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static ValueTask SetFileLastWriteTimeAsync(this IFileSystem fs, string path, DateTime lastWriteTime, CancellationToken cancellationToken = default)
        => fs.SetFileLastWriteTimeUtcAsync(path, lastWriteTime.ToUniversalTime(), cancellationToken);

    /// <summary>
    /// Sets the date and time that a directory was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="lastWriteTime">The date and time the directory was last written to. This value is expressed in local time.</param>
    public static void SetDirectoryLastWriteTime(this IFileSystem fs, string path, DateTime lastWriteTime)
        => fs.SetDirectoryLastWriteTimeUtc(path, lastWriteTime.ToUniversalTime());

    /// <summary>
    /// Sets the date and time that a directory was last written to.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path of the directory.</param>
    /// <param name="lastWriteTime">The date and time the directory was last written to. This value is expressed in local time.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static ValueTask SetDirectoryLastWriteTimeAsync(this IFileSystem fs, string path, DateTime lastWriteTime, CancellationToken cancellationToken = default)
        => fs.SetDirectoryLastWriteTimeUtcAsync(path, lastWriteTime.ToUniversalTime(), cancellationToken);

    /// <summary>
    /// Opens a <see cref="Stream"/> on the specified path with read/write access with no sharing.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <returns>A <see cref="Stream"/> opened in the specified mode and path, with read/write access and not shared.</returns>
    public static Stream Open(this IFileSystem fs, string path, FileMode mode)
        => fs.Open(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None);

    /// <summary>
    /// Asynchronously opens a <see cref="Stream"/> on the specified path with read/write access with no sharing.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Stream"/> opened in the specified mode and path, with read/write access and not shared.</returns>
    public static Task<Stream> OpenAsync(this IFileSystem fs, string path, FileMode mode, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None, cancellationToken);

    /// <summary>
    /// Opens a <see cref="Stream"/> on the specified path, with the specified mode and access with no sharing.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <returns>An unshared <see cref="Stream"/> that provides access to the specified file, with the specified mode and access.</returns>
    public static Stream Open(this IFileSystem fs, string path, FileMode mode, FileAccess access)
        => fs.Open(path, mode, access, FileShare.None);

    /// <summary>
    /// Asynchronously opens a <see cref="Stream"/> on the specified path, with the specified mode and access with no sharing.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An unshared <see cref="Stream"/> that provides access to the specified file, with the specified mode and access.</returns>
    public static Task<Stream> OpenAsync(this IFileSystem fs, string path, FileMode mode, FileAccess access, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, mode, access, FileShare.None, cancellationToken);

    /// <summary>
    /// Asynchronously opens a <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
    public static async Task<Stream> OpenAsync(this IFileSystem fs, string path, FileMode mode, FileAccess access, FileShare share, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        _ = fs ?? throw new ArgumentNullException(nameof(fs));

        CancellationTokenSource? tokenSource = null;
        if (!cancellationToken.CanBeCanceled)
        {
            tokenSource = new(TimeSpan.FromSeconds(5));
            cancellationToken = tokenSource.Token;
        }
        using CancellationTokenSource? disposableTokenSource = tokenSource;

        if (pollingInterval <= TimeSpan.Zero)
            pollingInterval = TimeSpan.FromMilliseconds(50);

        while (true)
        {
            await Task.Delay(pollingInterval, cancellationToken).ConfigureAwait(false);

            try
            {
                return await fs.OpenAsync(path, mode, access, share, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                continue;
            }
        }
    }

    /// <summary>
    /// Opens an existing file for reading.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for reading.</param>
    /// <returns>A read-only <see cref="Stream"/> on the specified path.</returns>
    public static Stream OpenRead(this IFileSystem fs, string path)
        => fs.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <inheritdoc cref="OpenReadAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static Task<Stream> OpenReadAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, cancellationToken);

    /// <summary>
    /// Asynchronously opens an existing file for reading.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for reading.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A read-only <see cref="Stream"/> on the specified path.</returns>
    public static Task<Stream> OpenReadAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, pollingInterval, cancellationToken);

    /// <summary>
    /// Opens an existing UTF-8 encoded text file for reading.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for reading.</param>
    /// <returns>A <see cref="StreamReader"/> on the specified path.</returns>
    public static StreamReader OpenText(this IFileSystem fs, string path)
        => fs.OpenText(path, Encoding.UTF8);

    /// <inheritdoc cref="OpenTextAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static Task<StreamReader> OpenTextAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.OpenTextAsync(path, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously opens an existing UTF-8 encoded text file for reading.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for reading.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="StreamReader"/> on the specified path.</returns>
    public static Task<StreamReader> OpenTextAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.OpenTextAsync(path, Encoding.UTF8, pollingInterval, cancellationToken);

    /// <summary>
    /// Opens an existing file for reading.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for reading.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <returns>A <see cref="StreamReader"/> on the specified path.</returns>
    public static StreamReader OpenText(this IFileSystem fs, string path, Encoding encoding)
        => new(fs.OpenRead(path), encoding);

    /// <inheritdoc cref="OpenTextAsync(IFileSystem, string, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task<StreamReader> OpenTextAsync(this IFileSystem fs, string path, Encoding encoding, CancellationToken cancellationToken = default)
        => new(await fs.OpenReadAsync(path, cancellationToken).ConfigureAwait(false), encoding);

    /// <summary>
    /// Asynchronously opens an existing file for reading.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for reading.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="StreamReader"/> on the specified path.</returns>
    public static async Task<StreamReader> OpenTextAsync(this IFileSystem fs, string path, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => new(await fs.OpenReadAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), encoding);

    /// <summary>
    /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <returns>A byte array containing the contents of the file.</returns>
    public static byte[] ReadAllBytes(this IFileSystem fs, string path)
    {
        using Stream stream = fs.OpenRead(path);
        if (stream is not MemoryStream { Position: 0 } reader)
        {
            reader = new(Math.Max(stream.CanSeek ? (int)stream.Length : 0, 0));
            stream.CopyTo(reader);
        }
        return reader.ToArray();
    }

    /// <inheritdoc cref="ReadAllBytesAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
    {
        using Stream stream = await fs.OpenReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (stream is not MemoryStream { Position: 0 } reader)
        {
            reader = new(Math.Max(stream.CanSeek ? (int)stream.Length : 0, 0));
            await stream.CopyToAsync(reader, GetCopyBufferSize(stream), cancellationToken).ConfigureAwait(false);
        }
        return reader.ToArray();
    }

    /// <summary>
    /// Asynchronously opens a binary file, reads the contents of the file into a byte array, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A byte array containing the contents of the file.</returns>
    public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        using Stream stream = await fs.OpenReadAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false);
        if (stream is not MemoryStream { Position: 0 } reader)
        {
            reader = new(Math.Max(stream.CanSeek ? (int)stream.Length : 0, 0));
            await stream.CopyToAsync(reader, GetCopyBufferSize(stream), cancellationToken).ConfigureAwait(false);
        }
        return reader.ToArray();
    }

    /// <summary>
    /// Reads the lines of an existing UTF-8 encoded text file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <returns>All the lines of the file.</returns>
    public static IEnumerable<string> ReadLines(this IFileSystem fs, string path)
        => fs.ReadLines(path, Encoding.UTF8);

    /// <inheritdoc cref="ReadLinesAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static IAsyncEnumerable<string> ReadLinesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken)
        => fs.ReadLinesAsync(path, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously reads the lines of an existing UTF-8 encoded text file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The async enumerable that represents all the lines of the file.</returns>
    public static IAsyncEnumerable<string> ReadLinesAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken)
        => fs.ReadLinesAsync(path, Encoding.UTF8, pollingInterval, cancellationToken);

    /// <summary>
    /// Read the lines of a file that has a specified encoding.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <returns>All the lines of the file.</returns>
    public static IEnumerable<string> ReadLines(this IFileSystem fs, string path, Encoding encoding)
    {
        using StreamReader reader = fs.OpenText(path, encoding);
        while (reader.ReadLine() is string line)
            yield return line;
    }

    /// <inheritdoc cref="ReadLinesAsync(IFileSystem, string, Encoding, TimeSpan, CancellationToken)"/>
    public static async IAsyncEnumerable<string> ReadLinesAsync(this IFileSystem fs, string path, Encoding encoding, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using StreamReader reader = await fs.OpenTextAsync(path, encoding, cancellationToken).ConfigureAwait(false);
        while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return line;
        }
    }

    /// <summary>
    /// Asynchronously read the lines of a file that has a specified encoding.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The async enumerable that represents all the lines of the file.</returns>
    public static async IAsyncEnumerable<string> ReadLinesAsync(this IFileSystem fs, string path, Encoding encoding, TimeSpan pollingInterval, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using StreamReader reader = await fs.OpenTextAsync(path, encoding, pollingInterval, cancellationToken).ConfigureAwait(false);
        while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return line;
        }
    }

    /// <summary>
    /// Opens an existing UTF-8 encoded text file, reads all lines of the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <returns>A string array containing all lines of the file.</returns>
    public static string[] ReadAllLines(this IFileSystem fs, string path)
        => fs.ReadAllLines(path, Encoding.UTF8);

    /// <inheritdoc cref="ReadAllLinesAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static Task<string[]> ReadAllLinesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.ReadAllLinesAsync(path, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously opens an existing UTF-8 encoded text file, reads all lines of the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A string array containing all lines of the file.</returns>
    public static Task<string[]> ReadAllLinesAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.ReadAllLinesAsync(path, Encoding.UTF8, pollingInterval, cancellationToken);

    /// <summary>
    /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <returns>A string array containing all lines of the file.</returns>
    public static string[] ReadAllLines(this IFileSystem fs, string path, Encoding encoding)
        => fs.ReadLines(path, encoding).ToArray();

    /// <inheritdoc cref="ReadAllLinesAsync(IFileSystem, string, Encoding, TimeSpan, CancellationToken)"/>
    public static Task<string[]> ReadAllLinesAsync(this IFileSystem fs, string path, Encoding encoding, CancellationToken cancellationToken = default)
        => fs.ReadLinesAsync(path, encoding, cancellationToken).ToArrayAsync();

    /// <summary>
    /// Asynchronously opens a file, reads all lines of the file with the specified encoding, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A string array containing all lines of the file.</returns>
    public static Task<string[]> ReadAllLinesAsync(this IFileSystem fs, string path, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.ReadLinesAsync(path, encoding, pollingInterval, cancellationToken).ToArrayAsync();

    /// <summary>
    /// Opens an existing UTF-8 encoded text file, reads all the text in the file into a string, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <returns>A string containing all the text in the file.</returns>
    public static string ReadAllText(this IFileSystem fs, string path)
        => fs.ReadAllText(path, Encoding.UTF8);

    /// <inheritdoc cref="ReadAllTextAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static Task<string> ReadAllTextAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously opens an existing UTF-8 encoded text file, reads all the text in the file into a string, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A string containing all the text in the file.</returns>
    public static Task<string> ReadAllTextAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.ReadAllTextAsync(path, Encoding.UTF8, pollingInterval, cancellationToken);

    /// <summary>
    /// Opens a file, reads all text in the file with the specified encoding, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <returns>A string containing all the text in the file.</returns>
    public static string ReadAllText(this IFileSystem fs, string path, Encoding encoding)
    {
        using StreamReader reader = fs.OpenText(path, encoding);
        return reader.ReadToEnd();
    }

    /// <inheritdoc cref="ReadAllTextAsync(IFileSystem, string, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string path, Encoding encoding, CancellationToken cancellationToken = default)
    {
        using StreamReader reader = await fs.OpenTextAsync(path, encoding, cancellationToken).ConfigureAwait(false);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously opens a file, reads all text in the file with the specified encoding, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to read.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A string containing all the text in the file.</returns>
    public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string path, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        using StreamReader reader = await fs.OpenTextAsync(path, encoding, pollingInterval, cancellationToken).ConfigureAwait(false);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Opens an existing file or creates a new file for writing.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <returns>An unshared <see cref="Stream"/> object on the specified path with <see cref="FileAccess.Write"/> access.</returns>
    public static Stream OpenWrite(this IFileSystem fs, string path)
        => fs.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

    /// <inheritdoc cref="OpenWriteAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static Task<Stream> OpenWriteAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, cancellationToken);

    /// <summary>
    /// Asynchronously opens an existing file or creates a new file for writing.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An unshared <see cref="Stream"/> object on the specified path with <see cref="FileAccess.Write"/> access.</returns>
    public static Task<Stream> OpenWriteAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, pollingInterval, cancellationToken);

    /// <summary>
    /// Opens the file if it exists and seeks to the end of the file, or creates a new file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <returns>An unshared <see cref="Stream"/> object on the specified path with <see cref="FileAccess.Write"/> access.</returns>
    internal static Stream OpenAppend(this IFileSystem fs, string path)
        => fs.Open(path, FileMode.Append, FileAccess.Write, FileShare.None);

    /// <inheritdoc cref="OpenAppendAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    internal static Task<Stream> OpenAppendAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.Append, FileAccess.Write, FileShare.None, cancellationToken);

    /// <summary>
    /// Asynchronously opens the file if it exists and seeks to the end of the file, or creates a new file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An unshared <see cref="Stream"/> object on the specified path with <see cref="FileAccess.Write"/> access.</returns>
    internal static Task<Stream> OpenAppendAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.Append, FileAccess.Write, FileShare.None, pollingInterval, cancellationToken);

    /// <summary>
    /// Appends the specified byte array to the end of the file at the given path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append to. The file is created if it doesn't already exist.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    public static void AppendAllBytes(this IFileSystem fs, string path, byte[] bytes)
        => WriteAllBytes(fs.OpenAppend(path), bytes);

    /// <inheritdoc cref="AppendAllBytesAsync(IFileSystem, string, byte[], TimeSpan, CancellationToken)"/>
    public static async Task AppendAllBytesAsync(this IFileSystem fs, string path, byte[] bytes, CancellationToken cancellationToken = default)
        => await WriteAllBytesAsync(await fs.OpenAppendAsync(path, cancellationToken).ConfigureAwait(false), bytes, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously appends the specified byte array to the end of the file at the given path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append to. The file is created if it doesn't already exist.</param>
    /// <param name="bytes">The bytes to append to the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public static async Task AppendAllBytesAsync(this IFileSystem fs, string path, byte[] bytes, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllBytesAsync(await fs.OpenAppendAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), bytes, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Appends lines to a file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The lines to append to the file.</param>
    public static void AppendAllLines(this IFileSystem fs, string path, IEnumerable<string> contents)
        => WriteAllLines(fs.AppendText(path), contents);

    /// <inheritdoc cref="AppendAllLinesAsync(IFileSystem, string, IEnumerable{string}, TimeSpan, CancellationToken)"/>
    public static async Task AppendAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.AppendTextAsync(path, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously appends lines to a file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The lines to append to the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public static async Task AppendAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.AppendTextAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Appends lines to a file by using a specified encoding, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The lines to append to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    public static void AppendAllLines(this IFileSystem fs, string path, IEnumerable<string> contents, Encoding encoding)
        => WriteAllLines(fs.AppendText(path, encoding), contents);

    /// <inheritdoc cref="AppendAllLinesAsync(IFileSystem, string, IEnumerable{string}, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task AppendAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.AppendTextAsync(path, encoding, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously appends lines to a file by using a specified encoding, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the lines to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The lines to append to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public static async Task AppendAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.AppendTextAsync(path, encoding, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Opens a file, appends the specified string to the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the specified string to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The string to append to the file.</param>
    public static void AppendAllText(this IFileSystem fs, string path, string? contents)
        => WriteAllText(fs.AppendText(path), contents);

    /// <inheritdoc cref="AppendAllTextAsync(IFileSystem, string, string?, TimeSpan, CancellationToken)"/>
    public static async Task AppendAllTextAsync(this IFileSystem fs, string path, string? contents, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.AppendTextAsync(path, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously opens a file, appends the specified string to the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the specified string to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The string to append to the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public static async Task AppendAllTextAsync(this IFileSystem fs, string path, string? contents, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.AppendTextAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Appends the specified string to the file using the specified encoding, creating the file if it does not already exist.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the specified string to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The string to append to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    public static void AppendAllText(this IFileSystem fs, string path, string? contents, Encoding encoding)
        => WriteAllText(fs.AppendText(path, encoding), contents);

    /// <inheritdoc cref="AppendAllTextAsync(IFileSystem, string, string?, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task AppendAllTextAsync(this IFileSystem fs, string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.AppendTextAsync(path, encoding, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously appends the specified string to the file using the specified encoding, creating the file if it does not already exist.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to append the specified string to. The file is created if it doesn't already exist.</param>
    /// <param name="contents">The string to append to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public static async Task AppendAllTextAsync(this IFileSystem fs, string path, string? contents, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.AppendTextAsync(path, encoding, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file, or to a new file if the specified file does not exist.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to the file to append to.</param>
    /// <returns>A stream writer that appends UTF-8 encoded text to the specified file or to a new file.</returns>
    public static StreamWriter AppendText(this IFileSystem fs, string path)
        => new(fs.OpenAppend(path), Encoding.UTF8);

    /// <inheritdoc cref="AppendTextAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static async Task<StreamWriter> AppendTextAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => new(await fs.OpenAppendAsync(path, cancellationToken).ConfigureAwait(false), Encoding.UTF8);

    /// <summary>
    /// Asynchronously creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file, or to a new file if the specified file does not exist.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to the file to append to.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A stream writer that appends UTF-8 encoded text to the specified file or to a new file.</returns>
    public static async Task<StreamWriter> AppendTextAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => new(await fs.OpenAppendAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), Encoding.UTF8);

    /// <summary>
    /// Creates a <see cref="StreamWriter"/> that appends text to an existing file using the provided encoding, or to a new file if the specified file does not exist.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to the file to append to.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <returns>A stream writer that appends text to the specified file using the provided encoding.</returns>
    public static StreamWriter AppendText(this IFileSystem fs, string path, Encoding encoding)
        => new(fs.OpenAppend(path), encoding);

    /// <inheritdoc cref="AppendTextAsync(IFileSystem, string, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task<StreamWriter> AppendTextAsync(this IFileSystem fs, string path, Encoding encoding, CancellationToken cancellationToken = default)
        => new(await fs.OpenAppendAsync(path, cancellationToken).ConfigureAwait(false), encoding);

    /// <summary>
    /// Asynchronously creates a <see cref="StreamWriter"/> that appends text to an existing file using the provided encoding, or to a new file if the specified file does not exist.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path to the file to append to.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A stream writer that appends text to the specified file using the provided encoding.</returns>
    public static async Task<StreamWriter> AppendTextAsync(this IFileSystem fs, string path, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => new(await fs.OpenAppendAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), encoding);

    /// <summary>
    /// Creates, or truncates and overwrites, a file in the specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path and name of the file to create.</param>
    /// <returns>A <see cref="Stream"/> that provides read/write access to the file specified in path.</returns>
    public static Stream Create(this IFileSystem fs, string path)
        => fs.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

    /// <inheritdoc cref="CreateAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static Task<Stream> CreateAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, cancellationToken);

    /// <summary>
    /// Asynchronously creates, or truncates and overwrites, a file in the specified path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path and name of the file to create.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Stream"/> that provides read/write access to the file specified in path.</returns>
    public static Task<Stream> CreateAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => fs.OpenAsync(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, pollingInterval, cancellationToken);

    /// <summary>
    /// Creates or opens a file for writing UTF-8 encoded text. If the file already exists, its contents are replaced.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <returns>A <see cref="StreamWriter"/> that writes to the specified file using UTF-8 encoding.</returns>
    public static StreamWriter CreateText(this IFileSystem fs, string path)
        => new(fs.Create(path), Encoding.UTF8);

    /// <inheritdoc cref="CreateTextAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static async Task<StreamWriter> CreateTextAsync(this IFileSystem fileSystem, string path, CancellationToken cancellationToken = default)
        => new(await fileSystem.CreateAsync(path, cancellationToken).ConfigureAwait(false), Encoding.UTF8);

    /// <summary>
    /// Asynchronously creates or opens a file for writing UTF-8 encoded text. If the file already exists, its contents are replaced.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="StreamWriter"/> that writes to the specified file using UTF-8 encoding.</returns>
    public static async Task<StreamWriter> CreateTextAsync(this IFileSystem fs, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => new(await fs.CreateAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), Encoding.UTF8);

    /// <summary>
    /// Creates or opens a file for writing text using the specified encoding. If the file already exists, its contents are replaced.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <returns>A <see cref="StreamWriter"/> that writes to the specified file using the provided encoding.</returns>
    public static StreamWriter CreateText(this IFileSystem fs, string path, Encoding encoding)
        => new(fs.Create(path), encoding);

    /// <inheritdoc cref="CreateTextAsync(IFileSystem, string, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task<StreamWriter> CreateTextAsync(this IFileSystem fs, string path, Encoding encoding, CancellationToken cancellationToken = default)
        => new(await fs.CreateAsync(path, cancellationToken).ConfigureAwait(false), encoding);

    /// <summary>
    /// Asynchronously creates or opens a file for writing text using the specified encoding. If the file already exists, its contents are replaced.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to be opened for writing.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="StreamWriter"/> that writes to the specified file using the provided encoding.</returns>
    public static async Task<StreamWriter> CreateTextAsync(this IFileSystem fs, string path, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => new(await fs.CreateAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), encoding);

    /// <summary>
    /// Creates a new file, writes the specified byte array to the file, and then closes the file. If the target file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="bytes">The bytes to write to the file.</param>
    public static void WriteAllBytes(this IFileSystem fs, string path, byte[] bytes)
        => WriteAllBytes(fs.Create(path), bytes);

    /// <inheritdoc cref="WriteAllBytesAsync(IFileSystem, string, byte[], TimeSpan, CancellationToken)"/>
    public static async Task WriteAllBytesAsync(this IFileSystem fs, string path, byte[] bytes, CancellationToken cancellationToken = default)
        => await WriteAllBytesAsync(await fs.CreateAsync(path, cancellationToken).ConfigureAwait(false), bytes, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously creates a new file, writes the specified byte array to the file, and then closes the file. If the target file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="bytes">The bytes to write to the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task WriteAllBytesAsync(this IFileSystem fs, string path, byte[] bytes, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllBytesAsync(await fs.CreateAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), bytes, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Creates a new file, writes a collection of strings to the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The lines to write to the file.</param>
    public static void WriteAllLines(this IFileSystem fs, string path, IEnumerable<string> contents)
        => WriteAllLines(fs.CreateText(path), contents);

    /// <inheritdoc cref="WriteAllLinesAsync(IFileSystem, string, IEnumerable{string}, TimeSpan, CancellationToken)"/>
    public static async Task WriteAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.CreateTextAsync(path, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously creates a new file, writes the specified lines to the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The lines to write to the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task WriteAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.CreateTextAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Creates a new file by using the specified encoding, writes a collection of strings to the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The lines to write to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    public static void WriteAllLines(this IFileSystem fs, string path, IEnumerable<string> contents, Encoding encoding)
        => WriteAllLines(fs.CreateText(path, encoding), contents);

    /// <inheritdoc cref="WriteAllLinesAsync(IFileSystem, string, IEnumerable{string}, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task WriteAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.CreateTextAsync(path, encoding, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously creates a new file by using the specified encoding, writes a collection of strings to the file, and then closes the file.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The lines to write to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task WriteAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> contents, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllLinesAsync(await fs.CreateTextAsync(path, encoding, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Creates a new file, writes the specified string to the file, and then closes the file. If the target file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The string to write to the file.</param>
    public static void WriteAllText(this IFileSystem fs, string path, string? contents)
        => WriteAllText(fs.CreateText(path), contents);

    /// <inheritdoc cref="WriteAllTextAsync(IFileSystem, string, string?, TimeSpan, CancellationToken)"/>
    public static async Task WriteAllTextAsync(this IFileSystem fs, string path, string? contents, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.CreateTextAsync(path, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously creates a new file, writes the specified string to the file, and then closes the file. If the target file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The string to write to the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task WriteAllTextAsync(this IFileSystem fs, string path, string? contents, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.CreateTextAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The string to write to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    public static void WriteAllText(this IFileSystem fs, string path, string? contents, Encoding encoding)
        => WriteAllText(fs.CreateText(path, encoding), contents);

    /// <inheritdoc cref="WriteAllTextAsync(IFileSystem, string, string?, Encoding, TimeSpan, CancellationToken)"/>
    public static async Task WriteAllTextAsync(this IFileSystem fs, string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.CreateTextAsync(path, encoding, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is truncated and overwritten.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The string to write to the file.</param>
    /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task WriteAllTextAsync(this IFileSystem fs, string path, string? contents, Encoding encoding, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
        => await WriteAllTextAsync(await fs.CreateTextAsync(path, encoding, pollingInterval, cancellationToken).ConfigureAwait(false), contents, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Writes the specified bytes to the provided <see cref="Stream"/> and closes the stream.
    /// </summary>
    /// <param name="stream">The stream to which the bytes will be written. The stream is closed after writing.</param>
    /// <param name="bytes">The byte array to write to the stream.</param>
    private static void WriteAllBytes(Stream stream, byte[] bytes)
    {
        _ = bytes ?? throw new ArgumentNullException(nameof(bytes));
        using (stream)
            stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Asynchronously writes the specified bytes to the provided <see cref="Stream"/> and closes the stream.
    /// </summary>
    /// <param name="stream">The stream to which the bytes will be written. The stream is closed after writing.</param>
    /// <param name="bytes">The byte array to write to the stream.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private static async Task WriteAllBytesAsync(Stream stream, byte[] bytes, CancellationToken cancellationToken)
    {
        _ = bytes ?? throw new ArgumentNullException(nameof(bytes));
        using (stream)
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes all lines to the specified <see cref="StreamWriter"/> and closes the writer.
    /// </summary>
    /// <param name="writer">The <see cref="StreamWriter"/> used to write the lines. The writer is closed after writing.</param>
    /// <param name="contents">The lines to write to the stream.</param>
    private static void WriteAllLines(StreamWriter writer, IEnumerable<string> contents)
    {
        _ = contents ?? throw new ArgumentNullException(nameof(contents));
        using (writer)
        {
            foreach (string line in contents)
                writer.WriteLine(line);
        }
    }

    /// <summary>
    /// Asynchronously writes all lines to the specified <see cref="StreamWriter"/> and closes the writer.
    /// </summary>
    /// <param name="writer">The <see cref="StreamWriter"/> used to write the lines. The writer is closed after writing.</param>
    /// <param name="contents">The lines to write to the stream.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private static async Task WriteAllLinesAsync(StreamWriter writer, IEnumerable<string> contents, CancellationToken cancellationToken)
    {
        _ = contents ?? throw new ArgumentNullException(nameof(contents));
        using (writer)
        {
            foreach (string line in contents)
            {
#if NETSTANDARD2_0
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(line).ConfigureAwait(false);
#else
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
#endif
            }
        }
    }

    /// <summary>
    /// Writes the specified text to the provided <see cref="StreamWriter"/> and closes the writer.
    /// </summary>
    /// <param name="writer">The <see cref="StreamWriter"/> used to write the text. The writer is closed after writing.</param>
    /// <param name="contents">The text to write to the stream.</param>
    private static void WriteAllText(StreamWriter writer, string? contents)
    {
        using (writer)
            writer.Write(contents ?? string.Empty);
    }

    /// <summary>
    /// Asynchronously writes the specified text to the provided <see cref="StreamWriter"/> and closes the writer.
    /// </summary>
    /// <param name="writer">The <see cref="StreamWriter"/> used to write the text. The writer is closed after writing.</param>
    /// <param name="contents">The text to write to the stream. If <see langword="null"/>, an empty string is written.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private static async Task WriteAllTextAsync(StreamWriter writer, string? contents, CancellationToken cancellationToken)
    {
        using (writer)
        {
#if NETSTANDARD2_0
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteAsync(contents ?? string.Empty).ConfigureAwait(false);
#else
            await writer.WriteAsync((contents ?? string.Empty).AsMemory(), cancellationToken).ConfigureAwait(false);
#endif
        }
    }

    /// <summary>
    /// Determines the appropriate buffer size for copying data from the specified <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> for which the buffer size is being calculated.</param>
    /// <returns>The size of the buffer to use for copying data from the stream.</returns>
    private static int GetCopyBufferSize(Stream stream)
    {
        // See:
        // https://github.com/dotnet/runtime/blob/2979d1b8e2958b8e96302560faf6ad82ca859509/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs#L118

        // This value was originally picked to be the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo{Async} buffer is short-lived and is likely to be collected at Gen0, and it offers a significant improvement in Copy
        // performance. Since then, the base implementations of CopyTo{Async} have been updated to use ArrayPool, which will end up rounding
        // this size up to the next power of two (131,072), which will by default be on the large object heap. However, most of the time
        // the buffer should be pooled, the LOH threshold is now configurable and thus may be different than 85K, and there are measurable
        // benefits to using the larger buffer size. So, for now, this value remains.
        const int DefaultCopyBufferSize = 81920;

        if (!stream.CanSeek)
            return DefaultCopyBufferSize;

        // There are no bytes left in the stream to copy.
        // However, because CopyTo{Async} is virtual, we need to
        // ensure that any override is still invoked to provide its
        // own validation, so we use the smallest legal buffer size here.
        long length = stream.Length;
        long position = stream.Position;
        if (length <= position)
            return 1;

        long remaining = length - position;
        if (remaining > 0)
            return (int)Math.Min(DefaultCopyBufferSize, remaining);

        // In the case of a positive overflow, stick to the default size.
        return DefaultCopyBufferSize;
    }
}
