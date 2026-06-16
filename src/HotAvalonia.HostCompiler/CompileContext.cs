namespace HotAvalonia.HostCompiler;

/// <summary>
/// The resolved, view-independent inputs for compiling views (shared across a one-shot compile and every
/// iteration of a watch session).
/// </summary>
internal sealed record CompileContext(
    string ClosureDirectory,
    string AvaloniaVersion,
    string TargetFramework,
    IReadOnlyList<string> ExcludePatterns);
