using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotAvalonia.Helpers;
using HotAvalonia.IO;
using FileSystemProvider = HotAvalonia.IO.FileSystem;

namespace HotAvalonia;

/// <summary>
/// Provides methods to locate the source code of assemblies
/// containing Avalonia controls.
/// </summary>
public sealed class AvaloniaProjectLocator
{
    /// <summary>
    /// The file system provider used for locating projects
    /// </summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// A cache for storing the project paths associated with assemblies.
    /// </summary>
    private readonly ConditionalWeakTable<Assembly, string> _cache;

    /// <summary>
    /// A collection of hint-providing functions used to infer
    /// the project paths of assemblies.
    /// </summary>
    private readonly ConcurrentBag<Func<Assembly, string?>> _hints;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaProjectLocator"/> class.
    /// </summary>
    public AvaloniaProjectLocator() : this(IO.FileSystem.Current)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaProjectLocator"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system provider used for locating projects.</param>
    public AvaloniaProjectLocator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _cache = new();
        _hints = new();
    }

    /// <summary>
    /// Gets the file system provider used for locating projects
    /// </summary>
    public IFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Registers a hint-providing function that can be used
    /// to infer the project path of an assembly.
    /// </summary>
    /// <param name="hint">The hint-providing function to register.</param>
    public void AddHint(Func<Assembly, string?> hint)
    {
        _ = hint ?? throw new ArgumentNullException(nameof(hint));

        _hints.Add(hint);
    }

    /// <summary>
    /// Registers a hint mapping an assembly name to its project path.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <param name="directoryName">The project path associated with the assembly.</param>
    public void AddHint(string assemblyName, string directoryName)
    {
        _ = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        _ = directoryName ?? throw new ArgumentNullException(nameof(directoryName));

        AddHint(x => x.GetName().Name == assemblyName ? directoryName : null);
    }

    /// <summary>
    /// Registers a hint mapping an assembly to its project path.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="directoryName">The project path associated with the assembly.</param>
    public void AddHint(Assembly assembly, string directoryName)
    {
        _ = assembly ?? throw new ArgumentNullException(nameof(assembly));

#if NETSTANDARD2_0
        // Technically, this is not thread-safe, but who cares.
        string fullDirectoryName = _fileSystem.GetFullPath(directoryName);
        _cache.Remove(assembly);
        _cache.Add(assembly, fullDirectoryName);
#else
        _cache.AddOrUpdate(assembly, _fileSystem.GetFullPath(directoryName));
#endif
    }

    /// <summary>
    /// Registers a hint mapping a control type to its associated XAML file.
    /// </summary>
    /// <param name="type">The control type.</param>
    /// <param name="fileName">The file name of the type's associated XAML file.</param>
    public void AddHint(Type type, string fileName)
    {
        _ = type ?? throw new ArgumentNullException(nameof(type));
        _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
        if (!AvaloniaRuntimeXamlScanner.TryExtractControlUri(type, out Uri? uri))
            return;

        Assembly assembly = type.Assembly;
        string directoryName = UriHelper.ResolveHostPath(uri, fileName);
        AddHint(assembly, directoryName);
    }

    /// <summary>
    /// Attempts to get the cached project path fof the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to get the cached project path for.</param>
    /// <param name="directoryName">
    /// When this method returns, contains the cached project path, if any;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the project path was found in the cache;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool TryGetCachedDirectoryName(Assembly assembly, [NotNullWhen(true)] out string? directoryName)
    {
        if (_cache.TryGetValue(assembly, out directoryName))
            return true;

        directoryName = _hints
            .Select(x => x(assembly))
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .OrderByDescending(_fileSystem.DirectoryExists)
            .Select(_fileSystem.GetFullPath!)
            .FirstOrDefault();

        if (directoryName is not null)
        {
            AddHint(assembly, directoryName);
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="TryGetDirectoryName(Assembly, AvaloniaControlInfo?, out string?)"/>
    public bool TryGetDirectoryName(Assembly assembly, [NotNullWhen(true)] out string? directoryName)
    {
        if (TryGetCachedDirectoryName(assembly, out directoryName))
            return true;

        IEnumerable<AvaloniaControlInfo> controls = AvaloniaRuntimeXamlScanner.FindAvaloniaControls(assembly);
        return TryGetDirectoryName(assembly, controls, out directoryName);
    }

    /// <inheritdoc cref="TryGetDirectoryName(Assembly, AvaloniaControlInfo?, out string?)"/>
    /// <param name="controls">The Avalonia controls located within the assembly.</param>
    internal bool TryGetDirectoryName(Assembly assembly, IEnumerable<AvaloniaControlInfo> controls, [NotNullWhen(true)] out string? directoryName)
        => TryGetDirectoryName(assembly, controls.FirstOrDefault(), out directoryName);

    /// <summary>
    /// Attempts to infer the project path of the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to infer the project path for.</param>
    /// <param name="control">An Avalonia control located within the assembly.</param>
    /// <param name="directoryName">
    /// When this method returns, contains the inferred project path, if any;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the project path was found;
    /// otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetDirectoryName(Assembly assembly, AvaloniaControlInfo? control, [NotNullWhen(true)] out string? directoryName)
    {
        if (TryGetCachedDirectoryName(assembly, out directoryName))
            return true;

        if (control is null)
            return false;

        // We expect the assembly that contains the control to be accessible locally.
        string? controlPath = control.PopulateMethod.GetFilePath(FileSystemProvider.Current);
        controlPath ??= control.PopulateMethod.GetFilePath(_fileSystem);
        if (!_fileSystem.FileExists(controlPath))
            return false;

        directoryName = UriHelper.ResolveHostPath(control.Uri, _fileSystem.GetFullPath(controlPath));
        AddHint(assembly, directoryName);
        return true;
    }

    /// <inheritdoc cref="GetDirectoryName(Assembly, AvaloniaControlInfo?)"/>
    public string GetDirectoryName(Assembly assembly)
    {
        if (!TryGetDirectoryName(assembly, out string? directoryName))
            return ThrowDirectoryNotFoundException(assembly);

        return directoryName;
    }

    /// <inheritdoc cref="GetDirectoryName(Assembly, AvaloniaControlInfo?)"/>
    /// <param name="controls">The Avalonia controls located within the assembly.</param>
    internal string GetDirectoryName(Assembly assembly, IEnumerable<AvaloniaControlInfo> controls)
        => GetDirectoryName(assembly, controls.FirstOrDefault());

    /// <summary>
    /// Infers the project path of the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to infer the project path for.</param>
    /// <param name="control">An Avalonia control located within the assembly.</param>
    /// <returns>The project path of the specified assembly.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the project path cannot be found.</exception>
    internal string GetDirectoryName(Assembly assembly, AvaloniaControlInfo? control)
    {
        if (!TryGetDirectoryName(assembly, control, out string? directoryName))
            return ThrowDirectoryNotFoundException(assembly);

        return directoryName;
    }

    /// <summary>
    /// Throws a <see cref="DirectoryNotFoundException"/> indicating that the project path
    /// of the specified assembly could not be found.
    /// </summary>
    /// <param name="assembly">The assembly that caused the exception.</param>
    /// <returns>This method does not return a value. It always throws an exception.</returns>
    /// <exception cref="DirectoryNotFoundException"/>
    private static string ThrowDirectoryNotFoundException(Assembly assembly)
        => throw new DirectoryNotFoundException($"The project path of the assembly '{assembly.FullName}' could not be found.");
}
