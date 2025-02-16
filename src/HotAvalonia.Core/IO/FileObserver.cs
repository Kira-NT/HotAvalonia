using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace HotAvalonia.IO;

/// <summary>
/// Provides an observable mechanism for monitoring a file, notifying subscribers of updates.
/// </summary>
/// <typeparam name="T">The type of data provided to observers.</typeparam>
internal sealed class FileObserver<T> : IObservable<T>, IObserver<FileSystemEventArgs>
{
    /// <summary>
    /// A function that provides the data to be observed.
    /// </summary>
    private readonly Func<T> _provider;

    /// <summary>
    /// A dictionary that maintains the observers subscribed to this <see cref="FileObserver{T}"/>.
    /// </summary>
    private readonly ConcurrentDictionary<IObserver<T>, Entry> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileObserver{T}"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system where <paramref name="fileName"/> can be found.</param>
    /// <param name="fileName">The name of the file being observed.</param>
    /// <param name="provider">The function that provides the observed data.</param>
    public FileObserver(IFileSystem fileSystem, string fileName, Func<T> provider)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _ = provider ?? throw new ArgumentNullException(nameof(provider));

        _provider = provider;
        _entries = new();

        SharedFileObserver.Subscribe(this, fileName, fileSystem);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
        => _entries.GetOrAdd(observer, x => new(this, x));

    /// <inheritdoc/>
    void IObserver<FileSystemEventArgs>.OnCompleted()
    {
        foreach (IObserver<T> observer in _entries.Keys)
        {
            try
            {
                observer.OnCompleted();
            }
            catch
            {
                // Just ignore it, it's not our problem.
            }
        }
    }

    /// <inheritdoc/>
    void IObserver<FileSystemEventArgs>.OnError(Exception error)
    {
        foreach (IObserver<T> observer in _entries.Keys)
        {
            try
            {
                observer.OnError(error);
            }
            catch
            {
                // Just ignore it, it's not our problem.
            }
        }
    }

    /// <inheritdoc/>
    void IObserver<FileSystemEventArgs>.OnNext(FileSystemEventArgs value)
    {
        T nextValue;
        try
        {
            nextValue = _provider();
        }
        catch (Exception error)
        {
            ((IObserver<FileSystemEventArgs>)this).OnError(error);
            return;
        }

        foreach (IObserver<T> observer in _entries.Keys)
        {
            try
            {
                observer.OnNext(nextValue);
            }
            catch
            {
                // Just ignore it, it's not our problem.
            }
        }
    }

    /// <summary>
    /// Represents a subscription entry that connects an observer to the <see cref="FileObserver{T}"/>.
    /// </summary>
    private sealed class Entry : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="FileObserver{T}"/> instance associated with this entry.
        /// </summary>
        public FileObserver<T> FileObserver { get; }

        /// <summary>
        /// Gets the observer associated with this entry.
        /// </summary>
        public IObserver<T> Observer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class.
        /// </summary>
        /// <param name="fileObserver">The <see cref="FileObserver{T}"/> that owns this entry.</param>
        /// <param name="observer">The observer associated with this entry.</param>
        public Entry(FileObserver<T> fileObserver, IObserver<T> observer)
        {
            FileObserver = fileObserver;
            Observer = observer;
        }

        /// <summary>
        /// Disposes of this entry, removing the observer from the <see cref="FileObserver{T}"/>.
        /// </summary>
        public void Dispose() => FileObserver._entries.TryRemove(Observer, out _);
    }
}

