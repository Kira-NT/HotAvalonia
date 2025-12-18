using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace HotAvalonia.Fody.Cecil;

/// <summary>
/// Encapsulates a field within a type definition.
/// </summary>
internal sealed class CecilField
{
    /// <summary>
    /// The type that declares this field.
    /// </summary>
    private readonly CecilType _declaringType;

    /// <summary>
    /// A function that selects the field definition from a <see cref="TypeDefinition"/>.
    /// </summary>
    private readonly Func<TypeDefinition, FieldDefinition?> _selector;

    /// <summary>
    /// Initializes a new instance of the <see cref="CecilField"/> class.
    /// </summary>
    /// <param name="declaringType">The type that declares this field.</param>
    /// <param name="selector">A function that selects a field definition from a <see cref="TypeDefinition"/>.</param>
    public CecilField(CecilType declaringType, Func<TypeDefinition, FieldDefinition?> selector)
    {
        _declaringType = declaringType;
        _selector = selector;
    }

    /// <summary>
    /// Implicitly converts a <see cref="CecilField"/> to a <see cref="FieldDefinition"/>.
    /// </summary>
    /// <param name="field">The <see cref="CecilField"/> to convert.</param>
    /// <returns>A <see cref="FieldDefinition"/> instance representing the specified field.</returns>
    [return: NotNullIfNotNull(nameof(field))]
    public static implicit operator FieldDefinition?(CecilField? field) => field?.Definition;

    /// <summary>
    /// Implicitly converts a <see cref="CecilField"/> to a <see cref="FieldReference"/>.
    /// </summary>
    /// <param name="field">The <see cref="CecilField"/> to convert.</param>
    /// <returns>A <see cref="FieldReference"/> instance representing the specified field.</returns>
    [return: NotNullIfNotNull(nameof(field))]
    public static implicit operator FieldReference?(CecilField? field) => field?.Reference;

    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    public string Name => Definition.Name;

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public CecilType DeclaringType => _declaringType;

    /// <summary>
    /// Gets the field definition.
    /// </summary>
    public FieldDefinition Definition => field ??= _selector(_declaringType.Definition) ?? throw new MissingFieldException();

    /// <summary>
    /// Gets the imported field reference.
    /// </summary>
    public FieldReference Reference => field ??= ImportFieldReference(_declaringType, Definition);

    /// <summary>
    /// Imports a field reference using the declaring type's resolver.
    /// </summary>
    /// <param name="type">The declaring type of the field.</param>
    /// <param name="definition">The field definition to import.</param>
    /// <returns>The imported <see cref="FieldReference"/>.</returns>
    private static FieldReference ImportFieldReference(CecilType type, FieldDefinition definition)
    {
        FieldReference baseReference = type.TypeResolver.ModuleDefinition.ImportReference(definition);
        if (!type.WeakType.IsGenericType || type.WeakType.IsGenericTypeDefinition)
            return baseReference;

        FieldReference genericReference = new(baseReference.Name, baseReference.FieldType, type.Reference);
        return type.TypeResolver.ModuleDefinition.ImportReference(genericReference, type.Reference);
    }
}
