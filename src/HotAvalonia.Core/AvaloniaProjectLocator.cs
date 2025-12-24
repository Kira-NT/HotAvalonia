using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotAvalonia.Helpers;
using HotAvalonia.IO;
using HotAvalonia.Logging;
using HotAvalonia.Xaml;
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
        ArgumentNullException.ThrowIfNull(fileSystem);

        _fileSystem = fileSystem;
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
        ArgumentNullException.ThrowIfNull(hint);

        _hints.Add(hint);
    }

    /// <summary>
    /// Registers a hint mapping an assembly name to its project path.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <param name="directoryName">The project path associated with the assembly.</param>
    public void AddHint(string assemblyName, string directoryName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);
        ArgumentNullException.ThrowIfNull(directoryName);

        AddHint(x => x.GetName().Name == assemblyName ? directoryName : null);
    }

    /// <summary>
    /// Registers a hint mapping an assembly to its project path.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="directoryName">The project path associated with the assembly.</param>
    public void AddHint(Assembly assembly, string directoryName)
    {
        ArgumentNullException.ThrowIfNull(assembly);

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
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(fileName);
        if (!XamlScanner.TryExtractDocumentUri(type, out Uri? uri))
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

    /// <inheritdoc cref="TryGetDirectoryName(Assembly, CompiledXamlDocument?, out string?)"/>
    public bool TryGetDirectoryName(Assembly assembly, [NotNullWhen(true)] out string? directoryName)
    {
        if (TryGetCachedDirectoryName(assembly, out directoryName))
            return true;

        IEnumerable<CompiledXamlDocument> documents = XamlScanner.GetDocuments(assembly);
        return TryGetDirectoryName(assembly, documents, out directoryName);
    }

    /// <inheritdoc cref="TryGetDirectoryName(Assembly, CompiledXamlDocument?, out string?)"/>
    /// <param name="documents">The compiled XAML documents located within the assembly.</param>
    internal bool TryGetDirectoryName(Assembly assembly, IEnumerable<CompiledXamlDocument> documents, [NotNullWhen(true)] out string? directoryName)
        => TryGetDirectoryName(assembly, documents.FirstOrDefault(), out directoryName);

    /// <summary>
    /// Attempts to infer the project path of the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to infer the project path for.</param>
    /// <param name="document">A compiled XAML document located within the assembly.</param>
    /// <param name="directoryName">
    /// When this method returns, contains the inferred project path, if any;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the project path was found;
    /// otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetDirectoryName(Assembly assembly, CompiledXamlDocument? document, [NotNullWhen(true)] out string? directoryName)
    {
        if (TryGetCachedDirectoryName(assembly, out directoryName))
            return true;

        if (document is null)
            return false;

        string? documentPath = GetDocumentPath(document);
        if (!_fileSystem.FileExists(documentPath))
            return false;

        directoryName = UriHelper.ResolveHostPath(document.Uri, _fileSystem.GetFullPath(documentPath));
        AddHint(assembly, directoryName);
        return true;
    }

    /// <inheritdoc cref="GetDocumentPath(CompiledXamlDocument, IFileSystem)"/>
    private string? GetDocumentPath(CompiledXamlDocument document)
    {
        // We expect the assembly that contains the control to be accessible locally.
        string? documentPath = GetDocumentPath(document, FileSystemProvider.Current);
        if (string.IsNullOrEmpty(documentPath) && _fileSystem != FileSystemProvider.Current)
            documentPath = GetDocumentPath(document, _fileSystem);

        return documentPath;
    }

    /// <summary>
    /// Gets the file path of the source code file from which the specified document was compiled.
    /// </summary>
    /// <param name="document">The document for which to get the source file path.</param>
    /// <param name="fileSystem">The file system where the source code is expected to reside.</param>
    /// <returns>
    /// The file path of the source code file from which the specified document was compiled,
    /// or <c>null</c> if it could not be found.
    /// </returns>
    private string? GetDocumentPath(CompiledXamlDocument document, IFileSystem fileSystem)
    {
        try
        {
            return document.PopulateMethod.GetFilePath(fileSystem);
        }
        catch (Exception e)
        {
            Logger.LogDebug(this, "Failed to locate the source code for '{Uri}': {Exception}", document.Uri, e);
            return null;
        }
    }

    /// <inheritdoc cref="GetDirectoryName(Assembly, CompiledXamlDocument?)"/>
    public string GetDirectoryName(Assembly assembly)
    {
        if (!TryGetDirectoryName(assembly, out string? directoryName))
            return ThrowDirectoryNotFoundException(assembly);

        return directoryName;
    }

    /// <inheritdoc cref="GetDirectoryName(Assembly, CompiledXamlDocument?)"/>
    /// <param name="documents">The compiled XAML documents located within the assembly.</param>
    internal string GetDirectoryName(Assembly assembly, IEnumerable<CompiledXamlDocument> documents)
        => GetDirectoryName(assembly, documents.FirstOrDefault());

    /// <summary>
    /// Infers the project path of the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to infer the project path for.</param>
    /// <param name="document">A compiled XAML document located within the assembly.</param>
    /// <returns>The project path of the specified assembly.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the project path cannot be found.</exception>
    internal string GetDirectoryName(Assembly assembly, CompiledXamlDocument? document)
    {
        if (!TryGetDirectoryName(assembly, document, out string? directoryName))
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
