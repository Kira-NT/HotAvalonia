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

/// <summary>
/// Provides extension methods for <see cref="ITypeResolver"/>.
/// </summary>
internal static class TypeResolverExtensions
{
    /// <summary>
    /// Gets the <see cref="CecilType"/> corresponding to the specified <see cref="WeakType"/>.
    /// </summary>
    /// <param name="resolver">The type resolver used to resolve the type.</param>
    /// <param name="type">The weak type to resolve.</param>
    /// <returns>A <see cref="CecilType"/> representing the resolved type.</returns>
    public static CecilType GetType(this ITypeResolver resolver, WeakType type) => new(type, resolver);
}
