using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HotAvalonia.Helpers;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Provides methods for building dynamic enumeration types.
/// </summary>
internal sealed class DynamicEnumBuilder : IDisposable
{
    /// <summary>
    /// The underlying <see cref="EnumBuilder"/> used to build the enumeration type.
    /// </summary>
    private readonly EnumBuilder _builder;

    /// <summary>
    /// The dynamic code scope.
    /// </summary>
    private readonly DynamicCodeScope _scope;

    /// <summary>
    /// Defines a dynamic enumeration type that is a value type with a single non-static field
    /// called <c>value__</c> of the specified <paramref name="underlyingType"/>.
    /// </summary>
    /// <param name="module">The <see cref="ModuleBuilder"/> where the enumeration type resides.</param>
    /// <param name="name">The full path of the enumeration type.</param>
    /// <param name="visibility">The type attributes for the enumeration.</param>
    /// <param name="underlyingType">The underlying type for the enumeration. This must be a built-in integer type.</param>
    public DynamicEnumBuilder(ModuleBuilder module, string name, TypeAttributes visibility, Type? underlyingType)
    {
        _ = module ?? throw new ArgumentNullException(nameof(module));
        _ = name ?? throw new ArgumentNullException(nameof(name));

#pragma warning disable RS0030 // Do not use banned APIs
        _scope = DynamicCodeScope.Create(name, nameof(DynamicEnumBuilder));
        _builder = module.DefineEnum(name, visibility, underlyingType ?? typeof(int));
#pragma warning restore RS0030 // Do not use banned APIs
    }

    /// <summary>
    /// Implicitly converts a <see cref="DynamicEnumBuilder"/> to its corresponding <see cref="EnumBuilder"/>.
    /// </summary>
    /// <param name="dynamicEnumBuilder">The <see cref="DynamicEnumBuilder"/> to convert.</param>
    /// <returns>A <see cref="EnumBuilder"/> representing the enum.</returns>
    [return: NotNullIfNotNull(nameof(dynamicEnumBuilder))]
    public static implicit operator EnumBuilder?(DynamicEnumBuilder? dynamicEnumBuilder)
        => dynamicEnumBuilder?._builder;

    /// <inheritdoc cref="EnumBuilder.SetCustomAttribute(CustomAttributeBuilder)"/>
    public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        => _builder.SetCustomAttribute(customBuilder);

    /// <inheritdoc cref="EnumBuilder.DefineLiteral(string, object)"/>
    public FieldBuilder DefineLiteral(string literalName, object literalValue)
        => _builder.DefineLiteral(literalName, literalValue);

    /// <inheritdoc cref="EnumBuilder.CreateTypeInfo()"/>
    public TypeInfo CreateTypeInfo()
    {
        TypeInfo type = _builder.CreateTypeInfo();
        LoggingHelper.LogInfo("Created dynamic enum: {Type}", type);
        return type;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
        => _builder.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object obj)
        => obj is DynamicEnumBuilder other && _builder.Equals(other._builder);

    /// <inheritdoc/>
    public override string ToString()
        => _builder.ToString();

    /// <inheritdoc/>
    public void Dispose()
        => _scope.Dispose();
}
