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
    /// Gets the port number the client should connect to.
    /// </summary>
    [Output]
    public string? Port { get; private set; }

    /// <summary>
    /// Gets the secret used for authentication.
    /// </summary>
    [Output]
    public string? Secret { get; private set; }

    /// <inheritdoc/>
    protected override void ExecuteCore()
    {
        FileSystemServerConfig config = FileSystemServerConfig.Load(FileSystemServerConfigPath);
        if (!IPAddress.TryParse(Address, out IPAddress? ip))
        {
            ip = InterNetwork.GetLocalAddress();
            if (ip is null && !IPAddress.TryParse(FallbackAddress, out ip))
                ip = IPAddress.Loopback;
        }

        Address = ip.ToString();
        Port = config.Port.ToString();
        Secret = config.Secret ?? string.Empty;
    }
}
