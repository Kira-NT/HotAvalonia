#if !NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static class ArgumentOutOfRangeExceptionExtensions
{
    extension(ArgumentOutOfRangeException)
    {
        public static void ThrowIfNegative<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : unmanaged, IComparable<T>
        {
            if (value.CompareTo(default) < 0)
                ThrowNegative(value, paramName);
        }

        public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : unmanaged, IComparable<T>
        {
            if (value.CompareTo(other) > 0)
                ThrowGreater(value, other, paramName);
        }

        public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : unmanaged, IComparable<T>
        {
            if (value.CompareTo(other) < 0)
                ThrowLess(value, other, paramName);
        }

        [DoesNotReturn]
        private static void ThrowNegative<T>(T value, string? paramName)
            => throw new ArgumentOutOfRangeException($"{paramName} ('{value}') must be a non-negative value.");

        [DoesNotReturn]
        private static void ThrowGreater<T>(T value, T other, string? paramName)
            => throw new ArgumentOutOfRangeException($"{paramName} ('{value}') must be less than or equal to '{other}'.");

        [DoesNotReturn]
        private static void ThrowLess<T>(T value, T other, string? paramName)
            => throw new ArgumentOutOfRangeException($"{paramName} ('{value}') must be greater than or equal to '{other}'.");
    }
}
#endif
