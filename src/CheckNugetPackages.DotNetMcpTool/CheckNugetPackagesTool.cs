using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CheckNugetPackages.DotNetMcpTool;

[McpServerToolType]
public class CheckNugetPackagesTool
{
    [McpServerTool(Name = "CheckNugetPackages"), Description("Scan and generate reports for Nuget package dependencies in projects")]
    public static void Process(
        [Description("One or more directory paths to scan for NuGet packages. If not provided, scans current directory.")] 
        string[]? directories = null,
        [Description("Report types to generate (valid values: csv, html). If not provided, generates both CSV and HTML reports.")] 
        string[]? reportTypes = null,
        [Description("Directory where reports will be saved. If not provided, saves to current directory.")] 
        string? reportDirectory = null)
    {
        var parsedArgs = new ParsedArguments(
            Directories: directories?.ToList() ?? [Directory.GetCurrentDirectory()],
            ReportTypes: reportTypes?.ToList() ?? ["csv", "html"],
            ReportDirectory: reportDirectory
        );
        
        PackageScanner.Run(parsedArgs);
    }
}
