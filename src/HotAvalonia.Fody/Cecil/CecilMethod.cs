using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace HotAvalonia.Fody.Cecil;

/// <summary>
/// Encapsulates a method within a type definition.
/// </summary>
internal sealed class CecilMethod
{
    /// <summary>
    /// The type that declares this method.
    /// </summary>
    private readonly CecilType _declaringType;

    /// <summary>
    /// A function that selects the method definition from a <see cref="TypeDefinition"/>.
    /// </summary>
    private readonly Func<TypeDefinition, MethodDefinition?> _selector;

    /// <summary>
    /// The cached method definition, if any.
    /// </summary>
    private MethodDefinition? _definition;

    /// <summary>
    /// The cached method reference, if any.
    /// </summary>
    private MethodReference? _reference;

    /// <summary>
    /// Initializes a new instance of the <see cref="CecilMethod"/> class.
    /// </summary>
    /// <param name="declaringType">The type that declares this method.</param>
    /// <param name="selector">A function that selects a method definition from a <see cref="TypeDefinition"/>.</param>
    public CecilMethod(CecilType declaringType, Func<TypeDefinition, MethodDefinition?> selector)
    {
        _declaringType = declaringType;
        _selector = selector;
    }

    /// <summary>
    /// Implicitly converts a <see cref="CecilMethod"/> to a <see cref="MethodDefinition"/>.
    /// </summary>
    /// <param name="method">The <see cref="CecilMethod"/> to convert.</param>
    /// <returns>A <see cref="MethodDefinition"/> instance representing the specified method.</returns>
    [return: NotNullIfNotNull(nameof(method))]
    public static implicit operator MethodDefinition?(CecilMethod? method) => method?.Definition;

    /// <summary>
    /// Implicitly converts a <see cref="CecilMethod"/> to a <see cref="MethodReference"/>.
    /// </summary>
    /// <param name="method">The <see cref="CecilMethod"/> to convert.</param>
    /// <returns>A <see cref="MethodReference"/> instance representing the specified method.</returns>
    [return: NotNullIfNotNull(nameof(method))]
    public static implicit operator MethodReference?(CecilMethod? method) => method?.Reference;

    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    public string Name => Definition.Name;

    /// <summary>
    /// Gets the type that declares this method.
    /// </summary>
    public CecilType DeclaringType => _declaringType;

    /// <summary>
    /// Gets the method definition.
    /// </summary>
    public MethodDefinition Definition => _definition ??= _selector(_declaringType.Definition) ?? throw new MissingMethodException();

    /// <summary>
    /// Gets the imported method reference.
    /// </summary>
    public MethodReference Reference => _reference ??= ImportMethodReference(_declaringType, Definition);

    /// <summary>
    /// Imports a method reference from using the declaring type's resolver.
    /// </summary>
    /// <param name="type">The declaring type of the method.</param>
    /// <param name="definition">The method definition to import.</param>
    /// <returns>The imported <see cref="MethodReference"/>.</returns>
    private static MethodReference ImportMethodReference(CecilType type, MethodDefinition definition)
    {
        MethodReference baseReference = type.TypeResolver.ModuleDefinition.ImportReference(definition);
        if (!type.WeakType.IsGenericType || type.WeakType.IsGenericTypeDefinition)
            return baseReference;

        MethodReference genericReference = new(baseReference.Name, baseReference.ReturnType)
        {
            DeclaringType = type.Reference,
            HasThis = baseReference.HasThis,
            ExplicitThis = baseReference.ExplicitThis,
            CallingConvention = baseReference.CallingConvention,
        };
        foreach (ParameterDefinition parameter in baseReference.Parameters)
            genericReference.Parameters.Add(parameter);

        return type.TypeResolver.ModuleDefinition.ImportReference(genericReference, type.Reference);
    }
}
