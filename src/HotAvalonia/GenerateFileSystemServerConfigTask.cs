using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using HotAvalonia.Net;
using Microsoft.Build.Framework;

namespace HotAvalonia;

/// <summary>
/// Generates a file system server configuration file.
/// </summary>
public sealed class GenerateFileSystemServerConfigTask : MSBuildTask
{
    /// <summary>
    /// Gets or sets the root directory for the file system server.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Gets or sets the fallback root directory to use if <see cref="Root"/> does not refer to an existing directory.
    /// </summary>
    public string? FallbackRoot { get; set; }

    /// <summary>
    /// Gets or sets the secret used for authentication.
    /// If not provided, a random secret may be generated or <see cref="SecretUtf8"/> is used.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the secret used for authentication in UTF-8 format.
    /// If provided, it is converted to a Base64 string when <see cref="Secret"/> is not specified.
    /// </summary>
    public string? SecretUtf8 { get; set; }

    /// <summary>
    /// Gets or sets the network address on which the file system server listens.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the port number on which the file system server listens.
    /// If not specified, a new port is allocated automatically.
    /// </summary>
    public string? Port { get; set; }

    /// <summary>
    /// Gets or sets the path to the certificate used for secure communications.
    /// </summary>
    public string? Certificate { get; set; }

    /// <summary>
    /// Gets or sets the maximum search depth for directory file searches.
    /// </summary>
    public string? MaxSearchDepth { get; set; }

    /// <summary>
    /// Gets or sets the timeout duration in milliseconds before the server shuts down
    /// if no clients have connected during the provided time frame.
    /// </summary>
    public string? Timeout { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server should accept shutdown requests from clients.
    /// </summary>
    public string? AllowShutDownRequests { get; set; }

    /// <summary>
    /// Gets or sets the file path where the generated configuration file will be saved.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = null!;

    /// <inheritdoc/>
    protected override void ExecuteCore()
    {
        FileSystemServerConfig config = new()
        {
            Root = !string.IsNullOrEmpty(Root) && Directory.Exists(Root) ? Path.GetFullPath(Root) : FindRoot(FallbackRoot),
            Secret = string.IsNullOrEmpty(Secret) ? Convert.ToBase64String(string.IsNullOrEmpty(SecretUtf8) ? GenerateSecret() : Encoding.UTF8.GetBytes(SecretUtf8)) : Secret,
            Address = Address,
            Port = int.TryParse(Port, out int port) && port is > 0 and <= ushort.MaxValue ? port : InterNetwork.GetAvailablePort(ProtocolType.Tcp),
            Certificate = !string.IsNullOrEmpty(Certificate) && File.Exists(Certificate) ? Path.GetFullPath(Certificate) : null,
            MaxSearchDepth = int.TryParse(MaxSearchDepth, out int maxSearchDepth) ? maxSearchDepth : 0,
            Timeout = int.TryParse(Timeout, out int timeout) ? timeout : 0,
            AllowShutDownRequests = bool.TryParse(AllowShutDownRequests, out bool allowShutDownRequests) && allowShutDownRequests,
        };

        config.Save(OutputPath);
    }

    /// <summary>
    /// Generates a random secret as a byte array.
    /// </summary>
    /// <returns>A byte array containing the generated secret.</returns>
    private static byte[] GenerateSecret()
    {
        const int MinByteCount = 32;
        const int MaxByteCount = 64;

        int byteCount = new Random().Next(MinByteCount, MaxByteCount);
        byte[] bytes = new byte[byteCount];
        using RNGCryptoServiceProvider rng = new();
        rng.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// Finds the server root directory by searching upward from the specified candidate directory
    /// until a directory containing a solution file (*.sln) is found.
    /// </summary>
    /// <param name="rootDirectoryCandidate">The candidate directory to start the search.</param>
    /// <returns>
    /// The root directory if found; otherwise, if <paramref name="rootDirectoryCandidate"/> exists,
    /// it is returned; or <c>null</c> if no suitable root is found.
    /// </returns>
    private static string? FindRoot(string? rootDirectoryCandidate)
    {
        if (rootDirectoryCandidate is not null)
            rootDirectoryCandidate = Path.GetFullPath(rootDirectoryCandidate);

        string? currentDirectory = rootDirectoryCandidate;
        while (currentDirectory is { Length: > 0 })
        {
            try
            {
                bool hasSolution = Directory.EnumerateFiles(currentDirectory, "*.sln", SearchOption.TopDirectoryOnly).Any();
                if (hasSolution)
                    return currentDirectory;
            }
            catch
            {
                // Ignore directories we don't have access to.
            }
            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        return !string.IsNullOrEmpty(rootDirectoryCandidate) && Directory.Exists(rootDirectoryCandidate) ? rootDirectoryCandidate : null;
    }
}
