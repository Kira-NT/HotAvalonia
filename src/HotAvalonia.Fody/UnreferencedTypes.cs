using HotAvalonia.Fody.Cecil;

namespace HotAvalonia.Fody;

/// <summary>
/// Contains type names for types that are referenced by name only and may be used for reflection or other late-binding purposes.
/// </summary>
internal static class UnreferencedTypes
{
    /// <summary>
    /// Represents the fully-qualified type name for <c>Avalonia.AppBuilder</c>.
    /// </summary>
    public static readonly TypeName Avalonia_AppBuilder = "Avalonia.AppBuilder";

    /// <summary>
    /// Represents the fully-qualified type name for the compiled Avalonia XAML resources.
    /// </summary>
    public static readonly TypeName CompiledAvaloniaXaml_AvaloniaResources = "CompiledAvaloniaXaml.!AvaloniaResources";

    /// <summary>
    /// Represents the fully-qualified type name for <c>HotAvalonia.AvaloniaHotReloadExtensions</c>.
    /// </summary>
    public static readonly TypeName HotAvalonia_AvaloniaHotReloadExtensions = "HotAvalonia.AvaloniaHotReloadExtensions";

    /// <summary>
    /// Represents the fully-qualified type name for <c>HotAvalonia.IO.IFileSystem</c>.
    /// </summary>
    public static readonly TypeName HotAvalonia_IO_IFileSystem = "HotAvalonia.IO.IFileSystem";
}
