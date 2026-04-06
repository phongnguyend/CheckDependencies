using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CheckNugetPackages.DotNetMcpTool;

[McpServerToolType]
public class CheckNugetPackagesTool
{
    [McpServerTool(Name = "CheckNugetPackages"), Description("Scan and generate reports for Nuget package dependencies in projects")]
    public static async Task<List<PackageEntry>> ProcessAsync(
        [Description("One or more directory paths to scan for NuGet packages. If not provided, scans current directory.")] 
        string[]? directories = null,
        [Description("Report types to generate (valid values: csv, html, md). If not provided, generates CSV, HTML, and Markdown reports.")] 
        string[]? reportTypes = null,
        [Description("Directory where reports will be saved. If not provided, saves to current directory.")] 
        string? reportDirectory = null,
        [Description("When true, scans project.assets.json for all direct and transitive dependencies instead of only scanning .csproj files.")] 
        bool includeTransitive = false)
    {
        var parsedArgs = new ParsedArguments(
            Directories: directories?.ToList() ?? [Directory.GetCurrentDirectory()],
            ReportTypes: reportTypes?.ToList() ?? ["csv", "html", "md"],
            ReportDirectory: reportDirectory,
            IncludeTransitive: includeTransitive
        );
        
        var packageGroups = await PackageScanner.RunAsync(parsedArgs);
        ReportsWriter.Write(packageGroups, parsedArgs);
        return packageGroups;
    }

    [McpServerTool(Name = "GetNugetPackageVersion"), Description("Get information about a specific version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry> GetVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName,
        [Description("The package version or version range (e.g. 13.0.3 or [1.0.0,2.0.0))")] string version)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, version);
        return info.ResolvedVersion;
    }

    [McpServerTool(Name = "GetNugetPackageLatestVersion"), Description("Get information about the latest version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry> GetLatestVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, null);
        return info.LatestVersion;
    }

    [McpServerTool(Name = "GetNugetPackageLatestPatchVersion"), Description("Get information about the latest patch version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry?> GetLatestPatchVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName,
        [Description("The current package version to find the latest patch for (e.g. 13.0.0)")] string currentVersion)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, currentVersion);
        return info.LatestPatchVersion;
    }

    [McpServerTool(Name = "GetNugetPackageLatestMinorVersion"), Description("Get information about the latest minor version of a NuGet package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry?> GetLatestMinorVersionAsync(
        [Description("The NuGet package name (e.g. Newtonsoft.Json)")] string packageName,
        [Description("The current package version to find the latest minor for (e.g. 13.0.0)")] string currentVersion)
    {
        var info = await NugetPackageResolver.GetPackageInfoAsync(packageName, currentVersion);
        return info.LatestMinorVersion;
    }
}
