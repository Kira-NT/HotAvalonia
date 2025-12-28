#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;

namespace System;

internal static class StringPolyfill
{
    extension(string)
    {
        [SkipLocalsInit]
        public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            Span<char> chars = length <= 384 ? stackalloc char[length] : new char[length];
            action(chars, state);
            return chars.ToString();
        }
    }
}
#endif
