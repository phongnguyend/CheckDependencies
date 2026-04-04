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
        bool packageLockScan = false)
    {
        var parsedArgs = new ParsedArguments(
            Directories: directories?.ToList() ?? [Directory.GetCurrentDirectory()],
            ReportTypes: reportTypes?.ToList() ?? ["csv", "html", "md"],
            ReportDirectory: reportDirectory,
            PackageLockScan: packageLockScan
        );
        
        await PackageScanner.RunAsync(parsedArgs);
    }
}
