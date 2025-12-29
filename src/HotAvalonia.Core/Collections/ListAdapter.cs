using System.Collections;
using System.Diagnostics;

namespace HotAvalonia.Collections;

/// <summary>
/// Provides list semantics over an underlying <see cref="ICollection{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <param name="collection">The collection that needs to be treated as a list.</param>
[DebuggerDisplay("Count = {Count}")]
internal sealed class ListAdapter<T>(ICollection<T> collection) : IList<T>, IReadOnlyList<T>
{
    /// <inheritdoc/>
    public T this[int index]
    {
        get => collection.ElementAt(index);

        set
        {
            if (collection is IList<T> list)
            {
                list[index] = value;
            }
            else
            {
                InvalidOperationException.Throw();
            }
        }
    }

    /// <inheritdoc/>
    public int Count => collection.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => collection.IsReadOnly;

    /// <inheritdoc/>
    public void Add(T item) => collection.Add(item);

    /// <inheritdoc/>
    public void Insert(int index, T item)
    {
        if (collection is IList<T> list)
        {
            list.Insert(index, item);
        }
        else
        {
            InvalidOperationException.Throw();
        }
    }

    /// <inheritdoc/>
    public bool Remove(T item) => collection.Remove(item);

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        if (collection is IList<T> list)
        {
            list.RemoveAt(index);
        }
        else
        {
            collection.Remove(collection.ElementAt(index));
        }
    }

    /// <inheritdoc/>
    public void Clear() => collection.Clear();

    /// <inheritdoc/>
    public bool Contains(T item) => collection.Contains(item);

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        if (collection is IList<T> list)
            return list.IndexOf(item);

        InvalidOperationException.Throw();
        return -1;
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)collection).GetEnumerator();
}
