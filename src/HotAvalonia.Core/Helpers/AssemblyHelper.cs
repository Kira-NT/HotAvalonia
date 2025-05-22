using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HotAvalonia.Helpers;

/// <summary>
/// Provides utility methods for interacting with assemblies.
/// </summary>
internal static class AssemblyHelper
{
    /// <summary>
    /// Attempts to load an assembly with the specified name.
    /// </summary>
    /// <param name="name">The name of the assembly to load.</param>
    /// <param name="assembly">
    /// When this method returns, contains the loaded <see cref="Assembly"/>
    /// if the assembly is successfully loaded; otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the assembly is successfully loaded; otherwise, <c>false</c>.</returns>
    public static bool TryLoad(string name, [NotNullWhen(true)] out Assembly? assembly)
    {
        try
        {
            assembly = Assembly.Load(name);
            return true;
        }
        catch
        {
            assembly = null;
            return false;
        }
    }

    /// <summary>
    /// Retrieves all loadable types from a given assembly.
    /// </summary>
    /// <param name="assembly">The assembly from which to retrieve types.</param>
    /// <returns>An enumerable of types available in the provided assembly.</returns>
    /// <remarks>
    /// This method attempts to get all types from the assembly, but in case of a
    /// <see cref="ReflectionTypeLoadException"/>, it will return the types that are loadable.
    /// </remarks>
    public static IEnumerable<Type> GetLoadedTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(static x => x is not null)!;
        }
    }
}
