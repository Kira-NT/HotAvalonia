using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HotAvalonia.Helpers;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Provides methods for building dynamic types.
/// </summary>
internal sealed class DynamicTypeBuilder : IDisposable
{
    /// <summary>
    /// The underlying <see cref="TypeBuilder"/> used to build the type.
    /// </summary>
    private readonly TypeBuilder _builder;

    /// <summary>
    /// The dynamic code scope.
    /// </summary>
    private readonly DynamicCodeScope _scope;

    /// <summary>
    /// Defines a dynamic type.
    /// </summary>
    /// <param name="module">The <see cref="ModuleBuilder"/> where the type resides.</param>
    /// <param name="name">The full path of the type.</param>
    /// <param name="attributes">The attributes of the defined type.</param>
    /// <param name="parent">The type that the defined type extends.</param>
    /// <param name="packingSize">The packing size of the type.</param>
    /// <param name="typeSize">The total size of the type.</param>
    public DynamicTypeBuilder(ModuleBuilder module, string name, TypeAttributes attributes, Type? parent, PackingSize packingSize, int typeSize)
    {
        _ = module ?? throw new ArgumentNullException(nameof(module));
        _ = name ?? throw new ArgumentNullException(nameof(name));

#pragma warning disable RS0030 // Do not use banned APIs
        _scope = DynamicCodeScope.Create(name, nameof(DynamicTypeBuilder));
        _builder = module.DefineType(name, attributes, parent, packingSize, typeSize);
#pragma warning restore RS0030 // Do not use banned APIs
    }

    /// <inheritdoc cref="DynamicTypeBuilder(ModuleBuilder, string, TypeAttributes, Type, PackingSize, int)"/>
    public DynamicTypeBuilder(ModuleBuilder module, string name)
        : this(module, name, TypeAttributes.NotPublic, parent: null, PackingSize.Unspecified, typeSize: 0)
    {
    }

    /// <inheritdoc cref="DynamicTypeBuilder(ModuleBuilder, string, TypeAttributes, Type, PackingSize, int)"/>
    public DynamicTypeBuilder(ModuleBuilder module, string name, TypeAttributes attributes)
        : this(module, name, attributes, parent: null, PackingSize.Unspecified, typeSize: 0)
    {
    }

    /// <inheritdoc cref="DynamicTypeBuilder(ModuleBuilder, string, TypeAttributes, Type, PackingSize, int)"/>
    public DynamicTypeBuilder(ModuleBuilder module, string name, TypeAttributes attributes, Type? parent)
        : this(module, name, attributes, parent, PackingSize.Unspecified, typeSize: 0)
    {
    }

    /// <inheritdoc cref="DynamicTypeBuilder(ModuleBuilder, string, TypeAttributes, Type, PackingSize, int)"/>
    public DynamicTypeBuilder(ModuleBuilder module, string name, TypeAttributes attributes, Type? parent, int typeSize)
        : this(module, name, attributes, parent, PackingSize.Unspecified, typeSize)
    {
    }

    /// <inheritdoc cref="DynamicTypeBuilder(ModuleBuilder, string, TypeAttributes, Type, PackingSize, int)"/>
    public DynamicTypeBuilder(ModuleBuilder module, string name, TypeAttributes attributes, Type? parent, PackingSize packingSize)
        : this(module, name, attributes, parent, packingSize, typeSize: 0)
    {
    }

    /// <inheritdoc cref="DynamicTypeBuilder(ModuleBuilder, string, TypeAttributes, Type, PackingSize, int)"/>
    /// <param name="interfaces">The list of interfaces that the type implements.</param>
    public DynamicTypeBuilder(ModuleBuilder module, string name, TypeAttributes attributes, Type? parent, Type[]? interfaces)
        : this(module, name, attributes, parent, PackingSize.Unspecified, typeSize: 0)
    {
        foreach (Type interfaceImpl in interfaces ?? [])
            _builder.AddInterfaceImplementation(interfaceImpl);
    }

