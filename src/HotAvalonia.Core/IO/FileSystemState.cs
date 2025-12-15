using System.Runtime.InteropServices;
using System.Text;

namespace HotAvalonia.IO;

/// <summary>
/// Represents the state of the file system.
/// </summary>
internal sealed class FileSystemState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemState"/> class with specified file system properties.
    /// </summary>
    /// <param name="pathComparison">The string comparison type for path operations.</param>
    /// <param name="directorySeparatorChar">The primary directory separator character.</param>
    /// <param name="altDirectorySeparatorChar">The alternative directory separator character.</param>
    /// <param name="volumeSeparatorChar">The character used to separate volume or drive information.</param>
    /// <param name="currentDirectory">The current working directory.</param>
    public FileSystemState(StringComparison pathComparison, char directorySeparatorChar, char altDirectorySeparatorChar, char volumeSeparatorChar, string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(currentDirectory);

        PathComparison = pathComparison;
        DirectorySeparatorChar = directorySeparatorChar;
        AltDirectorySeparatorChar = altDirectorySeparatorChar;
        VolumeSeparatorChar = volumeSeparatorChar;
        CurrentDirectory = currentDirectory;
    }

#pragma warning disable RS0030 // Do not use banned APIs
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemState"/> class
    /// with default values derived from the current environment and platform settings.
    /// </summary>
    private FileSystemState()
    {
        PathComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.CurrentCultureIgnoreCase
            : StringComparison.CurrentCulture;

        DirectorySeparatorChar = Path.DirectorySeparatorChar;
        AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
        VolumeSeparatorChar = Path.VolumeSeparatorChar;
        CurrentDirectory = Environment.CurrentDirectory;
    }
#pragma warning restore RS0030 // Do not use banned APIs

    /// <summary>
    /// Gets the current file system state.
    /// </summary>
    public static FileSystemState Current => new();

    /// <summary>
    /// Gets the <see cref="StringComparer"/> used to compare file or directory paths.
    /// </summary>
    public StringComparer PathComparer => PathComparison switch
    {
        StringComparison.CurrentCulture => StringComparer.CurrentCulture,
        StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
        StringComparison.InvariantCulture => StringComparer.InvariantCulture,
        StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
        StringComparison.Ordinal => StringComparer.Ordinal,
        StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
        _ => throw new ArgumentOutOfRangeException(nameof(PathComparison)),
    };

    /// <summary>
    /// Gets the <see cref="StringComparison"/> mode used for path comparisons.
    /// </summary>
    public StringComparison PathComparison { get; }

    /// <summary>
    /// Gets a platform-specific character used to separate directory levels
    /// in a path string that reflects a hierarchical file system organization.
    /// </summary>
    public char DirectorySeparatorChar { get; }

    /// <summary>
    /// Gets a platform-specific alternate character used to separate directory levels
    /// in a path string that reflects a hierarchical file system organization.
    /// </summary>
    public char AltDirectorySeparatorChar { get; }

    /// <summary>
    /// Gets a platform-specific volume separator character.
    /// </summary>
    public char VolumeSeparatorChar { get; }

    /// <summary>
    /// Gets the fully qualified path of the current working directory.
    /// </summary>
    public string CurrentDirectory { get; }

    /// <summary>
    /// Serializes the current <see cref="FileSystemState"/> instance to a byte array.
    /// </summary>
    /// <returns>A byte array representing the serialized state of this instance.</returns>
    public byte[] ToByteArray()
    {
        int currentDirectoryByteCount = Encoding.UTF8.GetByteCount(CurrentDirectory);
        int byteCount = 2 * sizeof(int) + 3 * sizeof(char) + currentDirectoryByteCount;
        byte[] bytes = new byte[byteCount];

        BitConverter.TryWriteBytes(bytes.AsSpan(0, sizeof(int)), (int)PathComparison);
        BitConverter.TryWriteBytes(bytes.AsSpan(sizeof(int), sizeof(char)), DirectorySeparatorChar);
        BitConverter.TryWriteBytes(bytes.AsSpan(sizeof(int) + 1 * sizeof(char), sizeof(char)), AltDirectorySeparatorChar);
        BitConverter.TryWriteBytes(bytes.AsSpan(sizeof(int) + 2 * sizeof(char), sizeof(char)), VolumeSeparatorChar);
        BitConverter.TryWriteBytes(bytes.AsSpan(sizeof(int) + 3 * sizeof(char), sizeof(int)), currentDirectoryByteCount);
        Encoding.UTF8.GetBytes(CurrentDirectory, 0, CurrentDirectory.Length, bytes, bytes.Length - currentDirectoryByteCount);
        return bytes;
    }

    /// <summary>
    /// Creates an instance of <see cref="FileSystemState"/> from a serialized byte array.
    /// </summary>
    /// <param name="bytes">The byte array representing the serialized file system state.</param>
    /// <returns>A <see cref="FileSystemState"/> instance with properties derived from the byte array.</returns>
    public static FileSystemState FromByteArray(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        int minByteCount = 2 * sizeof(int) + 3 * sizeof(char);
        if (bytes.Length < minByteCount)
            throw new ArgumentException($"Byte array must be at least {minByteCount} bytes long.", nameof(bytes));

        StringComparison pathComparison = (StringComparison)BitConverter.ToInt32(bytes.AsSpan(0, sizeof(int)));
        char directorySeparatorChar = BitConverter.ToChar(bytes.AsSpan(sizeof(int), sizeof(char)));
        char altDirectorySeparatorChar = BitConverter.ToChar(bytes.AsSpan(sizeof(int) + sizeof(char), sizeof(char)));
        char volumeSeparatorChar = BitConverter.ToChar(bytes.AsSpan(sizeof(int) + 2 * sizeof(char), sizeof(char)));
        int currentDirectoryByteCount = BitConverter.ToChar(bytes.AsSpan(sizeof(int) + 3 * sizeof(char), sizeof(int)));

        int expectedByteCount = minByteCount + currentDirectoryByteCount;
        if (bytes.Length < expectedByteCount)
            throw new ArgumentException($"Byte array must be exactly {expectedByteCount} bytes long.", nameof(bytes));

        string currentDirectory = Encoding.UTF8.GetString(bytes, bytes.Length - currentDirectoryByteCount, currentDirectoryByteCount);
        return new(pathComparison, directorySeparatorChar, altDirectorySeparatorChar, volumeSeparatorChar, currentDirectory);
    }
}
