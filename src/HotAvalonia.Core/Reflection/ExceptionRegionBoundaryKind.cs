namespace HotAvalonia.Reflection;

/// <summary>
/// Specifies the kind of an exception region boundary.
/// </summary>
[Flags]
internal enum ExceptionRegionBoundaryKind
{
    /// <summary>
    /// Indicates an unknown boundary kind.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates a <c>filter</c> region.
    /// </summary>
    Filter = 1 << 0,

    /// <summary>
    /// Indicates a <c>catch</c> region.
    /// </summary>
    Catch = 1 << 1,

    /// <summary>
    /// Indicates a <c>finally</c> region.
    /// </summary>
    Finally = 1 << 2,

    /// <summary>
    /// Indicates a <c>fault</c> region.
    /// </summary>
    Fault = 1 << 3,

    /// <summary>
    /// Indicates a <c>try</c> region.
    /// </summary>
    Try = 1 << 4,

    /// <summary>
    /// Indicates the start of a region.
    /// </summary>
    Start = 1 << 6,

    /// <summary>
    /// Indicates the end of a region.
    /// </summary>
    End = 1 << 7,

    /// <summary>
    /// Indicates the start of a <c>try</c> region.
    /// </summary>
    TryStart = Try | Start,

    /// <summary>
    /// Indicates the end of a <c>try</c> region.
    /// </summary>
    TryEnd = Try | End,

    /// <summary>
    /// Indicates the start of a <c>filter</c> region.
    /// </summary>
    FilterStart = Filter | Start,

    /// <summary>
    /// Indicates the end of a <c>filter</c> region.
    /// </summary>
    FilterEnd = Filter | End,

    /// <summary>
    /// Indicates the start of a <c>catch</c> region.
    /// </summary>
    CatchStart = Catch | Start,

    /// <summary>
    /// Indicates the end of a <c>catch</c> region.
    /// </summary>
    CatchEnd = Catch | End,

    /// <summary>
    /// Indicates the start of a <c>finally</c> region.
    /// </summary>
    FinallyStart = Finally | Start,

    /// <summary>
    /// Indicates the end of a <c>finally</c> region.
    /// </summary>
    FinallyEnd = Finally | End,

    /// <summary>
    /// Indicates the start of a <c>fault</c> region.
    /// </summary>
    FaultStart = Fault | Start,

    /// <summary>
    /// Indicates the end of a <c>fault</c> region.
    /// </summary>
    FaultEnd = Fault | End,

    /// <summary>
    /// A mask that selects the exception region kind.
    /// </summary>
    RegionKindMask = Try | Filter | Catch | Finally | Fault,

    /// <summary>
    /// A mask that selects a value indicating whether the boundary
    /// represents the start or the end of a region.
    /// </summary>
    BoundaryKindMask = Start | End,
}
