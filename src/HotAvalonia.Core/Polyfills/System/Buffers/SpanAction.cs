#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
namespace System.Buffers;

internal delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);
#endif
