using System.Reflection;
using System.Xml.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using HotAvalonia.Xaml;

namespace HotAvalonia.Helpers;

/// <summary>
/// Provides utility methods for manipulating and obtaining information about Avalonia controls.
/// </summary>
internal static class AvaloniaControlHelper
{
    /// <summary>
    /// Loads an Avalonia control from XAML markup and initializes it.
    /// </summary>
    /// <param name="xaml">The XAML markup to load the control from.</param>
    /// <param name="uri">The URI that identifies the XAML source.</param>
    /// <param name="control">The optional control object to be populated.</param>
    /// <param name="assembly">The assembly defining the control.</param>
    /// <param name="compiledPopulateMethod">The newly compiled populate method, if the compilation was successful.</param>
    /// <returns>An object representing the loaded Avalonia control.</returns>
    /// <remarks>
    /// This method replaces static resources with their dynamic counterparts before loading the control.
    /// </remarks>
    public static object Load(string xaml, Uri uri, object? control, Assembly? assembly, out MethodInfo? compiledPopulateMethod)
    {
        _ = xaml ?? throw new ArgumentNullException(nameof(xaml));
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        // `Avalonia.Markup.Xaml.Loader` does not handle scenarios where
        // the control population logic needs to reference private members,
        // which are commonly used for subscribing to events (e.g., `Click`,
        // `TextChanged`, etc.). To circumvent this problem, we need to
        // patch the dynamic XAML assembly with `IgnoresAccessChecksToAttribute`.
        assembly ??= AssetLoader.GetAssembly(uri);
        if (assembly is not null)
            XamlScanner.DynamicXamlAssembly?.AllowAccessTo(assembly);

        string patchedXaml = ReplaceStaticResourceWithDynamicResource(xaml);
        patchedXaml = ReplaceMergeResourceIncludeWithResourceInclude(patchedXaml);

        bool useCompiledBindings = XamlScanner.UsesCompiledBindingsByDefault(assembly);
        RuntimeXamlLoaderDocument xamlDocument = new(uri, control, patchedXaml);
        RuntimeXamlLoaderConfiguration xamlConfig = new() { LocalAssembly = assembly, UseCompiledBindingsByDefault = useCompiledBindings };
        HashSet<MethodInfo> oldPopulateMethods = new(XamlScanner.FindDynamicPopulateMethods(uri));

        object loadedControl = AvaloniaRuntimeXamlLoader.Load(xamlDocument, xamlConfig);

        compiledPopulateMethod = XamlScanner
                    .FindDynamicPopulateMethods(uri)
                    .FirstOrDefault(x => !oldPopulateMethods.Contains(x));

        return loadedControl;
    }

    /// <summary>
    /// Replaces all static resources with their dynamic counterparts within a XAML markup.
    /// </summary>
    /// <param name="xaml">The XAML markup containing static resources.</param>
    /// <returns>The XAML markup with static resources replaced by their dynamic counterparts.</returns>
    private static string ReplaceStaticResourceWithDynamicResource(string xaml)
    {
        const string StaticResourceName = "\"{'StaticResource' ";
        const string DynamicResourceName = "\"{'DynamicResource' ";

        return xaml.Replace(StaticResourceName, DynamicResourceName);
    }

    /// <summary>
    /// Replaces all <c>MergeResourceInclude</c> elements with
    /// semantically identical <c>ResourceInclude</c>s.
    /// </summary>
    /// <param name="xaml">The XAML markup to process.</param>
    /// <returns>The XAML markup with <c>MergeResourceInclude</c>s replaced by <c>ResourceInclude</c>s.</returns>
    private static string ReplaceMergeResourceIncludeWithResourceInclude(string xaml)
    {
        const string MergeResourceIncludeName = "MergeResourceInclude";
        const string ResourceIncludeName = "ResourceInclude";

        if (!xaml.Contains(MergeResourceIncludeName))
            return xaml;

        XDocument document = XDocument.Parse(xaml, LoadOptions.None);
        XName oldName = XName.Get(MergeResourceIncludeName, document.Root.Name.NamespaceName);
        XName newName = XName.Get(ResourceIncludeName, document.Root.Name.NamespaceName);
        foreach (XElement mergeResourceInclude in document.Descendants(oldName))
            mergeResourceInclude.Name = newName;

        return document.ToString(SaveOptions.DisableFormatting);
    }
}
