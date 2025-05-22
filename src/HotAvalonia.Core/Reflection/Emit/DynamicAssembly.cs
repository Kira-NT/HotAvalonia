using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Defines and represents a dynamic assembly.
/// </summary>
internal sealed class DynamicAssembly : Assembly, IRuntimeAccessManager
{
    /// <summary>
    /// The dynamic module used to define new types and methods.
    /// </summary>
    private readonly ModuleBuilder _module;

    /// <summary>
    /// Defines a dynamic assembly that has the specified name and access rights.
    /// </summary>
    /// <param name="name">The name of the assembly.</param>
    /// <param name="access">The access rights of the assembly.</param>
    public DynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
    {
        _ = name ?? throw new ArgumentNullException(nameof(name));

#pragma warning disable RS0030 // Do not use banned APIs
        using DynamicCodeScope scope = DynamicCodeScope.Create(name.Name, nameof(DynamicAssembly));
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, access);
        _module = assembly.DefineDynamicModule(assembly.ManifestModule.ScopeName);
#pragma warning restore RS0030 // Do not use banned APIs
    }

    /// <summary>
    /// Defines a new assembly that has the specified name, access rights, and attributes.
    /// </summary>
    /// <param name="name">The name of the assembly.</param>
    /// <param name="access">The access rights of the assembly.</param>
    /// <param name="assemblyAttributes">A collection that contains the attributes of the assembly.</param>
    public DynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
        : this(name, access)
    {
        foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes ?? [])
            SetCustomAttribute(assemblyAttribute);
    }

    /// <summary>
    /// Gets a shared <see cref="DynamicAssembly"/> instance.
    /// </summary>
    [field: AllowNull]
    public static DynamicAssembly Shared
    {
        get
        {
            // This getter lacks in the thread-safety department to justify the name "Shared".
            // However, it works just fine for now, so I don't really care.
            // But maybe consider adding a lock of some kind in the future.
            if (field is not null)
                return field;

            Assembly parentAssembly = typeof(DynamicAssembly).Assembly;
            field = new(new($"<{parentAssembly.GetName().Name}>k__DynamicAssembly"), AssemblyBuilderAccess.RunAndCollect);
            field.AllowAccessTo(parentAssembly);
            return field;
        }
    }

    /// <inheritdoc/>
    public override string? FullName => _module.Assembly.FullName;

    /// <inheritdoc/>
    public override string Location => _module.Assembly.Location;

    /// <inheritdoc/>
    public override string? CodeBase => _module.Assembly.CodeBase;

    /// <inheritdoc/>
    public override string EscapedCodeBase => _module.Assembly.EscapedCodeBase;

    /// <inheritdoc/>
    public override IEnumerable<Module> Modules => _module.Assembly.Modules;

    /// <inheritdoc/>
    public override Module ManifestModule => _module.Assembly.ManifestModule;

    /// <inheritdoc/>
    public override MethodInfo? EntryPoint => _module.Assembly.EntryPoint;

    /// <inheritdoc/>
    public override IEnumerable<CustomAttributeData> CustomAttributes => _module.Assembly.CustomAttributes;

    /// <inheritdoc/>
    public override IEnumerable<TypeInfo> DefinedTypes => _module.Assembly.DefinedTypes;

    /// <inheritdoc/>
    public override IEnumerable<Type> ExportedTypes => _module.Assembly.ExportedTypes;

    /// <inheritdoc/>
    public override bool IsDynamic => true;

    /// <inheritdoc/>
    public override bool ReflectionOnly => _module.Assembly.ReflectionOnly;

    /// <inheritdoc/>
    public override bool GlobalAssemblyCache => _module.Assembly.GlobalAssemblyCache;

    /// <inheritdoc/>
    public override long HostContext => _module.Assembly.HostContext;

    /// <inheritdoc/>
    public override string ImageRuntimeVersion => _module.Assembly.ImageRuntimeVersion;

    /// <inheritdoc/>
    public override SecurityRuleSet SecurityRuleSet => _module.Assembly.SecurityRuleSet;

    /// <inheritdoc/>
    public override event ModuleResolveEventHandler ModuleResolve
    {
        add => _module.Assembly.ModuleResolve += value;
        remove => _module.Assembly.ModuleResolve -= value;
    }

    /// <inheritdoc/>
    public override AssemblyName GetName()
        => _module.Assembly.GetName();

    /// <inheritdoc/>
    public override AssemblyName GetName(bool copiedName)
        => _module.Assembly.GetName(copiedName);

    /// <inheritdoc/>
    public void AllowAccessTo(Assembly assembly)
        => ((AssemblyBuilder)_module.Assembly).AllowAccessTo(assembly);

    /// <inheritdoc/>
    public void AllowAccessTo(Module module)
        => ((AssemblyBuilder)_module.Assembly).AllowAccessTo(module);

    /// <inheritdoc/>
    public void AllowAccessTo(MemberInfo member)
        => ((AssemblyBuilder)_module.Assembly).AllowAccessTo(member);

    /// <inheritdoc cref="AssemblyBuilder.SetCustomAttribute(CustomAttributeBuilder)"/>
    public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        => ((AssemblyBuilder)_module.Assembly).SetCustomAttribute(customBuilder);

    /// <summary>
    /// Defines a global method.
    /// </summary>
    /// <param name="name">The name of the global method.</param>
    /// <param name="returnType">The return type of the global method.</param>
    /// <param name="parameterTypes">The types of the parameters of the global method.</param>
    /// <param name="skipVisibility">
    /// <c>true</c> to skip JIT visibility checks on types and members accessed by the MSIL of the global method;
    /// otherwise, <c>false</c>.
    /// </param>
    /// <returns>The defined global method.</returns>
    public DynamicMethodBuilder DefineMethod(string name, Type? returnType, Type[]? parameterTypes, bool skipVisibility = false)
        => new(name, returnType, parameterTypes, _module, skipVisibility);

    /// <summary>
    /// Defines an enumeration type that is a value type with a single non-static field called <c>value__</c> of the specified type.
    /// </summary>
    /// <param name="name">The full path of the enumeration type.</param>
    /// <param name="visibility">The type attributes for the enumeration.</param>
    /// <param name="underlyingType">The underlying type for the enumeration. This must be a built-in integer type.</param>
    /// <returns>The defined enumeration.</returns>
    public DynamicEnumBuilder DefineEnum(string name, TypeAttributes visibility = TypeAttributes.NotPublic, Type? underlyingType = null)
        => new(_module, name, visibility, underlyingType);

    /// <summary>
    /// Defines a type.
    /// </summary>
    /// <param name="name">The full path of the type.</param>
    /// <param name="attributes">The attributes of the defined type.</param>
    /// <param name="parent">The type that the defined type extends.</param>
    /// <param name="packingSize">The packing size of the type.</param>
    /// <param name="typeSize">The total size of the type.</param>
    /// <returns>The defined type.</returns>
    public DynamicTypeBuilder DefineType(string name, TypeAttributes attributes = TypeAttributes.NotPublic, Type? parent = null, PackingSize packingSize = PackingSize.Unspecified, int typeSize = 0)
        => new(_module, name, attributes, parent, packingSize, typeSize);

    /// <inheritdoc/>
    public override bool IsDefined(Type attributeType, bool inherit)
        => _module.Assembly.IsDefined(attributeType, inherit);

    /// <inheritdoc/>
    public override object[] GetCustomAttributes(bool inherit)
        => _module.Assembly.GetCustomAttributes(inherit);

    /// <inheritdoc/>
    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        => _module.Assembly.GetCustomAttributes(attributeType, inherit);

    /// <inheritdoc/>
    public override IList<CustomAttributeData> GetCustomAttributesData()
        => _module.Assembly.GetCustomAttributesData();

    /// <inheritdoc/>
    public override Type[] GetExportedTypes()
        => _module.Assembly.GetExportedTypes();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
    /// <inheritdoc/>
    public override Type[] GetForwardedTypes()
        => _module.Assembly.GetForwardedTypes();
