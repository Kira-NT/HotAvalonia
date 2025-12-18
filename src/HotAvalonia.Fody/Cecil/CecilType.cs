using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace HotAvalonia.Fody.Cecil;

/// <summary>
/// Represents a Cecil type.
/// </summary>
internal sealed class CecilType
{
    /// <summary>
    /// The underlying weak type representation.
    /// </summary>
    private readonly WeakType _type;

    /// <summary>
    /// The type resolver used to resolve type definitions.
    /// </summary>
    private readonly ITypeResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="CecilType"/> class.
    /// </summary>
    /// <param name="type">The weak type representation.</param>
    /// <param name="resolver">The type resolver to use for resolving type definitions.</param>
    public CecilType(WeakType type, ITypeResolver resolver)
    {
        _type = type;
        _resolver = resolver;
    }

    /// <summary>
    /// Implicitly converts a <see cref="CecilType"/> to a <see cref="Cecil.WeakType"/>.
    /// </summary>
    /// <param name="type">The <see cref="CecilType"/> to convert.</param>
    /// <returns>A <see cref="Cecil.WeakType"/> instance representing the specified type.</returns>
    [return: NotNullIfNotNull(nameof(type))]
    public static implicit operator WeakType?(CecilType? type) => type?._type;

    /// <summary>
    /// Implicitly converts a <see cref="CecilType"/> to a <see cref="TypeDefinition"/>.
    /// </summary>
    /// <param name="type">The <see cref="CecilType"/> to convert.</param>
    /// <returns>A <see cref="TypeDefinition"/> instance representing the specified type.</returns>
    [return: NotNullIfNotNull(nameof(type))]
    public static implicit operator TypeDefinition?(CecilType? type) => type?.Definition;

    /// <summary>
    /// Implicitly converts a <see cref="CecilType"/> to a <see cref="TypeReference"/>.
    /// </summary>
    /// <param name="type">The <see cref="CecilType"/> to convert.</param>
    /// <returns>A <see cref="TypeReference"/> instance representing the specified type.</returns>
    [return: NotNullIfNotNull(nameof(type))]
    public static implicit operator TypeReference?(CecilType? type) => type?.Reference;

    /// <summary>
    /// Gets the weak type representation of this instance.
    /// </summary>
    public WeakType WeakType => _type;

    /// <summary>
    /// Gets the type resolver used used by this instance.
    /// </summary>
    public ITypeResolver TypeResolver => _resolver;

    /// <summary>
    /// Gets the full name of the type.
    /// </summary>
    public string FullName => _type.FullName;

    /// <summary>
    /// Gets the type definition.
    /// </summary>
    public TypeDefinition Definition => field ??= ResolveTypeDefinition(_type, _resolver);

    /// <summary>
    /// Gets the imported type reference.
    /// </summary>
    public TypeReference Reference => field ??= ImportTypeReference(_type, Definition, _resolver);

    /// <summary>
    /// Retrieves a method from the type using the specified selector.
    /// </summary>
    /// <param name="selector">
    /// A function that selects a method definition from a <see cref="TypeDefinition"/>.
    /// </param>
    /// <returns>A <see cref="CecilMethod"/> representing the selected method.</returns>
    public CecilMethod GetMethod(Func<TypeDefinition, MethodDefinition?> selector) => new(this, selector);

    /// <summary>
    /// Retrieves a field from the type using the specified selector.
    /// </summary>
    /// <param name="selector">
    /// A function that selects a field definition from a <see cref="TypeDefinition"/>.
    /// </param>
    /// <returns>A <see cref="CecilField"/> representing the selected field.</returns>
    public CecilField GetField(Func<TypeDefinition, FieldDefinition?> selector) => new(this, selector);

    /// <summary>
    /// Retrieves a property from the type using the specified selector.
    /// </summary>
    /// <param name="selector">
    /// A function that selects a property definition from a <see cref="TypeDefinition"/>.
    /// </param>
    /// <returns>A <see cref="CecilProperty"/> representing the selected property.</returns>
    public CecilProperty GetProperty(Func<TypeDefinition, PropertyDefinition?> selector) => new(this, selector);

    /// <summary>
    /// Resolves the type definition for the specified weak type using the provided resolver.
    /// </summary>
    /// <param name="type">The weak type for which to resolve the definition.</param>
    /// <param name="resolver">The resolver used to locate the type definition.</param>
    /// <returns>The resolved <see cref="TypeDefinition"/>.</returns>
    private static TypeDefinition ResolveTypeDefinition(WeakType type, ITypeResolver resolver)
    {
        WeakType baseType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return resolver.FindTypeDefinition(baseType.FullName);
    }

    /// <summary>
    /// Imports a type reference using the specified resolver.
    /// </summary>
    /// <param name="type">The weak type representing the type.</param>
    /// <param name="definition">The type definition to import.</param>
    /// <param name="resolver">The resolver used for importing the type reference.</param>
    /// <returns>The imported <see cref="TypeReference"/>.</returns>
    private static TypeReference ImportTypeReference(WeakType type, TypeDefinition definition, ITypeResolver resolver)
    {
        TypeReference baseReference = resolver.ModuleDefinition.ImportReference(definition);
        if (!type.IsGenericType || type.IsGenericTypeDefinition)
            return baseReference;

        WeakType[] genericArguments = type.GetGenericArguments();
        TypeReference[] genericArgumentReferences = Array.ConvertAll(genericArguments, x => ImportTypeReference(x, ResolveTypeDefinition(x, resolver), resolver));
        GenericInstanceType genericDefinition = baseReference.MakeGenericInstanceType(genericArgumentReferences);
        return resolver.ModuleDefinition.ImportReference(genericDefinition);
    }
}
