using System.Xml.Linq;

namespace CheckNugetPackages;

public class PackageScanner
{
    public static async Task RunAsync(ParsedArguments arguments)
    {
        var packages = new List<(string Name, string Version, string Project)>();

        foreach (var directory in arguments.Directories)
        {
            var packagesInPackagesConfigureFiles = ScanPackagesInPackagesConfigureFiles(directory);
            var packagesInCsProjectFiles = ScanPackagesInCsProjectFiles(directory);
            packages.AddRange(packagesInPackagesConfigureFiles);
            packages.AddRange(packagesInCsProjectFiles);
        }

        // Fetch license information from NuGet API
        Console.WriteLine("Fetching license information from NuGet API...");
        var packageInfoMap = await NugetPackageResolver.GetLicensesAsync(
            packages.Select(p => (p.Name, p.Version)).Distinct());
        Console.WriteLine("License information fetched.");

        var packageGroups = packages.GroupBy(x => new { x.Name, x.Version })
            .Select(g =>
            {
                var info = packageInfoMap.TryGetValue((g.Key.Name, g.Key.Version), out var pi) ? pi : null;
                return new PackageEntry(
                    g.Key.Name,
                    g.Key.Version,
                    string.Join(", ", g.Select(x => x.Project)),
                    $"https://www.nuget.org/packages/{g.Key.Name}/{g.Key.Version}",
                    info?.License,
                    info?.PublishedDate);
            })
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Version).ToList();

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

            CsvReportGenerator.Generate(csvPath, packageGroups, ignoredPackages);
        }

        // Generate HTML file if requested
        if (arguments.ReportTypes.Contains("html"))
        {
            var htmlPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.html"
                : Path.Combine(arguments.ReportDirectory, "packages.html");

            HtmlReportGenerator.Generate(htmlPath, "NuGet Packages Report", packageGroups, ignoredPackages);
        }

        // Generate Markdown file if requested
        if (arguments.ReportTypes.Contains("md"))
        {
            var mdPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.md"
                : Path.Combine(arguments.ReportDirectory, "packages.md");

            MarkdownReportGenerator.Generate(mdPath, "NuGet Packages Report", packageGroups, ignoredPackages);
        }

        static List<(string Name, string Version, string Project)> ScanPackagesInPackagesConfigureFiles(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "packages.config", SearchOption.AllDirectories);
            var packages = new List<(string Name, string Version, string Project)>();

            foreach (var file in files)
            {
                var projectName = new DirectoryInfo(Path.GetDirectoryName(file)).Name;
                XDocument xdoc = XDocument.Load(file);
                var packagesNode = xdoc.Descendants("packages").First();
                var packageNodes = packagesNode.Descendants("package");
                foreach (var node in packageNodes)
                {
                    var packageName = node.Attribute("id")?.Value;
                    var packageVersion = node.Attribute("version")?.Value;

                    packages.Add((packageName, packageVersion, projectName));
                }
            }

            return packages;
        }

        static List<(string Name, string Version, string Project)> ScanPackagesInCsProjectFiles(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories);
            var packages = new List<(string Name, string Version, string Project)>();

            foreach (var file in files)
            {
                var projectName = new DirectoryInfo(Path.GetDirectoryName(file)).Name;
                XDocument xdoc = XDocument.Load(file);
                var ItemGroupNodes = xdoc.Descendants("ItemGroup");
                foreach (var ItemGroupNode in ItemGroupNodes)
                {
                    var packageNodes = ItemGroupNode.Descendants("PackageReference");
                    foreach (var node in packageNodes)
                    {
                        var packageName = node.Attribute("Include")?.Value;
                        var packageVersion = node.Attribute("Version")?.Value;

                        if (string.IsNullOrWhiteSpace(packageName))
                            continue;

                        packages.Add((packageName, packageVersion, projectName));
                    }
                }

            }

            return packages;
        }
    }
}
