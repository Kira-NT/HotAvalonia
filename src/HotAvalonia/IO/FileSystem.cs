using System.Reflection;
using System.Reflection.Emit;
using HotAvalonia.Helpers;

namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for working with the file system.
/// </summary>
public static class FileSystem
{
    /// <summary>
    /// Gets an empty, read-only file system.
    /// </summary>
    public static IFileSystem Empty { get; } = new EmptyFileSystem();

    /// <summary>
    /// Gets the current file system.
    /// </summary>
    public static IFileSystem Current { get; } = new LocalFileSystem();

    /// <summary>
    /// A factory function used to instantiate <see cref="FileSystemEventArgs"/> objects.
    /// </summary>
    private static readonly Func<WatcherChangeTypes, string, string, FileSystemEventArgs> s_fileSystemEventArgsFactory = CreateFileSystemEventArgsFactory();

    /// <summary>
    /// Creates a new instance of the <see cref="FileSystemEventArgs"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with the event.</param>
    /// <param name="changeType">One of the <see cref="WatcherChangeTypes"/> values, which represents the kind of change detected in the file system.</param>
    /// <param name="fullPath">The fully qualified path of the affected file or directory.</param>
    /// <returns>A new <see cref="FileSystemEventArgs"/> instance for the specified file system event.</returns>
    public static FileSystemEventArgs CreateFileSystemEventArgs(this IFileSystem fileSystem, WatcherChangeTypes changeType, string fullPath)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = fullPath ?? throw new ArgumentNullException(nameof(fullPath));

