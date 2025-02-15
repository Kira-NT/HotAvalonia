using System.Reflection;
using HotAvalonia.Fody.Cecil;
using Mono.Cecil;

namespace HotAvalonia.Fody.Helpers;

/// <summary>
/// Provides extension methods and related functionality for working with type references.
/// </summary>
internal static class TypeReferenceHelper
{
    /// <inheritdoc cref="GetField(TypeDefinition?, string, BindingFlags, TypeName)"/>
    public static FieldDefinition? GetField(this TypeDefinition? type, string name)
        => type.GetFieldsCore(name, BindingFlags.Default).SingleOrDefault();

    /// <inheritdoc cref="GetField(TypeDefinition?, string, BindingFlags, TypeName)"/>
    public static FieldDefinition? GetField(this TypeDefinition? type, string name, TypeName fieldType)
        => type.GetField(name, BindingFlags.Default, fieldType);

    /// <inheritdoc cref="GetField(TypeDefinition?, string, BindingFlags, TypeName)"/>
    public static FieldDefinition? GetField(this TypeDefinition? type, string name, BindingFlags bindingFlags)
        => type.GetFieldsCore(name, bindingFlags).SingleOrDefault();

    /// <summary>
    /// Searches for the field defined for the given <see cref="TypeDefinition"/> that matches the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for fields.</param>
    /// <param name="name">The name of the field to find.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="fieldType">The type of the field to find.</param>
    /// <returns>A field defined for the given type that matches the specified constraints, if found; otherwise, <c>null</c>.</returns>
    public static FieldDefinition? GetField(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName fieldType)
        => type.GetFieldsCore(name, bindingFlags).Where(x => x.FieldType == fieldType).SingleOrDefault();

    /// <inheritdoc cref="GetFieldsCore"/>
    public static IEnumerable<FieldDefinition> GetFields(this TypeDefinition? type)
        => type.GetFieldsCore(null, BindingFlags.Default);

    /// <inheritdoc cref="GetFieldsCore"/>
    public static IEnumerable<FieldDefinition> GetFields(this TypeDefinition? type, BindingFlags bindingFlags)
        => type.GetFieldsCore(null, bindingFlags);

    /// <summary>
    /// Searches for the fields defined for the given <see cref="TypeDefinition"/> that match the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for fields.</param>
    /// <param name="name">The name of the fields to find.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <returns>An enumerable containing fields defined for the given type that match the specified constraints.</returns>
    internal static IEnumerable<FieldDefinition> GetFieldsCore(this TypeDefinition? type, string? name, BindingFlags bindingFlags)
    {
        if (type is not { HasFields: true })
            return [];

        if (bindingFlags is BindingFlags.Default)
            bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        return type.Fields.Where(x => x.Matches(name, bindingFlags));
    }


    /// <inheritdoc cref="GetProperty(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static PropertyDefinition? GetProperty(this TypeDefinition? type, string name)
        => type.GetProperties(name).SingleOrDefault();

    /// <inheritdoc cref="GetProperty(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static PropertyDefinition? GetProperty(this TypeDefinition? type, string name, BindingFlags bindingFlags)
        => type.GetProperties(name, bindingFlags).SingleOrDefault();

    /// <inheritdoc cref="GetProperty(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static PropertyDefinition? GetProperty(this TypeDefinition? type, string name, TypeName[] parameterTypes)
        => type.GetProperties(name, parameterTypes).SingleOrDefault();

    /// <inheritdoc cref="GetProperty(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static PropertyDefinition? GetProperty(this TypeDefinition? type, string name, TypeName[] parameterTypes, TypeName propertyType)
        => type.GetProperties(name, parameterTypes, propertyType).SingleOrDefault();

    /// <inheritdoc cref="GetProperty(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static PropertyDefinition? GetProperty(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes)
        => type.GetProperties(name, bindingFlags, parameterTypes).SingleOrDefault();

    /// <summary>
    /// Searches for the property defined for the given <see cref="TypeDefinition"/> that matches the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for properties.</param>
    /// <param name="name">The name of the property to find.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="parameterTypes">An array of <see cref="TypeName"/> objects representing the expected parameter types.</param>
    /// <param name="propertyType">The type of the property to find.</param>
    /// <returns>A property defined for the given type that matches the specified constraints, if found; otherwise, <c>null</c>.</returns>
    public static PropertyDefinition? GetProperty(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes, TypeName propertyType)
        => type.GetProperties(name, bindingFlags, parameterTypes, propertyType).SingleOrDefault();

