using System.Reflection;

namespace HotAvalonia.Fody.Reflection;

/// <summary>
/// Provides pre-combined <see cref="BindingFlags"/> values for common reflection scenarios.
/// </summary>
internal static class BindingFlag
{
    /// <summary>
    /// Gets the binding flags for public instance members.
    /// </summary>
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>
    /// Gets the binding flags for non-public instance members.
    /// </summary>
    public const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Gets the binding flags for any instance members, both public and non-public.
    /// </summary>
    public const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Gets the binding flags for public static members.
    /// </summary>
    public const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

    /// <summary>
    /// Gets the binding flags for non-public static members.
    /// </summary>
    public const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

    /// <summary>
    /// Gets the binding flags for any static members, both public and non-public.
    /// </summary>
    public const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    /// <summary>
    /// Gets the binding flags for any members, including both instance and static as well as public and non-public.
    /// </summary>
    public const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
}
