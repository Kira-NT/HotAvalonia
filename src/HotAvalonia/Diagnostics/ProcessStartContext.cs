using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace HotAvalonia.Diagnostics;

/// <summary>
/// Provides functionality for starting and managing external processes,
/// with optional support for invoking managed assemblies via the .NET host.
/// </summary>
internal sealed partial class ProcessStartContext
{
    /// <summary>
    /// Gets or sets the path to the <c>dotnet</c> host executable.
    /// </summary>
    [AllowNull]
    public string DotnetPath
    {
        get => field is { Length: > 0 } ? field : (field = GetDotnetFileName());
        set => field = value;
    }

    /// <summary>
    /// Gets or sets the default timeout, in milliseconds, used when waiting for a process to exit.
    /// </summary>
    /// <remarks>
    /// A value of <c>-1</c> indicates an infinite timeout.
    /// </remarks>
    public int Timeout { get; set; } = -1;

    /// <summary>
    /// Starts a process, waits for it to exit, and captures its output.
    /// </summary>
    /// <param name="fileName">The name of the executable or managed assembly to run.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    /// <param name="timeout">
    /// An optional timeout, in milliseconds, to wait for process completion.
    /// If <c>null</c>, <see cref="Timeout"/> is used.
    /// </param>
    /// <returns>A tuple containing the process exit code, standard output, and standard error.</returns>
    public (int ExitCode, string Output, string Error) Run(string fileName, IEnumerable<string?>? arguments = null, IEnumerable<KeyValuePair<string, string?>>? environmentVariables = null, int? timeout = null)
    {
        Process? process = Start(fileName, arguments, environmentVariables);
        if (process is null)
            return (0, string.Empty, string.Empty);

        int exitCode = WaitForExit(process, timeout ?? Timeout, out string stdout, out string stderr);
        return (exitCode, stdout, stderr);
    }

    /// <summary>
    /// Starts a process and returns the associated <see cref="Process"/> instance.
    /// </summary>
    /// <param name="fileName">The name of the executable or managed assembly to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    /// <returns>The started <see cref="Process"/> instance.</returns>
    public Process? Start(string fileName, IEnumerable<string?>? arguments = null, IEnumerable<KeyValuePair<string, string?>>? environmentVariables = null)
    {
        (fileName, arguments) = UseDotnet(fileName, arguments ?? []);
        ProcessStartInfo info = CreateProcessStartInfo(fileName, arguments, environmentVariables ?? []);
        return Process.Start(info);
    }

    /// <summary>
    /// Starts a process in the background without waiting for its completion.
    /// </summary>
    /// <param name="fileName">The name of the executable or managed assembly to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    public void StartDaemon(string fileName, IEnumerable<string?>? arguments = null, IEnumerable<KeyValuePair<string, string?>>? environmentVariables = null)
    {
        (fileName, arguments) = UseDotnet(fileName, arguments ?? []);
        environmentVariables ??= [];
        if (Environment.OSVersion.Platform is PlatformID.Unix)
        {
            // OSVersion.Platform is a rather peculiar property, as it always returns either
            // Win32NT or Unix, at least in a .NET Standard 2.0-compliant environment, even
            // though other PlatformIDs exist (e.g., MacOSX, which was introduced solely for
            // Silverlight; however, as we all know, Silverlight is no more).
            //
            // Thus, while one should generally use the modern IsOSPlatform(...) method
            // to detect the current OS, OSVersion.Platform remains pretty useful when
            // you simply need to determine whether the operating system belongs to
            // the broad family of Unix-like systems, instead of checking for just one
            // or two very specific flavors.
            //
            // https://github.com/dotnet/runtime/issues/21660
            StartUnixBackgroundProcess(fileName, arguments, environmentVariables);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StartWindowsBackgroundProcess(fileName, arguments, environmentVariables);
        }
        else
        {
            // Who are you then?
            StartBackgroundProcess(fileName, arguments, environmentVariables);
        }
    }

    /// <summary>
    /// Starts a background process without waiting for its completion.
    /// </summary>
    /// <param name="fileName">The name of the executable to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    private static void StartBackgroundProcess(string fileName, IEnumerable<string?> arguments, IEnumerable<KeyValuePair<string, string?>> environmentVariables)
    {
        ProcessStartInfo info = CreateProcessStartInfo(fileName, arguments, environmentVariables);
        info.WindowStyle = ProcessWindowStyle.Hidden;
        info.RedirectStandardInput = info.RedirectStandardOutput = info.RedirectStandardError = false;
        _ = Process.Start(info);
    }

    /// <summary>
    /// Creates and configures a <see cref="ProcessStartInfo"/> instance.
    /// </summary>
    /// <param name="fileName">The name of the executable to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    /// <returns>A configured <see cref="ProcessStartInfo"/> instance.</returns>
    private static ProcessStartInfo CreateProcessStartInfo(string fileName, IEnumerable<string?> arguments, IEnumerable<KeyValuePair<string, string?>> environmentVariables)
    {
        ProcessStartInfo info = new(fileName, FormatArguments(arguments))
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (KeyValuePair<string, string?> environmentEntry in environmentVariables)
            info.Environment[environmentEntry.Key] = environmentEntry.Value;

        return info;
    }

