using System.Buffers;
using System.Security.Cryptography;

namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for interacting with a remote file system over a network.
/// </summary>
partial class RemoteFileSystem
{
    /// <summary>
    /// Gets the hostname of the remote file system.
    /// </summary>
    public static string Name { get; } = $"{nameof(HotAvalonia)}-{nameof(RemoteFileSystem)}";

    /// <summary>
    /// Gets the version of the remote file system protocol.
    /// </summary>
    public static Guid Version { get; } = new Guid("{2A5D1629-9596-41BC-B1D2-C35DA1A350B8}");

    /// <summary>
    /// Asynchronously reads a packet from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the packet from.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A freshly received packet.</returns>
    private static async Task<(ushort Id, ActionType Action, byte[] Data)> ReadPacketAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        const int bufferLength = sizeof(ushort) + sizeof(byte) + sizeof(int);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
        await stream.ReadExactlyAsync(buffer, 0, bufferLength, cancellationToken).ConfigureAwait(false);

        ushort id = BitConverter.ToUInt16(buffer.AsSpan(0, sizeof(ushort)));
        ActionType action = (ActionType)buffer[sizeof(ushort)];
        int length = BitConverter.ToInt32(buffer.AsSpan(sizeof(ushort) + sizeof(byte), sizeof(int)));
        ArrayPool<byte>.Shared.Return(buffer);

        byte[] data = new byte[length];
        await stream.ReadExactlyAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);

        if (action is ActionType.ThrowException)
            throw new RemoteFileSystemException(id, data);

        return (id, action, data);
    }

    /// <summary>
    /// Asynchronously writes a packet to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write the packet to.</param>
    /// <param name="id">The packet ID.</param>
    /// <param name="action">The action type.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private static async Task WritePacketAsync(Stream stream, ushort id, ActionType action, byte[] data, CancellationToken cancellationToken = default)
    {
        const int bufferLength = sizeof(ushort) + sizeof(byte) + sizeof(int);
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferLength);
        Span<byte> buffer = rentedBuffer.AsSpan(0, bufferLength);

        BitConverter.TryWriteBytes(buffer.Slice(0, sizeof(ushort)), id);
        buffer[sizeof(ushort)] = (byte)action;
        BitConverter.TryWriteBytes(buffer.Slice(sizeof(ushort) + sizeof(byte)), data.Length);

        await stream.WriteAsync(rentedBuffer, 0, bufferLength, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);

        ArrayPool<byte>.Shared.Return(rentedBuffer);
    }

    /// <summary>
    /// Creates a handshake packet using the provided secret key.
    /// </summary>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="salt">The salt used for hashing the secret.</param>
    /// <returns>The handshake packet.</returns>
    private static byte[] CreateHandshakePacket(byte[] secret, byte[] salt)
    {
        using SHA256 hash = SHA256.Create();
        byte[] versionBytes = Version.ToByteArray();

        hash.TransformBlock(versionBytes, 0, versionBytes.Length, null, 0);
        hash.TransformBlock(secret, 0, secret.Length, null, 0);
        hash.TransformFinalBlock(salt, 0, salt.Length);
        return hash.Hash!;
    }

    /// <summary>
    /// Specifies actions that can be performed in the remote file system.
    /// </summary>
    private enum ActionType : byte
    {
        /// <summary>
        /// Indicates that the connection should be kept alive.
        /// </summary>
        KeepAlive = 0,

        /// <summary>
        /// Performs the initial handshake.
        /// </summary>
        PerformHandshake,

        /// <summary>
        /// Indicates that the client should shut down
        /// when the end of the stream is reached.
        /// </summary>
        ShutdownOnEndOfStream,

        /// <summary>
        /// Creates a file system watcher.
        /// </summary>
        CreateFileSystemWatcher,

        /// <summary>
        /// Gets the current state of the file system.
        /// </summary>
        GetFileSystemState,

        /// <summary>
        /// Determines whether the specified directory exists.
        /// </summary>
        DirectoryExists,

        /// <summary>
        /// Gets files in a directory.
        /// </summary>
        GetFiles,

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        FileExists,

        /// <summary>
        /// Gets the date and time, in Coordinated Universal Time (UTC),
        /// that the specified file or directory was last written to.
        /// </summary>
        GetLastWriteTimeUtc,

        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        OpenRead,

        /// <summary>
        /// Throws an exception.
        /// </summary>
        ThrowException = 255
    }
}
