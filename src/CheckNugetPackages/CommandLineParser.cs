namespace CheckNugetPackages;

public record ParsedArguments(List<string> Directories, List<string> ReportTypes, string? ReportDirectory);

public static class CommandLineParser
{
    public static ParsedArguments ParseParameters(string[] args)
    {
        var directories = new List<string>();
        var reportTypes = new List<string>();
        string? reportDirectory = null;
        
        // Default directories if no arguments provided
        var defaultDirectories = new List<string>
        {
            @"D:\Project1\API",
            @"D:\Project2\API",
        };
        
        // Default report types if not specified
        var defaultReportTypes = new List<string> { "csv", "html" };
        
        if (args.Length == 0)
        {
            return new ParsedArguments(defaultDirectories, defaultReportTypes, null);
        }
        
        var i = 0;
        
        // Parse directories (all arguments before first parameter starting with --)
        while (i < args.Length && !args[i].StartsWith("--"))
        {
            directories.Add(args[i]);
            i++;
        }
        
        // If no directories were specified, use defaults
        if (directories.Count == 0)
        {
            directories.AddRange(defaultDirectories);
        }
        
        // Parse parameters
        while (i < args.Length)
        {
            var param = args[i];
            
            if (param == "--report-type")
            {
                i++; // Move to next argument
                // Collect all report type values until next parameter or end
                while (i < args.Length && !args[i].StartsWith("--"))
                {
                    var reportType = args[i].ToLowerInvariant();
                    if (reportType == "csv" || reportType == "html")
                    {
                        if (!reportTypes.Contains(reportType))
                        {
                            reportTypes.Add(reportType);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Invalid report type '{args[i]}'. Valid values are 'csv' and 'html'.");
                    }
                    i++;
                }
            }
            else if (param == "--report-directory")
            {
                i++; // Move to next argument
                if (i < args.Length && !args[i].StartsWith("--"))
                {
                    reportDirectory = args[i];
                    i++;
                }
                else
                {
                    Console.WriteLine("Warning: --report-directory parameter requires a value.");
                }
            }
            else
            {
                Console.WriteLine($"Warning: Unknown parameter '{param}' ignored.");
                i++;
            }
        }
        
        // If no report types were specified, use defaults
        if (reportTypes.Count == 0)
        {
            reportTypes.AddRange(defaultReportTypes);
        }
        
        return new ParsedArguments(directories, reportTypes, reportDirectory);
    }
}