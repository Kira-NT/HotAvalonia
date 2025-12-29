using System.Collections;

namespace HotAvalonia.Collections;

/// <summary>
/// Provides a thread-safe enumerator over an <see cref="IReadOnlyList{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the sequence.</typeparam>
internal sealed class SynchronizedEnumerable<T> : IEnumerable<T>
{
    /// <summary>
    /// The list being enumerated.
    /// </summary>
    private readonly IReadOnlyList<T> _list;

    /// <summary>
    /// The synchronization object used to guard access during enumeration.
    /// </summary>
    private readonly object _lock;

    /// <inheritdoc cref="SynchronizedEnumerable(IReadOnlyList{T}, object)"/>
    public SynchronizedEnumerable(IReadOnlyList<T> list) : this(list, new())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedEnumerable{T}"/> class.
    /// </summary>
    /// <param name="list">The list to enumerate.</param>
    /// <param name="syncRoot">An object used to synchronize access during enumeration.</param>
    public SynchronizedEnumerable(IReadOnlyList<T> list, object syncRoot)
    {
        _list = list;
        _lock = syncRoot;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerates the elements of a <see cref="SynchronizedEnumerable{T}"/> in a thread-safe manner.
    /// </summary>
    private sealed class Enumerator : IEnumerator<T>
    {
        /// <summary>
        /// The parent enumerable being enumerated.
        /// </summary>
        private readonly SynchronizedEnumerable<T> _enumerable;

        /// <summary>
        /// The current index within the list.
        /// </summary>
        /// <remarks>
        /// An index of <c>-1</c> indicates that enumeration has not yet started.
        /// </remarks>
        private int _index;

        /// <summary>
        /// The current element in the enumeration.
        /// </summary>
        private T? _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> class.
        /// </summary>
        /// <param name="enumerable">The <see cref="SynchronizedEnumerable{T}"/> being enumerated.</param>
        public Enumerator(SynchronizedEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
            _index = -1;
            _current = default;
        }

        /// <inheritdoc/>
        object? IEnumerator.Current => _current;

        /// <inheritdoc/>
        public T Current => _current!;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            int index = _index + 1;
            lock (_enumerable._lock)
            {
                if (index < _enumerable._list.Count)
                {
                    _index = index;
                    _current = _enumerable._list[index];
                    return true;
                }
            }
            _current = default;
            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _index = -1;
            _current = default;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose() { }
    }
}
