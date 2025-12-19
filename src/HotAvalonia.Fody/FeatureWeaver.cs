using System.Xml.Linq;
using Fody;
using HotAvalonia.Fody.MSBuild;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TypeSystem = Fody.TypeSystem;

namespace HotAvalonia.Fody;

/// <summary>
/// Represents a base class for feature-specific weaving logic.
/// </summary>
internal abstract class FeatureWeaver
{
    /// <summary>
    /// The root module weaver providing context and shared functionality.
    /// </summary>
    protected readonly ModuleWeaver _root;

    /// <summary>
    /// The name of this weaver.
    /// </summary>
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureWeaver"/> class.
    /// </summary>
    /// <param name="root">The root module weaver providing context and shared functionality.</param>
    protected FeatureWeaver(ModuleWeaver root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _name = GetType().Name.Replace("Weaver", string.Empty);
    }

    /// <summary>
    /// Gets the name of this weaver.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets a value indicating whether the weaver is enabled.
    /// </summary>
    public bool Enabled => this["Enable", false];

    /// <summary>
    /// Gets the string value of the configuration attribute with the specified name.
    /// </summary>
    /// <param name="name">The name of the attribute to retrieve.</param>
    /// <returns>The attribute value as a string if found; otherwise, an empty string.</returns>
    protected string this[string name] => Config.Attribute(name)?.Value ?? string.Empty;

    /// <summary>
    /// Gets the boolean value of the configuration attribute with the specified name.
    /// </summary>
    /// <param name="name">The name of the attribute to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the attribute is not present or cannot be parsed as a boolean.</param>
    /// <returns>The parsed boolean value if the attribute exists and is valid; otherwise, the specified <paramref name="defaultValue"/>.</returns>
    protected bool this[string name, bool defaultValue] => bool.TryParse(Config.Attribute(name)?.Value, out bool x) ? x : defaultValue;

    /// <summary>
    /// Gets the integer value of the configuration attribute with the specified name.
    /// </summary>
    /// <param name="name">The name of the attribute to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the attribute is not present or cannot be parsed as an integer.</param>
    /// <returns>The parsed integer value if the attribute exists and is valid; otherwise, the specified <paramref name="defaultValue"/>.</returns>
    protected int this[string name, int defaultValue] => int.TryParse(Config.Attribute(name)?.Value, out int x) ? x : defaultValue;

    /// <inheritdoc cref="BaseModuleWeaver.Config"/>
    public XElement Config => field ??= _root.Config.Element(_name) ?? new(_name);

    /// <inheritdoc cref="BaseModuleWeaver.ModuleDefinition"/>
    public ModuleDefinition ModuleDefinition => _root.ModuleDefinition;

    /// <inheritdoc cref="BaseModuleWeaver.AssemblyResolver"/>
    public IAssemblyResolver AssemblyResolver => _root.AssemblyResolver;

    /// <inheritdoc cref="BaseModuleWeaver.TypeSystem"/>
    public TypeSystem TypeSystem => _root.TypeSystem;

    /// <inheritdoc cref="BaseModuleWeaver.AssemblyFilePath"/>
    public string AssemblyFilePath => _root.AssemblyFilePath;

    /// <inheritdoc cref="BaseModuleWeaver.ProjectDirectoryPath"/>
    public string ProjectDirectoryPath => _root.ProjectDirectoryPath;

    /// <inheritdoc cref="BaseModuleWeaver.ProjectFilePath"/>
    public string ProjectFilePath => _root.ProjectFilePath;

    /// <inheritdoc cref="ModuleWeaver.Project"/>
    public MSBuildProject Project => _root.Project;

    /// <inheritdoc cref="ModuleWeaver.ReferencedProjects"/>
    public IEnumerable<MSBuildProject> ReferencedProjects => _root.ReferencedProjects;

    /// <inheritdoc cref="BaseModuleWeaver.DocumentationFilePath"/>
    public string? DocumentationFilePath => _root.DocumentationFilePath;

    /// <inheritdoc cref="BaseModuleWeaver.AddinDirectoryPath"/>
    public string AddinDirectoryPath => _root.AddinDirectoryPath;

    /// <inheritdoc cref="BaseModuleWeaver.SolutionDirectoryPath"/>
    public string SolutionDirectoryPath => _root.SolutionDirectoryPath;

    /// <inheritdoc cref="ModuleWeaver.SolutionFilePath"/>
    public string SolutionFilePath => _root.Solution.FullPath;

    /// <inheritdoc cref="ModuleWeaver.Solution"/>
    public MSBuildSolution Solution => _root.Solution;

    /// <inheritdoc cref="BaseModuleWeaver.References"/>
    public string References => _root.References;

    /// <inheritdoc cref="BaseModuleWeaver.ReferenceCopyLocalPaths"/>
    public List<string> ReferenceCopyLocalPaths => _root.ReferenceCopyLocalPaths;

    /// <inheritdoc cref="BaseModuleWeaver.RuntimeCopyLocalPaths"/>
    public List<string> RuntimeCopyLocalPaths => _root.RuntimeCopyLocalPaths;

    /// <inheritdoc cref="BaseModuleWeaver.DefineConstants"/>
    public List<string> DefineConstants => _root.DefineConstants;


    /// <inheritdoc cref="BaseModuleWeaver.GetAssembliesForScanning"/>
    public virtual IEnumerable<string> GetAssembliesForScanning() => [];

    /// <inheritdoc cref="BaseModuleWeaver.Execute"/>
    public abstract void Execute();

    /// <inheritdoc cref="BaseModuleWeaver.AfterWeaving"/>
    public virtual void AfterWeaving()
    {
    }

    /// <inheritdoc cref="BaseModuleWeaver.Cancel"/>
    public virtual void Cancel()
    {
    }


    /// <inheritdoc cref="BaseModuleWeaver.FindTypeDefinition"/>
    public TypeDefinition FindTypeDefinition(string name) => _root.FindTypeDefinition(name);

    /// <inheritdoc cref="BaseModuleWeaver.TryFindTypeDefinition"/>
    public bool TryFindTypeDefinition(string name, out TypeDefinition? type) => _root.TryFindTypeDefinition(name, out type);

    /// <inheritdoc cref="BaseModuleWeaver.ResolveAssembly"/>
    public AssemblyDefinition? ResolveAssembly(string name) => _root.ResolveAssembly(name);

    /// <inheritdoc cref="BaseModuleWeaver.WriteDebug(string)"/>
    public void WriteDebug(string message) => _root.WriteDebug(message);

    /// <inheritdoc cref="BaseModuleWeaver.WriteInfo(string)"/>
    public void WriteInfo(string message) => _root.WriteInfo(message);

    /// <inheritdoc cref="BaseModuleWeaver.WriteWarning(string)"/>
    public void WriteWarning(string message) => _root.WriteWarning(message);

    /// <inheritdoc cref="BaseModuleWeaver.WriteWarning(string, SequencePoint?)"/>
    public void WriteWarning(string message, SequencePoint? sequencePoint) => _root.WriteWarning(message, sequencePoint);

    /// <inheritdoc cref="BaseModuleWeaver.WriteError(string)"/>
    public void WriteError(string message) => _root.WriteError(message);

    /// <inheritdoc cref="BaseModuleWeaver.WriteError(string, SequencePoint?)"/>
    public void WriteError(string message, SequencePoint? sequencePoint) => _root.WriteError(message, sequencePoint);

    /// <inheritdoc cref="BaseModuleWeaver.WriteMessage(string, MessageImportance)"/>
    public void WriteMessage(string message, MessageImportance importance) => _root.WriteMessage(message, importance);
}
