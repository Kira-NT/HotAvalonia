using System.Xml.Serialization;

namespace HotAvalonia;

/// <summary>
/// Represents the configuration settings for a file system server.
/// </summary>
public sealed class FileSystemServerConfig
{
    /// <summary>
    /// Gets or sets the root directory for the file system server.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Gets or sets the secret used for authentication.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the network address on which the file system server listens.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the port number on which the file system server listens.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the path to the certificate used for secure communications.
    /// </summary>
    public string? Certificate { get; set; }

    /// <summary>
    /// Gets or sets the maximum search depth for directory file searches.
    /// </summary>
    public int MaxSearchDepth { get; set; }

    /// <summary>
    /// Gets or sets the timeout duration in milliseconds before the server shuts down
    /// if no clients have connected during the provided time frame.
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server should accept shutdown requests from clients.
    /// </summary>
    public bool AllowShutDownRequests { get; set; }

    /// <summary>
    /// Converts the configuration into a list of command-line arguments.
    /// </summary>
    /// <param name="secretName">
    /// An optional secret name used to reference the secret via an environment variable.
    /// If not provided, the secret is included inline using the prefix <c>text:</c>;
    /// otherwise, it is referenced using the given name and prefix <c>env:</c>.
    /// </param>
    /// <returns>A list containing the command-line arguments representing the current configuration.</returns>
    public List<string> ToArguments(string? secretName = null)
    {
        List<string> arguments = new();

        if (Root is { Length: > 0 })
        {
            arguments.Add("--root");
            arguments.Add(Path.GetFullPath(Root));
        }

        if (Secret is { Length: > 0 })
        {
            arguments.Add("--secret");
            arguments.Add(secretName is { Length: > 0 } ? $"env:base64:{secretName}" : $"text:base64:{Secret}");
        }

        if (Address is { Length: > 0 })
        {
            arguments.Add("--address");
            arguments.Add(Address);
        }

        if (Port > 0)
        {
            arguments.Add("--port");
            arguments.Add($"{Port}");
        }

        if (Certificate is { Length: > 0 })
        {
            arguments.Add("--certificate");
            arguments.Add(Path.GetFullPath(Certificate));
        }

        if (MaxSearchDepth > 0)
        {
            arguments.Add("--max-search-depth");
            arguments.Add($"{MaxSearchDepth}");
        }

        if (Timeout > 0)
        {
            arguments.Add("--timeout");
            arguments.Add($"{Timeout}");
        }

        if (AllowShutDownRequests)
            arguments.Add("--allow-shutdown-requests");

        return arguments;
    }

    /// <summary>
    /// Saves the current configuration settings to the specified file.
    /// </summary>
    /// <param name="path">The file path where the configuration will be saved.</param>
    public void Save(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? directoryName = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directoryName))
            Directory.CreateDirectory(directoryName);

        using FileStream file = File.Open(fullPath, FileMode.Create);
        XmlSerializer serializer = new(typeof(FileSystemServerConfig));
        serializer.Serialize(file, this);
    }

    /// <summary>
    /// Loads configuration settings from the specified file.
    /// </summary>
    /// <param name="path">The file path from which to load the configuration.</param>
    /// <returns>A <see cref="FileSystemServerConfig"/> instance populated with the settings from the file.</returns>
    public static FileSystemServerConfig Load(string path)
    {
        using FileStream file = File.OpenRead(path);
        XmlSerializer serializer = new(typeof(FileSystemServerConfig));
        return (FileSystemServerConfig)serializer.Deserialize(file);
    }
}
