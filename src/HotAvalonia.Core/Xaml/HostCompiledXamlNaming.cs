namespace HotAvalonia.Xaml;

/// <summary>
/// The naming contract shared between the device-side host-compiled XAML loader and the host-side
/// compiler that produces the populate DLLs. Both sides must agree on these values, so they live in
/// one place (this file is linked into the host compiler tool).
/// </summary>
internal static class HostCompiledXamlNaming
{
    /// <summary>
    /// The assembly-name prefix the host compiler must use when naming each populate DLL.
    /// The <see cref="System.AssemblyLoadEventArgs"/> hook skips these so a transient populate DLL is
    /// never mistaken for a new project to watch (which also avoids a native Mono load-hook assertion on iOS).
    /// </summary>
    public const string AssemblyNamePrefix = "HotAvaloniaHostCompiled_";

    /// <summary>
    /// The suffix appended to a view's <c>.axaml</c> path to locate its host-compiled sidecar DLL
    /// (e.g. <c>MyView.axaml</c> -&gt; <c>MyView.axaml.hotreload.dll</c>).
    /// </summary>
    public const string SidecarSuffix = ".hotreload.dll";
}
