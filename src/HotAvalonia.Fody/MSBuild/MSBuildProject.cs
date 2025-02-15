namespace HotAvalonia.Fody.MSBuild;

/// <summary>
/// Represents an MSBuild project file.
/// </summary>
public sealed class MSBuildProject : MSBuildFile
{
    /// <summary>
    /// The cached assembly name of the project, if any.
    /// </summary>
    private string? _assemblyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildProject"/> class.
    /// </summary>
    /// <param name="path">The path to the project file.</param>
    public MSBuildProject(string path) : base(path)
    {
        _assemblyName = null;
    }

    /// <summary>
    /// Gets the assembly name associated with the project.
    /// </summary>
    public string AssemblyName => _assemblyName ??= ReadAssemblyName();

    /// <summary>
    /// Reads and parses the assembly name from the project file content.
    /// </summary>
    /// <returns>
    /// The assembly name extracted from the project file.
    /// </returns>
    private string ReadAssemblyName()
    {
        // 99% of projects out there default to using
        // the project file name as the assembly name.
        //
        // Also, there are a few strange folks who like
        // to specify the assembly name explicitly, as in:
        // <AssemblyName>ProjectFileNameWithoutExtension</AssemblyName>
        //
        // However, if neither of the above applies, we can't determine
        // what the assembly name is supposed to be without actually
        // running MSBuild, which, obviously, isn't an option.
        //
        // Honestly, there's also an edge case where <AssemblyName>
        // might be specified outside of the project file
        // (e.g., in a .props or .targets file),
        // but if your setup is THAT weird - my man, that's on you!
        string assemblyName = Path.GetFileNameWithoutExtension(FullPath);
        if (!Content.Contains("<AssemblyName>"))
            return assemblyName;

        if (Content.Contains($"<AssemblyName>{assemblyName}</AssemblyName>"))
            return assemblyName;

        return string.Empty;
    }
}
