using Mono.Cecil;

namespace HotAvalonia.Fody;

/// <summary>
/// Excludes specified assembly references from module definitions and cleans up related copy-local paths.
/// </summary>
internal sealed class ReferencesWeaver : FeatureWeaver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferencesWeaver"/> class.
    /// </summary>
    /// <param name="root">The root module weaver providing context and shared functionality.</param>
    public ReferencesWeaver(ModuleWeaver root) : base(root)
    {
    }

    /// <summary>
    /// Gets the collection of assembly reference names to exclude.
    /// </summary>
    private IEnumerable<string> Exclude => this[nameof(Exclude)].Split([';'], StringSplitOptions.RemoveEmptyEntries);

    /// <inheritdoc/>
    public override void Execute()
    {
        foreach (string referenceName in Exclude)
            ExcludeReference(referenceName, ModuleDefinition, _root.ReferenceCopyLocalPaths, _root.RuntimeCopyLocalPaths);
    }

    /// <summary>
    /// Removes the specified assembly reference from the module and cleans up related copy-local paths.
    /// </summary>
    /// <param name="referenceName">The name of the reference to exclude.</param>
    /// <param name="module">The module definition from which to remove the reference.</param>
    /// <param name="copyLocalPathsCollections">The collections of paths to remove the reference from.</param>
    private static void ExcludeReference(string referenceName, ModuleDefinition module, params IEnumerable<List<string>> copyLocalPathsCollections)
    {
        referenceName = referenceName.Trim();
        if (string.IsNullOrWhiteSpace(referenceName))
            return;

        AssemblyNameReference assemblyReference = module.AssemblyReferences.FirstOrDefault(x => x.Name == referenceName);
        if (assemblyReference is not null)
            module.AssemblyReferences.Remove(assemblyReference);

        foreach (List<string> copyLocalPaths in copyLocalPathsCollections)
            copyLocalPaths.RemoveAll(x => referenceName.Equals(Path.GetFileNameWithoutExtension(x), StringComparison.OrdinalIgnoreCase));
    }
}
