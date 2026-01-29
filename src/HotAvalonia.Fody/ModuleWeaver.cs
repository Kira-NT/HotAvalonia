using Fody;
using HotAvalonia.Fody.Cecil;
using HotAvalonia.Fody.MSBuild;

namespace HotAvalonia.Fody;

/// <summary>
/// Represents the main module weaver that orchestrates feature-specific weaving tasks.
/// </summary>
public sealed class ModuleWeaver : BaseModuleWeaver, ITypeResolver
{
    /// <summary>
    /// The collection of feature weavers used to perform specific weaving tasks.
    /// </summary>
    private readonly FeatureWeaver[] _features;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleWeaver"/> class.
    /// </summary>
    public ModuleWeaver() => _features =
    [
        new PopulateOverrideWeaver(this),
        new ReferencesWeaver(this),
        new UseHotReloadWeaver(this),
    ];

    /// <summary>
    /// Gets the target solution.
    /// </summary>
    public MSBuildSolution Solution
    {
        get
        {
            if (field is not null)
                return field;

            string? solutionFilePath = Config?.Attribute("SolutionPath")?.Value;
            solutionFilePath ??= Config?.Element("Solution")?.Attribute("Path")?.Value;
            if (solutionFilePath is { Length: > 0 } && File.Exists(solutionFilePath))
                return field = new(solutionFilePath);

            if (MSBuildSolution.TryGetFromDirectory(SolutionDirectoryPath, out field))
                return field;

            if (MSBuildSolution.TryGetTopLevel(ProjectDirectoryPath, out field))
                return field;

            return field = new(string.Empty);
        }
    }

    /// <summary>
    /// Gets the target project.
    /// </summary>
    public MSBuildProject Project => field ??= new(ProjectFilePath, ModuleDefinition.Assembly.Name.Name);

    /// <summary>
    /// Gets all projects referenced by the current build, including the project that initiated it.
    /// </summary>
    public IEnumerable<MSBuildProject> ReferencedProjects
    {
        get
        {
            if (field is not null)
                return field;

            field = Config?.Element("Solution").Elements("Project")
                .Select(x => (Path: x.Attribute("Path")?.Value, AssemblyName: x.Attribute("AssemblyName")?.Value))
                .Where(x => !string.IsNullOrEmpty(x.Path))
                .Select(x => new MSBuildProject(x.Path!, x.AssemblyName))
                .ToArray();

            if (field is null || !field.Any())
                field = [Project, .. Solution.Projects];

            return field;
        }
    }

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
}
