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
}
