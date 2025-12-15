using System.Text;

namespace HotAvalonia.IO;

/// <summary>
/// Represents an exception that occurs during remote file system operations.
/// </summary>
internal sealed class RemoteFileSystemException : Exception
{
    /// <summary>
    /// Gets the unique identifier associated with the exception.
    /// </summary>
    public ushort Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystemException"/> class
    /// with the specified identifier and exception data in byte array format.
    /// </summary>
    /// <param name="id">The unique identifier for the exception.</param>
    /// <param name="value">A byte array representing the serialized exception details.</param>
    public RemoteFileSystemException(ushort id, byte[] value)
        : this(id, ToException(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystemException"/> class
    /// with the specified identifier and an underlying exception.
    /// </summary>
    /// <param name="id">The unique identifier for the exception.</param>
    /// <param name="exception">The underlying exception instance.</param>
    public RemoteFileSystemException(ushort id, Exception exception)
        : base(exception.Message, exception)
    {
        Id = id;
    }

    /// <summary>
    /// Serializes the provided <see cref="Exception"/> instance into a byte array.
    /// </summary>
    /// <param name="exception">The exception to serialize.</param>
    /// <returns>A byte array representing the serialized exception.</returns>
    internal static byte[] GetBytes(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        exception = (exception as RemoteFileSystemException)?.InnerException ?? exception;
        Type exceptionType = string.IsNullOrEmpty(exception.GetType().FullName) ? typeof(Exception) : exception.GetType();
        string typeName = $"{exceptionType.FullName}, {exceptionType.Assembly.GetName()?.Name}";
        string message = exception.Message ?? string.Empty;

        int typeNameByteCount = Encoding.UTF8.GetByteCount(typeName);
        int messageByteCount = Encoding.UTF8.GetByteCount(message);
        int byteCount = sizeof(int) + typeNameByteCount + messageByteCount;
        byte[] buffer = new byte[byteCount];

        BitConverter.TryWriteBytes(buffer.AsSpan(0, sizeof(int)), typeNameByteCount);
        Encoding.UTF8.GetBytes(typeName, 0, typeName.Length, buffer, sizeof(int));
        Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, sizeof(int) + typeNameByteCount);
        return buffer;
    }

    /// <summary>
    /// Converts a serialized exception represented as a byte array back to an <see cref="Exception"/> instance.
    /// </summary>
    /// <param name="value">The byte array representing the serialized exception.</param>
    /// <returns>An <see cref="Exception"/> instance based on the byte array content.</returns>
    internal static Exception ToException(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        int typeNameByteCount = BitConverter.ToInt32(value.AsSpan(0, sizeof(int)));
        int messageByteCount = value.Length - sizeof(int) - typeNameByteCount;
        string typeName = Encoding.UTF8.GetString(value, sizeof(int), typeNameByteCount);
        string message = Encoding.UTF8.GetString(value, sizeof(int) + typeNameByteCount, messageByteCount);

        Exception? exception = null;
#if !NATIVE_AOT
        try
        {
            Type? exceptionType = Type.GetType(typeName);
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                exceptionType = typeof(Exception);

            exception = (Exception?)Activator.CreateInstance(exceptionType, message);
        }
        catch { }
#endif

        exception ??= new(message);
        return exception;
    }
}
