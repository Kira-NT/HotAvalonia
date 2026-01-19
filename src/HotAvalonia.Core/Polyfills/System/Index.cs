#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal readonly struct Index : IEquatable<Index>
{
    private readonly int _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Index(int value, bool fromEnd = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        _value = fromEnd ? ~value : value;
    }

    private Index(int value)
    {
        _value = value;
    }

    public static implicit operator Index(int value) => FromStart(value);

    public static Index Start => new(0);

    public static Index End => new(~0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromStart(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromEnd(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        return new(~value);
    }

    public int Value => _value < 0 ? ~_value : _value;

    public bool IsFromEnd => _value < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(int length)
    {
        int offset = _value;
        if (IsFromEnd)
            offset += length + 1;

        return offset;
    }

    public override bool Equals([NotNullWhen(true)] object? value) => value is Index index && _value == index._value;

    public bool Equals(Index other) => _value == other._value;

    public override int GetHashCode() => _value;

    public override string ToString() => IsFromEnd ? '^' + Value.ToString() : ((uint)Value).ToString();
}
#endif
