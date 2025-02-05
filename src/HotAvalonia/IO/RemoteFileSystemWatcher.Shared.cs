namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for monitoring file system changes remotely.
/// </summary>
partial class RemoteFileSystemWatcher
{
    /// <summary>
    /// Asynchronously reads a packet from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the packet from.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A freshly received packet.</returns>
    private static async Task<(ActionType Action, byte[] Data)> ReadPacketAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        const int bufferLength = sizeof(byte) + sizeof(int);
        byte[] buffer = new byte[bufferLength];
        await stream.ReadExactlyAsync(buffer, 0, bufferLength, cancellationToken).ConfigureAwait(false);

        ActionType action = (ActionType)buffer[0];
        int length = BitConverter.ToInt32(buffer.AsSpan(sizeof(byte), sizeof(int)));

        byte[] data = new byte[length];
        await stream.ReadExactlyAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);

        return (action, data);
    }

    /// <summary>
    /// Asynchronously writes a packet to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write the packet to.</param>
    /// <param name="action">The action type.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private static async Task WritePacketAsync(Stream stream, ActionType action, byte[] data, CancellationToken cancellationToken = default)
    {
        const int bufferLength = sizeof(byte) + sizeof(int);
        byte[] buffer = new byte[bufferLength];

        buffer[0] = (byte)action;
        BitConverter.TryWriteBytes(buffer.AsSpan(sizeof(byte), sizeof(int)), data.Length);

        await stream.WriteAsync(buffer, 0, bufferLength, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Represents the type of actions that can be sent or received in file system watcher operations.
    /// </summary>
    private enum ActionType : byte
    {
        /// <summary>
        /// Indicates that the connection should be kept alive.
        /// </summary>
        KeepAlive = 0,

        /// <summary>
        /// Sets the path of the directory to watch.
        /// </summary>
        SetPath,

        /// <summary>
        /// Sets a value indicating whether the component is enabled.
        /// </summary>
        SetEnableRaisingEvents,

        /// <summary>
        /// Sets a value indicating whether subdirectories within the specified path should be monitored.
        /// </summary>
        SetIncludeSubdirectories,

        /// <summary>
        /// Sets the filter string used to determine what files are monitored in a directory.
        /// </summary>
        SetFilter,

        /// <summary>
        /// Sets the type of changes to watch for.
        /// </summary>
        SetNotifyFilter,

        /// <summary>
        /// Occurs when a file or directory is created.
        /// </summary>
        RaiseCreated,

        /// <summary>
        /// Occurs when a file or directory is deleted.
        /// </summary>
        RaiseDeleted,

        /// <summary>
        /// Occurs when a file or directory is changed.
        /// </summary>
        RaiseChanged,

        /// <summary>
        /// Occurs when a file or directory is renamed.
        /// </summary>
        RaiseRenamed,

        /// <summary>
        /// Occurs when the file system watcher is unable to continue monitoring changes.
        /// </summary>
        RaiseError,
    }
}
