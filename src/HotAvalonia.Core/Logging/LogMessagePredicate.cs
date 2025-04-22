namespace HotAvalonia.Logging;

/// <summary>
/// Represents a delegate used to filter <see cref="LogMessage"/> instances.
/// </summary>
/// <param name="message">The log message to evaluate.</param>
/// <returns><c>true</c> if the message should be processed; otherwise, <c>false</c>.</returns>
internal delegate bool LogMessagePredicate(in LogMessage message);