    /// <inheritdoc cref="GetPropertiesCore"/>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type)
        => type.GetPropertiesCore(null, BindingFlags.Default, null);

    /// <inheritdoc cref="GetPropertiesCore"/>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type, BindingFlags bindingFlags)
        => type.GetPropertiesCore(null, bindingFlags, null);

    /// <inheritdoc cref="GetPropertiesCore"/>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type, string name)
        => type.GetPropertiesCore(name, BindingFlags.Default, null);

    /// <inheritdoc cref="GetPropertiesCore"/>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type, string name, BindingFlags bindingFlags)
        => type.GetPropertiesCore(name, bindingFlags, null);

    /// <inheritdoc cref="GetPropertiesCore"/>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type, string name, TypeName[] parameterTypes)
        => type.GetPropertiesCore(name, BindingFlags.Default, parameterTypes);

    /// <inheritdoc cref="GetProperties(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type, string name, TypeName[] parameterTypes, TypeName propertyType)
        => type.GetProperties(name, BindingFlags.Default, parameterTypes, propertyType);

    /// <inheritdoc cref="GetPropertiesCore"/>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes)
        => type.GetPropertiesCore(name, bindingFlags, parameterTypes);

    /// <inheritdoc cref="GetPropertiesCore"/>
    /// <param name="propertyType">The type of properties to find.</param>
    public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes, TypeName propertyType)
        => type.GetPropertiesCore(name, bindingFlags, parameterTypes).Where(x => x.PropertyType == propertyType);

    /// <summary>
    /// Searches for the properties defined for the given <see cref="TypeDefinition"/> that match the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for properties.</param>
    /// <param name="name">The name of the properties to find.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="parameterTypes">An array of <see cref="TypeName"/> objects representing the expected parameter types.</param>
    /// <returns>An enumerable containing properties defined for the given type that match the specified constraints.</returns>
    internal static IEnumerable<PropertyDefinition> GetPropertiesCore(this TypeDefinition? type, string? name, BindingFlags bindingFlags, TypeName[]? parameterTypes)
    {
        if (type is not { HasProperties: true })
            return [];

        if (bindingFlags is BindingFlags.Default)
            bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        return type.Properties.Where(x => x.Matches(name, bindingFlags)).WithParameters(parameterTypes);
    }


    /// <inheritdoc cref="GetConstructor(TypeDefinition?, BindingFlags, TypeName[])"/>
    public static MethodDefinition? GetConstructor(this TypeDefinition? type, TypeName[] types)
        => type.GetConstructors(types).SingleOrDefault();

    /// <summary>
    /// Searches for the constructor defined for the given <see cref="TypeDefinition"/> that matches the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for constructors.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="parameterTypes">An array of <see cref="TypeName"/> objects representing the expected parameter types.</param>
    /// <returns>A constructor defined for the given type that matches the specified constraints, if found; otherwise, <c>null</c>.</returns>
    public static MethodDefinition? GetConstructor(this TypeDefinition? type, BindingFlags bindingFlags, TypeName[] parameterTypes)
        => type.GetConstructors(bindingFlags, parameterTypes).SingleOrDefault();

    /// <inheritdoc cref="GetConstructorsCore"/>
    public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition? type)
        => type.GetConstructorsCore(BindingFlags.Default, null);

    /// <inheritdoc cref="GetConstructorsCore"/>
    public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition? type, BindingFlags bindingFlags)
        => type.GetConstructorsCore(bindingFlags, null);

    /// <inheritdoc cref="GetConstructorsCore"/>
    public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition? type, TypeName[] parameterTypes)
        => type.GetConstructorsCore(BindingFlags.Default, parameterTypes);

    /// <inheritdoc cref="GetConstructorsCore"/>
    public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition? type, BindingFlags bindingFlags, TypeName[] parameterTypes)
        => type.GetConstructorsCore(bindingFlags, parameterTypes);

    /// <summary>
    /// Searches for the constructors defined for the given <see cref="TypeDefinition"/> that match the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for constructors.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="parameterTypes">An array of <see cref="TypeName"/> objects representing the expected parameter types.</param>
    /// <returns>An enumerable containing constructors defined for the given type that match the specified constraints.</returns>
    internal static IEnumerable<MethodDefinition> GetConstructorsCore(this TypeDefinition? type, BindingFlags bindingFlags, TypeName[]? parameterTypes)
    {
        if (type is not { HasMethods: true })
            return [];

        if (bindingFlags is BindingFlags.Default)
            bindingFlags = BindingFlags.Public | BindingFlags.Instance;

        return type.Methods.Where(x => x.IsConstructor && x.Matches(null, bindingFlags)).WithParameters(parameterTypes);
    }


    /// <inheritdoc cref="GetMethod(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static MethodDefinition? GetMethod(this TypeDefinition? type, string name)
        => type.GetMethods(name).SingleOrDefault();

    /// <inheritdoc cref="GetMethod(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static MethodDefinition? GetMethod(this TypeDefinition? type, string name, BindingFlags bindingFlags)
        => type.GetMethods(name, bindingFlags).SingleOrDefault();

    /// <inheritdoc cref="GetMethod(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static MethodDefinition? GetMethod(this TypeDefinition? type, string name, TypeName[] parameterTypes)
        => type.GetMethods(name, parameterTypes).SingleOrDefault();

    /// <inheritdoc cref="GetMethod(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static MethodDefinition? GetMethod(this TypeDefinition? type, string name, TypeName[] parameterTypes, TypeName returnType)
        => type.GetMethods(name, parameterTypes, returnType).SingleOrDefault();

    /// <inheritdoc cref="GetMethod(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static MethodDefinition? GetMethod(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes)
        => type.GetMethods(name, bindingFlags, parameterTypes).SingleOrDefault();

    /// <summary>
    /// Searches for the method defined for the given <see cref="TypeDefinition"/> that matches the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for methods.</param>
    /// <param name="name">The name of the method to find.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="parameterTypes">An array of <see cref="TypeName"/> objects representing the expected parameter types.</param>
    /// <param name="returnType">The return type of the method.</param>
    /// <returns>A method defined for the given type that matches the specified constraints, if found; otherwise, <c>null</c>.</returns>
    public static MethodDefinition? GetMethod(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes, TypeName returnType)
        => type.GetMethods(name, bindingFlags, parameterTypes, returnType).SingleOrDefault();

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type)
        => type.GetMethodsCore(null, BindingFlags.Default, null);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type, BindingFlags bindingFlags)
        => type.GetMethodsCore(null, bindingFlags, null);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type, string name)
        => type.GetMethodsCore(name, BindingFlags.Default, null);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type, string name, BindingFlags bindingFlags)
        => type.GetMethodsCore(name, bindingFlags, null);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type, string name, TypeName[] parameterTypes)
        => type.GetMethodsCore(name, BindingFlags.Default, parameterTypes);

    /// <inheritdoc cref="GetMethods(TypeDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type, string name, TypeName[] parameterTypes, TypeName returnType)
        => type.GetMethods(name, BindingFlags.Default, parameterTypes, returnType);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes)
        => type.GetMethodsCore(name, bindingFlags, parameterTypes);

    /// <inheritdoc cref="GetMethodsCore"/>
    /// <param name="returnType">The return type of the methods.</param>
    public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition? type, string name, BindingFlags bindingFlags, TypeName[] parameterTypes, TypeName returnType)
        => type.GetMethodsCore(name, bindingFlags, parameterTypes).Where(x => x.ReturnType == returnType);

    /// <summary>
    /// Searches for the methods defined for the given <see cref="TypeDefinition"/> that match the specified constraints.
    /// </summary>
    /// <param name="type">The type to search for methods.</param>
    /// <param name="name">The name of the methods to find.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="parameterTypes">An array of <see cref="TypeName"/> objects representing the expected parameter types.</param>
    /// <returns>An enumerable containing methods defined for the given type that match the specified constraints.</returns>
    internal static IEnumerable<MethodDefinition> GetMethodsCore(this TypeDefinition? type, string? name, BindingFlags bindingFlags, TypeName[]? parameterTypes)
    {
        if (type is not { HasMethods: true })
            return [];

        if (bindingFlags is BindingFlags.Default)
            bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        return type.Methods.Where(x => !x.IsConstructor && x.Matches(name, bindingFlags)).WithParameters(parameterTypes);
    }


    /// <summary>
    /// Determines whether the specified field matches the provided constraints.
    /// </summary>
    /// <param name="field">The field to evaluate.</param>
    /// <param name="name">
    /// The name to match against the field's name.
    /// If <c>null</c>, the name is ignored.
    /// </param>
    /// <param name="bindingFlags">
    /// A bitwise combination of the enumeration values that
    /// specify how the check is conducted.
    /// </param>
    /// <returns><c>true</c> if the field matches the specified constraints; otherwise, <c>false</c>.</returns>
    private static bool Matches(this FieldDefinition field, string? name, BindingFlags bindingFlags)
    {
        StringComparison comparison = StringComparison.Ordinal + (bindingFlags.HasFlag(BindingFlags.IgnoreCase) ? 1 : 0);
        if (name is not null && !field.Name.Equals(name, comparison))
            return false;

        if (!bindingFlags.HasFlag(field.IsStatic ? BindingFlags.Static : BindingFlags.Instance))
            return false;

        if (!bindingFlags.HasFlag(field.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic))
            return false;

        return true;
    }

    /// <summary>
    /// Determines whether the specified property matches the provided constraints.
    /// </summary>
    /// <param name="property">The property to evaluate.</param>
    /// <param name="name">
    /// The name to match against the property's name.
    /// If <c>null</c>, the name is ignored.
    /// </param>
    /// <param name="bindingFlags">
    /// A bitwise combination of the enumeration values that
    /// specify how the check is conducted.
    /// </param>
    /// <returns><c>true</c> if the property matches the specified constraints; otherwise, <c>false</c>.</returns>
    private static bool Matches(this PropertyDefinition property, string? name, BindingFlags bindingFlags)
    {
        StringComparison comparison = StringComparison.Ordinal + (bindingFlags.HasFlag(BindingFlags.IgnoreCase) ? 1 : 0);
        if (name is not null && !property.Name.Equals(name, comparison))
            return false;

        bool isStatic = property.GetMethod is { IsStatic: true } || property.SetMethod is { IsStatic: true };
        if (!bindingFlags.HasFlag(isStatic ? BindingFlags.Static : BindingFlags.Instance))
            return false;

        bool isPublic = property.GetMethod is { IsPublic: true } || property.SetMethod is { IsPublic: true };
        if (!bindingFlags.HasFlag(isPublic ? BindingFlags.Public : BindingFlags.NonPublic))
            return false;

        return true;
    }

    /// <summary>
    /// Determines whether the specified method matches the provided constraints.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <param name="name">
    /// The name to match against the method's name.
    /// If <c>null</c>, the name is ignored.
    /// </param>
    /// <param name="bindingFlags">
    /// A bitwise combination of the enumeration values that
    /// specify how the check is conducted.
    /// </param>
    /// <returns><c>true</c> if the method matches the specified constraints; otherwise, <c>false</c>.</returns>
    private static bool Matches(this MethodDefinition method, string? name, BindingFlags bindingFlags)
    {
        StringComparison comparison = StringComparison.Ordinal + (bindingFlags.HasFlag(BindingFlags.IgnoreCase) ? 1 : 0);
        if (name is not null && !method.Name.Equals(name, comparison))
            return false;

        if (!bindingFlags.HasFlag(method.IsStatic ? BindingFlags.Static : BindingFlags.Instance))
            return false;

        if (!bindingFlags.HasFlag(method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic))
            return false;

        return true;
    }


    /// <summary>
    /// Filters a collection of properties, returning only those that have matching parameter types.
    /// </summary>
    /// <param name="properties">The properties to filter.</param>
    /// <param name="parameterTypes">
    /// An array of <see cref="TypeName"/> objects representing the expected parameter types.
    /// If <c>null</c>, no filtering is applied.
    /// </param>
    /// <returns>An enumerable containing properties with matching parameter types.</returns>
    private static IEnumerable<PropertyDefinition> WithParameters(this IEnumerable<PropertyDefinition> properties, TypeName[]? parameterTypes)
    {
        if (parameterTypes is null)
            return properties;

        if (parameterTypes.Length == 0)
            return properties.Where(static x => !x.HasParameters);

        return properties.Where(x =>
        {
            if (!x.HasParameters || x.Parameters.Count != parameterTypes.Length)
                return false;

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] != x.Parameters[i].ParameterType)
                    return false;
            }

            return true;
        });
    }

    /// <summary>
    /// Filters a collection of methods, returning only those that have matching parameter types.
    /// </summary>
    /// <param name="methods">The methods to filter.</param>
    /// <param name="parameterTypes">
    /// An array of <see cref="TypeName"/> objects representing the expected parameter types.
    /// If <c>null</c>, no filtering is applied.
    /// </param>
    /// <returns>An enumerable containing methods with matching parameter types.</returns>
    private static IEnumerable<MethodDefinition> WithParameters(this IEnumerable<MethodDefinition> methods, TypeName[]? parameterTypes)
    {
        if (parameterTypes is null)
            return methods;

        if (parameterTypes.Length == 0)
            return methods.Where(static x => !x.HasParameters);

        return methods.Where(x =>
        {
            if (!x.HasParameters || x.Parameters.Count != parameterTypes.Length)
                return false;

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] != x.Parameters[i].ParameterType)
                    return false;
            }

            return true;
        });
    }
}