#endif

    /// <inheritdoc/>
    public override Type[] GetTypes()
        => _module.Assembly.GetTypes();

    /// <inheritdoc/>
    public override Type? GetType(string name)
        => _module.Assembly.GetType(name);

    /// <inheritdoc/>
    public override Type? GetType(string name, bool throwOnError)
        => _module.Assembly.GetType(name, throwOnError);

    /// <inheritdoc/>
    public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        => _module.Assembly.GetType(name, throwOnError, ignoreCase);

    /// <inheritdoc/>
    public override object? CreateInstance(string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object[]? args, CultureInfo? culture, object[]? activationAttributes)
        => _module.Assembly.CreateInstance(typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);

    /// <inheritdoc/>
    public override FileStream[] GetFiles(bool getResourceModules)
        => _module.Assembly.GetFiles(getResourceModules);

    /// <inheritdoc/>
    public override FileStream[] GetFiles()
        => _module.Assembly.GetFiles();

    /// <inheritdoc/>
    public override FileStream GetFile(string name)
        => _module.Assembly.GetFile(name);

    /// <inheritdoc/>
    public override AssemblyName[] GetReferencedAssemblies()
        => _module.Assembly.GetReferencedAssemblies();

    /// <inheritdoc/>
    public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        => _module.Assembly.GetSatelliteAssembly(culture, version);

    /// <inheritdoc/>
    public override Assembly GetSatelliteAssembly(CultureInfo culture)
        => _module.Assembly.GetSatelliteAssembly(culture);

    /// <inheritdoc/>
    public override Module[] GetLoadedModules(bool getResourceModules)
        => _module.Assembly.GetLoadedModules(getResourceModules);

    /// <inheritdoc/>
    public override Module[] GetModules(bool getResourceModules)
        => _module.Assembly.GetModules(getResourceModules);

    /// <inheritdoc/>
    public override Module? GetModule(string name)
        => _module.Assembly.GetModule(name);

    /// <inheritdoc/>
    public override Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
        => _module.Assembly.LoadModule(moduleName, rawModule, rawSymbolStore);

    /// <inheritdoc/>
    public override string[] GetManifestResourceNames()
        => _module.Assembly.GetManifestResourceNames();

    /// <inheritdoc/>
    public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        => _module.Assembly.GetManifestResourceInfo(resourceName);

    /// <inheritdoc/>
    public override Stream? GetManifestResourceStream(Type type, string name)
        => _module.Assembly.GetManifestResourceStream(type, name);

    /// <inheritdoc/>
    public override Stream? GetManifestResourceStream(string name)
        => _module.Assembly.GetManifestResourceStream(name);

    /// <inheritdoc/>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
        => _module.Assembly.GetObjectData(info, context);

    /// <inheritdoc/>
    public override int GetHashCode()
        => _module.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object? o)
        => o is DynamicAssembly asm && asm._module == _module;

    /// <inheritdoc/>
    public override string ToString()
        => _module.Assembly.ToString();
}
