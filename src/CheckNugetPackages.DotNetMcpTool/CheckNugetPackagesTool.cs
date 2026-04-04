using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CheckNugetPackages.DotNetMcpTool;

[McpServerToolType]
public class CheckNugetPackagesTool
{
    [McpServerTool(Name = "CheckNugetPackages"), Description("Scan and generate reports for Nuget package dependencies in projects")]
    public static async Task ProcessAsync(
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
        
        await PackageScanner.RunAsync(parsedArgs);
    }
}
