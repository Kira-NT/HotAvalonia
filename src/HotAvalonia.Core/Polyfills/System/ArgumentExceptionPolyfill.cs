using System.Diagnostics.CodeAnalysis;

namespace System;

internal static class ArgumentExceptionPolyfill
{
    extension(ArgumentException)
    {
        [DoesNotReturn]
        internal static void Throw(string? paramName, string? message = null)
            => throw new ArgumentException(message, paramName);
    }
}
