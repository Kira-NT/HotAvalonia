using Avalonia.Markup.Xaml;

namespace HotAvalonia.Xaml;

/// <summary>
/// Compiles XAML documents into a collection of types.
/// </summary>
/// <param name="documents">A read-only collection of XAML documents to compile.</param>
/// <param name="config">The configuration settings for the XAML compiler.</param>
/// <returns>An enumerable collection of compiled types.</returns>
internal delegate IEnumerable<Type> CompileXamlFunc(IReadOnlyCollection<RuntimeXamlLoaderDocument> documents, RuntimeXamlLoaderConfiguration config);