/// <summary>
/// Provides an observable mechanism for monitoring multiple files at once,
/// reducing resource consumption, and notifying subscribers of updates.
/// </summary>
/// <remarks>
/// The sole reason this exists is that the Linux implementation of <see cref="IFileSystemWatcher"/>
/// is, to say the least, an utter and complete disaster. Each new instantiation of the said class
/// creates its own <c>inotify</c> instance, quickly exhausting the available pool limit, which is
/// often quite low by itself. Therefore, under no circumstances is it viable to waste so many
/// resources watching a single file.
///
/// <br/><br/>
///
/// Thus, we do something similar to what should have been done on the .NET side from
/// the very start: we maintain a single <see cref="IFileSystemWatcher"/> to monitor
/// every file we need. However, this introduces a challenge: due to the design of
/// <see cref="IFileSystemWatcher"/>, we cannot monitor a specific set of directories.
/// We <b>must</b> monitor some root directory and <b>all</b> its descendants.
/// To solve this, we determine the common path shared by two file paths and watch it,
/// because this is guaranteed to include the files we care about. This <i>should</i>
/// be fine for our scenario, as, realistically, the files that a user of <c>HotAvalonia</c>
/// would want to monitor are likely located fairly close together, even in multi-project
/// setups. However, more generally, it's definitely possible to accidentally subscribe to
/// file system events raised <b>anywhere</b> on the system if the only shared part of two
/// monitored file paths is the root directory.
///
/// <br/><br/>
///
/// Additionally, on Windows and macOS, we face a problem where two file paths
/// may not share any common path at all - this occurs when the files are
/// located on different drives. For this reason, we allow the creation of
/// multiple instances of <see cref="SharedFileObserver"/> rather than making
/// it a singleton. Each instance represents a volume to which it is attached.
///
/// <br/><br/>
///
/// See: https://github.com/dotnet/runtime/issues/62869
/// </remarks>
file sealed class SharedFileObserver : IDisposable
{
    /// <summary>
    /// A dictionary that maintains a mapping of volume identifiers to
    /// their corresponding <see cref="SharedFileObserver"/> instances.
    /// </summary>
    private static readonly ConditionalWeakTable<IFileSystem, ConcurrentDictionary<string, SharedFileObserver>> s_volumeObservers = new();


    /// <summary>
    /// The <see cref="IFileSystemWatcher"/> instance used to monitor the files.
    /// </summary>
    private readonly IFileSystemWatcher _watcher;

    /// <summary>
    /// A dictionary that maintains a mapping of the monitored files to their
    /// corresponding observers.
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentBag<WeakReference<IObserver<FileSystemEventArgs>>>> _observers;

    /// <summary>
    /// A synchronization lock for managing concurrent access
    /// to the internal state of this instance.
    /// </summary>
    private readonly object _lock;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedFileObserver"/> class.
    /// </summary>
    public SharedFileObserver(IFileSystem fileSystem)
    {
        _observers = new(fileSystem.PathComparer);
        _lock = new();

        _watcher = fileSystem.CreateFileSystemWatcher();
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Error += OnError;
        _watcher.IncludeSubdirectories = true;
        _watcher.NotifyFilter = NotifyFilters.CreationTime
            | NotifyFilters.LastWrite
            | NotifyFilters.FileName
            | NotifyFilters.DirectoryName;
    }

    /// <summary>
    /// Gets the file system associated with this observer.
    /// </summary>
    public IFileSystem FileSystem => _watcher.FileSystem;

    /// <summary>
    /// Subscribes an observer to changes for the specified file.
    /// </summary>
    /// <param name="observer">The observer to be notified of file events.</param>
    /// <param name="fileName">The name of the file to monitor.</param>
    /// <param name="fileSystem">The file system where <paramref name="fileName"/> can be found.</param>
    public static void Subscribe(IObserver<FileSystemEventArgs> observer, string fileName, IFileSystem fileSystem)
    {
        _ = observer ?? throw new ArgumentNullException(nameof(observer));
        _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        fileName = fileSystem.GetFullPath(fileName);
        string volumeName = fileSystem.GetVolumeName(fileName);

        s_volumeObservers
            .GetOrCreateValue(fileSystem)
            .GetOrAdd(volumeName, key => new(fileSystem))
            .SubscribeCore(observer, fileName);
    }

    /// <inheritdoc cref="Subscribe"/>
    private void SubscribeCore(IObserver<FileSystemEventArgs> observer, string fileName)
    {
        fileName = _watcher.FileSystem.GetFullPath(fileName);
        _observers.AddOrUpdate(
            fileName,
            key => new() { new(observer) },
            (key, value) => { value.Add(new(observer)); return value; }
        );

        Watch(fileName);
    }

    /// <summary>
    /// Adds the specified file to the list of files being watched.
    /// </summary>
    /// <param name="fileName">The name of the file to watch.</param>
    private void Watch(string fileName)
    {
        fileName = FileSystem.GetFullPath(fileName);

        lock (_lock)
        {
            string? newPath = string.IsNullOrEmpty(_watcher.Path)
                ? FileSystem.GetDirectoryName(fileName)
                : FileSystem.GetCommonPath(fileName, _watcher.Path);
            if (newPath is null or { Length: 0 })
                return;

            _watcher.Path = newPath;
            _watcher.EnableRaisingEvents = true;
        }
    }

    /// <summary>
    /// Handles the <see cref="IFileSystemWatcher"/> events to notify
    /// observers of file changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The file system event data.</param>
    private void OnChanged(object sender, FileSystemEventArgs args)
    {
        OnNext(args.FullPath, args);

        if (args is RenamedEventArgs renamedArgs)
            OnNext(renamedArgs.OldFullPath, renamedArgs);
    }

    /// <summary>
    /// Notifies the observers of a specific file about a file system event.
    /// </summary>
    /// <param name="fileName">The name of the file affected by the event.</param>
    /// <param name="args">The event data.</param>
    private void OnNext(string fileName, FileSystemEventArgs args)
    {
        fileName = FileSystem.GetFullPath(fileName);
        if (!_observers.TryGetValue(fileName, out ConcurrentBag<WeakReference<IObserver<FileSystemEventArgs>>>? weakObservers))
            return;

        int hits = 0;
        int misses = 0;
        foreach (WeakReference<IObserver<FileSystemEventArgs>> weakObserver in weakObservers)
        {
            if (!weakObserver.TryGetTarget(out IObserver<FileSystemEventArgs>? observer) || observer is null)
            {
                ++misses;
                continue;
            }

            ++hits;
            try
            {
                observer.OnNext(args);
            }
            catch
            {
                // Just ignore it, it's not our problem.
            }
        }

        // I would much prefer using a `ConditionalWeakTable` instead of all this,
        // but it only allows retrieving all the stored keys starting from
        // .NET Standard 2.1, and we are targeting .NET Standard 2.0.
        // Therefore, we need to manually check from time to time if
        // there are too many stale references stored in our observer
        // bag and clean it up a bit.
        if (misses > (hits + 1) * 4)
        {
            _observers.AddOrUpdate(
                fileName,
                key => new(weakObservers.Where(static x => x.TryGetTarget(out IObserver<FileSystemEventArgs>? y) && y is not null)),
                static (key, value) => new(value.Where(static x => x.TryGetTarget(out IObserver<FileSystemEventArgs>? y) && y is not null))
            );
        }
    }

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Error"/> event,
    /// propagating the error to observers.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The error event data.</param>
    private void OnError(object sender, ErrorEventArgs e)
        => OnError(e.GetException());

    /// <summary>
    /// Propagates an error to all observers.
    /// </summary>
    /// <param name="error">The exception representing the error.</param>
    private void OnError(Exception error)
    {
        IEnumerable<WeakReference<IObserver<FileSystemEventArgs>>> weakObservers = _observers.Values.SelectMany(static x => x);
        foreach (WeakReference<IObserver<FileSystemEventArgs>> weakObserver in weakObservers)
        {
            if (!weakObserver.TryGetTarget(out IObserver<FileSystemEventArgs>? observer) || observer is null)
                continue;

            observer.OnError(error);
        }
    }

    /// <summary>
    /// Notifies all observers that the observation is complete.
    /// </summary>
    private void OnCompleted()
    {
        IEnumerable<WeakReference<IObserver<FileSystemEventArgs>>> weakObservers = _observers.Values.SelectMany(static x => x);
        foreach (WeakReference<IObserver<FileSystemEventArgs>> weakObserver in weakObservers)
        {
            if (!weakObserver.TryGetTarget(out IObserver<FileSystemEventArgs>? observer) || observer is null)
                continue;

            observer.OnCompleted();
        }
    }

    /// <summary>
    /// Releases the resources used by the <see cref="SharedFileObserver"/>
    /// and stops watching for file changes.
    /// </summary>
    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Renamed -= OnChanged;
        _watcher.Created -= OnChanged;
        _watcher.Deleted -= OnChanged;
        _watcher.Error -= OnError;
        _watcher.Dispose();

        OnCompleted();
    }
}
