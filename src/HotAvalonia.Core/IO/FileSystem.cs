using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using HotAvalonia.Helpers;

namespace HotAvalonia.IO;

/// <summary>
/// Provides access to different types of file system implementations.
/// </summary>
public static class FileSystem
{
    /// <summary>
    /// Represents a sentinel timestamp indicating a missing or nonexistent file system entry,
    /// initialized to 1601-01-01 00:00:00 UTC, which corresponds to the Windows file time epoch start.
    /// </summary>
    internal static readonly DateTime s_missingFileSystemEntryTimestampUtc = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// The length of time, in milliseconds, before a synchronous attempt
    /// to connect to a remote file system times out.
    /// </summary>
    private const int RemoteFileSystemConnectionTimeout = 5000;

    /// <summary>
    /// Gets an empty, read-only file system.
    /// </summary>
    public static IFileSystem Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => EmptyFileSystem.Instance;
    }

    /// <summary>
    /// Gets the current file system.
    /// </summary>
    public static IFileSystem Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LocalFileSystem.Instance;
    }

    /// <inheritdoc cref="Cache(IFileSystem, bool)"/>
    public static IFileSystem Cache(this IFileSystem fileSystem)
        => Cache(fileSystem, leaveOpen: false);

    /// <summary>
    /// Wraps the specified <see cref="IFileSystem"/> instance in a caching layer.
    /// </summary>
    /// <param name="fileSystem">The underlying file system to wrap.</param>
    /// <param name="leaveOpen"><c>true</c> to leave the underlying file system open after this object is disposed; otherwise, <c>false</c>.</param>
    /// <returns>
    /// A new <see cref="IFileSystem"/> instance that caches file system operations
    /// performed on the specified <paramref name="fileSystem"/>.
    /// </returns>
    internal static IFileSystem Cache(this IFileSystem fileSystem, bool leaveOpen)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        // There's no need to wrap an existing caching layer with another one,
        // and an empty file system doesn't need caching since it's read-only.
        if (fileSystem is CachingFileSystem or EmptyFileSystem)
            return fileSystem;

        return new CachingFileSystem(fileSystem, leaveOpen);
    }

    /// <summary>
    /// Connects to a remote file system.
    /// </summary>
    /// <param name="endpoint">The remote endpoint to connect to.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <returns>A new instance of <see cref="IFileSystem"/> representing the remote file system.</returns>
    public static IFileSystem Connect(IPEndPoint endpoint, byte[] secret)
    {
        using CancellationTokenSource cancellationTokenSource = new(RemoteFileSystemConnectionTimeout);
        return ConnectAsync(endpoint, secret, cancellationTokenSource.Token).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Attempts to connect to a remote file system.
    /// If the connection fails, returns <paramref name="fallbackFileSystem"/> instead.
    /// </summary>
    /// <param name="endpoint">The remote endpoint to connect to.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="fallbackFileSystem">The fallback file system to use in case of failure.</param>
    /// <returns>
    /// A new instance of <see cref="IFileSystem"/> representing the remote file system
    /// if the connection attempt was successful; otherwise, <paramref name="fallbackFileSystem"/>.
    /// </returns>
    [return: NotNullIfNotNull(nameof(fallbackFileSystem))]
    public static IFileSystem? Connect(IPEndPoint endpoint, byte[] secret, IFileSystem? fallbackFileSystem)
    {
        using CancellationTokenSource cancellationTokenSource = new(RemoteFileSystemConnectionTimeout);
        return ConnectAsync(endpoint, secret, fallbackFileSystem, cancellationTokenSource.Token).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Connects to a remote file system using configuration options inferred from the current assemblies.
    /// </summary>
    /// <returns>A new instance of <see cref="IFileSystem"/> representing the remote file system.</returns>
    [Obsolete("Use 'Connect(IPEndPoint, byte[])' instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IFileSystem Connect()
        => throw new InvalidOperationException("Configuration options for connecting to the remote file system have not been provided.");

    /// <summary>
    /// Attempts to connect to a remote file system using configuration options inferred from the current assemblies.
    /// If the connection fails, returns <paramref name="fallbackFileSystem"/> instead.
    /// </summary>
    /// <param name="fallbackFileSystem">The fallback file system to use in case of failure.</param>
    /// <returns>
    /// A new instance of <see cref="IFileSystem"/> representing the remote file system
    /// if the connection attempt was successful; otherwise, <paramref name="fallbackFileSystem"/>.
    /// </returns>
    [Obsolete("Use 'Connect(IPEndPoint, byte[], IFileSystem?)' instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [return: NotNullIfNotNull(nameof(fallbackFileSystem))]
    public static IFileSystem? Connect(IFileSystem? fallbackFileSystem)
    {
        LoggingHelper.LogError("Unable to determine configuration options for connecting to the remote file system.");
        return fallbackFileSystem;
    }

    // HotAvalonia.Fody appends `IPEndPoint, byte[]` to the list of arguments passed to the stub method.
    // Thus, when using `(IFileSystem?)` we need `(IFileSystem?, IPEndPoint, byte[])` to be available.
    // Technically, I could change that, but appending arguments is so much easier than prepending, because
    // it doesn't involve any stack analysis. So, yeah, this method is only here because I'm lazy.
    /// <inheritdoc cref="Connect(IPEndPoint, byte[], IFileSystem?)"/>
    [Obsolete("Use 'Connect(IPEndPoint, byte[], IFileSystem?)' instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [return: NotNullIfNotNull(nameof(fallbackFileSystem))]
    public static IFileSystem? Connect(IFileSystem? fallbackFileSystem, IPEndPoint endpoint, byte[] secret)
        => Connect(endpoint, secret, fallbackFileSystem);

    /// <summary>
    /// Asynchronously connects to a remote file system.
    /// </summary>
    /// <param name="endpoint">The remote endpoint to connect to.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A new instance of <see cref="IFileSystem"/> representing the remote file system.</returns>
    public static async Task<IFileSystem> ConnectAsync(IPEndPoint endpoint, byte[] secret, CancellationToken cancellationToken = default)
    {
        _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _ = secret ?? throw new ArgumentNullException(nameof(secret));

        return await RemoteFileSystem.ConnectAsync(endpoint, secret, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to asynchronously connect to a remote file system.
    /// If the connection fails, returns <paramref name="fallbackFileSystem"/> instead.
    /// </summary>
    /// <param name="endpoint">The remote endpoint to connect to.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="fallbackFileSystem">The fallback file system to use in case of failure.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A new instance of <see cref="IFileSystem"/> representing the remote file system
    /// if the connection attempt was successful; otherwise, <paramref name="fallbackFileSystem"/>.
    /// </returns>
    [return: NotNullIfNotNull(nameof(fallbackFileSystem))]
    public static async Task<IFileSystem?> ConnectAsync(IPEndPoint endpoint, byte[] secret, IFileSystem? fallbackFileSystem, CancellationToken cancellationToken = default)
    {
        _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _ = secret ?? throw new ArgumentNullException(nameof(secret));

        try
        {
            return await RemoteFileSystem.ConnectAsync(endpoint, secret, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LoggingHelper.LogError("Failed to connect to the remote file system at {Endpoint}: {Exception}", endpoint, e);
            return fallbackFileSystem;
        }
    }
}
