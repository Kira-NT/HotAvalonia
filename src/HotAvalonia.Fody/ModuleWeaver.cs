using System.Xml.Linq;
using Fody;
using HotAvalonia.Fody.MSBuild;

namespace HotAvalonia.Fody;

/// <summary>
/// Represents the main module weaver that orchestrates feature-specific weaving tasks.
/// </summary>
public sealed class ModuleWeaver : BaseModuleWeaver
{
    /// <summary>
    /// The collection of feature weavers used to perform specific weaving tasks.
    /// </summary>
    private readonly FeatureWeaver[] _features;

    /// <summary>
    /// The target solution, if any.
    /// </summary>
    private MSBuildSolution? _solution;

    /// <summary>
    /// The target project, if any.
    /// </summary>
    private MSBuildProject? _project;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleWeaver"/> class.
    /// </summary>
    public ModuleWeaver() => _features =
    [
        new PopulateOverrideWeaver(this),
    ];

    /// <summary>
    /// Gets the target solution.
    /// </summary>
    public MSBuildSolution Solution => _solution ??= GetSolution(Config, SolutionDirectoryPath, ProjectDirectoryPath);

    /// <summary>
    /// Gets the full file path of the target solution.
    /// </summary>
    public string SolutionFilePath => Solution.FullPath;

    /// <summary>
    /// Gets the target project.
    /// </summary>
    public MSBuildProject Project => _project ??= new(ProjectFilePath);

    /// <inheritdoc/>
    public override IEnumerable<string> GetAssembliesForScanning()
        => _features
            .SelectMany(x => x.GetAssembliesForScanning())
            .Concat(["mscorlib", "netstandard", "System.Runtime"]);

    /// <inheritdoc/>
    public override void Execute()
    {
        WriteInfo($"Starting weaving '{AssemblyFilePath}'...");

        foreach (FeatureWeaver feature in _features)
        {
            if (!feature.Enabled)
                continue;

            WriteInfo($"Running '{feature.GetType().Name}' against '{AssemblyFilePath}'...");
            feature.Execute();
        }
    }

    /// <inheritdoc/>
    public override void AfterWeaving()
    {
        foreach (FeatureWeaver feature in _features)
        {
            if (!feature.Enabled)
                continue;

            feature.AfterWeaving();
        }

        WriteInfo($"Finished weaving '{AssemblyFilePath}'!");
    }

    /// <inheritdoc/>
    public override void Cancel()
    {
        foreach (FeatureWeaver feature in _features)
        {
            if (!feature.Enabled)
                continue;

            feature.Cancel();
        }
    }

    /// <summary>
    /// Resolves an <see cref="MSBuildSolution"/> instance from the provided context.
    /// </summary>
    /// <param name="config">The configuration containing the solution path.</param>
    /// <param name="solutionDirectory">The directory in which to search for an existing solution file.</param>
    /// <param name="projectDirectory">The project directory used as a fallback to locate the solution.</param>
    /// <returns>A new instance of <see cref="MSBuildSolution"/> resolved from the provided context.</returns>
    private static MSBuildSolution GetSolution(XElement config, string? solutionDirectory, string? projectDirectory)
    {
        string? solutionFilePath = config?.Attribute("SolutionPath")?.Value;
        if (solutionFilePath is { Length: > 0 })
            return new(solutionFilePath);

        if (MSBuildSolution.TryGetFromDirectory(solutionDirectory, out MSBuildSolution? solution))
            return solution;

        if (MSBuildSolution.TryGetTopLevel(projectDirectory, out solution))
            return solution;

        return new(string.Empty);
    }
}
