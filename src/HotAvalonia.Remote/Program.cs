using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HotAvalonia.IO;

namespace HotAvalonia.Remote;

/// <summary>
/// Provides the entry point for <c>HotAvalonia.Remote</c> and orchestrates its execution.
/// </summary>
public sealed class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns><c>0</c> if the application ran successfully; otherwise, <c>1</c>.</returns>
    internal static async Task<int> Main(string[] args)
    {
        using CancellationTokenSource cancellationTokenSource = new();
        using PosixSignalRegistration sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, _ => cancellationTokenSource.Cancel());
        using PosixSignalRegistration sigquit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, _ => cancellationTokenSource.Cancel());
        using PosixSignalRegistration? sigterm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => cancellationTokenSource.Cancel());
        return await RunAsync(args, cancellationTokenSource.Token) ? 0 : 1;
    }

    /// <summary>
    /// Asynchronously runs the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the application ran successfully; otherwise, <c>false</c>.</returns>
    public static async Task<bool> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (!ProcessArguments(args, out RemoteFileSystem? remoteFileSystem, out int timeout))
        {
            Error();
            Error(Help);
            return false;
        }

        if (remoteFileSystem is null)
            return true;

        try
        {
            using CancellationTokenSource? cancellationTokenSource = timeout > 0 ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : null;
            using Timer? timer = timeout > 0 ? new(_ => (remoteFileSystem.ClientCount == 0 ? cancellationTokenSource : null)?.Cancel(), null, timeout, timeout) : null;
            cancellationToken = cancellationTokenSource?.Token ?? cancellationToken;

            using RemoteFileSystem fileSystem = remoteFileSystem;
            await fileSystem.RunAsync(cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            Error(e.Message);
            return false;
        }
    }

    /// <summary>
    /// Gets the default root directory for the remote file system, which is the current directory.
    /// </summary>
    public static string DefaultRoot => Environment.CurrentDirectory;

    /// <summary>
    /// Gets the default endpoint on which the remote file system listens for incoming connections.
    /// </summary>
    public static IPEndPoint DefaultEndpoint => new(IPAddress.Any, 20158);

    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public static string Name
    {
        get
        {
            Assembly assembly = typeof(Program).Assembly;
            string assemblyName = assembly.GetName().Name ?? string.Empty;
            if (assembly != Assembly.GetEntryAssembly())
                return assemblyName;

            string assemblyFileName = Path.GetFileName(assembly.Location);
            string entryFileName = Path.GetFileName(Environment.GetCommandLineArgs().FirstOrDefault() ?? string.Empty);
            return entryFileName == assemblyFileName ? $"dotnet {entryFileName} --" : entryFileName;
        }
    }

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    public static string Version => typeof(Program).Assembly.GetName().Version?.ToString(3) ?? string.Empty;

    /// <summary>
    /// Gets the help text for the application.
    /// </summary>
    public static string Help => $"""
        Usage: {Name} [options]

        Run a secure remote file system server compatible with HotAvalonia.

        Examples:
          {Name} --root "C:/Files" --secret text:MySecret --address 192.168.1.100 --port 8080
          {Name} -r "/home/user/files" -s env:MY_SECRET -e 0.0.0.0:20158 --allow-shutdown-requests

        Options:
          -h, --help
              Displays this help page.

          -v, --version
              Displays the application version.

          -r, --root <root>
              Specifies the root directory for the remote file system.
              The value must be a valid URI.
              Default: The current working directory.

          -s, --secret <secret>
              Specifies the secret used for authenticating connections.
              The secret can be provided in several formats:
                • text:<secret>
                    Provide the secret as plain text (UTF-8 encoded).
                • text:utf8:<secret>
                    Provide the secret as plain text (UTF-8 encoded).
                • text:base64:<base64secret>
                    Provide the secret as a Base64-encoded string.
                • env:<env-var>
                    Read the secret from the specified environment variable as plain text.
                • env:utf8:<env-var>
                    Read the secret from the specified environment variable as plain text.
                • env:base64:<env-var>
                    Read the secret from the specified environment variable as a Base64-encoded string.
                • file:<path>
                    Read the secret from the file at the specified path.
                • stdin
                    Read the secret from standard input as plain text.
                • stdin:utf8
                    Read the secret from standard input as plain text.
                • stdin:base64
                    Read the secret from standard input as a Base64-encoded string.

          -a, --address <address>
              Specifies the IP address on which the server listens.
              Default: All available network interfaces.

          -p, --port <port>
              Specifies the port number on which the server listens.
              The port must be a positive integer between 1 and 65535.
              Default: 20158.

          -e, --endpoint <endpoint>
              Specifies the complete endpoint (IP address and port) for the server in the format "IP:port".
              This option overrides the individual --address and --port settings.

          -c, --certificate <path>
              Specifies the path to the X.509 certificate file used for securing connections.
              If provided, the server will use the certificate to establish SSL/TLS communication.

          -d, --max-search-depth <depth>
              Specifies the maximum search depth for file searches.
              A positive value limits the number of file paths returned.
              A value of 0 or less indicates no limit.
              Default: 0.

          -t, --timeout <timeout>
              Specifies the timeout duration in milliseconds before the server shuts down
              if no clients have connected during the provided time frame.
              A positive value sets the timeout period.
              A value of 0 or less indicates no timeout.
              Default: 0.

          --allow-shutdown-requests
              When specified, allows the server to accept shutdown requests from clients.
        """;

    /// <summary>
    /// Writes an informational message to the standard output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <returns>Always returns <c>true</c>.</returns>
    private static bool Info(string? message = null)
    {
        Console.Out.WriteLine(message);
        return true;
    }

    /// <summary>
    /// Writes an error message to the standard error output.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    /// <returns>Always returns <c>false</c>.</returns>
    private static bool Error(string? message = null)
    {
        Console.Error.WriteLine(message);
        return false;
    }

    /// <summary>
    /// Processes command-line arguments to create and configure a <see cref="RemoteFileSystem"/> instance.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <param name="remoteFileSystem">
    /// When this method returns, contains the configured <see cref="RemoteFileSystem"/> instance if the parsing succeeded;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <param name="timeout">
    /// When this method returns, contains the timeout duration in milliseconds before the server
    /// should shut down if no clients have connected during the specified time frame.
    /// </param>
    /// <returns>
    /// <c>true</c> if the arguments were successfully processed and a <see cref="RemoteFileSystem"/> instance was created;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool ProcessArguments(ReadOnlySpan<string> args, out RemoteFileSystem? remoteFileSystem, out int timeout)
    {
        timeout = 0;
        remoteFileSystem = null;

        string root = DefaultRoot;
        byte[]? secret = null;
        IPEndPoint? endpoint = DefaultEndpoint;
        IPAddress? address = endpoint.Address;
        int port = endpoint.Port;
        X509Certificate? certificate = null;
        int maxSearchDepth = 0;
        bool allowShutdownRequests = false;

        while (!args.IsEmpty)
        {
            switch (args[0])
            {
                case "-h":
                case "--help":
                    return Info(Help);

                case "-v":
                case "--version":
                    return Info(Version);

                case "-r" when args.Length >= 2:
                case "--root" when args.Length >= 2:
                    if (!Uri.TryCreate(root, UriKind.RelativeOrAbsolute, out _))
                        return Error("Invalid root path provided. Please specify a valid URI for the root directory.");

                    root = args[1];
                    args = args.Slice(1);
                    break;

                case "-s" when args.Length >= 2:
                case "--secret" when args.Length >= 2:
                    if (!TryParseSecret(args[1], out secret))
                        return Error("Invalid secret format. The secret must be a valid Base64 string.");

                    args = args.Slice(1);
                    break;

                case "-a" when args.Length >= 2:
                case "--address" when args.Length >= 2:
                    if (!IPAddress.TryParse(args[1], out address))
                        return Error("Invalid IP address provided.");

                    args = args.Slice(1);
                    break;

                case "-p" when args.Length >= 2:
                case "--port" when args.Length >= 2:
                    if (!int.TryParse(args[1], out port) || port is not (> 0 and <= ushort.MaxValue))
                        return Error("Invalid port number provided. Port must be a positive integer in the range of (0;65535].");

                    args = args.Slice(1);
                    break;

                case "-e" when args.Length >= 2:
                case "--endpoint" when args.Length >= 2:
                    if (!IPEndPoint.TryParse(args[1], out endpoint))
                        return Error("Invalid endpoint provided. Expected format: '<IP>:<port>'.");

                    address = endpoint.Address;
                    port = endpoint.Port;
                    args = args.Slice(1);
                    break;

                case "-c" when args.Length >= 2:
                case "--certificate" when args.Length >= 2:
                    if (!TryImportX509Certificate(args[1], out certificate))
                        return Error("Invalid certificate file. Unable to import the certificate from the specified path.");

                    args = args.Slice(1);
                    break;

                case "-d" when args.Length >= 2:
                case "--max-search-depth" when args.Length >= 2:
                    if (!int.TryParse(args[1], out maxSearchDepth))
                        return Error("Invalid max search depth provided. Must be a valid integer.");

                    args = args.Slice(1);
                    break;

                case "-t" when args.Length >= 2:
                case "--timeout" when args.Length >= 2:
                    if (!int.TryParse(args[1], out timeout))
                        return Error("Invalid timeout provided. Must be a valid integer.");

                    args = args.Slice(1);
                    break;

                case "--allow-shutdown-requests":
                    allowShutdownRequests = true;
                    break;

                default:
                    return Error($"Unrecognized argument: '{args[0]}'.");
            }
            args = args.Slice(1);
        }

        endpoint = new(address, port);
        if (secret is null or { Length: 0 })
            return Error("A valid secret is required. Please specify a non-empty secret using the '--secret' option.");

        remoteFileSystem = certificate is null ? new(root, secret, endpoint) : new(root, secret, endpoint, certificate);
        remoteFileSystem.MaxSearchDepth = maxSearchDepth;
        remoteFileSystem.AllowShutdownRequests = allowShutdownRequests;
        return true;
    }

    /// <summary>
    /// Attempts to parse a secret from the specified argument string.
    /// </summary>
    /// <param name="arg">The secret argument string.</param>
    /// <param name="secret">
    /// When this method returns, contains the parsed secret as a byte array if successful;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the secret was successfully parsed; otherwise, <c>false</c>.
    /// </returns>
    private static bool TryParseSecret(string arg, [NotNullWhen(true)] out byte[]? secret)
    {
        string[] format = arg.Split(':', count: 3);
        switch (format)
        {
            case ["text", "utf8", string value]:
                secret = Encoding.UTF8.GetBytes(value);
                return true;

            case ["text", "base64", string value]:
                return TryGetBase64Bytes(value, out secret) || Error("Invalid Base64-encoded secret provided.");

            case ["text", .. string[] value]:
                secret = Encoding.UTF8.GetBytes(string.Join(':', value));
                return true;

            case ["env", "utf8", string env]:
                secret = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable(env) ?? string.Empty);
                return true;

            case ["env", "base64", string env]:
                return TryGetBase64Bytes(Environment.GetEnvironmentVariable(env) ?? string.Empty, out secret)
                    || Error($"Environment variable '{env}' does not contain a valid Base64-encoded secret.");

            case ["env", .. string[] env]:
                secret = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable(string.Join(':', env)) ?? string.Empty);
                return true;

            case ["file", .. string[] pathParts]:
                string path = string.Join(':', pathParts);
                secret = File.Exists(path) ? File.ReadAllBytes(path) : null;
                return secret is not null || Error($"Secret file not found at '{path}'.");

            case ["fd", "0", "utf8"]:
            case ["fd", "0"]:
            case ["stdin", "utf8"]:
            case ["stdin"]:
                string utf8Input = Console.IsInputRedirected ? Console.In.ReadToEnd() : Console.In.ReadLine() ?? string.Empty;
                secret = Encoding.UTF8.GetBytes(utf8Input);
                return true;

            case ["fd", "0", "base64"]:
            case ["stdin", "base64"]:
                string base64Input = Console.IsInputRedirected ? Console.In.ReadToEnd() : Console.In.ReadLine() ?? string.Empty;
                return TryGetBase64Bytes(base64Input, out secret) || Error("Invalid Base64-encoded secret provided.");

            default:
                secret = null;
                return false;
        }
    }

    /// <summary>
    /// Attempts to decode the provided Base64-encoded string into a byte array.
    /// </summary>
    /// <param name="value">The Base64-encoded string to decode.</param>
    /// <param name="bytes">
    /// When this method returns, contains the decoded byte array
    /// if the conversion succeeded; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the string was successfully decoded;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool TryGetBase64Bytes(string value, [NotNullWhen(true)] out byte[]? bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value.Trim());
            return true;
        }
        catch
        {
            bytes = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to import an X.509 certificate from the specified file.
    /// </summary>
    /// <param name="path">The path to the certificate file.</param>
    /// <param name="certificate">
    /// When this method returns, contains the imported <see cref="X509Certificate"/>
    /// if the import succeeded; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the certificate was successfully imported;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool TryImportX509Certificate(string path, [NotNullWhen(true)] out X509Certificate? certificate)
    {
        try
        {
#if NET9_0_OR_GREATER
            certificate = X509CertificateLoader.LoadPkcs12FromFile(path, null);
#else
            certificate = X509Certificate.CreateFromCertFile(path);
#endif
            return true;
        }
        catch
        {
            certificate = null;
            return false;
        }
    }
}
