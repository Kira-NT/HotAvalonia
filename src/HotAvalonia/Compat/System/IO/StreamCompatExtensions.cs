namespace System.IO;

/// <summary>
/// Provides extension methods for <see cref="Stream"/>
/// to backport functionality from newer .NET versions.
/// </summary>
internal static class StreamCompatExtensions
{
    /// <summary>
    /// Asynchronously reads <paramref name="count"/> number of bytes from the given stream,
    /// advances the position within the stream, and monitors cancellation requests.
    /// </summary>
    /// <param name="stream">The stream to read the bytes from.</param>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The byte offset in <paramref name="buffer"/> at which to begin writing data from the stream.</param>
    /// <param name="count">The number of bytes to be read from the current stream.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before reading count number of bytes.</exception>
    public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException();

            totalRead += read;
        }
    }
}
