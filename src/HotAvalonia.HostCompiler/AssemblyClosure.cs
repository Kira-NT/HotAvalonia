using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace HotAvalonia.HostCompiler;

/// <summary>
/// Indexes a set of reference assemblies (the "closure" of the target app's pre-link build output) so the
/// compiler can resolve which assembly defines a given XAML <c>clr-namespace</c> and enumerate the
/// assemblies that need <c>IgnoresAccessChecksTo</c>. Uses <see cref="MetadataReader"/> only, so it is
/// fully cross-platform (no Mono/<c>monodis</c>).
/// </summary>
internal sealed class AssemblyClosure
{
    private readonly Dictionary<string, string> _namespaceToAssembly;
    private readonly List<string> _assemblyNames;

    private AssemblyClosure(Dictionary<string, string> namespaceToAssembly, List<string> assemblyNames)
    {
        _namespaceToAssembly = namespaceToAssembly;
        _assemblyNames = assemblyNames;
    }

    /// <summary>
    /// The simple names of every referenced (non-excluded) assembly in the closure.
    /// </summary>
    public IReadOnlyList<string> AssemblyNames => _assemblyNames;

    /// <summary>
    /// Resolves the assembly that defines types in the given CLR namespace.
    /// </summary>
    public bool TryGetAssemblyForNamespace(string clrNamespace, out string assemblyName)
        => _namespaceToAssembly.TryGetValue(clrNamespace, out assemblyName!);

    /// <summary>
    /// Scans every managed DLL under <paramref name="closureDirectory"/> (skipping files whose name matches
    /// an entry in <paramref name="excludePatterns"/>) and builds the namespace/assembly indexes.
    /// </summary>
    public static AssemblyClosure Load(string closureDirectory, IReadOnlyList<string> excludePatterns)
    {
        Regex[] excludes = [.. excludePatterns.Select(ToFileNameRegex)];
        Dictionary<string, string> namespaceToAssembly = new(StringComparer.Ordinal);
        List<string> assemblyNames = [];

        foreach (string dllPath in Directory.EnumerateFiles(closureDirectory, "*.dll"))
        {
            string fileName = Path.GetFileName(dllPath);
            if (excludes.Any(rx => rx.IsMatch(fileName)))
                continue;

            string? assemblyName = TryIndexAssembly(dllPath, namespaceToAssembly);
            if (assemblyName is not null)
                assemblyNames.Add(assemblyName);
        }

        return new AssemblyClosure(namespaceToAssembly, assemblyNames);
    }

    private static string? TryIndexAssembly(string dllPath, Dictionary<string, string> namespaceToAssembly)
    {
        try
        {
            using FileStream stream = File.OpenRead(dllPath);
            using PEReader peReader = new(stream);
            if (!peReader.HasMetadata)
                return null;

            MetadataReader reader = peReader.GetMetadataReader();
            if (!reader.IsAssembly)
                return null;

            string assemblyName = reader.GetString(reader.GetAssemblyDefinition().Name);

            foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
            {
                TypeDefinition type = reader.GetTypeDefinition(handle);

                // Only top-level types carry a namespace worth indexing; nested types have a declaring type.
                if (!type.GetDeclaringType().IsNil)
                    continue;

                if (type.Namespace.IsNil)
                    continue;

                string ns = reader.GetString(type.Namespace);
                if (ns.Length == 0)
                    continue;

                // First assembly that defines the namespace wins (matches the original single-assembly recipe).
                namespaceToAssembly.TryAdd(ns, assemblyName);
            }

            return assemblyName;
        }
        catch (BadImageFormatException)
        {
            // Native or otherwise unreadable image - not part of the managed closure.
            return null;
        }
    }

    private static Regex ToFileNameRegex(string pattern)
    {
        string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal) + "$";
        return new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
