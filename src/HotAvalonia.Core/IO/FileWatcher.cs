using HotAvalonia.Collections;

namespace HotAvalonia.IO;

/// <summary>
/// Monitors the file system events for a specified directory and its subdirectories.
/// </summary>
internal sealed class FileWatcher : IDisposable
{
    /// <summary>
    /// The set of tracked file paths.
    /// </summary>
    private readonly HashSet<string> _files;

    /// <summary>
    /// The cache of filesystem events.
    /// </summary>
    private readonly MemoryCache<FileSystemEventArgs> _eventCache;

    /// <summary>
    /// The object used for locking in thread-safe operations.
    /// </summary>
    private readonly object _lock;

    /// <summary>
    /// The file system associated with this instance.
    /// </summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// The native file system watcher used for monitoring.
    /// </summary>
    private IFileSystemWatcher? _systemWatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileWatcher"/> class.
    /// </summary>
    /// <param name="rootPath">The root directory to be watched.</param>
    /// <param name="fileSystem">The file system where <paramref name="rootPath"/> can be found.</param>
    public FileWatcher(string rootPath, IFileSystem fileSystem)
    {
        // The minimum time difference required to consider a write operation as unique.
        // See: https://en.wikipedia.org/wiki/Mental_chronometry#Measurement_and_mathematical_descriptions
        const double MinWriteTimeDifference = 150;

        _ = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = fileSystem.DirectoryExists(rootPath) ? rootPath : throw new DirectoryNotFoundException(rootPath);

        DirectoryName = rootPath;
        _fileSystem = fileSystem;
        _systemWatcher = CreateFileSystemWatcher(rootPath, fileSystem);
        _files = new(fileSystem.PathComparer);
        _eventCache = new(TimeSpan.FromMilliseconds(MinWriteTimeDifference));
        _lock = new();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileWatcher"/> class.
    /// </summary>
    /// <param name="rootPath">The root directory to be watched.</param>
    /// <param name="fileNames">The initial list of file names to be tracked.</param>
    /// <param name="fileSystem">The file system where <paramref name="rootPath"/> can be found.</param>
    public FileWatcher(string rootPath, IFileSystem fileSystem, IEnumerable<string> fileNames)
        : this(rootPath, fileSystem)
    {
        _ = fileNames ?? throw new ArgumentNullException(nameof(fileNames));

        foreach (string fileName in fileNames)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName));

