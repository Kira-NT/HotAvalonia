using System.Net;
using HotAvalonia.Net;
using Microsoft.Build.Framework;

namespace HotAvalonia;

/// <summary>
/// Retrieves file system client configuration settings from an existing server configuration file.
/// </summary>
public sealed class GetFileSystemClientConfigTask : MSBuildTask
{
    /// <summary>
    /// Gets or sets the file path to the file system server configuration.
    /// </summary>
    [Required]
    public string FileSystemServerConfigPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the network address the client should connect to.
    /// </summary>
    [Output]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the fallback network address used if <see cref="Address"/>
    /// is not set and cannot be determined automatically.
    /// </summary>
    public string? FallbackAddress { get; set; }

    /// <summary>
    /// Gets the secret used for authentication.
    /// </summary>
    [Output]
    public string? Secret { get; private set; }

    /// <inheritdoc/>
    protected override void ExecuteCore()
    {
        FileSystemServerConfig config = FileSystemServerConfig.Load(FileSystemServerConfigPath);
        int port = config.Port;
        string ip = Address ?? string.Empty;
        if (!IPAddress.TryParse(ip, out _))
        {
            IPAddress? ipAddress = InterNetwork.GetLocalAddress();
            if (ipAddress is null && !IPAddress.TryParse(FallbackAddress ?? string.Empty, out ipAddress))
                ipAddress = IPAddress.Loopback;

            ip = ipAddress.ToString();
        }

        Address = ip.IndexOf(':') >= 0 ? $"[{ip}]:{port}" : $"{ip}:{port}";
        Secret = config.Secret ?? string.Empty;
    }
}
