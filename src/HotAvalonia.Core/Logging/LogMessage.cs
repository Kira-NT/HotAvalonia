using System.Text;
using Avalonia.Logging;

namespace HotAvalonia.Logging;

/// <summary>
/// Represents a structured log message with timestamp, level, source, format, and arguments.
/// </summary>
internal readonly struct LogMessage
{
    /// <summary>
    /// The timestamp when the log message was created.
    /// </summary>
    public readonly DateTimeOffset Timestamp;

    /// <summary>
    /// The severity level of the log message.
    /// </summary>
    public readonly LogEventLevel Level;

    /// <summary>
    /// The logical area associated with the message.
    /// </summary>
    public readonly string Area;

    /// <summary>
    /// The source object that generated the log message.
    /// </summary>
    public readonly object? Source;

    /// <summary>
    /// The message format string.
    /// </summary>
    public readonly string Format;

    /// <summary>
    /// The arguments to be applied to the <see cref="Format"/> string.
    /// </summary>
    public readonly object?[] Arguments;

    /// <inheritdoc cref="LogMessage(DateTimeOffset, LogEventLevel, string, object?, string, object?[])"/>
    public LogMessage(LogEventLevel level, string area, object? source, string format, params object?[] args)
    {
        Timestamp = DateTimeOffset.UtcNow;
        Level = level;
        Area = area;
        Source = source;
        Format = format;
        Arguments = args;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogMessage"/> struct.
    /// </summary>
    /// <param name="timestamp">The timestamp of the log message.</param>
    /// <param name="level">The log level of the message.</param>
    /// <param name="area">The area or category of the message.</param>
    /// <param name="source">The source object that generated the message.</param>
    /// <param name="format">The message format string.</param>
    /// <param name="args">The arguments to format the message.</param>
    public LogMessage(DateTimeOffset timestamp, LogEventLevel level, string area, object? source, string format, params object?[] args)
    {
        Timestamp = timestamp;
        Level = level;
        Area = area;
        Source = source;
        Format = format;
        Arguments = args;
    }

    /// <summary>
    /// Returns a formatted string representation of the log message.
    /// </summary>
    /// <returns>A formatted string representation of the log message.</returns>
    public override string ToString()
    {
        if (Format is not { Length: > 0 })
            return string.Empty;

        StringBuilder result = new(Format.Length + 64);
        if (Timestamp > DateTimeOffset.MinValue)
            result.Append(Timestamp.ToString("[HH:mm:ss.fff] "));

        if (!string.IsNullOrEmpty(Area))
            result.Append('[').Append(Area).Append(']').Append(' ');

        string levelName = Level switch
        {
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "FATAL",
            _ => string.Empty,
        };
        if (!string.IsNullOrEmpty(levelName))
            result.Append('[').Append(levelName).Append(']').Append(' ');

        AppendFormat(result, Format, Arguments ?? []);

        if (Source is not null)
            result.Append(' ').Append('(').Append(Source.GetType().Name).Append('#').Append(Source.GetHashCode()).Append(')');

        return result.ToString();
    }

    /// <summary>
    /// Appends a formatted string to the builder using the specified format and arguments.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="args">An array of objects to format.</param>
    private static void AppendFormat(StringBuilder builder, string format, object?[] args)
    {
        // Note, this implementation is quite forgiving, as Avalonia's own formatting logic
        // simply shrugs off poorly formatted log messages. We do the exact same here to make sure
        // malformed logs don't crash a user's app (or at least that we're not the reason they do).
        // This means we accept format items that are never closed (e.g., "{0") by treating the remainder
        // of the string as a parameter name, and we discard dangling closing brackets that don't actually
        // close anything (e.g., "0}" will just print "0").
        //
        // Also, unlike Avalonia, we **do** support per-item format strings.
        // So, while Avalonia's default log sink would log "0x{0:x2}" as "0x32"
        // when given `32` as an argument, ours would correctly output "0x20".
        for (int i = 0, j = 0; i < format.Length;)
        {
            switch (format[i++])
            {
                case '{':
                    int openingBraceCount = 1;
                    while (i < format.Length && format[i] is '{')
                    {
                        i++;
                        openingBraceCount++;
                    }
                    builder.Append('{', openingBraceCount / 2);
                    if ((openingBraceCount & 1) == 0)
                        continue;

                    int argFormatIndex = i;
                    int closingBraceIndex = format.IndexOf('}', argFormatIndex);
                    if (closingBraceIndex < 0)
                        closingBraceIndex = format.Length;

                    i = closingBraceIndex + 1;
                    int closingBraceCount = 1;
                    while (i < format.Length && format[i] is '}')
                    {
                        i++;
                        closingBraceCount++;
                    }

                    object? arg = j < args.Length ? args[j++] : null;
                    AppendFormat(builder, format.AsSpan(argFormatIndex, closingBraceIndex - argFormatIndex), arg);
                    builder.Append('}', closingBraceCount / 2);
                    break;

                case '}':
                    if (i < format.Length && format[i] is '}')
                    {
                        i++;
                        builder.Append('}');
                    }
                    break;

                case char c:
                    builder.Append(c);
                    break;
            }
        }
    }

    /// <summary>
    /// Appends a formatted value to the builder.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
    /// <param name="format">The format string.</param>
    /// <param name="value">The value to format and append.</param>
    private static void AppendFormat(StringBuilder builder, ReadOnlySpan<char> format, object? value)
    {
        int valueFormatStart = format.IndexOf(':');
        if (valueFormatStart < 0)
            valueFormatStart = format.Length;

        int alignmentStart = format.IndexOf(',');
        if (alignmentStart <= 0)
            alignmentStart = valueFormatStart;

        // If the format string doesn't contain any signs of custom formatting,
        // short-circuit and append the value as-is.
        if (alignmentStart == format.Length)
        {
            builder.Append(value);
            return;
        }

        // Otherwise, reconstruct the format string by stripping the argument name and wrapping
        // the rest into an indexed format string, allowing StringBuilder to handle it as usual.
        // So, for example, "Foo,-4:x2" becomes "{0,-4:x2}".
        ReadOnlySpan<char> subFormat = format.Slice(alignmentStart);
        int reconstructedFormatLength = subFormat.Length + 3;
        Span<char> reconstructedFormat = reconstructedFormatLength < 256 ? stackalloc char[reconstructedFormatLength] : new char[reconstructedFormatLength];
        reconstructedFormat[0] = '{';
        reconstructedFormat[1] = '0';
        subFormat.CopyTo(reconstructedFormat.Slice(2));
        reconstructedFormat[reconstructedFormat.Length - 1] = '}';
        builder.AppendFormat(reconstructedFormat.ToString(), value);
    }
}
