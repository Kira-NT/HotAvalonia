using Mono.Cecil;

namespace HotAvalonia.Fody.Cecil;

/// <summary>
/// Provides methods to resolve type definitions and module definitions.
/// </summary>
internal interface ITypeResolver
{
    /// <summary>
    /// Gets the module definition associated with the resolver.
    /// </summary>
    ModuleDefinition ModuleDefinition { get; }

    /// <summary>
    /// Finds the type definition for the specified type name.
    /// </summary>
    /// <param name="name">The full name of the type to locate.</param>
    /// <returns>The <see cref="TypeDefinition"/> corresponding to the specified name.</returns>
    TypeDefinition FindTypeDefinition(string name);
}