    /// <summary>
    /// Determines whether the specified file should be executed
    /// via the .NET host and adjusts the command accordingly.
    /// </summary>
    /// <param name="fileName">The original file name.</param>
    /// <param name="args">The original argument sequence.</param>
    /// <returns>A tuple containing the resolved file name and updated argument sequence.</returns>
    private (string FileName, IEnumerable<string?> Arguments) UseDotnet(string fileName, IEnumerable<string?> args)
    {
        if (fileName.EndsWith(".dll"))
        {
            args = ["exec", fileName, .. args];
            fileName = DotnetPath;
        }
        return (fileName, args);
    }

    /// <summary>
    /// Gets the file name of the <c>dotnet</c> host executable.
    /// </summary>
    /// <returns>The file name of the <c>dotnet</c> host executable.</returns>
    private static string GetDotnetFileName()
    {
        string? fileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(fileName))
            return fileName;

        string? directoryName = GetDotnetDirectoryName();
        fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        return string.IsNullOrEmpty(directoryName) ? fileName : Path.Combine(directoryName, fileName);
    }

    /// <summary>
    /// Gets the directory containing the <c>dotnet</c> host.
    /// </summary>
    /// <returns>The directory path, or <c>null</c> if it could not be located.</returns>
    private static string? GetDotnetDirectoryName()
    {
        string? root = null;
        if (Environment.Is64BitProcess != Environment.Is64BitOperatingSystem)
        {
            root = Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)");
            root = string.IsNullOrEmpty(root) ? Environment.GetEnvironmentVariable("DOTNET_ROOT_X86") : root;
        }
        else if (RuntimeInformation.ProcessArchitecture is Architecture.Arm64)
        {
            root = Environment.GetEnvironmentVariable("DOTNET_ROOT_X64");
        }

        if (string.IsNullOrEmpty(root))
            root = Environment.GetEnvironmentVariable("DOTNET_ROOT");

        if (string.IsNullOrEmpty(root) && Environment.Is64BitProcess)
            root = Environment.GetEnvironmentVariable("DOTNET_ROOT_X64");

        return string.IsNullOrEmpty(root) ? null : root;
    }

    /// <summary>
    /// Waits for the specified process to exit.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">The timeout, in milliseconds, to wait for process completion.</param>
    /// <param name="stdout">When this method returns, contains the captured standard output.</param>
    /// <param name="stderr">When this method returns, contains the captured standard error.</param>
    /// <returns>The exit code of the process.</returns>
    private static int WaitForExit(Process process, int timeout, out string stdout, out string stderr)
    {
        StringBuilder? errorBuilder = null;
        if (process.StartInfo.RedirectStandardError)
        {
            errorBuilder = new();
            process.ErrorDataReceived += (s, e) => errorBuilder.AppendLine(e.Data);
            process.BeginErrorReadLine();
        }

        stdout = process.StartInfo.RedirectStandardOutput ? process.StandardOutput.ReadToEnd() : string.Empty;
        process.WaitForExit(timeout);
        stderr = errorBuilder?.ToString() ?? string.Empty;
        return process.ExitCode;
    }

    /// <summary>
    /// Formats a sequence of arguments into a command-line string.
    /// </summary>
    /// <param name="args">The arguments to format.</param>
    /// <returns>A properly escaped command-line argument string.</returns>
    private static string FormatArguments(IEnumerable<string?> args)
    {
        if (args is ICollection<string?> { Count: 0 })
            return string.Empty;

        StringBuilder sb = new(128);
        foreach (string? arg in args)
            AppendArgument(sb, arg);

        return sb.ToString();
    }

    /// <summary>
    /// Appends a single argument to a command-line builder, escaping it if needed.
    /// </summary>
    /// <param name="builder">The command-line builder.</param>
    /// <param name="argument">The argument to append.</param>
    private static void AppendArgument(StringBuilder builder, string? argument)
    {
        if (builder.Length > 0)
            builder.Append(' ');

        if (argument is not { Length: > 0 })
        {
            builder.Append('"').Append('"');
            return;
        }

        if (!RequiresEscaping(argument))
        {
            builder.Append(argument);
            return;
        }

        builder.Append('"');
        for (int i = 0; i < argument.Length;)
        {
            switch (argument[i++])
            {
                case '\\':
                    int backslashCount = 1;
                    while (i < argument.Length && argument[i] is '\\')
                    {
                        i++;
                        backslashCount++;
                    }

                    if (i == argument.Length)
                    {
                        builder.Append('\\', backslashCount * 2);
                    }
                    else if (argument[i] == '"')
                    {
                        builder.Append('\\', backslashCount * 2 + 1).Append('"');
                        i++;
                    }
                    else
                    {
                        builder.Append('\\', backslashCount);
                    }
                    break;

                case '"':
                    builder.Append('\\').Append('"');
                    break;

                case char c:
                    builder.Append(c);
                    break;
            }
        }
        builder.Append('"');
    }

    /// <summary>
    /// Determines whether an argument requires escaping when formatted for the command line.
    /// </summary>
    /// <param name="arg">The argument to evaluate.</param>
    /// <returns><c>true</c> if the argument requires escaping; otherwise, <c>false</c>.</returns>
    private static bool RequiresEscaping(string arg)
    {
        foreach (char c in arg)
        {
            if (char.IsWhiteSpace(c) || c is '"')
                return true;
        }
        return false;
    }
}
