namespace HotAvalonia.Fody.MSBuild;

/// <summary>
/// Represents an MSBuild project file.
/// </summary>
public sealed class MSBuildProject : MSBuildFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildProject"/> class.
    /// </summary>
    /// <param name="path">The path to the project file.</param>
    /// <param name="assemblyName">The assembly name associated with the project.</param>
    public MSBuildProject(string path, string? assemblyName = null) : base(path)
    {
        // If the assembly name is provided in its fully qualified form (e.g., "AssemblyName, Version=1.0.0.0, ..."),
        // strip everything beyond the actual name.
        AssemblyName = assemblyName?.Split(',')[0].Trim();
    }

    /// <summary>
    /// Gets the assembly name associated with the project, if any.
    /// </summary>
    public string? AssemblyName { get; }
}
