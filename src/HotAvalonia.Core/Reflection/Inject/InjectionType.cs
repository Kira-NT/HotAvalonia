namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Represents the different types of injection techniques that can be performed by the <see cref="CallbackInjector"/>.
/// </summary>
[Flags]
internal enum InjectionType
{
    /// <summary>
    /// Indicates that the current environment does not support injections.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that injections are performed via code-cave-based method detouring.
    /// </summary>
    CodeCave = 1 << 0,

    /// <summary>
    /// Indicates that injections are performed via direct function pointer replacement.
    /// </summary>
    PointerSwap = 1 << 1,

    /// <summary>
    /// Indicates that injections are performed via MonoMod-based method detouring.
    /// </summary>
    MonoMod = 1 << 2,

    /// <summary>
    /// Represents the default set of allowed injection techniques.
    /// </summary>
    Default = CodeCave | PointerSwap | MonoMod,
}
