using System.Text.RegularExpressions;

namespace HotAvalonia.HostCompiler;

/// <summary>
/// Rewrites a source <c>.axaml</c> into the classless form the host compiler builds: the <c>x:Class</c>
/// attribute is removed, and every bare <c>clr-namespace:</c> xmlns that the closure can resolve gets an
/// <c>;assembly=</c> qualifier so its types bind the REFERENCED (device) identities rather than the
/// throwaway compiling assembly. The root element is left untouched (Avalonia views are rooted on their
/// own type), so the generated populate binds that type.
/// </summary>
internal static partial class AxamlTransform
{
    /// <summary>
    /// Transforms <paramref name="xaml"/> and reports the full <c>x:Class</c> type name (empty if none).
    /// </summary>
    public static string Transform(string xaml, AssemblyClosure closure, out string viewClassName)
    {
        // The XAML-language prefix is conventionally "x"; resolve it from the document, default to "x".
        Match namespaceMatch = XamlNamespaceRegex().Match(xaml);
        string xamlPrefix = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "x";

        // Capture and strip "<prefix>:Class=...".
        Regex classRegex = new(
            $@"\s*{Regex.Escape(xamlPrefix)}:Class\s*=\s*""([^""]*)""",
            RegexOptions.CultureInvariant);
        Match classMatch = classRegex.Match(xaml);
        viewClassName = classMatch.Success ? classMatch.Groups[1].Value : string.Empty;
        string result = classRegex.Replace(xaml, string.Empty, 1);

        // Append ";assembly=" to each bare clr-namespace xmlns the closure can resolve.
        result = ClrNamespaceRegex().Replace(result, match =>
        {
            string clrNamespace = match.Groups[2].Value;
            if (!closure.TryGetAssemblyForNamespace(clrNamespace, out string assembly))
                return match.Value;

            return $@"xmlns{match.Groups[1].Value}=""clr-namespace:{clrNamespace};assembly={assembly}""";
        });

        return result;
    }

    [GeneratedRegex(@"xmlns:(\w+)\s*=\s*""http://schemas\.microsoft\.com/winfx/2006/xaml""", RegexOptions.CultureInvariant)]
    private static partial Regex XamlNamespaceRegex();

    [GeneratedRegex(@"xmlns(:\w+)?\s*=\s*""clr-namespace:([^"";]+)""", RegexOptions.CultureInvariant)]
    private static partial Regex ClrNamespaceRegex();
}
