using Avalonia.Logging;

namespace HotAvalonia.Helpers;

/// <summary>
/// Provides utility methods for logging within the hot reload context.
/// </summary>
internal static class LoggingHelper
{
    /// <summary>
    /// The logger instance used for debug-level messages.
    /// </summary>
    private static readonly ParametrizedLogger? s_debug = CreateLogger(LogEventLevel.Debug);

    /// <summary>
    /// The logger instance used for informational messages.
    /// </summary>
    private static readonly ParametrizedLogger? s_info = CreateLogger(LogEventLevel.Information);

    /// <summary>
    /// The logger instance used for warning-level messages.
    /// </summary>
    private static readonly ParametrizedLogger? s_warning = CreateLogger(LogEventLevel.Warning);

    /// <summary>
    /// The logger instance used for error-level messages.
    /// </summary>
    private static readonly ParametrizedLogger? s_error = CreateLogger(LogEventLevel.Error);


    /// <inheritdoc cref="LogDebug{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug(string messageTemplate)
        => s_debug?.Log(source: null, $" {messageTemplate}");

    /// <inheritdoc cref="LogDebug{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug<T0>(string messageTemplate, T0 arg0)
        => s_debug?.Log(source: null, $" {messageTemplate}", arg0);

    /// <inheritdoc cref="LogDebug{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug<T0, T1>(string messageTemplate, T0 arg0, T1 arg1)
        => s_debug?.Log(source: null, $" {messageTemplate}", arg0, arg1);

    /// <inheritdoc cref="LogDebug{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug<T0, T1, T2>(string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_debug?.Log(source: null, $" {messageTemplate}", arg0, arg1, arg2);

    /// <inheritdoc cref="LogDebug{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug(object? source, string messageTemplate)
        => s_debug?.Log(source, messageTemplate);

    /// <inheritdoc cref="LogDebug{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug<T0>(object? source, string messageTemplate, T0 arg0)
        => s_debug?.Log(source, messageTemplate, arg0);

    /// <inheritdoc cref="LogDebug{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug<T0, T1>(object? source, string messageTemplate, T0 arg0, T1 arg1)
        => s_debug?.Log(source, messageTemplate, arg0, arg1);

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogDebug<T0, T1, T2>(object? source, string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_debug?.Log(source, messageTemplate, arg0, arg1, arg2);


    /// <inheritdoc cref="LogInfo{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo(string messageTemplate)
        => s_info?.Log(source: null, $" {messageTemplate}");

    /// <inheritdoc cref="LogInfo{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo<T0>(string messageTemplate, T0 arg0)
        => s_info?.Log(source: null, $" {messageTemplate}", arg0);

    /// <inheritdoc cref="LogInfo{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo<T0, T1>(string messageTemplate, T0 arg0, T1 arg1)
        => s_info?.Log(source: null, $" {messageTemplate}", arg0, arg1);

    /// <inheritdoc cref="LogInfo{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo<T0, T1, T2>(string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_info?.Log(source: null, $" {messageTemplate}", arg0, arg1, arg2);

    /// <inheritdoc cref="LogInfo{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo(object? source, string messageTemplate)
        => s_info?.Log(source, messageTemplate);

    /// <inheritdoc cref="LogInfo{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo<T0>(object? source, string messageTemplate, T0 arg0)
        => s_info?.Log(source, messageTemplate, arg0);

    /// <inheritdoc cref="LogInfo{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo<T0, T1>(object? source, string messageTemplate, T0 arg0, T1 arg1)
        => s_info?.Log(source, messageTemplate, arg0, arg1);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogInfo<T0, T1, T2>(object? source, string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_info?.Log(source, messageTemplate, arg0, arg1, arg2);


    /// <inheritdoc cref="LogWarning{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning(string messageTemplate)
        => s_warning?.Log(source: null, $" {messageTemplate}");

    /// <inheritdoc cref="LogWarning{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning<T0>(string messageTemplate, T0 arg0)
        => s_warning?.Log(source: null, $" {messageTemplate}", arg0);

    /// <inheritdoc cref="LogWarning{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning<T0, T1>(string messageTemplate, T0 arg0, T1 arg1)
        => s_warning?.Log(source: null, $" {messageTemplate}", arg0, arg1);

    /// <inheritdoc cref="LogWarning{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning<T0, T1, T2>(string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_warning?.Log(source: null, $" {messageTemplate}", arg0, arg1, arg2);

    /// <inheritdoc cref="LogWarning{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning(object? source, string messageTemplate)
        => s_warning?.Log(source, messageTemplate);

    /// <inheritdoc cref="LogWarning{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning<T0>(object? source, string messageTemplate, T0 arg0)
        => s_warning?.Log(source, messageTemplate, arg0);

    /// <inheritdoc cref="LogWarning{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning<T0, T1>(object? source, string messageTemplate, T0 arg0, T1 arg1)
        => s_warning?.Log(source, messageTemplate, arg0, arg1);

    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogWarning<T0, T1, T2>(object? source, string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_warning?.Log(source, messageTemplate, arg0, arg1, arg2);


    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogError(string messageTemplate)
        => s_error?.Log(source: null, $" {messageTemplate}");

    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogError<T0>(string messageTemplate, T0 arg0)
        => s_error?.Log(source: null, $" {messageTemplate}", arg0);

    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogError<T0, T1>(string messageTemplate, T0 arg0, T1 arg1)
        => s_error?.Log(source: null, $" {messageTemplate}", arg0, arg1);

    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogError<T0, T1, T2>(string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_error?.Log(source: null, $" {messageTemplate}", arg0, arg1, arg2);

    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogError(object? source, string messageTemplate)
        => s_error?.Log(source, messageTemplate);

    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogError<T0>(object? source, string messageTemplate, T0 arg0)
        => s_error?.Log(source, messageTemplate, arg0);

    /// <inheritdoc cref="LogError{T0, T1, T2}(object?, string, T0, T1, T2)"/>
    public static void LogError<T0, T1>(object? source, string messageTemplate, T0 arg0, T1 arg1)
        => s_error?.Log(source, messageTemplate, arg0, arg1);

    /// <summary>
    /// Logs an error-level message.
    /// </summary>
    /// <typeparam name="T0">The type of the first object to format.</typeparam>
    /// <typeparam name="T1">The type of the second object to format.</typeparam>
    /// <typeparam name="T2">The type of the third object to format.</typeparam>
    /// <param name="source">The object from which the event originates.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="arg0">The first object to format.</param>
    /// <param name="arg1">The second object to format.</param>
    /// <param name="arg2">The third object to format.</param>
    public static void LogError<T0, T1, T2>(object? source, string messageTemplate, T0 arg0, T1 arg1, T2 arg2)
        => s_error?.Log(source, messageTemplate, arg0, arg1, arg2);


    /// <summary>
    /// Creates a parametrized logger for the specified log level, ensuring that the effective log level
    /// respects the override defined by <see cref="HotReloadFeatures.LogLevelOverride"/>.
    /// </summary>
    /// <param name="logLevel">The base log level to use when creating the logger.</param>
    /// <returns>
    /// A <see cref="ParametrizedLogger"/> instance configured for the effective log level,
    /// or <c>null</c> if a logger cannot be obtained.
    /// </returns>
    private static ParametrizedLogger? CreateLogger(LogEventLevel logLevel)
    {
        LogEventLevel logLevelOverride = HotReloadFeatures.LogLevelOverride;
        if (logLevel < logLevelOverride)
            logLevel = logLevelOverride;

        return Logger.TryGet(logLevel, nameof(HotAvalonia));
    }
}
