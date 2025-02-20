using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HotAvalonia.Helpers;

/// <summary>
/// Provides helper methods for network-related operations.
/// </summary>
internal static class NetworkHelper
{
    /// <summary>
    /// Attempts to retrieve the local IP address of the current machine.
    /// </summary>
    /// <returns>The <see cref="IPAddress"/> representing the local address if one is found; otherwise, <c>null</c>.</returns>
    public static IPAddress? GetLocalAddress()
    {
        try
        {
            // Since no actual connection is established, the address may (and in this case, probably will)
            // be completely unreachable. However, it should at least be valid, and it's also a good idea
            // to choose one from our preferred subnet (i.e., 192.168.0.0/16).
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Unspecified);
            socket.Connect("192.168.255.255", ushort.MaxValue);
            IPAddress? address = (socket.LocalEndPoint as IPEndPoint)?.Address;
            if (address is not null)
                return address;
        }
        catch { }

        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x is { NetworkInterfaceType: not NetworkInterfaceType.Loopback, OperationalStatus: OperationalStatus.Up })
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Where(x => x.Address.AddressFamily is AddressFamily.InterNetwork)
            .OrderByDescending(x => x.Address.GetAddressBytes()[0])
            .FirstOrDefault()?.Address;
    }

    /// <summary>
    /// Gets an available network port for the specified protocol.
    /// </summary>
    /// <param name="protocol">The protocol for which to obtain an available port.</param>
    /// <returns>An available port number.</returns>
    public static int GetAvailablePort(ProtocolType protocol)
    {
        SocketType socketType = protocol switch
        {
            ProtocolType.Tcp => SocketType.Stream,
            ProtocolType.Udp => SocketType.Dgram,
            _ => SocketType.Unknown,
        };

        try
        {
            // The TCP/UDP stack will allocate a new port for us if we set it to 0.
            using Socket? socket = socketType is SocketType.Unknown ? null : new(AddressFamily.InterNetwork, socketType, protocol);
            socket?.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            if (socket?.LocalEndPoint is IPEndPoint endpoint)
                return endpoint.Port;
        }
        catch { }

        // If something went wrong, just choose a random port from the range 49152-65535,
        // which represents private (or ephemeral) ports that cannot be registered with IANA,
        // and then pray that we get lucky.
        // Note, '65536' is not a typo, because the upper bound of `.Next()` is exclusive.
        return new Random().Next(49152, 65536);
    }
}