    /// <summary>
    /// Implicitly converts a <see cref="DynamicTypeBuilder"/> to its corresponding <see cref="TypeBuilder"/>.
    /// </summary>
    /// <param name="dynamicTypeBuilder">The <see cref="DynamicTypeBuilder"/> to convert.</param>
    /// <returns>A <see cref="TypeBuilder"/> representing the type.</returns>
    [return: NotNullIfNotNull(nameof(dynamicTypeBuilder))]
    public static implicit operator TypeBuilder?(DynamicTypeBuilder? dynamicTypeBuilder)
        => dynamicTypeBuilder?._builder;

    /// <inheritdoc cref="TypeBuilder.IsCreated()"/>
    public bool IsCreated()
        => _builder.IsCreated();

    /// <inheritdoc cref="TypeBuilder.SetCustomAttribute(CustomAttributeBuilder)"/>
    public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        => _builder.SetCustomAttribute(customBuilder);

    /// <inheritdoc cref="TypeBuilder.SetParent(Type)"/>
    public void SetParent(Type? parent)
        => _builder.SetParent(parent);

    /// <inheritdoc cref="TypeBuilder.AddInterfaceImplementation(Type)"/>
    public void AddInterfaceImplementation(Type interfaceType)
        => _builder.AddInterfaceImplementation(interfaceType);

    /// <inheritdoc cref="TypeBuilder.DefineTypeInitializer()"/>
    public ConstructorBuilder DefineTypeInitializer()
        => _builder.DefineTypeInitializer();

    /// <inheritdoc cref="TypeBuilder.DefineDefaultConstructor(MethodAttributes)"/>
    public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
        => _builder.DefineDefaultConstructor(attributes);

    /// <inheritdoc cref="TypeBuilder.DefineConstructor(MethodAttributes, CallingConventions, Type[])"/>
    public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
        => _builder.DefineConstructor(attributes, callingConvention, parameterTypes);

    /// <inheritdoc cref="TypeBuilder.DefineConstructor(MethodAttributes, CallingConventions, Type[], Type[][], Type[][])"/>
    public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        => _builder.DefineConstructor(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);

