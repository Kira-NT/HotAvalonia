using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace HotAvalonia.Fody.Cecil;

/// <summary>
/// Encapsulates a property within a type definition.
/// </summary>
internal sealed class CecilProperty
{
    /// <summary>
    /// The type that declares this property.
    /// </summary>
    private readonly CecilType _declaringType;

    /// <summary>
    /// A function that selects the property definition from a <see cref="TypeDefinition"/>.
    /// </summary>
    private readonly Func<TypeDefinition, PropertyDefinition?> _selector;

    /// <summary>
    /// Initializes a new instance of the <see cref="CecilProperty"/> class.
    /// </summary>
    /// <param name="declaringType">The type that declares this property.</param>
    /// <param name="selector">A function that selects a property definition from a <see cref="TypeDefinition"/>.</param>
    public CecilProperty(CecilType declaringType, Func<TypeDefinition, PropertyDefinition?> selector)
    {
        _declaringType = declaringType;
        _selector = selector;
    }

    /// <summary>
    /// Implicitly converts a <see cref="CecilProperty"/> to a <see cref="PropertyDefinition"/>.
    /// </summary>
    /// <param name="property">The <see cref="CecilProperty"/> to convert.</param>
    /// <returns>A <see cref="PropertyDefinition"/> instance representing the specified property.</returns>
    [return: NotNullIfNotNull(nameof(property))]
    public static implicit operator PropertyDefinition?(CecilProperty? property) => property?.Definition;

    /// <summary>
    /// Implicitly converts a <see cref="CecilProperty"/> to a <see cref="PropertyReference"/>.
    /// </summary>
    /// <param name="property">The <see cref="CecilProperty"/> to convert.</param>
    /// <returns>A <see cref="PropertyReference"/> instance representing the specified property.</returns>
    [return: NotNullIfNotNull(nameof(property))]
    public static implicit operator PropertyReference?(CecilProperty? property) => property?.Reference;

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string Name => Definition.Name;

    /// <summary>
    /// Gets the type that declares this property.
    /// </summary>
    public CecilType DeclaringType => _declaringType;

    /// <summary>
    /// Gets the method used to retrieve the property value, if available.
    /// </summary>
    public CecilMethod? GetMethod => field ??= Definition.GetMethod is { } m ? new(_declaringType, _ => m) : null;

    /// <summary>
    /// Gets the method used to set the property value, if available.
    /// </summary>
    public CecilMethod? SetMethod => field ??= Definition.SetMethod is { } m ? new(_declaringType, _ => m) : null;

    /// <summary>
    /// Gets the property definition.
    /// </summary>
    public PropertyDefinition Definition => field ??= _selector(_declaringType.Definition) ?? throw new MissingMemberException();

    /// <summary>
    /// Gets the imported property reference.
    /// </summary>
    public PropertyReference Reference => Definition;
}
