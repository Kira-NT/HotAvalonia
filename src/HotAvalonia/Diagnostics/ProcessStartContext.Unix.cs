using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HotAvalonia.Diagnostics;

internal sealed partial class ProcessStartContext
{
    /// <summary>
    /// Starts a background process on Unix-like platforms without waiting for its completion.
    /// </summary>
    /// <param name="fileName">The name of the executable to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    private void StartUnixBackgroundProcess(string fileName, IEnumerable<string?> arguments, IEnumerable<KeyValuePair<string, string?>> environmentVariables)
    {
        // To start a background process on a Unix system, we generate a simple shell script:
        //
        // nohup "$__NH_ARG0" "$__NH_ARG1" ... "$__NH_ARGN" &
        //
        // Where __NH_ARGX is a positional argument passed via an environment variable, so we
        // do not have to deal with escaping it according to the rules of a potentially
        // unknown shell. This approach is completely safe, since the only character forbidden
        // in environment variables is '\0'. And now guess which character is forbidden in
        // arguments as well, as it denotes the end of a null-terminated string?
        //
        // The script is then executed by the system's default shell,
        // which is conventionally required to be POSIX-compliant.
        //
        // Also, since nohup starting our program must be immediately moved to the background,
        // allowing the shell to exit, we verify that this has in fact happened in order to
        // ensure that the operation has succeeded.
        ProcessStartInfo info = CreateUnixBackgroundProcessStartInfo(fileName, arguments, environmentVariables);
        Process proxyProcess = Process.Start(info) ?? throw new PlatformNotSupportedException();
        int exitCode = WaitForExit(proxyProcess, Timeout, out string output, out string error);
        if (exitCode != 0)
            throw new ExternalException((string.IsNullOrWhiteSpace(error) ? output : error).Trim(), exitCode);
    }

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> configured to start
    /// a background process on Unix-like platforms.
    /// </summary>
    /// <param name="fileName">The name of the executable to start.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="environmentVariables">The environment variables to apply to the process.</param>
    /// <returns>A configured <see cref="ProcessStartInfo"/> instance.</returns>
    private static ProcessStartInfo CreateUnixBackgroundProcessStartInfo(string fileName, IEnumerable<string?> arguments, IEnumerable<KeyValuePair<string, string?>> environmentVariables)
    {
        ProcessStartInfo info = CreateProcessStartInfo("/bin/sh", [], environmentVariables);

        StringBuilder argumentBuilder = new(128);
        argumentBuilder.Append("-c \"nohup ");
        int argIndex = 0;
        foreach (string? arg in new[] { fileName }.Concat(arguments))
        {
            string argName = $"__NH_ARG{argIndex++}";
            argumentBuilder.AppendFormat(@"\""${0}\"" ", argName);
            info.Environment[argName] = arg;
        }
        argumentBuilder.Append(">/dev/null 0>&1 2>&1 &\"");
        info.Arguments = argumentBuilder.ToString();

        return info;
    }
}