            string fullFileName = fileSystem.GetFullPath(fileName);
            _files.Add(fullFileName);
        }
    }

    /// <summary>
    /// Occurs when a change in a file is detected.
    /// </summary>
    public event FileSystemEventHandler? Changed;

    /// <summary>
    /// Occurs when a move operation for a file is detected.
    /// </summary>
    public event RenamedEventHandler? Renamed;

    /// <summary>
    /// Occurs when an error is encountered during file monitoring.
    /// </summary>
    public event ErrorEventHandler? Error;

    /// <summary>
    /// The root directory being watched.
    /// </summary>
    public string DirectoryName { get; }

    /// <summary>
    /// The file system associated with this instance.
    /// </summary>
    public IFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// The list of tracked files.
    /// </summary>
    public IEnumerable<string> FileNames
    {
        get
        {
            lock (_lock)
                return _files.ToArray();
        }
    }

    /// <summary>
    /// Starts watching a specified file.
    /// </summary>
    /// <param name="fileName">The name of the file to be watched.</param>
    public void Watch(string fileName)
    {
        _ = fileName ?? throw new ArgumentNullException(nameof(fileName));

        fileName = _fileSystem.GetFullPath(fileName);

        lock (_lock)
            _files.Add(fileName);
    }

    /// <summary>
    /// Stops watching a specified file.
    /// </summary>
    /// <param name="fileName">The name of the file to stop watching.</param>
    public void Unwatch(string fileName)
    {
        _ = fileName ?? throw new ArgumentNullException(nameof(fileName));

        fileName = _fileSystem.GetFullPath(fileName);

        lock (_lock)
            _files.Remove(fileName);
    }

    /// <summary>
    /// Handles the events of file creation and deletion.
    /// Determines if a file move has taken place.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event arguments containing information about the file change.</param>
    private void OnCreatedOrDeleted(object sender, FileSystemEventArgs args)
        => OnFileSystemEvent(args);

    /// <summary>
    /// Handles the event of a file change.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event arguments containing information about the file change.</param>
    private void OnChanged(object sender, FileSystemEventArgs args)
        => OnFileSystemEvent(args, () => Changed?.Invoke(this, args));

    /// <summary>
    /// Handles the event when a file is renamed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">Event arguments containing the old and new name of the file.</param>
    private void OnRenamed(object sender, RenamedEventArgs args)
        => OnFileSystemEvent(args, () => Renamed?.Invoke(this, args), () =>
        {
            _files.Remove(args.OldFullPath);
            _files.Add(args.FullPath);
        });

    /// <summary>
    /// Handles any error that occurs during file watching.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event arguments containing error details.</param>
    private void OnError(object sender, ErrorEventArgs args)
        => Error?.Invoke(this, args);

    /// <summary>
    /// Handles a filesystem event, performing necessary actions based on the event type.
    /// </summary>
    /// <param name="args">The event arguments containing information about the filesystem operation.</param>
    /// <param name="path">The path of the file associated with the event.</param>
    /// <param name="handler">A handler for the event.</param>
    /// <param name="sync">An action to synchronize changes after processing the event.</param>
    private void OnFileSystemEvent(FileSystemEventArgs args, Action? handler = null, Action? sync = null)
    {
        lock (_lock)
        {
            bool isFullPathWatched = IsWatchingFile(args.FullPath);
            bool isOldFullPathWatched = args is RenamedEventArgs renamed && IsWatchingFile(renamed.OldFullPath);
            if (!isFullPathWatched && !isOldFullPathWatched)
            {
                _eventCache.Add(args);
                return;
            }

            if (IsDuplicateEvent(args))
                return;
        }

        if (!TryProcessComplexEvent(args))
            handler?.Invoke();

        lock (_lock)
        {
            _eventCache.Add(args);
            sync?.Invoke();
        }
    }

    /// <summary>
    /// Determines if a file is being watched.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <returns><c>true</c> if the file is being watched; otherwise, <c>false</c>.</returns>
    private bool IsWatchingFile(string fileName)
        => _files.Contains(fileName);

    /// <summary>
    /// Checks if the given filesystem event is a duplicate of any previously cached event.
    /// </summary>
    /// <param name="args">The event arguments containing information about the filesystem operation.</param>
    /// <returns><c>true</c> if the event is a duplicate; otherwise, <c>false</c>.</returns>
    private bool IsDuplicateEvent(FileSystemEventArgs args)
    {
        // Currently, we only care about filtering duplicate change events:
        // - They trigger most of the unnecessary work.
        // - They are the most straightforward ones to detect.
        //
        // Since duplicates of other events don't cause as much harm,
        // let's just skip them for now and return to this matter later,
        // if it becomes a problem.
        if (args.ChangeType is not WatcherChangeTypes.Changed)
            return false;

        WatcherChangeTypes type = args.ChangeType;
        string path = args.FullPath;
        IEqualityComparer<string> fileNameComparer = _fileSystem.PathComparer;

        return _eventCache.Any(x => x.ChangeType == type && fileNameComparer.Equals(x.FullPath, path));
    }

    /// <summary>
    /// Tries to process complex filesystem events that combine multiple atomic operations.
    /// </summary>
    /// <param name="args">The event arguments containing information about the filesystem operation.</param>
    /// <returns><c>true</c> if a complex event (change or move operation) was successfully processed; otherwise, <c>false</c>.</returns>
    private bool TryProcessComplexEvent(FileSystemEventArgs args)
        => TryProcessComplexChange_NTFS(args)
        || TryProcessComplexChange_ReFS(args)
        || TryProcessComplexMove(args);

    /// <summary>
    /// Tries to process a complex move operation that involves copying a file to a new destination and then deleting the original.
    /// </summary>
    /// <remarks>
    /// Such operation involves the following steps:
    /// <list type="number">
    /// <item>
    /// <description>A file is created at a new location, effectively copying the original file (e.g., `Source.axaml` -> `Target.axaml`).</description>
    /// </item>
    /// <item>
    /// <description>The original file is deleted (e.g., `Source.axaml` is deleted).</description>
    /// </item>
    /// </list>
    /// Instead of emitting a separate 'created' event for the new location, and a 'deleted' event for the original location,
    /// this method consolidates them and emits a single 'moved' event.
    /// </remarks>
    /// <param name="args">The event arguments containing information about the filesystem operation.</param>
    /// <returns><c>true</c> if a complex move operation was successfully processed; otherwise, <c>false</c>.</returns>
    private bool TryProcessComplexMove(FileSystemEventArgs args)
    {
        if (args.ChangeType is not (WatcherChangeTypes.Created or WatcherChangeTypes.Deleted))
            return false;

        WatcherChangeTypes oppositeChangeType = args.ChangeType is WatcherChangeTypes.Created ? WatcherChangeTypes.Deleted : WatcherChangeTypes.Created;
        string fileName = _fileSystem.GetFileName(args.FullPath);
        StringComparer fileNameComparer = _fileSystem.PathComparer;
        string? newFullPath = null;
        string? oldFullPath = null;

        lock (_lock)
        {
            FileSystemEventArgs? oppositeEvent = _eventCache.FirstOrDefault(x => x.ChangeType == oppositeChangeType && fileNameComparer.Equals(x.FullPath, fileName));
            if (oppositeEvent is null)
                return false;

            (newFullPath, oldFullPath) = args.ChangeType is WatcherChangeTypes.Created
                ? (args.FullPath, oppositeEvent.FullPath)
                : (oppositeEvent.FullPath, args.FullPath);
        }

        OnRenamed(this, _fileSystem.CreateFileSystemEventArgs(WatcherChangeTypes.Renamed, newFullPath, oldFullPath));
        return true;
    }

    /// <summary>
    /// Tries to process a complex file modification operation that is commonly performed by some IDEs, such as Visual Studio.
    /// </summary>
    /// <remarks>
    /// Such operation involves several steps on NTFS drives:
    /// <list type="number">
    /// <item>
    /// <description>A copy of the original file is created (e.g., `mgwudjxu.mzo-`). This file will temporarily store applied changes.</description>
    /// </item>
    /// <item>
    /// <description>Changes are applied to the copy.</description>
    /// </item>
    /// <item>
    /// <description>The original file is given a temporary and randomly assigned filename (e.g., `MainWindow.axaml-RF44d4e140.TMP`).</description>
    /// </item>
    /// <item>
    /// <description>The edited copy is moved back to the original file's location (`mgwudjxu.mzo-` -> `MainWindow.axaml`).</description>
    /// </item>
    /// <item>
    /// <description>If no errors occurred during the process, the original file is deleted (`MainWindow.axaml-RF44d4e140.TMP`).</description>
    /// </item>
    /// </list>
    /// In contrast to responding to each individual event, this method cumulatively processes them and emits a single "changed" event.
    /// </remarks>
    /// <param name="args">The event arguments containing information about the filesystem operation.</param>
    /// <returns><c>true</c> if a complex change operation was successfully processed; otherwise, <c>false</c>.</returns>
    private bool TryProcessComplexChange_NTFS(FileSystemEventArgs args)
    {
        if (args.ChangeType is not WatcherChangeTypes.Deleted)
            return false;

        string path = args.FullPath;
        StringComparer fileNameComparer = _fileSystem.PathComparer;
        string? previousPath;
        lock (_lock)
        {
            previousPath = _eventCache
                .OfType<RenamedEventArgs>()
                .FirstOrDefault(x => fileNameComparer.Equals(x.FullPath, path))?.OldFullPath;
        }

        if (!_fileSystem.FileExists(previousPath))
            return false;

        lock (_lock)
        {
            _files.Remove(path);
            _files.Add(previousPath);
        }

        Renamed?.Invoke(this, _fileSystem.CreateFileSystemEventArgs(WatcherChangeTypes.Renamed, previousPath, path));
        Changed?.Invoke(this, _fileSystem.CreateFileSystemEventArgs(WatcherChangeTypes.Changed, previousPath));
        return true;
    }

    /// <summary>
    /// Tries to process a complex file modification operation that is commonly performed by some IDEs, such as Visual Studio.
    /// </summary>
    /// <remarks>
    /// Such operation involves several steps on ReFS drives:
    /// <list type="number">
    /// <item>
    /// <description>An edited copy of the original file is created (e.g., `mgwudjxu.mzo-`).</description>
    /// </item>
    /// <item>
    /// <description>The original file is deleted and its contents are written to a temporary file (e.g., `MainWindow.axaml-RF44d4e140.TMP`).</description>
    /// </item>
    /// <item>
    /// <description>The edited copy is moved back to the original file's location (`mgwudjxu.mzo-` -> `MainWindow.axaml`).</description>
    /// </item>
    /// <item>
    /// <description>If no errors occurred during the process, the original file is deleted (`MainWindow.axaml-RF44d4e140.TMP`).</description>
    /// </item>
    /// </list>
    /// In contrast to responding to each individual event, this method cumulatively processes them and emits a single "changed" event.
    /// </remarks>
    /// <param name="args">The event arguments containing information about the filesystem operation.</param>
    /// <returns><c>true</c> if a complex change operation was successfully processed; otherwise, <c>false</c>.</returns>
    private bool TryProcessComplexChange_ReFS(FileSystemEventArgs args)
    {
        if (args is not RenamedEventArgs renamedArgs)
            return false;

        // We only want to catch an event when an untracked file
        // takes place of the one we're actually watching.
        if (!IsWatchingFile(renamedArgs.FullPath) || IsWatchingFile(renamedArgs.OldFullPath))
            return false;

        string path = args.FullPath;
        StringComparer fileNameComparer = _fileSystem.PathComparer;
        bool wasDeleted;
        lock (_lock)
        {
            wasDeleted = _eventCache
                .Any(x => x.ChangeType is WatcherChangeTypes.Deleted
                    && fileNameComparer.Equals(x.FullPath, path));
        }

        if (!wasDeleted || !_fileSystem.FileExists(path))
            return false;

        // `FileWatcher` currently does not propagate `Deleted` events to its subscribers.
        // However, if this ever changes, we will need to create a synthetic `Created` event here as well.
        Changed?.Invoke(this, _fileSystem.CreateFileSystemEventArgs(WatcherChangeTypes.Changed, path));
        return true;
    }

    /// <summary>
    /// Disposes resources used by this file watcher.
    /// </summary>
    public void Dispose()
    {
        DisposeFileSystemWatcher(_systemWatcher);
        _systemWatcher = null;
    }

    /// <summary>
    /// Creates and configures a new <see cref="IFileSystemWatcher"/> for the specified root path.
    /// </summary>
    /// <param name="rootPath">The root directory to watch.</param>
    /// <param name="fileSystem">The file system where <paramref name="rootPath"/> can be found.</param>
    /// <returns>The created and configured <see cref="IFileSystemWatcher"/>.</returns>
    private IFileSystemWatcher CreateFileSystemWatcher(string rootPath, IFileSystem fileSystem)
    {
        IFileSystemWatcher watcher = fileSystem.CreateFileSystemWatcher();
        watcher.Path = rootPath;
        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.LastWrite
                | NotifyFilters.DirectoryName
                | NotifyFilters.FileName;
        watcher.Created += OnCreatedOrDeleted;
        watcher.Deleted += OnCreatedOrDeleted;
        watcher.Changed += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    /// <summary>
    /// Disposes a given <see cref="IFileSystemWatcher"/> and detaches all its event handlers.
    /// </summary>
    /// <param name="watcher">The <see cref="IFileSystemWatcher"/> to dispose.</param>
    private void DisposeFileSystemWatcher(IFileSystemWatcher? watcher)
    {
        if (watcher is null)
            return;

        watcher.EnableRaisingEvents = false;
        watcher.Created -= OnCreatedOrDeleted;
        watcher.Deleted -= OnCreatedOrDeleted;
        watcher.Changed -= OnChanged;
        watcher.Renamed -= OnRenamed;
        watcher.Error -= OnError;
        watcher.Dispose();
    }
}
