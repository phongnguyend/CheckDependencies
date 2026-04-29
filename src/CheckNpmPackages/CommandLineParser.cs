namespace CheckNpmPackages;

public record ParsedArguments(
    List<string> Directories,
    List<string> ReportTypes,
    string? ReportDirectory,
    bool IncludeTransitive = false,
    bool CheckLatestPatch = false,
    bool CheckLatestMinor = false,
    bool CheckLatest = false,
    bool IncludePrerelease = false);

public static class CommandLineParser
{
    public static ParsedArguments ParseParameters(string[] args)
    {
        var directories = new List<string>();
        var reportTypes = new List<string>();
        string? reportDirectory = null;
        bool includeTransitive = false;
        bool checkLatestPatch = false;
        bool checkLatestMinor = false;
        bool checkLatest = false;
        bool includePrerelease = false;

        // Default directories if no arguments provided
        var defaultDirectories = new List<string>
        {
            Directory.GetCurrentDirectory()
        };

        // Default report types if not specified
        var defaultReportTypes = new List<string> { "csv", "html", "md" };

        if (args.Length == 0)
        {
            return new ParsedArguments(defaultDirectories, defaultReportTypes, null);
        }

        var i = 0;

        // Parse directories (all arguments before first parameter starting with --)
        while (i < args.Length && !args[i].StartsWith("--"))
        {
            directories.Add(RemoveQuotes(args[i]));
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
                    var reportType = RemoveQuotes(args[i]).ToLowerInvariant();
                    if (reportType == "csv" || reportType == "html" || reportType == "md")
                    {
                        if (!reportTypes.Contains(reportType))
                        {
                            reportTypes.Add(reportType);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Invalid report type '{args[i]}'. Valid values are 'csv', 'html', and 'md'.");
                    }
                    i++;
                }
            }
            else if (param == "--report-directory")
            {
                i++; // Move to next argument
                if (i < args.Length && !args[i].StartsWith("--"))
                {
                    reportDirectory = RemoveQuotes(args[i]);
                    i++;
                }
                else
                {
                    Console.WriteLine("Warning: --report-directory parameter requires a value.");
                }
            }
            else if (param == "--include-transitive")
            {
                includeTransitive = true;
                i++;
            }
            else if (param == "--check-latest-patch")
            {
                checkLatestPatch = true;
                i++;
            }
            else if (param == "--check-latest-minor")
            {
                checkLatestMinor = true;
                i++;
            }
            else if (param == "--check-latest")
            {
                checkLatest = true;
                i++;
            }
            else if (param == "--include-prerelease")
            {
                includePrerelease = true;
                i++;
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

        return new ParsedArguments(directories, reportTypes, reportDirectory, includeTransitive, checkLatestPatch, checkLatestMinor, checkLatest, includePrerelease);
    }

    private static string RemoveQuotes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
            (value.StartsWith("'") && value.EndsWith("'")))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }
}
