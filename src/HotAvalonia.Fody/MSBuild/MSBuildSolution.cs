using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HotAvalonia.Fody.MSBuild;

/// <summary>
/// Represents an MSBuild solution file and provides access to the projects it contains.
/// </summary>
public sealed class MSBuildSolution : MSBuildFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildSolution"/> class.
    /// </summary>
    /// <param name="path">The path to the solution file.</param>
    public MSBuildSolution(string path) : base(path)
    {
    }

    /// <summary>
    /// Gets the projects defined in the solution.
    /// </summary>
    public IEnumerable<MSBuildProject> Projects => field ??= Path.GetExtension(FullPath) switch
    {
        ".sln" => ParseSlnProjects(Content, DirectoryName).ToArray(),
        ".slnx" => ParseXmlProjects(Content, DirectoryName).ToArray(),
        _ => []
    };

    /// <summary>
    /// Parses the .sln file content to retrieve the projects it contains.
    /// </summary>
    /// <param name="slnContent">The contents of the solution file.</param>
    /// <param name="baseDirectory">The base directory used to resolve relative project paths.</param>
    /// <returns>
    /// A collection of <see cref="MSBuildProject"/> instances representing the projects in the solution.
    /// </returns>
    private static IEnumerable<MSBuildProject> ParseSlnProjects(string slnContent, string baseDirectory)
        => Regex.Matches(slnContent, "\\\"([^\"]+\\.[^\"]*proj)\\\"")
            .Cast<Match>()
            .Select(x => x.Groups[1].Value)
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => Uri.UnescapeDataString(x.Replace('\\', Path.DirectorySeparatorChar)))
            .Select(x => new MSBuildProject(Path.Combine(baseDirectory, x)));

    /// <summary>
    /// Parses the .slnx file content to retrieve the projects it contains.
    /// </summary>
    /// <param name="xmlContent">The contents of the solution file.</param>
    /// <param name="baseDirectory">The base directory used to resolve relative project paths.</param>
    /// <returns>
    /// A collection of <see cref="MSBuildProject"/> instances representing the projects in the solution.
    /// </returns>
    private static IEnumerable<MSBuildProject> ParseXmlProjects(string xmlContent, string baseDirectory)
    {
        XDocument document = XDocument.Parse(xmlContent);
        return document.Root?.Elements("Project")
            .Concat(document.Root.Elements("Folder").SelectMany(x => x.Elements("Project")))
            .Select(x => x.Attribute("Path")?.Value?.Replace('\\', Path.DirectorySeparatorChar))
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => new MSBuildProject(Path.Combine(baseDirectory, x))) ?? [];
    }

    /// <summary>
    /// Attempts to locate a solution file in the specified directory.
    /// </summary>
    /// <param name="path">The directory path to search for a solution file.</param>
    /// <param name="solution">
    /// When this method returns, contains the first <see cref="MSBuildSolution"/>
    /// found in the directory, if one exists; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a solution file was found; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetFromDirectory(string? path, [NotNullWhen(true)] out MSBuildSolution? solution)
    {
        solution = null;
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return false;

        try
        {
            solution = Directory.EnumerateFiles(path, "*.sln*")
                .Where(x => x.EndsWith(".slnx") || x.EndsWith(".sln"))
                .Select(x => new MSBuildSolution(x)).FirstOrDefault();
        }
        catch { }

        return solution is not null;
    }

    /// <summary>
    /// Attempts to locate a top-level solution file by searching upward
    /// in the directory hierarchy starting from the specified path.
    /// </summary>
    /// <param name="path">The starting directory path for the search.</param>
    /// <param name="solution">
    /// When this method returns, contains the top-level <see cref="MSBuildSolution"/> if found;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a top-level solution file was found; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetTopLevel(string? path, [NotNullWhen(true)] out MSBuildSolution? solution)
    {
        while (path is { Length: > 0 } && Directory.Exists(path))
        {
            if (TryGetFromDirectory(path, out solution))
                return true;

            path = Path.GetDirectoryName(path);
        }

        solution = null;
        return false;
    }
}
