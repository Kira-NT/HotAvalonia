using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using HotAvalonia.Net;
using Microsoft.Build.Framework;

namespace HotAvalonia;

/// <summary>
/// Generates a file system server configuration file.
/// </summary>
public sealed class GenerateFileSystemServerConfigTask : MSBuildTask
{
    /// <summary>
    /// Gets or sets the root directory for the file system server.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Gets or sets the fallback root directory to use if <see cref="Root"/> does not refer to an existing directory.
    /// </summary>
    public string? FallbackRoot { get; set; }

    /// <summary>
    /// Gets or sets the secret used for authentication.
    /// If not provided, a random secret may be generated or <see cref="SecretUtf8"/> is used.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the secret used for authentication in UTF-8 format.
    /// If provided, it is converted to a Base64 string when <see cref="Secret"/> is not specified.
    /// </summary>
    public string? SecretUtf8 { get; set; }

    /// <summary>
    /// Gets or sets the network address on which the file system server listens.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the port number on which the file system server listens.
    /// If not specified, a new port is allocated automatically.
    /// </summary>
    public string? Port { get; set; }

    /// <summary>
    /// Gets or sets the path to the certificate used for secure communications.
    /// </summary>
    public string? Certificate { get; set; }

    /// <summary>
    /// Gets or sets the maximum search depth for directory file searches.
    /// </summary>
    public string? MaxSearchDepth { get; set; }

