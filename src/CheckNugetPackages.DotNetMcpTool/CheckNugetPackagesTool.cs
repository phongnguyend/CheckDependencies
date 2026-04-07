using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CheckNugetPackages.DotNetMcpTool;

[McpServerToolType]
public class CheckNugetPackagesTool
{
    [McpServerTool(Name = "CheckNugetPackages"), Description("Scan and generate reports for Nuget package dependencies in projects")]
    public static async Task<GeneratedReports> ProcessAsync(
        [Description("One or more directory paths to scan for NuGet packages. If not provided, scans current directory.")] 
        string[]? directories = null,
        [Description("Report types to generate (valid values: csv, html, md). If not provided, generates CSV, HTML, and Markdown reports.")] 
        string[]? reportTypes = null,
        [Description("Directory where reports will be saved. If not provided, saves to current directory.")] 
        string? reportDirectory = null,
        [Description("When true, scans project.assets.json for all direct and transitive dependencies instead of only scanning .csproj files.")] 
        bool includeTransitive = false,
        [Description("When true, includes prerelease versions in package analysis. When false, only analyzes stable versions.")]
        bool includePrerelease = false,
        [Description("When true, generates and writes report files. When false, only returns package data without writing files.")] 
        bool writeReports = true)
    {
        var parsedArgs = new ParsedArguments(
            Directories: directories?.ToList() ?? [Directory.GetCurrentDirectory()],
            ReportTypes: reportTypes?.ToList() ?? ["csv", "html", "md"],
            ReportDirectory: reportDirectory,
            IncludeTransitive: includeTransitive,
            IncludePrerelease: includePrerelease
        );
        
        var packageGroups = await PackageScanner.RunAsync(parsedArgs);
        var generatedReportPaths = writeReports 
            ? ReportsWriter.Write(packageGroups, parsedArgs)
            : new List<string>();
        return new GeneratedReports(packageGroups, generatedReportPaths);
    }

    [McpServerTool(Name = "GetNugetPackageVersion"), Description("Get information about a specific version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry> GetVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName,
        [Description("The package version or version range (e.g. 13.0.3 or [1.0.0,2.0.0))")] string version,
        [Description("When true, includes prerelease versions in resolution. When false, only resolves to stable versions.")] 
        bool includePrerelease = false)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, version, includePrerelease);
        return info.ResolvedVersion;
    }

    [McpServerTool(Name = "GetNugetPackageLatestVersion"), Description("Get information about the latest version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry> GetLatestVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName,
        [Description("When true, includes prerelease versions. When false, only returns the latest stable version.")] 
        bool includePrerelease = false)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, null, includePrerelease);
        return info.LatestVersion;
    }

    [McpServerTool(Name = "GetNugetPackageLatestPatchVersion"), Description("Get information about the latest patch version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry?> GetLatestPatchVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName,
        [Description("The current package version to find the latest patch for (e.g. 13.0.0)")] string currentVersion,
        [Description("When true, includes prerelease versions. When false, only considers stable versions.")] 
        bool includePrerelease = false)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, currentVersion, includePrerelease);
        return info.LatestPatchVersion;
    }

    [McpServerTool(Name = "GetNugetPackageLatestMinorVersion"), Description("Get information about the latest minor version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry?> GetLatestMinorVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName,
        [Description("The current package version to find the latest minor for (e.g. 13.0.0)")] string currentVersion,
        [Description("When true, includes prerelease versions. When false, only considers stable versions.")] 
        bool includePrerelease = false)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, currentVersion, includePrerelease);
        return info.LatestMinorVersion;
    }
}
