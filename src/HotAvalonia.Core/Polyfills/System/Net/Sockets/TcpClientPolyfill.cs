#if !NET5_0_OR_GREATER
namespace System.Net.Sockets;

internal static class TcpClientPolyfill
{
    extension(TcpClient client)
    {
        public ValueTask ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
            => new(client.ConnectAsync(address, port).WithCancellation(cancellationToken));
    }
}
#endif
