using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using Avalonia.Logging;
using HotAvalonia.Reflection.Inject;

namespace HotAvalonia;

/// <summary>
/// Provides functionality to retrieve configuration values for
/// various hot reload features from environment variables.
/// </summary>
internal static class HotReloadFeatures
{
    /// <summary>
    /// Gets a set of allowed injection techniques.
    /// </summary>
    public static InjectionType InjectionType => GetEnum(nameof(InjectionType), InjectionType.Default);

    /// <summary>
    /// Gets a value that indicates whether the initial patching should be skipped.
    /// </summary>
    public static bool SkipInitialPatching => GetBoolean(nameof(SkipInitialPatching));

    /// <summary>
    /// Gets the minimum log level for hot reload-related events.
    /// </summary>
    public static LogEventLevel MinLogLevel => GetEnum(nameof(MinLogLevel), LogEventLevel.Debug);

    /// <summary>
    /// Gets the default timeout applied to hot reload–related operations.
    /// </summary>
    public static TimeSpan Timeout => TimeSpan.FromMilliseconds(GetInt32(nameof(Timeout), 10000));

    /// <summary>
    /// Gets the default hot reload mode.
    /// </summary>
    public static HotReloadMode Mode => GetEnum(nameof(Mode), HotReloadMode.Balanced);

    /// <summary>
    /// Gets the default hotkey used to trigger a manual hot reload event.
    /// </summary>
    public static KeyGesture? Hotkey => GetBoolean(nameof(Hotkey), true) ? KeyGesture.Parse(GetString(nameof(Hotkey), "Alt+F5")) : null;

    /// <summary>
    /// Retrieves the environment variable value associated with the specified feature name.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <param name="defaultValue">The default value to return if the environment variable is not set.</param>
    /// <returns>
    /// The string value of the environment variable, or <paramref name="defaultValue"/>
    /// if the variable is not set.
    /// </returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    internal static string? GetString(ReadOnlyMemory<char> featureName, string? defaultValue = null)
    {
        string variableName = GetEnvironmentVariableName(featureName);
        string? variableValue = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrEmpty(variableValue) ? defaultValue : variableValue;
    }

    /// <inheritdoc cref="GetString(ReadOnlyMemory{char}, string?)"/>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    internal static string? GetString(string featureName, string? defaultValue = null)
        => GetString(featureName.AsMemory(), defaultValue);

    /// <summary>
    /// Retrieves the environment variable value associated with the specified feature name
    /// and converts it to a <see cref="bool"/>.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <param name="defaultValue">The default value to return if the conversion fails.</param>
    /// <returns>
    /// The boolean representation of the environment variable, or <paramref name="defaultValue"/>
    /// if the conversion fails.
    /// </returns>
    internal static bool GetBoolean(ReadOnlyMemory<char> featureName, bool defaultValue = false)
    {
        string? stringValue = GetString(featureName);
        if (bool.TryParse(stringValue, out bool boolValue))
            return boolValue;

        if (int.TryParse(stringValue, out int intValue))
            return intValue != 0;

        return defaultValue;
    }

    /// <inheritdoc cref="GetBoolean(ReadOnlyMemory{char}, bool)"/>
    internal static bool GetBoolean(string featureName, bool defaultValue = false)
        => GetBoolean(featureName.AsMemory(), defaultValue);

    /// <summary>
    /// Retrieves the environment variable value associated with the specified feature name
    /// and converts it to a 32-bit integer.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <param name="defaultValue">The default integer value to return if the conversion fails.</param>
    /// <returns>
    /// The 32-bit integer representation of the environment variable, or <paramref name="defaultValue"/>
    /// if the conversion fails.
    /// </returns>
    internal static int GetInt32(ReadOnlyMemory<char> featureName, int defaultValue = 0)
        => int.TryParse(GetString(featureName), out int value) ? value : defaultValue;

    /// <inheritdoc cref="GetInt32(ReadOnlyMemory{char}, int)"/>
    internal static int GetInt32(string featureName, int defaultValue = 0)
        => GetInt32(featureName.AsMemory(), defaultValue);

    /// <summary>
    /// Retrieves the environment variable value associated with the specified feature name
    /// and converts it to the specified enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type to which the value should be converted.</typeparam>
    /// <param name="featureName">The feature name.</param>
    /// <param name="defaultValue">The default enumeration value to return if the conversion fails.</param>
    /// <returns>
    /// The enumeration value corresponding to the environment variable, or <paramref name="defaultValue"/>
    /// if the conversion fails.
    /// </returns>
    internal static TEnum GetEnum<TEnum>(ReadOnlyMemory<char> featureName, TEnum defaultValue = default) where TEnum : struct, Enum
        => Enum.TryParse(GetString(featureName), ignoreCase: true, out TEnum value) ? value : defaultValue;

    /// <inheritdoc cref="GetEnum{TEnum}(ReadOnlyMemory{char}, TEnum)"/>
    internal static TEnum GetEnum<TEnum>(string featureName, TEnum defaultValue = default) where TEnum : struct, Enum
        => GetEnum(featureName.AsMemory(), defaultValue);

    /// <summary>
    /// Formats a feature name written in PascalCase or camelCase into a SCREAMING_SNAKE_CASE
    /// environment variable name prefixed with "HOTAVALONIA_".
    /// </summary>
    /// <param name="featureName">The feature name to format.</param>
    /// <returns>The feature name formatted as an environment variable name corresponding to the said feature.</returns>
    private static string GetEnvironmentVariableName(ReadOnlyMemory<char> featureName)
    {
        const string prefix = "HOTAVALONIA_";
        ReadOnlySpan<char> name = featureName.Span;
        if (name.Length == 0)
            return prefix;

        int borderCount = 0;
        for (int i = name.Length - 2, s = name[^1] & 0x20, ns; i >= 0; s = ns, i--)
        {
            if ((ns = name[i] & 0x20) - s == 0x20)
                borderCount++;
        }

        return string.Create(prefix.Length + featureName.Length + borderCount, featureName, static (buffer, memory) =>
        {
            ReadOnlySpan<char> nameBuffer = memory.Span;
            ref char name = ref Unsafe.AsRef(in nameBuffer[0]);
            ref char envName = ref buffer[prefix.Length];
            ((ReadOnlySpan<char>)prefix).CopyTo(buffer);

            nint i = nameBuffer.Length - 1;
            nint j = buffer.Length - prefix.Length;
            for (int s = Unsafe.Add(ref name, i) & 0x20, ns; i >= 0; s = ns, i--)
            {
                int c = Unsafe.Add(ref name, i);
                if ((ns = c & 0x20) - s == 0x20)
                    Unsafe.Add(ref envName, --j) = '_';

                Unsafe.Add(ref envName, --j) = (char)(c & ~(((uint)(c - 'a') <= 'z' - 'a' ? 1 : 0) << 5));
            }
        });
    }
}
