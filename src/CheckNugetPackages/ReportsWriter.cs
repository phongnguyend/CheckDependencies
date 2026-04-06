namespace CheckNugetPackages;

public static class ReportsWriter
{
    public static void Write(List<PackageEntry> packageGroups, ParsedArguments arguments)
    {
        var ignoredPackages = new List<string>
        {
            //"System.",
            //"Microsoft."
        };

        // Generate CSV file if requested
        if (arguments.ReportTypes.Contains("csv"))
        {
            var csvPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.csv"
                : Path.Combine(arguments.ReportDirectory, "packages.csv");

            CsvReportGenerator.Generate(csvPath, packageGroups, ignoredPackages, arguments);
        }

        // Generate HTML file if requested
        if (arguments.ReportTypes.Contains("html"))
        {
            var htmlPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.html"
                : Path.Combine(arguments.ReportDirectory, "packages.html");

            HtmlReportGenerator.Generate(htmlPath, "NuGet Packages Report", packageGroups, ignoredPackages, arguments);
        }

        // Generate Markdown file if requested
        if (arguments.ReportTypes.Contains("md"))
        {
            var mdPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.md"
                : Path.Combine(arguments.ReportDirectory, "packages.md");

            MarkdownReportGenerator.Generate(mdPath, "NuGet Packages Report", packageGroups, ignoredPackages, arguments);
        }
    }
}
