using System.Text.Json;

namespace CheckNpmPackages;

public class PackageScanner
{
    public static async Task RunAsync(ParsedArguments arguments)
    {
        var packages = new List<(string Name, string Version, string Project)>();

        foreach (var directory in arguments.Directories)
        {
            var scannedPackages = ScanPackagesInPackageJsonFiles(directory);
            packages.AddRange(scannedPackages);
        }

        // Fetch license information from npm registry
        Console.WriteLine("Fetching license information from npm registry...");
        var packageInfoMap = await NpmPackgeResolver.GetLicensesAsync(
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
                    $"https://www.npmjs.com/package/{g.Key.Name}/v/{NpmPackgeResolver.FormatVersion(g.Key.Version)}",
                    info?.License,
                    info?.PublishedDate);
            })
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Version).ToList();

        var ignoredPackages = new List<string>
        {
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

            HtmlReportGenerator.Generate(htmlPath, "npm Packages Report", packageGroups, ignoredPackages);
        }

        // Generate Markdown file if requested
        if (arguments.ReportTypes.Contains("md"))
        {
            var mdPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.md"
                : Path.Combine(arguments.ReportDirectory, "packages.md");

            MarkdownReportGenerator.Generate(mdPath, "npm Packages Report", packageGroups, ignoredPackages);
        }

        static List<(string Name, string Version, string Project)> ScanPackagesInPackageJsonFiles(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "package.json", SearchOption.AllDirectories);
            var packages = new List<(string Name, string Version, string Project)>();

            foreach (var file in files)
            {
                if (file.Replace("\\", "/").Contains("/node_modules/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var package = GetPackage(file);
                var projectName = new DirectoryInfo(Path.GetDirectoryName(file)!).Name;

                if (package?.Dependencies != null)
                {
                    foreach (var node in package.Dependencies)
                    {
                        if (node.Value.StartsWith("file:"))
                            continue;

                        packages.Add((node.Key, node.Value, projectName));
                    }
                }

                if (package?.DevDependencies != null)
                {
                    foreach (var node in package.DevDependencies)
                    {
                        if (node.Value.StartsWith("file:"))
                            continue;

                        packages.Add((node.Key, node.Value, projectName));
                    }
                }
            }

            return packages;
        }

        static PackageJson? GetPackage(string file)
        {
            try
            {
                return JsonSerializer.Deserialize<PackageJson>(File.ReadAllText(file), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (Exception)
            {
                Console.WriteLine($"Warning: Failed to parse {file}");
                throw;
            }
        }
    }
}

public class PackageJson
{
    public Dictionary<string, string>? Dependencies { get; set; }

    public Dictionary<string, string>? DevDependencies { get; set; }
}
