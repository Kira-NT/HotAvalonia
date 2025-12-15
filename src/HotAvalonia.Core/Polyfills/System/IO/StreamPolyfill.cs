#if !NET7_0_OR_GREATER
namespace System.IO;

internal static class StreamPolyfill
{
    extension(Stream stream)
    {
        public async Task ReadExactlyAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    EndOfStreamException.Throw();

                totalRead += read;
            }
        }
    }
}
#endif
