using System.Text.Json;

namespace HotAvalonia.HostCompiler;

/// <summary>The values auto-discovered from a target app's restore/build output.</summary>
internal readonly record struct DiscoveryResult(string ClosureDirectory, string AvaloniaVersion, string TargetFramework);

/// <summary>
/// Auto-discovers the inputs the compiler needs (the pre-link reference closure, the exact Avalonia
/// version, and the compile target framework) from a target app's <c>obj/project.assets.json</c> and
/// <c>bin</c> output, so a consumer only has to point the tool at their app project.
/// </summary>
internal static class ProjectDiscovery
{
    private static readonly string[] s_rids = ["ios-arm64", "iossimulator-arm64", "iossimulator-x64"];

    /// <summary>
    /// Discovers the closure/version/TFM from the given app <c>.csproj</c> (or a directory containing one).
    /// </summary>
    /// <exception cref="InvalidOperationException">The app hasn't been restored/built, or the inputs can't be inferred.</exception>
    public static DiscoveryResult Discover(string appProjectOrDirectory)
    {
        string projectDir = ResolveProjectDirectory(appProjectOrDirectory);
        string assetsPath = Path.Combine(projectDir, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
            throw new InvalidOperationException($"No restore output at {assetsPath}. Build the app (Debug) first.");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        JsonElement root = document.RootElement;

        string iosTargetFramework = ResolveIosTargetFramework(root);
        string compileTargetFramework = iosTargetFramework.Split('-', 2)[0]; // net10.0-ios26.5 -> net10.0
        string avaloniaVersion = ResolveAvaloniaVersion(root);
        string closure = ResolveClosure(projectDir, iosTargetFramework);

        return new DiscoveryResult(closure, avaloniaVersion, compileTargetFramework);
    }

    private static string ResolveProjectDirectory(string appProjectOrDirectory)
    {
        if (File.Exists(appProjectOrDirectory))
            return Path.GetDirectoryName(Path.GetFullPath(appProjectOrDirectory))!;

        if (Directory.Exists(appProjectOrDirectory))
        {
            string[] projects = Directory.GetFiles(appProjectOrDirectory, "*.csproj");
            if (projects.Length == 1)
                return Path.GetFullPath(appProjectOrDirectory);

            throw new InvalidOperationException($"Expected exactly one .csproj in {appProjectOrDirectory}, found {projects.Length}.");
        }

        throw new InvalidOperationException($"App project or directory not found: {appProjectOrDirectory}");
    }

    private static string ResolveIosTargetFramework(JsonElement root)
    {
        if (root.TryGetProperty("project", out JsonElement project)
            && project.TryGetProperty("frameworks", out JsonElement frameworks))
        {
            foreach (JsonProperty framework in frameworks.EnumerateObject())
            {
                if (framework.Name.Contains("-ios", StringComparison.Ordinal))
                    return framework.Name;
            }
        }

        throw new InvalidOperationException("Could not find an iOS target framework in project.assets.json.");
    }

    private static string ResolveAvaloniaVersion(JsonElement root)
    {
        if (root.TryGetProperty("libraries", out JsonElement libraries))
        {
            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                if (library.Name.StartsWith("Avalonia/", StringComparison.Ordinal))
                    return library.Name["Avalonia/".Length..];
            }
        }

        throw new InvalidOperationException("Could not find the Avalonia package version in project.assets.json.");
    }

    private static string ResolveClosure(string projectDirectory, string iosTargetFramework)
    {
        string binRoot = Path.Combine(projectDirectory, "bin");
        List<DirectoryInfo> candidates = [];

        if (Directory.Exists(binRoot))
        {
            foreach (string tfmDirectory in Directory.EnumerateDirectories(binRoot, iosTargetFramework, SearchOption.AllDirectories))
            {
                foreach (string rid in s_rids)
                {
                    string candidate = Path.Combine(tfmDirectory, rid);

                    // The rid directory itself is the untrimmed pre-link closure; System.Runtime.dll (rather
                    // than only the linker-merged System.Private.CoreLib) confirms it isn't a trimmed .app build.
                    if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "System.Runtime.dll")))
                        candidates.Add(new DirectoryInfo(candidate));
                }
            }
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException($"No pre-link build output (bin/**/{iosTargetFramework}/<rid>/ with System.Runtime.dll) found. Build the iOS app (Debug) first.");

        string debugSegment = $"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}";
        return candidates
            .OrderByDescending(directory => directory.FullName.Contains(debugSegment, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(directory => directory.LastWriteTimeUtc)
            .First()
            .FullName;
    }
}
