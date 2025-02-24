using System.Diagnostics.CodeAnalysis;
using Avalonia.Logging;

namespace HotAvalonia;

/// <summary>
/// Provides functionality to retrieve configuration values for
/// various hot reload features from environment variables.
/// </summary>
internal static class HotReloadFeatures
{
    /// <summary>
    /// Gets a value that indicates whether injections should be disabled.
    /// </summary>
    public static bool DisableInjections => GetBoolean("DISABLE_INJECTIONS");

    /// <summary>
    /// Gets the log level override for hot reload-related events.
    /// </summary>
    public static LogEventLevel LogLevelOverride => GetEnum<LogEventLevel>("LOG_LEVEL_OVERRIDE");

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
    internal static string? GetString(string featureName, string? defaultValue = null)
    {
        string variableName = $"{nameof(HotAvalonia)}_{featureName}".ToUpperInvariant();
        string? variableValue = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrEmpty(variableValue) ? defaultValue : variableValue;
    }

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
    internal static bool GetBoolean(string featureName, bool defaultValue = false)
    {
        string? stringValue = GetString(featureName);
        if (bool.TryParse(stringValue, out bool boolValue))
            return boolValue;

        if (int.TryParse(stringValue, out int intValue))
            return intValue != 0;

        return defaultValue;
    }

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
    internal static int GetInt32(string featureName, int defaultValue = 0)
        => int.TryParse(GetString(featureName), out int value) ? value : defaultValue;

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
    internal static TEnum GetEnum<TEnum>(string featureName, TEnum defaultValue = default) where TEnum : struct, Enum
        => Enum.TryParse(GetString(featureName), ignoreCase: true, out TEnum value) ? value : defaultValue;
}
