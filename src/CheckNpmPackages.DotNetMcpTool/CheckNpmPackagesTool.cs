using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CheckNpmPackages.DotNetMcpTool;

[McpServerToolType]
public class CheckNpmPackagesTool
{
    [McpServerTool(Name = "CheckNpmPackages"), Description("Scan and generate reports for Npm package dependencies in projects")]
    public static async Task<GeneratedReports> ProcessAsync(
        [Description("One or more directory paths to scan for Npm packages. If not provided, scans current directory.")] 
        string[]? directories = null,
        [Description("Report types to generate (valid values: csv, html, md). If not provided, generates CSV, HTML, and Markdown reports.")] 
        string[]? reportTypes = null,
        [Description("Directory where reports will be saved. If not provided, saves to current directory.")] 
        string? reportDirectory = null,
        [Description("When true, scans package-lock.json for all direct and transitive dependencies instead of only scanning package.json.")] 
        bool includeTransitive = false,
        [Description("When true, generates and writes report files. When false, only returns package data without writing files.")] 
        bool writeReports = true)
    {
        var parsedArgs = new ParsedArguments(
            Directories: directories?.ToList() ?? [Directory.GetCurrentDirectory()],
            ReportTypes: reportTypes?.ToList() ?? ["csv", "html", "md"],
            ReportDirectory: reportDirectory,
            IncludeTransitive: includeTransitive
        );
        
        var packageGroups = await PackageScanner.RunAsync(parsedArgs);
        var generatedReportPaths = writeReports 
            ? ReportsWriter.Write(packageGroups, parsedArgs)
            : new List<string>();
        return new GeneratedReports(packageGroups, generatedReportPaths);
    }

    [McpServerTool(Name = "GetNpmPackageVersion"), Description("Get information about a specific version of an npm package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry> GetVersionAsync(
        [Description("The npm package name (e.g. lodash)")] string packageName,
        [Description("The package version or version range (e.g. 4.17.21 or ^4.17.0)")] string version)
    {
        var key = (packageName, version, (string?)null);
        var results = await NpmPackgeResolver.GetPackagesInfoAsync([key]);
        return results.TryGetValue(key, out var info) ? info.ResolvedVersion : new VersionEntry(null, null, null, null, null, null);
    }

    [McpServerTool(Name = "GetNpmPackageLatestVersion"), Description("Get information about the latest version of an npm package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry> GetLatestVersionAsync(
        [Description("The npm package name (e.g. lodash)")] string packageName)
    {
        var key = (packageName, (string?)null, (string?)null);
        var results = await NpmPackgeResolver.GetPackagesInfoAsync([key]);
        return results.TryGetValue(key, out var info) ? info.LatestVersion : new VersionEntry(null, null, null, null, null, null);
    }

    [McpServerTool(Name = "GetNpmPackageLatestPatchVersion"), Description("Get information about the latest patch version of an npm package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry?> GetLatestPatchVersionAsync(
        [Description("The npm package name (e.g. lodash)")] string packageName,
        [Description("The current package version to find the latest patch for (e.g. 4.17.0)")] string currentVersion)
    {
        var key = (packageName, currentVersion, (string?)null);
        var results = await NpmPackgeResolver.GetPackagesInfoAsync([key]);
        return results.TryGetValue(key, out var info) ? info.LatestPatchVersion : null;
    }

    [McpServerTool(Name = "GetNpmPackageLatestMinorVersion"), Description("Get information about the latest minor version of an npm package, including license, published date, deprecation and vulnerability status")]
    public static async Task<VersionEntry?> GetLatestMinorVersionAsync(
        [Description("The npm package name (e.g. lodash)")] string packageName,
        [Description("The current package version to find the latest minor for (e.g. 4.17.0)")] string currentVersion)
    {
        var key = (packageName, currentVersion, (string?)null);
        var results = await NpmPackgeResolver.GetPackagesInfoAsync([key]);
        return results.TryGetValue(key, out var info) ? info.LatestMinorVersion : null;
    }
}