        string name = fileSystem.GetFileName(fullPath);
        return s_fileSystemEventArgsFactory(changeType, name, fullPath);
    }

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

        using IDisposable context = MethodHelper.DefineDynamicMethod("CreateFileSystemEventArgs", returnType, parameterTypes, out DynamicMethod method);

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

        return (Func<WatcherChangeTypes, string, string, FileSystemEventArgs>)method.CreateDelegate(typeof(Func<WatcherChangeTypes, string, string, FileSystemEventArgs>));
    }

    /// <summary>
    /// A factory function used to instantiate <see cref="RenamedEventArgs"/> objects.
    /// </summary>
    private static readonly Func<WatcherChangeTypes, string, string, string, string, RenamedEventArgs> s_renamedEventArgsFactory = CreateRenamedEventArgsFactory();

    /// <summary>
    /// Creates a new instance of the <see cref="RenamedEventArgs"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with the event.</param>
    /// <param name="changeType">One of the <see cref="WatcherChangeTypes"/> values, which represents the kind of change detected in the file system.</param>
    /// <param name="fullPath">The fully qualified path of the affected file or directory.</param>
    /// <param name="oldFullPath">The previous fully qualified path of the affected file or directory.</param>
    /// <returns>A new <see cref="RenamedEventArgs"/> instance for the specified file system event.</returns>
    public static RenamedEventArgs CreateFileSystemEventArgs(this IFileSystem fileSystem, WatcherChangeTypes changeType, string fullPath, string oldFullPath)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = fullPath ?? throw new ArgumentNullException(nameof(fullPath));

        string name = fileSystem.GetFileName(fullPath);
        string oldName = fileSystem.GetFileName(oldFullPath);
        return s_renamedEventArgsFactory(changeType, name, fullPath, oldName, oldFullPath);
    }

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

        using IDisposable context = MethodHelper.DefineDynamicMethod("CreateRenamedEventArgs", returnType, parameterTypes, out DynamicMethod method);

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

        return (Func<WatcherChangeTypes, string, string, string, string, RenamedEventArgs>)method.CreateDelegate(typeof(Func<WatcherChangeTypes, string, string, string, string, RenamedEventArgs>));
    }

    /// <summary>
    /// Extracts the volume name from the given path.
    /// </summary>
    /// <param name="fileSystem">The file system <paramref name="path"/> belongs to.</param>
    /// <param name="path">The file path from which to extract the volume name.</param>
    /// <returns>The volume name if it exists; otherwise, an empty string.</returns>
    public static string GetVolumeName(this IFileSystem fileSystem, string path)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = path ?? throw new ArgumentNullException(nameof(path));

        int i = path.IndexOf(fileSystem.VolumeSeparatorChar);
        return i <= 0 ? string.Empty : path.Substring(0, i);
    }

    /// <summary>
    /// Finds the common base path shared by two file paths.
    /// </summary>
    /// <param name="fileSystem">
    /// The file system <paramref name="leftPath"/> and <paramref name="rightPath"/> belong to.
    /// </param>
    /// <param name="leftPath">The first file path to compare.</param>
    /// <param name="rightPath">The second file path to compare.</param>
    /// <returns>
    /// The longest common base path shared by both file paths.
    /// If no common path exists, an empty string is returned.
    /// </returns>
    public static string GetCommonPath(this IFileSystem fileSystem, string leftPath, string rightPath)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        StringComparison comparison = fileSystem.PathComparison;
        char directorySeparatorChar = fileSystem.DirectorySeparatorChar;
        char altDirectorySeparatorChar = fileSystem.AltDirectorySeparatorChar;

        ReadOnlySpan<char> left = fileSystem.GetFullPath(leftPath).AsSpan();
        ReadOnlySpan<char> right = fileSystem.GetFullPath(rightPath).AsSpan();
        int length = 0;

        while (true)
        {
            ReadOnlySpan<char> remLeft = left.Slice(length);
            ReadOnlySpan<char> remRight = right.Slice(length);
            int hasSeparator = 1;

            int nextLeftLength = remLeft.IndexOfAny(directorySeparatorChar, altDirectorySeparatorChar);
            if (nextLeftLength < 0)
            {
                nextLeftLength = remLeft.Length;
                hasSeparator = 0;
            }

            int nextRightLength = remRight.IndexOfAny(directorySeparatorChar, altDirectorySeparatorChar);
            if (nextRightLength < 0)
            {
                nextRightLength = remRight.Length;
                hasSeparator = 0;
            }

            if (nextLeftLength != nextRightLength)
                break;

            ReadOnlySpan<char> nextLeft = left.Slice(length, nextLeftLength);
            ReadOnlySpan<char> nextRight = right.Slice(length, nextRightLength);
            if (!nextLeft.Equals(nextRight, comparison))
                break;

            length += nextLeftLength + hasSeparator;
            if (length >= left.Length || length >= right.Length)
                break;
        }

        return left.Slice(0, length).ToString();
    }

    /// <summary>
    /// Asynchronously opens an existing file for reading.
    /// </summary>
    /// <param name="fileSystem">The file system where <paramref name="path"/> can be located.</param>
    /// <param name="path">The file to be opened for reading.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A read-only <see cref="Stream"/> on the specified path.</returns>
    public static async Task<Stream> OpenReadAsync(this IFileSystem fileSystem, string path, TimeSpan pollingInterval = default, CancellationToken cancellationToken = default)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

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
                return await fileSystem.OpenReadAsync(path).ConfigureAwait(false);
            }
            catch (IOException)
            {
                continue;
            }
        }
    }

    /// <summary>
    /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
    /// </summary>
    /// <param name="fileSystem">The file system where <paramref name="path"/> can be located.</param>
    /// <param name="path">The file to open for reading.</param>
    /// <returns>A byte array containing the contents of the file.</returns>
    public static byte[] ReadAllBytes(this IFileSystem fileSystem, string path)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        using Stream stream = fileSystem.OpenRead(path);
        using MemoryStream reader = new(stream.CanSeek ? (int)stream.Length : 0);
        stream.CopyTo(reader);

        return reader.ToArray();
    }

    /// <inheritdoc cref="ReadAllBytesAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fileSystem, string path, CancellationToken cancellationToken = default)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        using Stream stream = await fileSystem.OpenReadAsync(path, cancellationToken);
        using MemoryStream reader = new(stream.CanSeek ? (int)stream.Length : 0);
        await stream.CopyToAsync(reader, GetCopyBufferSize(stream), cancellationToken).ConfigureAwait(false);

        return reader.ToArray();
    }

    /// <summary>
    /// Asynchronously opens a binary file, reads the contents of the file into a byte array, and then closes the file.
    /// </summary>
    /// <param name="fileSystem">The file system where <paramref name="path"/> can be located.</param>
    /// <param name="path">The file to open for reading.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A byte array containing the contents of the file.</returns>
    public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fileSystem, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        using Stream stream = await fileSystem.OpenReadAsync(path, pollingInterval, cancellationToken);
        using MemoryStream reader = new(stream.CanSeek ? (int)stream.Length : 0);
        await stream.CopyToAsync(reader, GetCopyBufferSize(stream), cancellationToken).ConfigureAwait(false);

        return reader.ToArray();
    }

    /// <summary>
    /// Opens a text file, reads all the text in the file into a string, and then closes the file.
    /// </summary>
    /// <param name="fileSystem">The file system where <paramref name="path"/> can be located.</param>
    /// <param name="path">The file to open for reading.</param>
    /// <returns>A string containing all the text in the file.</returns>
    public static string ReadAllText(this IFileSystem fileSystem, string path)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        using Stream stream = fileSystem.OpenRead(path);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <inheritdoc cref="ReadAllTextAsync(IFileSystem, string, TimeSpan, CancellationToken)"/>
    public static async Task<string> ReadAllTextAsync(this IFileSystem fileSystem, string path, CancellationToken cancellationToken = default)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        using Stream stream = await fileSystem.OpenReadAsync(path, cancellationToken).ConfigureAwait(false);
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously opens a text file, reads all the text in the file into a string, and then closes the file.
    /// </summary>
    /// <param name="fileSystem">The file system where <paramref name="path"/> can be located.</param>
    /// <param name="path">The file to open for reading.</param>
    /// <param name="pollingInterval">The delay between retry attempts in the event of an <see cref="IOException"/>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A string containing all the text in the file.</returns>
    public static async Task<string> ReadAllTextAsync(this IFileSystem fileSystem, string path, TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        using Stream stream = await fileSystem.OpenReadAsync(path, pollingInterval, cancellationToken).ConfigureAwait(false);
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
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
