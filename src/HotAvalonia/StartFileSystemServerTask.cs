using System.Runtime.InteropServices;
using HotAvalonia.Diagnostics;
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
        string serverFullPath = Path.GetFullPath(FileSystemServerPath);
        FileSystemServerConfig config = FileSystemServerConfig.Load(FileSystemServerConfigPath);
        ProcessStartContext processContext = new() { DotnetPath = DotnetPath, Timeout = 5000 };

        (int exitCode, string serverVersionOutput, string serverError) = processContext.Run(serverFullPath, ["--version"]);
        Version serverVersion = Version.TryParse(serverVersionOutput.Trim(), out serverVersion) ? serverVersion : new(0, 0, 0);
        Version taskVersion = GetType().Assembly.GetName()?.Version ?? new(0, 0, 0);
        if (serverVersion.ToString(3) != taskVersion.ToString(3))
        {
            ExternalException? innerException = exitCode == 0 ? null : new(serverError.Trim(), exitCode);
            throw new InvalidOperationException($"Incompatible server version. Expected '{taskVersion}', got '{serverVersion}'. ('{serverFullPath}')", innerException);
        }

        string secretEnvironmentVariableName = "HARFS_SECRET";
        List<string> args = config.ToArguments(secretEnvironmentVariableName);
        Dictionary<string, string?> vars = new() { { secretEnvironmentVariableName, config.Secret } };
        processContext.StartDaemon(serverFullPath, args, vars);
    }
}
