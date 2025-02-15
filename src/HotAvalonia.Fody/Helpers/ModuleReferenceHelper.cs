using System.Reflection;
using HotAvalonia.Fody.Cecil;
using Mono.Cecil;

namespace HotAvalonia.Fody.Helpers;

/// <summary>
/// Provides extension methods and related functionality for working with module references.
/// </summary>
internal static class ModuleReferenceHelper
{
    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this ModuleDefinition? module, BindingFlags bindingFlags)
        => module.GetMethodsCore(null, bindingFlags, null);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this ModuleDefinition? module, string name)
        => module.GetMethodsCore(name, BindingFlags.Default, null);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this ModuleDefinition? module, string name, BindingFlags bindingFlags)
        => module.GetMethodsCore(name, bindingFlags, null);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this ModuleDefinition? module, string name, TypeName[] parameterTypes)
        => module.GetMethodsCore(name, BindingFlags.Default, parameterTypes);

    /// <inheritdoc cref="GetMethods(ModuleDefinition?, string, BindingFlags, TypeName[], TypeName)"/>
    public static IEnumerable<MethodDefinition> GetMethods(this ModuleDefinition? module, string name, TypeName[] parameterTypes, TypeName returnType)
        => module.GetMethods(name, BindingFlags.Default, parameterTypes, returnType);

    /// <inheritdoc cref="GetMethodsCore"/>
    public static IEnumerable<MethodDefinition> GetMethods(this ModuleDefinition? module, string name, BindingFlags bindingFlags, TypeName[] parameterTypes)
        => module.GetMethodsCore(name, bindingFlags, parameterTypes);

    /// <inheritdoc cref="GetMethodsCore"/>
    /// <param name="returnType">The return type of the methods.</param>
    public static IEnumerable<MethodDefinition> GetMethods(this ModuleDefinition? module, string name, BindingFlags bindingFlags, TypeName[] parameterTypes, TypeName returnType)
        => module.GetMethodsCore(name, bindingFlags, parameterTypes).Where(x => x.ReturnType == returnType);

    /// <summary>
    /// Searches for the methods defined in the given <see cref="ModuleDefinition"/> that match the specified constraints.
    /// </summary>
    /// <param name="module">The module to search for methods.</param>
    /// <param name="name">The name of the methods to find.</param>
    /// <param name="bindingFlags">A bitwise combination of the enumeration values that specify how the search is conducted.</param>
    /// <param name="parameterTypes">An array of <see cref="TypeName"/> objects representing the expected parameter types.</param>
    /// <returns>An enumerable containing methods defined in the given module that match the specified constraints.</returns>
    internal static IEnumerable<MethodDefinition> GetMethodsCore(this ModuleDefinition? module, string? name, BindingFlags bindingFlags, TypeName[]? parameterTypes)
    {
        if (module is not { HasTypes: true })
            return [];

        return module.Types.SelectMany(x => x.GetMethodsCore(name, bindingFlags, parameterTypes));
    }
}
