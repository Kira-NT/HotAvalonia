using System.Reflection;

namespace HotAvalonia.Xaml;

/// <summary>
/// Extracts the build and populate methods from a host-compiled XAML assembly.
/// </summary>
/// <remarks>
/// iOS forbids runtime <c>Reflection.Emit</c>, so the on-device <see cref="XamlCompiler"/> path is a
/// dead end there. Instead the Mac compiles a single changed view into a standalone "populate DLL"
/// (a host-side build step) using Avalonia's exact build-time XAML compiler, with
/// <c>x:Class</c> stripped so the generated resource methods bind the REFERENCED (device) root type:
///   <c>CompiledAvaloniaXaml.!AvaloniaResources::Build:&lt;uri&gt;(IServiceProvider) -&gt; TRoot</c>
///   <c>CompiledAvaloniaXaml.!AvaloniaResources::Populate:&lt;uri&gt;(IServiceProvider, TRoot)</c>
/// This loader finds that pair by root type (one view per DLL), so the device can drive its existing
/// emit-free <c>!XamlIlPopulateOverride</c> reload path with a Mac-compiled tree.
/// </remarks>
internal static class HostCompiledXamlLoader
{
    /// <summary>
    /// Determines whether the given assembly is a host-compiled populate carrier.
    /// </summary>
    /// <param name="assembly">The assembly to check.</param>
    /// <returns><c>true</c> if the assembly was produced by the host compiler; otherwise, <c>false</c>.</returns>
    public static bool IsHostCompiledAssembly(Assembly assembly)
        => assembly.GetName().Name?.StartsWith(HostCompiledXamlNaming.AssemblyNamePrefix, StringComparison.Ordinal) == true;


    /// <summary>
    /// Finds the build/populate pair in a host-compiled assembly that targets the given root type.
    /// </summary>
    /// <param name="assembly">The host-compiled (populate) assembly.</param>
    /// <param name="rootType">The root control type the methods must target (the live device type).</param>
    /// <param name="build">When this method returns, the build method, if found.</param>
    /// <param name="populate">When this method returns, the populate method, if found.</param>
    /// <returns><c>true</c> if a matching pair was found; otherwise, <c>false</c>.</returns>
    public static bool TryFindResourceMethods(Assembly assembly, Type rootType, out MethodBase build, out MethodInfo populate)
    {
        build = null!;
        populate = null!;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        foreach (Type type in GetTypesSafe(assembly))
        {
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                string name = method.Name;
                if (populate is null && name.StartsWith("Populate:", StringComparison.Ordinal) && XamlScanner.IsPopulateMethod(method))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 2 && parameters[1].ParameterType.IsAssignableFrom(rootType))
                        populate = method;
                }
                else if (build is null && name.StartsWith("Build:", StringComparison.Ordinal) && XamlScanner.IsBuildMethod(method)
                         && method is MethodInfo { ReturnType: Type returnType } && rootType.IsAssignableFrom(returnType))
                {
                    build = method;
                }

                if (build is not null && populate is not null)
                    return true;
            }
        }

        return build is not null && populate is not null;
    }

    private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(static t => t is not null)!;
        }
    }
}
