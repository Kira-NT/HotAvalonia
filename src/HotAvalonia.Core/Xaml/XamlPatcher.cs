using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace HotAvalonia.Xaml;

/// <summary>
/// Represents an abstract base class for patching XAML content.
/// </summary>
public abstract class XamlPatcher
{
    /// <summary>
    /// Gets an instance of a patcher that performs no operations.
    /// </summary>
    [field: MaybeNull]
    public static XamlPatcher Empty => field ??= new CombinedXamlPatcher([]);

    /// <summary>
    /// Gets the default instance of a XAML patcher.
    /// </summary>
    [field: MaybeNull]
    public static XamlPatcher Default => field ??= OptionalXamlPatcher.CombineEnabled
    (
        new StaticResourcePatcher(),
        new MergeResourceIncludePatcher()
    );

    /// <summary>
    /// Applies patching operations to the specified XAML content.
    /// </summary>
    /// <param name="xaml">The original XAML string.</param>
    /// <returns>The patched XAML string.</returns>
    public abstract string Patch(string xaml);


    /// <summary>
    /// Determines whether the specified XAML content requires patching.
    /// </summary>
    /// <param name="xaml">The XAML string to evaluate.</param>
    /// <returns><c>true</c> if the XAML content requires patching; otherwise, <c>false</c>.</returns>
    public abstract bool RequiresPatching(string xaml);
}

/// <summary>
/// Represents a patcher that combines multiple <see cref="XamlPatcher"/> instances into one.
/// </summary>
file sealed class CombinedXamlPatcher : XamlPatcher
{
    /// <summary>
    /// The collection of patchers to apply.
    /// </summary>
    private readonly XamlPatcher[] _patchers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CombinedXamlPatcher"/> class.
    /// </summary>
    /// <param name="patchers">An array of patchers to combine.</param>
    public CombinedXamlPatcher(XamlPatcher[] patchers)
        => _patchers = patchers;

    /// <inheritdoc/>
    public override string Patch(string xaml)
        => _patchers.Aggregate(xaml, static (xaml, patcher) => patcher.Patch(xaml));

    /// <inheritdoc/>
    public override bool RequiresPatching(string xaml)
        => _patchers.Any(x => x.RequiresPatching(xaml));
}

/// <summary>
/// Provides a base class for optional XAML patchers that can be enabled or disabled via environment variables.
/// </summary>
file abstract class OptionalXamlPatcher : XamlPatcher
{
    /// <summary>
    /// Gets a value indicating whether this patcher is enabled.
    /// </summary>
    /// <remarks>
    /// The patcher is disabled if an environment variable, named after the patcher's name,
    /// is set to "false" or "0".
    /// </remarks>
    public bool Enabled
    {
        get
        {
            string typeName = GetType().Name;
            string variableName = $"{nameof(HotAvalonia)}_{typeName}".ToUpperInvariant();
            string? variableValue = Environment.GetEnvironmentVariable(variableName);
            return bool.TryParse(variableValue, out bool enabled) ? enabled : variableValue != "0";
        }
    }

    /// <summary>
    /// Combines all enabled <see cref="OptionalXamlPatcher"/> instances
    /// from the given collection into a single patcher.
    /// </summary>
    /// <param name="patchers">An enumerable collection of optional patchers.</param>
    /// <returns>A <see cref="XamlPatcher"/> that represents the combination of all enabled patchers.</returns>
    public static XamlPatcher CombineEnabled(params IEnumerable<OptionalXamlPatcher> patchers)
        => new CombinedXamlPatcher(patchers.Where(x => x.Enabled).ToArray());
}

/// <summary>
/// A patcher that replaces static resource references with dynamic resource references in XAML content.
/// </summary>
file sealed class StaticResourcePatcher : OptionalXamlPatcher
{
    /// <summary>
    /// The pattern representing a static resource reference in the XAML.
    /// </summary>
    private const string StaticResourcePattern = "\"{'StaticResource' ";

    /// <summary>
    /// The pattern representing a dynamic resource reference in the XAML.
    /// </summary>
    private const string DynamicResourcePattern = "\"{'DynamicResource' ";

    /// <inheritdoc/>
    public override string Patch(string xaml) => xaml.Replace(StaticResourcePattern, DynamicResourcePattern);

    /// <inheritdoc/>
    public override bool RequiresPatching(string xaml) => xaml.Contains(StaticResourcePattern);
}

/// <summary>
/// A patcher that modifies XAML content by replacing <c>MergeResourceInclude</c>
/// elements with <c>ResourceInclude</c> elements.
/// </summary>
file sealed class MergeResourceIncludePatcher : OptionalXamlPatcher
{
    /// <summary>
    /// The element name for <c>MergeResourceInclude</c>s in the original XAML.
    /// </summary>
    private const string MergeResourceIncludeElementName = "MergeResourceInclude";

    /// <summary>
    /// The element name for <c>ResourceInclude</c>s to be used in the patched XAML.
    /// </summary>
    private const string ResourceIncludeElementName = "ResourceInclude";

    /// <inheritdoc/>
    public override string Patch(string xaml)
    {
        if (!xaml.Contains(MergeResourceIncludeElementName))
            return xaml;

        XDocument document = XDocument.Parse(xaml, LoadOptions.None);
        XName oldName = XName.Get(MergeResourceIncludeElementName, document.Root.Name.NamespaceName);
        XName newName = XName.Get(ResourceIncludeElementName, document.Root.Name.NamespaceName);
        foreach (XElement mergeResourceInclude in document.Descendants(oldName))
            mergeResourceInclude.Name = newName;

        return document.ToString(SaveOptions.DisableFormatting);
    }

    /// <inheritdoc/>
    public override bool RequiresPatching(string xaml) => xaml.Contains(MergeResourceIncludeElementName);
}
