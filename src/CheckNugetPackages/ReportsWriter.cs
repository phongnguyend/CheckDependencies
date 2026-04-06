namespace CheckNugetPackages;

public static class ReportsWriter
{
    public static List<string> Write(List<PackageEntry> packageGroups, ParsedArguments arguments)
    {
        var ignoredPackages = new List<string>
        {
            //"System.",
            //"Microsoft."
        };

        var generatedReports = new List<string>();

        // Generate CSV file if requested
        if (arguments.ReportTypes.Contains("csv"))
        {
            var csvPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.csv"
                : Path.Combine(arguments.ReportDirectory, "packages.csv");

            CsvReportGenerator.Generate(csvPath, packageGroups, ignoredPackages, arguments);
            generatedReports.Add(csvPath);
        }

        // Generate HTML file if requested
        if (arguments.ReportTypes.Contains("html"))
        {
            var htmlPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.html"
                : Path.Combine(arguments.ReportDirectory, "packages.html");

            HtmlReportGenerator.Generate(htmlPath, "NuGet Packages Report", packageGroups, ignoredPackages, arguments);
            generatedReports.Add(htmlPath);
        }

        // Generate Markdown file if requested
        if (arguments.ReportTypes.Contains("md"))
        {
            var mdPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.md"
                : Path.Combine(arguments.ReportDirectory, "packages.md");

            MarkdownReportGenerator.Generate(mdPath, "NuGet Packages Report", packageGroups, ignoredPackages, arguments);
            generatedReports.Add(mdPath);
        }

        return generatedReports;
    }
}
