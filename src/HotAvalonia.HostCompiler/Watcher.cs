using System.Collections.Concurrent;

namespace HotAvalonia.HostCompiler;

/// <summary>
/// Watches a directory for <c>.axaml</c> changes and recompiles each changed view (the host-side analog of
/// the device watcher). Debounced and serialized so rapid saves of the same view don't trigger overlapping
/// compiles.
/// </summary>
internal static class Watcher
{
    private const int DebounceMilliseconds = 400;
    private const int PollMilliseconds = 150;

    /// <summary>
    /// Runs the watch loop until <paramref name="cancellationToken"/> is signaled.
    /// </summary>
    public static void Run(string viewsDirectory, CompileContext context, CancellationToken cancellationToken)
    {
        ConcurrentDictionary<string, long> pending = new(StringComparer.OrdinalIgnoreCase);

        using FileSystemWatcher watcher = new(viewsDirectory)
        {
            Filter = "*.axaml",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };

        void Queue(string path) => pending[path] = Environment.TickCount64;
        watcher.Changed += (_, e) => Queue(e.FullPath);
        watcher.Created += (_, e) => Queue(e.FullPath);
        watcher.Renamed += (_, e) => Queue(e.FullPath);
        watcher.EnableRaisingEvents = true;

        Console.WriteLine($"Watching {viewsDirectory} for *.axaml changes (Ctrl-C to stop)...");

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.WaitHandle.WaitOne(PollMilliseconds);

            long now = Environment.TickCount64;
            foreach (KeyValuePair<string, long> entry in pending)
            {
                if (now - entry.Value < DebounceMilliseconds)
                    continue;

                if (pending.TryRemove(entry.Key, out _))
                    CompileOne(entry.Key, context);
            }
        }
    }

    private static void CompileOne(string viewPath, CompileContext context)
    {
        if (!File.Exists(viewPath))
            return;

        try
        {
            string output = HostXamlCompiler.Compile(new HostCompileOptions
            {
                ViewPath = viewPath,
                ClosureDirectory = context.ClosureDirectory,
                AvaloniaVersion = context.AvaloniaVersion,
                TargetFramework = context.TargetFramework,
                ExcludePatterns = context.ExcludePatterns,
            });
            Console.WriteLine($"compiled {Path.GetFileName(viewPath)} -> {output}");
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine($"failed {Path.GetFileName(viewPath)}: {e.Message}");
        }
    }
}
