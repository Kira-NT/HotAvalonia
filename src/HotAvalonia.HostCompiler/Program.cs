using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HotAvalonia.HostCompiler;

/// <summary>
/// Provides the entry point for <c>HotAvalonia.HostCompiler</c>, the host-side XAML compiler that produces
/// populate DLLs for AOT/iOS host-compiled hot reload.
/// </summary>
public static class Program
{
    private enum Verb
    {
        Compile,
        Watch,
    }

    private sealed record ParsedArguments(
        Verb Verb,
        string Path,
        string? AppProject,
        string? Closure,
        string? AvaloniaVersion,
        string? TargetFramework,
        string? Output,
        IReadOnlyList<string> Excludes);

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns><c>0</c> if the application ran successfully; otherwise, <c>1</c>.</returns>
    internal static int Main(string[] args)
    {
        if (!TryParse(args, out ParsedArguments? parsed))
        {
            Console.Error.WriteLine(Help);
            return 1;
        }

        try
        {
            CompileContext context = ResolveContext(parsed);
            return parsed.Verb == Verb.Watch ? RunWatch(parsed.Path, context) : RunCompile(parsed, context);
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int RunCompile(ParsedArguments parsed, CompileContext context)
    {
        string output = HostXamlCompiler.Compile(new HostCompileOptions
        {
            ViewPath = parsed.Path,
            ClosureDirectory = context.ClosureDirectory,
            AvaloniaVersion = context.AvaloniaVersion,
            TargetFramework = context.TargetFramework,
            ExcludePatterns = context.ExcludePatterns,
            OutputPath = parsed.Output,
        });
        Console.WriteLine(output);
        return 0;
    }

    private static int RunWatch(string viewsDirectory, CompileContext context)
    {
        if (!Directory.Exists(viewsDirectory))
            throw new InvalidOperationException($"Watch directory not found: {viewsDirectory}");

        using CancellationTokenSource cancellation = new();
        using PosixSignalRegistration sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, _ => cancellation.Cancel());
        using PosixSignalRegistration? sigterm = OperatingSystem.IsWindows()
            ? null
            : PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => cancellation.Cancel());

        Watcher.Run(viewsDirectory, context, cancellation.Token);
        return 0;
    }

    private static CompileContext ResolveContext(ParsedArguments parsed)
    {
        string? closure = parsed.Closure;
        string? avaloniaVersion = parsed.AvaloniaVersion;
        string? targetFramework = parsed.TargetFramework;

        if ((closure is null || avaloniaVersion is null || targetFramework is null) && parsed.AppProject is not null)
        {
            DiscoveryResult discovered = ProjectDiscovery.Discover(parsed.AppProject);
            closure ??= discovered.ClosureDirectory;
            avaloniaVersion ??= discovered.AvaloniaVersion;
            targetFramework ??= discovered.TargetFramework;
        }

        if (closure is null || avaloniaVersion is null)
            throw new InvalidOperationException("Provide --app-project for auto-discovery, or both --closure and --avalonia-version.");

        return new CompileContext(
            Path.GetFullPath(closure),
            avaloniaVersion,
            targetFramework ?? "net10.0",
            parsed.Excludes.Count > 0 ? parsed.Excludes : HostXamlCompiler.DefaultExcludePatterns);
    }

    private static bool TryParse(string[] args, [NotNullWhen(true)] out ParsedArguments? parsed)
    {
        parsed = null;
        if (args.Length == 0)
            return false;

        Verb verb = args[0] switch
        {
            "compile" => Verb.Compile,
            "watch" => Verb.Watch,
            _ => (Verb)(-1),
        };
        if ((int)verb < 0)
            return false;

        string? path = null;
        string? appProject = null;
        string? closure = null;
        string? avaloniaVersion = null;
        string? targetFramework = null;
        string? output = null;
        List<string> excludes = [];

        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--app-project":
                    if (!TryNext(args, ref i, out appProject))
                        return false;
                    break;
                case "--closure":
                    if (!TryNext(args, ref i, out closure))
                        return false;
                    break;
                case "--avalonia-version":
                    if (!TryNext(args, ref i, out avaloniaVersion))
                        return false;
                    break;
                case "--tfm":
                    if (!TryNext(args, ref i, out targetFramework))
                        return false;
                    break;
                case "--output":
                    if (!TryNext(args, ref i, out output))
                        return false;
                    break;
                case "--exclude":
                    if (!TryNext(args, ref i, out string? exclude))
                        return false;
                    excludes.Add(exclude);
                    break;
                default:
                    if (arg.StartsWith('-') || path is not null)
                        return false;
                    path = arg;
                    break;
            }
        }

        if (path is null)
            return false;

        parsed = new ParsedArguments(
            verb,
            Path.GetFullPath(path),
            appProject,
            closure,
            avaloniaVersion,
            targetFramework,
            output is null ? null : Path.GetFullPath(output),
            excludes);
        return true;
    }

    private static bool TryNext(string[] args, ref int index, out string value)
    {
        if (index + 1 >= args.Length)
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }

    private static string Name => Assembly.GetExecutingAssembly().GetName().Name ?? "HotAvalonia.HostCompiler";

    private static string Help => $"""
        {Name} - compiles changed Avalonia views into host-compiled populate DLLs (AOT/iOS hot reload).

        Usage:
          {Name} compile <view.axaml> (--app-project <csproj> | --closure <dir> --avalonia-version <ver>) [options]
          {Name} watch <views-dir>    (--app-project <csproj> | --closure <dir> --avalonia-version <ver>) [options]

        Inputs (auto-discovered from --app-project, or given explicitly):
          --app-project <csproj>  App project (or its dir); infers --closure, --avalonia-version and --tfm.
          --closure <dir>         The app's pre-link build-output directory (the device's reference closure).
          --avalonia-version <v>  The exact Avalonia package version the app uses.

        Options:
          --tfm <tfm>             Target framework of the compile project (default: net10.0).
          --output <path>         Output DLL path for 'compile' (default: <view>.axaml{Xaml.HostCompiledXamlNaming.SidecarSuffix}).
          --exclude <glob>        File-name glob excluded from the closure (repeatable; overrides defaults).
        """;
}
