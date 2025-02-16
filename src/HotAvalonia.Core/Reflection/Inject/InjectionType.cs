namespace HotAvalonia.Reflection.Inject;

/// <summary>
/// Represents the different types of injection techniques that can be performed.
/// </summary>
internal enum InjectionType
{
    /// <summary>
    /// Indicates that no injection technique is available in the current environment.
    /// </summary>
    None,

    /// <summary>
    /// Represents a native-level injection.
    /// </summary>
    /// <remarks>
    /// This technique hijacks the natively compiled (by JIT) methods and
    /// replaces them with stubs that lead to injected methods.
    ///
    /// It is the most reliable technique, however it's highly sensitive to such things as
    /// the runtime version, system architecture (e.g., x86, x86_64), etc.,
    /// making it much less portable.
    /// </remarks>
    Native,
}
