namespace HotAvalonia.Helpers;

/// <summary>
/// Provides extension methods for working with sequences.
/// </summary>
internal static class EnumerableHelper
{
    /// <summary>
    /// Asynchronously enumerates the elements of the given <see cref="IAsyncEnumerable{T}"/>
    /// and returns them as an array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the asynchronous sequence.</typeparam>
    /// <param name="source">The asynchronous sequence to enumerate.</param>
    /// <returns>An array containing all elements from the source sequence.</returns>
    public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source)
    {
        List<T> values = new();
        await foreach (T value in source.ConfigureAwait(false))
            values.Add(value);

        return values.ToArray();
    }

    /// <summary>
    /// Converts a synchronous <see cref="IEnumerable{T}"/> to an <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The synchronous sequence to convert.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> that yields elements from the source sequence.</returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        await default(ValueTask);
        foreach (T value in source)
            yield return value;
    }

    /// <summary>
    /// Converts a task that produces an <see cref="IEnumerable{T}"/> to an <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">A <see cref="Task{TResult}"/> that returns a synchronous sequence to convert.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> that yields elements from the resolved sequence.</returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<IEnumerable<T>> source)
    {
        IEnumerable<T> values = await source.ConfigureAwait(false);
        foreach (T value in values)
            yield return value;
    }
}
