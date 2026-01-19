#if NETSTANDARD2_0
using System.Runtime.CompilerServices;

namespace System;

internal readonly record struct Range(Index Start, Index End)
{
    public static Range All => new(Index.Start, Index.End);

    public static Range StartAt(Index start) => new(start, Index.End);

    public static Range EndAt(Index end) => new(Index.Start, end);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int Offset, int Length) GetOffsetAndLength(int length)
    {
        Index startIndex = Start;
        Index endIndex = End;
        int start = startIndex.IsFromEnd ? length - startIndex.Value : startIndex.Value;
        int end = endIndex.IsFromEnd ? length - endIndex.Value : endIndex.Value;
        if ((uint)end > (uint)length || (uint)start > (uint)end)
            ArgumentOutOfRangeException.Throw(nameof(length));

        return (start, end - start);
    }

    public override string ToString() => $"{Start}..{End}";
}
#endif
