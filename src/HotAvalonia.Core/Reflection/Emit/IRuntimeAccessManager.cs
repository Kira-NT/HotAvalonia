using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HotAvalonia.Helpers;

namespace HotAvalonia.Reflection.Emit;

/// <summary>
/// Defines methods for granting runtime access to otherwise inaccessible code elements.
/// </summary>
internal interface IRuntimeAccessManager
{
    /// <summary>
    /// Grants access to all internal types and members of the specified assembly.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to which access should be granted.</param>
    void AllowAccessTo(Assembly assembly);

    /// <summary>
    /// Grants access to all internal types and members of the specified module.
    /// </summary>
    /// <param name="module">The <see cref="Module"/> to which access should be granted.</param>
    void AllowAccessTo(Module module);

    /// <summary>
    /// Grants access to the the specified member.
    /// </summary>
    /// <param name="member">The <see cref="MemberInfo"/> to which access should be granted.</param>
    void AllowAccessTo(MemberInfo member);
}

/// <summary>
/// Defines methods for granting dynamic assemblies access to otherwise inaccessible members from other assemblies.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class RuntimeAccessManager
{
    /// <summary>
    /// The constructor reference for <c>IgnoresAccessChecksToAttribute(string assemblyName)</c>.
    /// </summary>
    private static readonly ConstructorInfo s_ignoresAccessChecksToAttributeCtor = CreateIgnoresAccessChecksToAttributeType().GetConstructor([typeof(string)])!;

    /// <summary>
    /// A weak table that tracks assemblies made accessible to dynamic assemblies through this class.
    /// </summary>
    private static readonly ConditionalWeakTable<AssemblyBuilder, HashSet<string>> s_accessibleAssemblies = new();

    /// <summary>
    /// Grants the dynamic assembly access to all internal types and members of the assembly with the specified name.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target assembly.</param>
    /// <param name="targetAssemblyName">The name of the assembly to which <paramref name="sourceAssembly"/> requires access.</param>
    private static void AllowAccessTo(this AssemblyBuilder sourceAssembly, string targetAssemblyName)
    {
        if (string.IsNullOrWhiteSpace(targetAssemblyName))
            return;

        HashSet<string> accessibleAssemblies = s_accessibleAssemblies.GetOrCreateValue(sourceAssembly);
        if (accessibleAssemblies.Contains(targetAssemblyName))
            return;

        CustomAttribute ignoresAccessCheckAttribute = new(s_ignoresAccessChecksToAttributeCtor, [targetAssemblyName]);
        sourceAssembly.SetCustomAttribute(ignoresAccessCheckAttribute);
        accessibleAssemblies.Add(targetAssemblyName);
    }

    /// <summary>
    /// Grants the dynamic assembly access to all internal types and members of the assembly with the specified name.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target assembly.</param>
    /// <param name="targetAssemblyName">The name of the assembly to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, AssemblyName targetAssemblyName)
    {
        _ = targetAssemblyName ?? throw new ArgumentNullException(nameof(targetAssemblyName));

        string? name = targetAssemblyName.Name;
        byte[]? key = targetAssemblyName.GetPublicKey();
        if (!string.IsNullOrWhiteSpace(name) && key is { Length: > 0 })
            name = $"{name}, PublicKey={BitConverter.ToString(key).Replace("-", string.Empty).ToUpperInvariant()}";

        sourceAssembly.AllowAccessTo(name);
    }

    /// <summary>
    /// Grants the dynamic assembly access to all internal types and members of the specified assembly.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target assembly.</param>
    /// <param name="targetAssembly">The assembly to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, Assembly targetAssembly)
    {
        _ = targetAssembly ?? throw new ArgumentNullException(nameof(targetAssembly));

        sourceAssembly.AllowAccessTo(targetAssembly.GetName());
    }

    /// <summary>
    /// Grants the dynamic assembly access to all internal types and members of the specified module.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target module.</param>
    /// <param name="targetModule">The module to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, Module targetModule)
    {
        _ = targetModule ?? throw new ArgumentNullException(nameof(targetModule));

        sourceAssembly.AllowAccessTo(targetModule.Assembly.GetName());
    }

    /// <summary>
    /// Grants the dynamic assembly access to the specified member.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target member.</param>
    /// <param name="targetMember">The member to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, MemberInfo targetMember)
    {
        switch (targetMember)
        {
            case null:
                throw new ArgumentNullException(nameof(targetMember));

            case Type type:
                sourceAssembly.AllowAccessTo(type);
                break;

            case MethodBase method:
                sourceAssembly.AllowAccessTo(method);
                break;

            case PropertyInfo propertyInfo:
                sourceAssembly.AllowAccessTo(propertyInfo);
                break;

            case FieldInfo fieldInfo:
                sourceAssembly.AllowAccessTo(fieldInfo);
                break;

            case EventInfo eventInfo:
                sourceAssembly.AllowAccessTo(eventInfo);
                break;

            default:
                sourceAssembly.AllowAccessTo(targetMember.Module.Assembly.GetName());
                break;
        }
    }

    /// <summary>
    /// Grants the dynamic assembly access to the specified type.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target type.</param>
    /// <param name="targetType">The type to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, Type targetType)
    {
        _ = targetType ?? throw new ArgumentNullException(nameof(targetType));

        IEnumerable<Assembly> assemblies = GetReferencedAssemblies(targetType);
        foreach (Assembly assembly in assemblies.Distinct())
            sourceAssembly.AllowAccessTo(assembly.GetName());
    }

    /// <summary>
    /// Grants the dynamic assembly access to the specified method.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target method.</param>
    /// <param name="targetMethod">The method to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, MethodBase targetMethod)
    {
        _ = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));

        IEnumerable<Assembly> assemblies = GetReferencedAssemblies(targetMethod);
        foreach (Assembly assembly in assemblies.Distinct())
            sourceAssembly.AllowAccessTo(assembly.GetName());
    }

    /// <summary>
    /// Grants the dynamic assembly access to the specified property.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target property.</param>
    /// <param name="targetProperty">The property to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, PropertyInfo targetProperty)
    {
        _ = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));

        IEnumerable<Assembly> assemblies = targetProperty.GetAccessors(nonPublic: true).SelectMany(GetReferencedAssemblies);
        foreach (Assembly assembly in assemblies.Distinct())
            sourceAssembly.AllowAccessTo(assembly.GetName());
    }

    /// <summary>
    /// Grants the dynamic assembly access to the specified field.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target field.</param>
    /// <param name="targetField">The field to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, FieldInfo targetField)
    {
        _ = targetField ?? throw new ArgumentNullException(nameof(targetField));

        IEnumerable<Assembly> assemblies = GetReferencedAssemblies(targetField.FieldType).Concat(targetField.DeclaringType is Type t ? GetReferencedAssemblies(t) : []);
        foreach (Assembly assembly in assemblies.Distinct())
            sourceAssembly.AllowAccessTo(assembly.GetName());
    }

    /// <summary>
    /// Grants the dynamic assembly access to the specified event.
    /// </summary>
    /// <param name="sourceAssembly">The dynamic assembly that requires access to the specified target event.</param>
    /// <param name="targetEvent">The event to which <paramref name="sourceAssembly"/> requires access.</param>
    public static void AllowAccessTo(this AssemblyBuilder sourceAssembly, EventInfo targetEvent)
    {
        _ = targetEvent ?? throw new ArgumentNullException(nameof(targetEvent));

        List<MethodInfo> methods = new(3);
        if (targetEvent.GetAddMethod(nonPublic: true) is MethodInfo addMethod)
            methods.Add(addMethod);

        if (targetEvent.GetRemoveMethod(nonPublic: true) is MethodInfo removeMethod)
            methods.Add(removeMethod);

        if (targetEvent.GetRaiseMethod(nonPublic: true) is MethodInfo raiseMethod)
            methods.Add(raiseMethod);

        IEnumerable<Assembly> assemblies = targetEvent.GetOtherMethods(nonPublic: true).Concat(methods).SelectMany(GetReferencedAssemblies);
        foreach (Assembly assembly in assemblies.Distinct())
            sourceAssembly.AllowAccessTo(assembly.GetName());
    }

    /// <summary>
    /// Returns a collection of assemblies referenced by the specified method.
    /// </summary>
    /// <param name="method">The method for which to retrieve the referenced assemblies.</param>
    /// <returns>A collection of assemblies referenced by the specified method.</returns>
    private static IEnumerable<Assembly> GetReferencedAssemblies(MethodBase method)
    {
        IEnumerable<Assembly> assemblies = method.GetParameters().SelectMany(static x => GetReferencedAssemblies(x.ParameterType));
        if (method is MethodInfo methodInfo)
            assemblies = assemblies.Concat(GetReferencedAssemblies(methodInfo.ReturnType));

        if (method.IsGenericMethod)
            assemblies = assemblies.Concat(method.GetGenericArguments().SelectMany(GetReferencedAssemblies));

        return assemblies.Concat(method.DeclaringType is Type t ? GetReferencedAssemblies(t) : [method.Module.Assembly]);
    }

    /// <summary>
    /// Returns a collection of assemblies referenced by the specified type.
    /// </summary>
    /// <param name="type">The type for which to retrieve the referenced assemblies.</param>
    /// <returns>A collection of assemblies referenced by the specified type.</returns>
    private static IEnumerable<Assembly> GetReferencedAssemblies(Type type)
    {
        if (type.IsGenericParameter)
            return [];

        IEnumerable<Assembly> assemblies = [type.Assembly];
        if (type.IsGenericType)
            assemblies = assemblies.Concat(type.GetGenericArguments().SelectMany(GetReferencedAssemblies));

        return assemblies;
    }

    /// <summary>
    /// Creates a dynamic type that represents the <c>System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute</c>.
    /// </summary>
    /// <remarks>
    /// This <i>undocumented</i> attribute allows bypassing access checks to internal members of a specified assembly.
    ///
    /// You can think of it as a long-lost cousin of the <see cref="InternalsVisibleToAttribute"/>, which works
    /// somewhat similarly, but in the opposite direction. I.e., instead of us giving another assembly permission
    /// to access our internals, the other assembly can happily help itself and freely get to our internal members.
    /// </remarks>
    /// <returns>The dynamically created type that represents <c>IgnoresAccessChecksToAttribute</c>.</returns>
    private static Type CreateIgnoresAccessChecksToAttributeType()
    {
        string fullName = "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute";
        if (DynamicAssembly.Shared.GetType(fullName, throwOnError: false) is Type existingType)
            return existingType;

        using DynamicTypeBuilder attributeBuilder = DynamicAssembly.Shared.DefineType(fullName, TypeAttributes.Public, typeof(Attribute));
        attributeBuilder.SetCustomAttribute(new CustomAttribute
        (
            typeof(AttributeUsageAttribute).GetConstructor([typeof(AttributeTargets)])!,
            [AttributeTargets.Assembly],

            [typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))!],
            [true]
        ));

        FieldBuilder nameFieldBuilder = attributeBuilder.DefineField("<AssemblyName>k__BackingField", typeof(string), FieldAttributes.Private);
        PropertyBuilder namePropertyBuilder = attributeBuilder.DefineProperty("AssemblyName", PropertyAttributes.None, typeof(string), []);
        MethodBuilder nameGetterBuilder = attributeBuilder.DefineMethod("get_AssemblyName", MethodAttributes.Public, typeof(string), []);
        ILGenerator nameIl = nameGetterBuilder.GetILGenerator();
        nameIl.Emit(OpCodes.Ldarg_0);
        nameIl.Emit(OpCodes.Ldfld, nameFieldBuilder);
        nameIl.Emit(OpCodes.Ret);
        namePropertyBuilder.SetGetMethod(nameGetterBuilder);

        ConstructorBuilder ctorBuilder = attributeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(string)]);
        ILGenerator ctorIl = ctorBuilder.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Ldarg_1);
        ctorIl.Emit(OpCodes.Stfld, nameFieldBuilder);
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(Attribute).GetInstanceConstructor()!);
        ctorIl.Emit(OpCodes.Ret);

        return attributeBuilder.CreateTypeInfo();
    }
}
