using System.Diagnostics.CodeAnalysis;

namespace System;

internal static class ExceptionPolyfill
{
    extension<TException>(TException) where TException : Exception, new()
    {
        [DoesNotReturn]
        public static void Throw() => throw new TException();
    }
}
