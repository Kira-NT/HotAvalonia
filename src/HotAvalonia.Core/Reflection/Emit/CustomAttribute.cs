using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Provides information about a custom attribute.
/// </summary>
internal sealed class CustomAttribute
{
    /// <summary>
    /// The field reference for <c>CustomAttributeBuilder.m_blob</c>.
    /// </summary>
    /// <remarks>
    /// Do NOT rely on the name of the field actually being <c>m_blob</c>.
    /// It's only true for CoreCLR, while Mono calls it differently.
    /// </remarks>
    private static readonly FieldInfo? s_customAttributeBuilderBlob = typeof(CustomAttributeBuilder)
        .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        .FirstOrDefault(x => x.FieldType == typeof(byte[]));

    /// <summary>
    /// The <see cref="CustomAttributeBuilder"/> used to build the custom attribute.
    /// </summary>
    private readonly CustomAttributeBuilder _customAttributeBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomAttribute"/> class.
    /// </summary>
    /// <param name="customAttributeBuilder">The <see cref="CustomAttributeBuilder"/> used to build the custom attribute.</param>
    private CustomAttribute(CustomAttributeBuilder customAttributeBuilder)
    {
        _customAttributeBuilder = customAttributeBuilder;
    }

    /// <inheritdoc cref="CustomAttribute(ConstructorInfo, object?[], PropertyInfo[], object?[], FieldInfo[], object?[])"/>
    public CustomAttribute(ConstructorInfo ctor, object?[] ctorArgs)
        : this(CreateCustomAttributeBuilder(ctor, ctorArgs, [], [], [], []))
    {
    }

    /// <inheritdoc cref="CustomAttribute(ConstructorInfo, object?[], PropertyInfo[], object?[], FieldInfo[], object?[])"/>
    public CustomAttribute(ConstructorInfo ctor, object?[] ctorArgs, PropertyInfo[] properties, object?[] propertyValues)
        : this(CreateCustomAttributeBuilder(ctor, ctorArgs, properties, propertyValues, [], []))
    {
    }

    /// <inheritdoc cref="CustomAttribute(ConstructorInfo, object?[], PropertyInfo[], object?[], FieldInfo[], object?[])"/>
    public CustomAttribute(ConstructorInfo ctor, object?[] ctorArgs, FieldInfo[] fields, object?[] fieldValues)
        : this(CreateCustomAttributeBuilder(ctor, ctorArgs, [], [], fields, fieldValues))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomAttribute"/> class.
    /// </summary>
    /// <param name="ctor">The constructor for the custom attribute.</param>
    /// <param name="ctorArgs">The arguments to the constructor of the custom attribute.</param>
    /// <param name="properties">Named properties of the custom attribute.</param>
    /// <param name="propertyValues">Values for the named properties of the custom attribute.</param>
    /// <param name="fields">Named fields of the custom attribute.</param>
    /// <param name="fieldValues">Values for the named fields of the custom attribute.</param>
    public CustomAttribute(ConstructorInfo ctor, object?[] ctorArgs, PropertyInfo[] properties, object?[] propertyValues, FieldInfo[] fields, object?[] fieldValues)
        : this(CreateCustomAttributeBuilder(ctor, ctorArgs, properties, propertyValues, fields, fieldValues))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomAttribute"/> class.
    /// </summary>
    /// <param name="data">The custom attribute data.</param>
    public CustomAttribute(CustomAttributeData data)
        : this(CreateCustomAttributeBuilder(data))
    {
    }

    /// <summary>
    /// Implicitly converts a <see cref="CustomAttribute"/> to its corresponding <see cref="CustomAttributeBuilder"/>.
    /// </summary>
    /// <param name="customAttribute">The <see cref="CustomAttribute"/> to convert.</param>
    /// <returns>A <see cref="CustomAttributeBuilder"/> representing the custom attribute.</returns>
    [return: NotNullIfNotNull(nameof(customAttribute))]
    public static implicit operator CustomAttributeBuilder?(CustomAttribute? customAttribute)
        => customAttribute?._customAttributeBuilder;

