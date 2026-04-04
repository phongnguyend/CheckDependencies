using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CheckNpmPackages.DotNetMcpTool;

[McpServerToolType]
public class CheckNpmPackagesTool
{
    [McpServerTool(Name = "CheckNpmPackages"), Description("Scan and generate reports for Npm package dependencies in projects")]
    public static async Task ProcessAsync(
        [Description("One or more directory paths to scan for Npm packages. If not provided, scans current directory.")] 
        string[]? directories = null,
        [Description("Report types to generate (valid values: csv, html, md). If not provided, generates CSV, HTML, and Markdown reports.")] 
        string[]? reportTypes = null,
        [Description("Directory where reports will be saved. If not provided, saves to current directory.")] 
        string? reportDirectory = null,
        [Description("When true, scans package-lock.json for all direct and transitive dependencies instead of only scanning package.json.")] 
        bool includeTransitive = false)
    {
        var parsedArgs = new ParsedArguments(
            Directories: directories?.ToList() ?? [Directory.GetCurrentDirectory()],
            ReportTypes: reportTypes?.ToList() ?? ["csv", "html", "md"],
            ReportDirectory: reportDirectory,
            IncludeTransitive: includeTransitive
        );
        
        await PackageScanner.RunAsync(parsedArgs);
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
}
