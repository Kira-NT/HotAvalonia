using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;

namespace HotAvalonia;

/// <summary>
/// Starts the file system server process using the specified configuration.
/// </summary>
public sealed class StartFileSystemServerTask : MSBuildTask
{
    /// <summary>
    /// Gets or sets the file path to the file system server executable.
    /// </summary>
    [Required]
    public string FileSystemServerPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file path to the file system server configuration.
    /// </summary>
    [Required]
    public string FileSystemServerConfigPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path to the dotnet executable.
    /// If not provided, the task attempts to locate it automatically.
    /// </summary>
    public string? DotnetPath { get; set; }

    /// <inheritdoc/>
    protected override void ExecuteCore()
    {
        const string SecretEnvironmentVariableName = "HARFS_SECRET";

        string serverFullPath = Path.GetFullPath(FileSystemServerPath);
        FileSystemServerConfig config = FileSystemServerConfig.Load(FileSystemServerConfigPath);

        string runnerPath;
        StringBuilder arguments = new();
        if (serverFullPath.EndsWith(".dll"))
        {
            runnerPath = DotnetPath ?? GetDotnetFileName();
            arguments.Append("exec \"").Append(serverFullPath).Append("\" ");
        }
        else
        {
            runnerPath = serverFullPath;
        }
        arguments.Append(config.ToArguments(SecretEnvironmentVariableName));

        ProcessStartInfo processInfo = new(runnerPath)
        {
            Arguments = arguments.ToString(),
            Environment = { { SecretEnvironmentVariableName, config.Secret } },
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(processInfo);
    }

    /// <summary>
    /// Gets the file name of the dotnet executable.
    /// </summary>
    /// <returns>The file name of the dotnet executable.</returns>
    private static string GetDotnetFileName()
    {
        string fileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(fileName))
            return fileName;

        fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        string directoryName = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (string.IsNullOrEmpty(directoryName))
            directoryName = Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)");

        return string.IsNullOrEmpty(directoryName) ? fileName : Path.Combine(directoryName, fileName);
    }
}