    /// <summary>
    /// Gets or sets the timeout duration in milliseconds before the server shuts down
    /// if no clients have connected during the provided time frame.
    /// </summary>
    public string? Timeout { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server should accept shutdown requests from clients.
    /// </summary>
    public string? AllowShutDownRequests { get; set; }

    /// <summary>
    /// Gets or sets the file path where the generated configuration file will be saved.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = null!;

    /// <inheritdoc/>
    protected override void ExecuteCore()
    {
        FileSystemServerConfig config = new()
        {
            Root = !string.IsNullOrEmpty(Root) && Directory.Exists(Root) ? Path.GetFullPath(Root) : FindSolutionRoot(FallbackRoot),
            Secret = string.IsNullOrEmpty(Secret) ? Convert.ToBase64String(string.IsNullOrEmpty(SecretUtf8) ? GenerateSecret() : Encoding.UTF8.GetBytes(SecretUtf8)) : Secret,
            Address = Address,
            Port = int.TryParse(Port, out int port) && port is > 0 and <= ushort.MaxValue ? port : InterNetwork.GetAvailablePort(ProtocolType.Tcp),
            Certificate = !string.IsNullOrEmpty(Certificate) && File.Exists(Certificate) ? Path.GetFullPath(Certificate) : null,
            MaxSearchDepth = int.TryParse(MaxSearchDepth, out int maxSearchDepth) ? maxSearchDepth : 0,
            Timeout = int.TryParse(Timeout, out int timeout) ? timeout : 0,
            AllowShutDownRequests = bool.TryParse(AllowShutDownRequests, out bool allowShutDownRequests) && allowShutDownRequests,
        };

        config.Save(OutputPath);
    }

    /// <summary>
    /// Generates a random secret as a byte array.
    /// </summary>
    /// <returns>A byte array containing the generated secret.</returns>
    private static byte[] GenerateSecret()
    {
        const int MinByteCount = 32;
        const int MaxByteCount = 64;

        int byteCount = new Random().Next(MinByteCount, MaxByteCount);
        byte[] bytes = new byte[byteCount];
        using RNGCryptoServiceProvider rng = new();
        rng.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// Attempts to locate the root directory of a solution starting from the specified path.
    /// </summary>
    /// <param name="rootCandidate">A path that is expected to be within a solution directory.</param>
    /// <returns>The full path to the solution root directory if one can be determined; otherwise, <c>null</c>.</returns>
    private static string? FindSolutionRoot(string? rootCandidate)
    {
        // There's no reason to continue if the search root itself does not exist.
        if (string.IsNullOrEmpty(rootCandidate) || !Directory.Exists(rootCandidate))
            return null;

        string searchRoot = Path.GetFullPath(rootCandidate);

        // By definition, a directory that contains a solution file
        // (either the modern .slnx or the legacy .sln) is the solution root.
        // So, naturally, this is the first thing we should look for.
        static bool IsSolutionRoot(string dir)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(dir, "*.sln*");
            return files.Any(x => x.EndsWith(".slnx") || x.EndsWith(".sln"));
        }
        if (TryFindDirectory(searchRoot, IsSolutionRoot, out string? root))
            return root;

        // If we didn't find a solution file (honestly, you don't really need one that much
        // nowadays), we can still look for files/directories that serve as "root markers".
        // This approach is a bit more finicky than looking for a literal solution root, but
        // it can still help us find one with a high degree of certainty.
        //
        // Here's what we consider a good "marker":
        // - nuget.config/global.json - commonly used to configure a .NET environment,
        //   need to be placed within a solution root to take effect.
        // - .git - honestly, this is the next best thing to look for if we didn't find
        //   a solution file, as it usually denotes the project in its entirety.
        // - .vscode/.idea - configuration directories for popular editors are usually
        //   placed at a solution root. What's the point of having them otherwise?
        //
        // Now, here are some honorable mentions and why they didn't make it into the list:
        // - Directory.Build.props/Directory.Build.targets - can be placed in any directory
        //   and are usually simply merged with their ancestors.
        // - .editorconfig - same as above.
        // - .github/.gitlab - there's no point in having one of these, if you don't have
        //   a git repository to upload, and we already have a check for .git.
        // - .vs - unlike .vscode, this directory is not user-controlled. It's merely a place
        //   for Visual Studio to store its garbage, and there's nothing to configure there.
        //   Moreover, this directory is usually created automatically whenever a user opens
        //   a directory containing a solution file. I.e., if it exists, we would have already
        //   succeeded by finding the solution file itself.
        static bool HasRootMarker(string dir)
        {
            ReadOnlySpan<string> rootMarkers = [
                "nuget.config", "NuGet.Config", "global.json",
                ".git", ".vscode", ".idea",
            ];
            foreach (string rootMarker in rootMarkers)
            {
                string path = Path.Combine(dir, rootMarker);
                if (File.Exists(path) || Directory.Exists(path))
                    return true;
            }
            return false;
        }
        if (TryFindDirectory(searchRoot, HasRootMarker, out root))
            return root;

        // If we're dealing with a project that doesn't have a solution file and
        // isn't contained within an initialized git repo, we can check whether
        // there are any "sibling" projects nearby. In the .NET world, it's quite
        // common for multiple projects within the same solution to share a common
        // prefix: "Foo", "Foo.Core", "Foo.App", etc., you get the idea.
        // That's the pattern we're looking for. If we detect it, we can safely move
        // one level outward from the current directory, ensuring that hot reload will
        // work for nearby referenced projects as well.
        if (HasSiblingProjects(searchRoot, out root))
            return root;

        // If everything else has failed, simply return the directory as-is.
        return searchRoot;
    }

    /// <summary>
    /// Attempts to find a directory within the ancestral hierarchy of
    /// the specified search root that satisfies the provided predicate.
    /// </summary>
    /// <param name="searchRoot">The directory from which to begin the search.</param>
    /// <param name="predicate">A predicate used to determine whether a directory satisfies the search condition.</param>
    /// <param name="directory">
    /// When this method returns <c>true</c>, contains the first directory for which
    /// <paramref name="predicate"/> returned <c>true</c>; otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if a matching directory is found; otherwise, <c>false</c>.</returns>
    private static bool TryFindDirectory(string? searchRoot, Func<string, bool> predicate, [NotNullWhen(true)] out string? directory)
    {
        string? currentDirectory = searchRoot;
        while (currentDirectory is { Length: > 0 })
        {
            try
            {
                if (predicate(currentDirectory))
                {
                    directory = currentDirectory;
                    return true;
                }
            }
            catch
            {
                // Ignore directories we don't have access to.
            }
            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        directory = null;
        return false;
    }

    /// <summary>
    /// Determines whether a directory has sibling project directories that share a common base name.
    /// </summary>
    /// <param name="projectDirectory">The directory representing a project.</param>
    /// <param name="parentDirectory">
    /// When this method returns <c>true</c>, contains the parent directory that holds the sibling projects.
    /// </param>
    /// <returns><c>true</c> if one or more sibling project directories are found; otherwise, <c>false</c>.</returns>
    private static bool HasSiblingProjects(string projectDirectory, [NotNullWhen(true)] out string? parentDirectory)
    {
        // Use DirectoryInfo instead of Path.* methods, so
        // we don't have to worry about trailing slashes:
        // new DirectoryInfo("/foo/bar/").Parent == "/foo"
        // Path.GetDirectoryName("/foo/bar/") == "/foo/bar"
        DirectoryInfo project = new(projectDirectory);
        DirectoryInfo? parent = project.Parent;
        if (parent is null)
        {
            parentDirectory = null;
            return false;
        }

        parentDirectory = parent.FullName;
        string baseName = project.Name.Split('.')[0];
        string prefix = $"{baseName}.";
        return parent.EnumerateDirectories().Any(x => x.Name != project.Name && (x.Name == baseName || x.Name.StartsWith(prefix)));
    }
}
