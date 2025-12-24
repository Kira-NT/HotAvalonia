using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using AvaloniaLogger = Avalonia.Logging.Logger;

namespace HotAvalonia.Logging;

/// <summary>
/// Provides methods for writing log messages to the Avalonia logging infrastructure.
/// </summary>
public static class Logger
{
    /// <summary>
    /// The log area associated with all messages emitted by this logger.
    /// </summary>
    public const string LogArea = nameof(HotAvalonia);

    /// <summary>
    /// The minimum log level attributed to all messages emitted by this logger.
    /// </summary>
    private static readonly LogEventLevel s_minLogLevel = HotReloadFeatures.LogLevelOverride;

    /// <summary>
    /// Logs diagnostic information about the current runtime environment.
    /// </summary>
    [ModuleInitializer]
    internal static void LogEnvironmentInfo()
    {
        LogDebug("OS: {OSDescription}", RuntimeInformation.OSDescription);
        LogDebug("Architecture: {ProcessArchitecture}/{OSArchitecture}", RuntimeInformation.ProcessArchitecture, RuntimeInformation.OSArchitecture);
        LogDebug("Framework: {FrameworkDescription}", RuntimeInformation.FrameworkDescription);
        LogDebug("HotAvalonia: {HotAvaloniaVersion}", typeof(Logger).Assembly.GetName().Version);
        LogDebug("Avalonia: {AvaloniaVersion}", typeof(AppBuilder).Assembly.GetName().Version);
        LogDebug("Avalonia.Markup.Xaml.Loader: {XamlLoaderVersion}", typeof(AvaloniaRuntimeXamlLoader).Assembly.GetName().Version);
    }

    /// <inheritdoc cref="LogDebug(object?, string, ReadOnlySpan{object?})"/>
    public static void LogDebug(string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Debug, source: null, messageTemplate, propertyValues);

    /// <summary>
    /// Logs a new debug event.
    /// </summary>
    /// <inheritdoc cref="Log(LogLevel, object?, string, ReadOnlySpan{object?})"/>
    [OverloadResolutionPriority(-1)]
    public static void LogDebug(object? source, string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Debug, source, messageTemplate, propertyValues);

    /// <inheritdoc cref="LogInfo(object?, string, ReadOnlySpan{object?})"/>
    public static void LogInfo(string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Information, source: null, messageTemplate, propertyValues);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <inheritdoc cref="Log(LogLevel, object?, string, ReadOnlySpan{object?})"/>
    [OverloadResolutionPriority(-1)]
    public static void LogInfo(object? source, string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Information, source, messageTemplate, propertyValues);

    /// <inheritdoc cref="LogWarning(object?, string, ReadOnlySpan{object?})"/>
    public static void LogWarning(string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Warning, source: null, messageTemplate, propertyValues);

    /// <summary>
    /// Logs a warning.
    /// </summary>
    /// <inheritdoc cref="Log(LogLevel, object?, string, ReadOnlySpan{object?})"/>
    [OverloadResolutionPriority(-1)]
    public static void LogWarning(object? source, string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Warning, source, messageTemplate, propertyValues);

    /// <inheritdoc cref="LogError(object?, string, ReadOnlySpan{object?})"/>
    public static void LogError(string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Error, source: null, messageTemplate, propertyValues);

    /// <summary>
    /// Logs an error.
    /// </summary>
    /// <inheritdoc cref="Log(LogLevel, object?, string, ReadOnlySpan{object?})"/>
    [OverloadResolutionPriority(-1)]
    public static void LogError(object? source, string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(LogLevel.Error, source, messageTemplate, propertyValues);

    /// <inheritdoc cref="Log(LogLevel, object?, string, ReadOnlySpan{object?})"/>
    public static void Log(LogLevel level, string messageTemplate, params ReadOnlySpan<object?> propertyValues)
        => Log(level, source: null, messageTemplate, propertyValues);

    /// <summary>
    /// Logs a new event.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="source">The object from which the event originates.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    [OverloadResolutionPriority(-1)]
    public static void Log(LogLevel level, object? source, string messageTemplate, params ReadOnlySpan<object?> propertyValues)
    {
        // Ensure that the event will be logged in the end before
        // committing to costly array and string allocations.
        ILogSink? sink = AvaloniaLogger.Sink;
        LogEventLevel logEventLevel = (LogEventLevel)Math.Max((int)level, (int)s_minLogLevel);
        if (sink?.IsEnabled(logEventLevel, LogArea) != true)
            return;

        // The sink provided by Avalonia does not format logs properly.
        // This is a futile attempt to fix it on our side.
        if (sink.GetType().Assembly == typeof(AvaloniaLogger).Assembly)
            messageTemplate = FormatAvaloniaMessageTemplate(messageTemplate);

        sink.Log(logEventLevel, LogArea, source, messageTemplate, propertyValues.ToArray());
    }

    /// <summary>
    /// Prepends a space to the original message template and strips surrounding quotes from substitution
    /// parameters in an attempt to circumvent the unfortunate design of Avalonia's default log sink.
    /// </summary>
    /// <param name="originalMessageTemplate">The message template to format.</param>
    /// <returns>A message template suitable for consumption by Avalonia's slightly broken log sink.</returns>
    [SkipLocalsInit]
    private static string FormatAvaloniaMessageTemplate(string originalMessageTemplate)
    {
        int length = originalMessageTemplate.Length + 1;
        Span<char> messageTemplate = length < 256 ? stackalloc char[length] : new char[length];
        messageTemplate[0] = ' ';
        ((ReadOnlySpan<char>)originalMessageTemplate).CopyTo(messageTemplate.Slice(1));

        int quoteStart = messageTemplate.IndexOf("'{");
        int quoteEnd = 0;
        while (quoteStart >= 0)
        {
            quoteStart += quoteEnd;
            quoteEnd = messageTemplate.Slice(quoteStart + 2).IndexOf('\'');
            if (quoteEnd < 0)
                break;

            quoteEnd += quoteStart + 2;
            messageTemplate.Slice(quoteStart + 1, quoteEnd - quoteStart - 1).CopyTo(messageTemplate.Slice(quoteStart));
            messageTemplate.Slice(quoteEnd + 1).CopyTo(messageTemplate.Slice(--quoteEnd));
            messageTemplate = messageTemplate.Slice(0, messageTemplate.Length - 2);
            quoteStart = messageTemplate.Slice(quoteEnd).IndexOf("'{");
        }

        return messageTemplate.ToString();
    }
}
