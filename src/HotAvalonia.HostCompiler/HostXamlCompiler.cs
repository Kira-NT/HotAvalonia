using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using HotAvalonia.Xaml;

namespace HotAvalonia.HostCompiler;

/// <summary>
/// Options controlling a single host-side XAML compile.
/// </summary>
internal sealed record HostCompileOptions
{
    /// <summary>The source <c>.axaml</c> file to compile.</summary>
    public required string ViewPath { get; init; }

    /// <summary>The pre-link build-output directory whose assemblies carry the device's exact identities.</summary>
    public required string ClosureDirectory { get; init; }

    /// <summary>The exact Avalonia package version the target app uses.</summary>
    public required string AvaloniaVersion { get; init; }

    /// <summary>The target framework of the throwaway compile project.</summary>
    public string TargetFramework { get; init; } = "net10.0";

    /// <summary>File-name glob patterns excluded from the reference closure (BCL + Avalonia).</summary>
    public IReadOnlyList<string> ExcludePatterns { get; init; } = HostXamlCompiler.DefaultExcludePatterns;

    /// <summary>The sidecar output path; defaults to <c>&lt;view&gt;.axaml.hotreload.dll</c> next to the source.</summary>
    public string? OutputPath { get; init; }
}

/// <summary>
/// Compiles one changed Avalonia view into a standalone "populate DLL" using Avalonia's exact build-time
/// XAML compiler (via a throwaway project + <c>dotnet build</c>), so an AOT/iOS device can load and apply
/// it without runtime <c>Reflection.Emit</c>.
/// </summary>
internal static class HostXamlCompiler
{
    /// <summary>BCL + Avalonia are excluded from the closure (the SDK and the Avalonia package supply them).</summary>
    public static readonly IReadOnlyList<string> DefaultExcludePatterns =
    [
        "System.*.dll", "Microsoft.*.dll", "mscorlib.dll", "netstandard.dll", "Mono.*.dll", "Avalonia*.dll",
    ];

    /// <summary>
    /// Compiles the view described by <paramref name="options"/> and returns the produced sidecar path.
    /// </summary>
    /// <exception cref="InvalidOperationException">The inputs are invalid or the compile failed.</exception>
    public static string Compile(HostCompileOptions options)
    {
        if (!File.Exists(options.ViewPath))
            throw new InvalidOperationException($"View not found: {options.ViewPath}");
        if (!Directory.Exists(options.ClosureDirectory))
            throw new InvalidOperationException($"Reference closure directory not found: {options.ClosureDirectory}");

        AssemblyClosure closure = AssemblyClosure.Load(options.ClosureDirectory, options.ExcludePatterns);

        string sourceXaml = File.ReadAllText(options.ViewPath);
        string transformed = AxamlTransform.Transform(sourceXaml, closure, out string viewClassName);

        string viewName = viewClassName.Length > 0
            ? viewClassName[(viewClassName.LastIndexOf('.') + 1)..]
            : Path.GetFileNameWithoutExtension(options.ViewPath);

        // Unique identity per content so the device's repeated Assembly.Load calls never collide.
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(transformed)))[..8];
        string assemblyName = $"{HostCompiledXamlNaming.AssemblyNamePrefix}{viewName}_{hash}";
        string outputPath = options.OutputPath ?? options.ViewPath + HostCompiledXamlNaming.SidecarSuffix;

        string projectDir = Directory.CreateTempSubdirectory("hotavalonia-hostcompile-").FullName;
        try
        {
            string viewFileName = viewName + ".axaml";
            Directory.CreateDirectory(Path.Combine(projectDir, "Views"));
            File.WriteAllText(Path.Combine(projectDir, "Views", viewFileName), transformed);
            File.WriteAllText(Path.Combine(projectDir, "IgnoresAccessChecks.cs"), BuildIgnoresAccessChecks(closure.AssemblyNames));

            string projectPath = Path.Combine(projectDir, "HotReloadView.csproj");
            File.WriteAllText(projectPath, BuildProject(options, assemblyName, viewFileName));

            RunDotnetBuild(projectPath, projectDir);

            string producedDll = Path.Combine(projectDir, "bin", "Debug", options.TargetFramework, assemblyName + ".dll");
            if (!File.Exists(producedDll))
                throw new InvalidOperationException($"Build succeeded but produced no DLL at {producedDll}.");

            // Write-then-rename so a file server never serves a half-written DLL.
            string tempOutput = $"{outputPath}.tmp.{Environment.ProcessId}";
            File.Copy(producedDll, tempOutput, overwrite: true);
            File.Move(tempOutput, outputPath, overwrite: true);
            return outputPath;
        }
        finally
        {
            try
            {
                Directory.Delete(projectDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup of the throwaway project; a leftover temp dir is harmless.
            }
        }
    }

    private static string BuildProject(HostCompileOptions options, string assemblyName, string viewFileName)
    {
        string closureDir = options.ClosureDirectory.Replace('\\', '/').TrimEnd('/');
        string excludes = string.Join(';', options.ExcludePatterns.Select(p => $"{closureDir}/{p}"));

        return $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{options.TargetFramework}</TargetFramework>
                <AssemblyName>{assemblyName}</AssemblyName>
                <Nullable>disable</Nullable>
                <ImplicitUsings>disable</ImplicitUsings>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
                <AvaloniaXamlIlDebuggerLaunch>false</AvaloniaXamlIlDebuggerLaunch>
                <Configuration>Debug</Configuration>
                <NoWarn>$(NoWarn);CS8021;NU1701;MSB3277</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Avalonia" Version="{options.AvaloniaVersion}" />
              </ItemGroup>
              <ItemGroup>
                <Compile Include="IgnoresAccessChecks.cs" />
                <AvaloniaXaml Include="Views/{viewFileName}" />
              </ItemGroup>
              <ItemGroup>
                <Reference Include="{closureDir}/*.dll" Exclude="{excludes}" />
              </ItemGroup>
            </Project>
            """;
    }

    private static string BuildIgnoresAccessChecks(IReadOnlyList<string> assemblyNames)
    {
        StringBuilder builder = new();

        // Assembly-level attributes must precede any type declaration (CS1730), so emit them first; the
        // attribute type defined below is still resolved across the file.
        foreach (string assemblyName in assemblyNames)
            builder.AppendLine($"[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"{assemblyName}\")]");

        builder.AppendLine("namespace System.Runtime.CompilerServices");
        builder.AppendLine("{");
        builder.AppendLine("    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]");
        builder.AppendLine("    internal sealed class IgnoresAccessChecksToAttribute : System.Attribute");
        builder.AppendLine("    {");
        builder.AppendLine("        public IgnoresAccessChecksToAttribute(string assemblyName) { AssemblyName = assemblyName; }");
        builder.AppendLine("        public string AssemblyName { get; }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void RunDotnetBuild(string projectPath, string workingDirectory)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Debug");
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("quiet");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start 'dotnet build'. Is the .NET SDK on PATH?");

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"'dotnet build' failed (exit {process.ExitCode}):\n{standardOutput}\n{standardError}");
    }
}