    /// <summary>
    /// Creates a new instance of the <see cref="CustomAttributeBuilder"/> class.
    /// </summary>
    /// <param name="ctor">The constructor for the custom attribute.</param>
    /// <param name="ctorArgs">The arguments to the constructor of the custom attribute.</param>
    /// <param name="properties">Named properties of the custom attribute.</param>
    /// <param name="propertyValues">Values for the named properties of the custom attribute.</param>
    /// <param name="fields">Named fields of the custom attribute.</param>
    /// <param name="fieldValues">Values for the named fields of the custom attribute.</param>
    /// <returns>A new <see cref="CustomAttributeBuilder"/> instance.</returns>
    private static CustomAttributeBuilder CreateCustomAttributeBuilder(ConstructorInfo ctor, object?[] ctorArgs, PropertyInfo[] properties, object?[] propertyValues, FieldInfo[] fields, object?[] fieldValues)
    {
        _ = ctor ?? throw new ArgumentNullException(nameof(ctor));

#pragma warning disable RS0030 // Do not use banned APIs
        using DynamicCodeScope scope = DynamicCodeScope.Create(ctor.DeclaringType?.FullName, nameof(CustomAttribute));
        return new(ctor, ctorArgs, properties, propertyValues!, fields, fieldValues!);
#pragma warning restore RS0030 // Do not use banned APIs
    }

    /// <summary>
    /// Creates a new instance of the <see cref="CustomAttributeBuilder"/> class.
    /// </summary>
    /// <param name="data">The custom attribute data.</param>
    /// <returns>A new <see cref="CustomAttributeBuilder"/> instance.</returns>
    private static CustomAttributeBuilder CreateCustomAttributeBuilder(CustomAttributeData data)
    {
        ConstructorInfo ctor = data.Constructor;
        object[] ctorArgs = data.ConstructorArguments.Select(ConvertTypedArgumentToObject).ToArray();
        IEnumerable<CustomAttributeNamedArgument> propertyArguments = data.NamedArguments.Where(static x => x.MemberInfo is PropertyInfo);
        PropertyInfo[] properties = propertyArguments.Select(static x => (PropertyInfo)x.MemberInfo).ToArray();
        object[] propertyValues = propertyArguments.Select(static x => ConvertTypedArgumentToObject(x.TypedValue)).ToArray();
        IEnumerable<CustomAttributeNamedArgument> fieldArguments = data.NamedArguments.Where(static x => x.MemberInfo is FieldInfo);
        FieldInfo[] fields = fieldArguments.Select(static x => (FieldInfo)x.MemberInfo).ToArray();
        object[] fieldValues = fieldArguments.Select(static x => ConvertTypedArgumentToObject(x.TypedValue)).ToArray();
        return CreateCustomAttributeBuilder(ctor, ctorArgs, properties, propertyValues, fields, fieldValues);
    }

    /// <summary>
    /// Converts a <see cref="CustomAttributeTypedArgument"/> into its corresponding runtime value.
    /// </summary>
    /// <param name="typedArgument">The typed argument to convert.</param>
    /// <returns>The corresponding runtime value.</returns>
    private static object ConvertTypedArgumentToObject(CustomAttributeTypedArgument typedArgument)
    {
        if (!typedArgument.ArgumentType.IsArray || typedArgument.Value is not IEnumerable collection)
            return typedArgument.Value;

        int i = 0;
        int count = collection.Cast<object>().Count();
        Array array = Array.CreateInstance(typedArgument.ArgumentType.GetElementType(), count);
        foreach (object element in collection)
        {
            object convertedElement = element is CustomAttributeTypedArgument nestedTypedArgument
                ? ConvertTypedArgumentToObject(nestedTypedArgument)
                : element;

            array.SetValue(convertedElement, i++);
        }
        return array;
    }

    /// <summary>
    /// Gets the binary serialized representation of this instance.
    /// </summary>
    public byte[] Attribute
    {
        get
        {
            byte[]? blob = (byte[]?)s_customAttributeBuilderBlob?.GetValue(_customAttributeBuilder);
            return (blob ?? throw new MissingFieldException(typeof(CustomAttributeBuilder).FullName, "m_blob")).ToArray();
        }
    }

    /// <summary>
    /// Returns the <see cref="CustomAttributeBuilder"/> used to build this attribute instance.
    /// </summary>
    /// <returns>The <see cref="CustomAttributeBuilder"/> used to build this instance.</returns>
    public CustomAttributeBuilder AsCustomAttributeBuilder()
        => _customAttributeBuilder;
}
