using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotAvalonia.Reflection;

/// <summary>
/// Represents a boundary within an exception handling region in a method body,
/// such as the start or end of a <c>try</c>, <c>catch</c>, <c>filter</c>,
/// <c>finally</c>, or <c>fault</c> block.
/// </summary>
internal readonly record struct ExceptionRegionBoundary : IComparable<ExceptionRegionBoundary>
{
    private readonly int _offset;

    private readonly ExceptionRegionBoundaryKind _kind;

    private readonly ExceptionHandlingClause _clause;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionRegionBoundary"/> class.
    /// </summary>
    /// <param name="offset">The offset within the method, in bytes, of this boundary.</param>
    /// <param name="kind">
    /// A value that specifies the kind of region and whether the boundary represents
    /// the start or the end of that region.
    /// </param>
    /// <param name="clause">The exception handling clause to which this boundary belongs.</param>
    public ExceptionRegionBoundary(int offset, ExceptionRegionBoundaryKind kind, ExceptionHandlingClause clause)
    {
        _offset = offset;
        _kind = kind;
        _clause = clause;
    }

    /// <summary>
    /// Gets the offset within the method, in bytes, of this boundary.
    /// </summary>
    public int Offset => _offset;

    /// <summary>
    /// Gets a value that describes the kind of exception region and whether this
    /// boundary represents the start or the end of that region.
    /// </summary>
    public ExceptionRegionBoundaryKind Kind => _kind;

    /// <summary>
    /// Gets the exception handling clause to which this boundary belongs.
    /// </summary>
    public ExceptionHandlingClause Clause => _clause;

    /// <summary>
    /// Deconstructs an exception handling clause into its constituent region boundaries.
    /// </summary>
    /// <param name="clause">The exception handling clause to deconstruct.</param>
    /// <returns>
    /// An array of <see cref="ExceptionRegionBoundary"/> instances that represent the
    /// start and end boundaries of all regions defined by the specified clause.
    /// </returns>
    public static ExceptionRegionBoundary[] ToBoundaries(ExceptionHandlingClause clause)
    {
        ArgumentNullException.ThrowIfNull(clause);

        ExceptionHandlingClauseOptions flags = clause.Flags;
        int count = 4 + ((int)(flags & ExceptionHandlingClauseOptions.Filter) << 1);
        ExceptionRegionBoundary[] boundaries = new ExceptionRegionBoundary[count];

        boundaries[0] = new(clause.TryOffset, ExceptionRegionBoundaryKind.TryStart, clause);
        boundaries[1] = new(clause.TryOffset + clause.TryLength, ExceptionRegionBoundaryKind.TryEnd, clause);

        if ((flags & ExceptionHandlingClauseOptions.Filter) != 0)
        {
            boundaries[2] = new(clause.FilterOffset, ExceptionRegionBoundaryKind.FilterStart, clause);
            boundaries[3] = new(clause.HandlerOffset, ExceptionRegionBoundaryKind.FilterEnd, clause);
            flags &= ~ExceptionHandlingClauseOptions.Filter;
        }

        ExceptionRegionBoundaryKind kind = (ExceptionRegionBoundaryKind)(flags + (2 << ((int)flags >>> 2)));
        boundaries[count - 2] = new(clause.HandlerOffset, kind | ExceptionRegionBoundaryKind.Start, clause);
        boundaries[count - 1] = new(clause.HandlerOffset + clause.HandlerLength, kind | ExceptionRegionBoundaryKind.End, clause);

        return boundaries;
    }

    /// <summary>
    /// Deconstructs a collection of exception handling clauses into their constituent region boundaries.
    /// </summary>
    /// <param name="clauses">A collection of exception handling clauses to deconstruct.</param>
    /// <returns>
    /// An array of <see cref="ExceptionRegionBoundary"/> instances that represent
    /// the start and end boundaries of all regions defined by the specified clauses.
    /// </returns>
    public static ExceptionRegionBoundary[] ToBoundaries(IEnumerable<ExceptionHandlingClause> clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        int count = 0;
        if (clauses is ICollection<ExceptionHandlingClause> clauseCollection)
        {
            count = clauseCollection.Count;
            if (count == 0)
                return [];
        }

        return ToBoundaries(clauses, count).ToArray();
    }

    /// <inheritdoc cref="ToBoundaries(IEnumerable{ExceptionHandlingClause})"/>
    /// <param name="count">
    /// An approximate number of exception handling clauses in the provided collection.
    /// May be 0 if unknown.
    /// </param>
    internal static List<ExceptionRegionBoundary> ToBoundaries(IEnumerable<ExceptionHandlingClause> clauses, int count)
    {
        List<ExceptionRegionBoundary> boundaries = new(count * 6);
        foreach (ExceptionHandlingClause clause in clauses)
        {
            int i = InsertTryStartBoundary(boundaries, new(clause.TryOffset, ExceptionRegionBoundaryKind.TryStart, clause));
            i = i >= 0 ? InsertBoundary(boundaries, new(clause.TryOffset + clause.TryLength, ExceptionRegionBoundaryKind.TryEnd, clause), i) : 0;

            ExceptionHandlingClauseOptions flags = clause.Flags;
            if ((flags & ExceptionHandlingClauseOptions.Filter) != 0)
            {
                i = InsertBoundary(boundaries, new(clause.FilterOffset, ExceptionRegionBoundaryKind.FilterStart, clause), i);
                i = InsertBoundary(boundaries, new(clause.HandlerOffset, ExceptionRegionBoundaryKind.FilterEnd, clause), i);
                flags &= ~ExceptionHandlingClauseOptions.Filter;
            }

            ExceptionRegionBoundaryKind kind = (ExceptionRegionBoundaryKind)(flags + (2 << ((int)flags >>> 2)));
            i = InsertBoundary(boundaries, new(clause.HandlerOffset, kind | ExceptionRegionBoundaryKind.Start, clause), i);
            _ = InsertBoundary(boundaries, new(clause.HandlerOffset + clause.HandlerLength, kind | ExceptionRegionBoundaryKind.End, clause), i);
        }
        return boundaries;
    }

    private static int InsertTryStartBoundary(List<ExceptionRegionBoundary> boundaries, ExceptionRegionBoundary boundary)
    {
        int i = boundaries.BinarySearch(boundary);
        if (i >= 0)
        {
            ExceptionHandlingClause clause = boundaries[i].Clause;
            if ((clause.TryOffset, clause.TryLength) == (boundary.Clause.TryOffset, boundary.Clause.TryLength))
                return -1;
        }
        else
        {
            i = ~i;
        }
        boundaries.Insert(i, boundary);
        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InsertBoundary(List<ExceptionRegionBoundary> boundaries, ExceptionRegionBoundary boundary, int index)
    {
        int i = boundaries.BinarySearch(index, boundaries.Count - index, boundary, comparer: null);
        boundaries.Insert(i ^= i >> 31, boundary);
        return i;
    }

    public int CompareTo(ExceptionRegionBoundary other)
    {
        int offset = _offset;
        int otherOffset = other._offset;
        if (offset < otherOffset)
            return -1;
        if (offset > otherOffset)
            return 1;

        int kindOrder = (sbyte)_kind - (sbyte)other._kind;
        if (kindOrder != 0)
            return kindOrder;

        return CompareToNested(other);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int CompareToNested(ExceptionRegionBoundary other)
    {
        int tryOffset = _clause.TryOffset;
        int otherTryOffset = other._clause.TryOffset;
        if (tryOffset < otherTryOffset)
            return 1;
        if (tryOffset > otherTryOffset)
            return -1;

        int tryLength = _clause.TryLength;
        int otherTryLength = other._clause.TryLength;
        if (tryLength < otherTryLength)
            return 1;
        if (tryLength > otherTryLength)
            return -1;

        return 0;
    }

    public override string ToString()
    {
        StringBuilder builder = new(22);
        builder.Append($"IL_{_offset:X4}: ");
        builder.Append((_kind & ExceptionRegionBoundaryKind.RegionKindMask) switch
        {
            ExceptionRegionBoundaryKind.Try => "try",
            ExceptionRegionBoundaryKind.Filter => "filter",
            ExceptionRegionBoundaryKind.Catch => "catch",
            ExceptionRegionBoundaryKind.Finally => "finally",
            ExceptionRegionBoundaryKind.Fault => "fault",
            _ => ((uint)_kind).ToString(),
        });
        builder.Append((_kind & ExceptionRegionBoundaryKind.BoundaryKindMask) switch
        {
            ExceptionRegionBoundaryKind.Start => ":start",
            ExceptionRegionBoundaryKind.End => ":end",
            _ => "",
        });
        return builder.ToString();
    }
}