    /// <inheritdoc cref="TypeBuilder.DefineEvent(string, EventAttributes, Type)"/>
    public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventType)
        => _builder.DefineEvent(name, attributes, eventType);

    /// <inheritdoc cref="TypeBuilder.DefineField(string, Type, FieldAttributes)"/>
    public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
        => _builder.DefineField(fieldName, type, attributes);

    /// <inheritdoc cref="TypeBuilder.DefineField(string, Type, Type[], Type[], FieldAttributes)"/>
    public FieldBuilder DefineField(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
        => _builder.DefineField(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);

    /// <inheritdoc cref="TypeBuilder.DefineGenericParameters(string[])"/>
    public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        => _builder.DefineGenericParameters(names);

    /// <inheritdoc cref="TypeBuilder.DefineInitializedData(string, byte[], FieldAttributes)"/>
    public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        => _builder.DefineInitializedData(name, data, attributes);

    /// <inheritdoc cref="TypeBuilder.DefineUninitializedData(string, int, FieldAttributes)"/>
    public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
        => _builder.DefineUninitializedData(name, size, attributes);

    /// <inheritdoc cref="TypeBuilder.DefineMethodOverride(MethodInfo, MethodInfo)"/>
    public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        => _builder.DefineMethodOverride(methodInfoBody, methodInfoDeclaration);

    /// <inheritdoc cref="TypeBuilder.DefineMethod(string, MethodAttributes, Type, Type[])"/>
    public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
        => _builder.DefineMethod(name, attributes, returnType, parameterTypes);

    /// <inheritdoc cref="TypeBuilder.DefineMethod(string, MethodAttributes, CallingConventions, Type, Type[], Type[], Type[], Type[][], Type[][])"/>
    public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        => _builder.DefineMethod(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

    /// <inheritdoc cref="TypeBuilder.DefineMethod(string, MethodAttributes, CallingConventions, Type, Type[])"/>
    public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
        => _builder.DefineMethod(name, attributes, callingConvention, returnType, parameterTypes);

    /// <inheritdoc cref="TypeBuilder.DefineMethod(string, MethodAttributes, CallingConventions)"/>
    public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
        => _builder.DefineMethod(name, attributes, callingConvention);

    /// <inheritdoc cref="TypeBuilder.DefineMethod(string, MethodAttributes)"/>
    public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
        => _builder.DefineMethod(name, attributes);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    /// <inheritdoc cref="TypeBuilder.DefinePInvokeMethod(string, string, string, MethodAttributes, CallingConventions, Type, Type[], Type[], Type[], Type[][], Type[][], CallingConvention, CharSet)"/>
    public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
        => _builder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, nativeCallConv, nativeCharSet);

    /// <inheritdoc cref="TypeBuilder.DefinePInvokeMethod(string, string, MethodAttributes, CallingConventions, Type, Type[], CallingConvention, CharSet)"/>
    public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        => _builder.DefinePInvokeMethod(name, dllName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);

    /// <inheritdoc cref="TypeBuilder.DefinePInvokeMethod(string, string, string, MethodAttributes, CallingConventions, Type, Type[], CallingConvention, CharSet)"/>
    public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        => _builder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
#endif

    /// <inheritdoc cref="TypeBuilder.DefineNestedType(string)"/>
    public TypeBuilder DefineNestedType(string name)
        => _builder.DefineNestedType(name);

    /// <inheritdoc cref="TypeBuilder.DefineNestedType(string, TypeAttributes)"/>
    public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
        => _builder.DefineNestedType(name, attr);

    /// <inheritdoc cref="TypeBuilder.DefineNestedType(string, TypeAttributes, Type)"/>
    public TypeBuilder DefineNestedType(string name, TypeAttributes attr, Type? parent)
        => _builder.DefineNestedType(name, attr, parent);

    /// <inheritdoc cref="TypeBuilder.DefineNestedType(string, TypeAttributes, Type, int)"/>
    public TypeBuilder DefineNestedType(string name, TypeAttributes attr, Type? parent, int typeSize)
        => _builder.DefineNestedType(name, attr, parent, typeSize);

    /// <inheritdoc cref="TypeBuilder.DefineNestedType(string, TypeAttributes, Type, PackingSize)"/>
    public TypeBuilder DefineNestedType(string name, TypeAttributes attr, Type? parent, PackingSize packSize)
        => _builder.DefineNestedType(name, attr, parent, packSize);

    /// <inheritdoc cref="TypeBuilder.DefineNestedType(string, TypeAttributes, Type, PackingSize, int)"/>
    public TypeBuilder DefineNestedType(string name, TypeAttributes attr, Type? parent, PackingSize packSize, int typeSize)
        => _builder.DefineNestedType(name, attr, parent, packSize, typeSize);

    /// <inheritdoc cref="TypeBuilder.DefineNestedType(string, TypeAttributes, Type, Type[])"/>
    public TypeBuilder DefineNestedType(string name, TypeAttributes attr, Type? parent, Type[]? interfaces)
        => _builder.DefineNestedType(name, attr, parent, interfaces);

    /// <inheritdoc cref="TypeBuilder.DefineProperty(string, PropertyAttributes, CallingConventions, Type, Type[])"/>
    public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
        => _builder.DefineProperty(name, attributes, callingConvention, returnType, parameterTypes);

    /// <inheritdoc cref="TypeBuilder.DefineProperty(string, PropertyAttributes, CallingConventions, Type, Type[], Type[], Type[], Type[][], Type[][])"/>
    public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        => _builder.DefineProperty(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

    /// <inheritdoc cref="TypeBuilder.DefineProperty(string, PropertyAttributes, Type, Type[])"/>
    public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type? returnType, Type[]? parameterTypes)
        => _builder.DefineProperty(name, attributes, returnType, parameterTypes);

    /// <inheritdoc cref="TypeBuilder.DefineProperty(string, PropertyAttributes, Type, Type[], Type[], Type[], Type[][], Type[][])"/>
    public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        => _builder.DefineProperty(name, attributes, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

    /// <inheritdoc cref="TypeBuilder.CreateTypeInfo()"/>
    public TypeInfo CreateTypeInfo()
    {
        TypeInfo type = _builder.CreateTypeInfo();
        LoggingHelper.LogInfo("Created dynamic type: {Type}", type);
        return type;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
        => _builder.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object obj)
        => obj is DynamicTypeBuilder other && _builder.Equals(other._builder);

    /// <inheritdoc cref="TypeBuilder.ToString()"/>
    public override string ToString()
        => _builder.ToString();

    /// <inheritdoc/>
    public void Dispose()
        => _scope.Dispose();
}
