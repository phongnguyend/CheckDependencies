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
        var licenseMap = await NugetLicenseResolver.GetLicensesAsync(
            packages.Select(p => (p.Name, p.Version)).Distinct());
        Console.WriteLine("License information fetched.");

        var packageGroups = packages.GroupBy(x => new { x.Name, x.Version })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Version,
                Projects = string.Join(", ", g.Select(x => x.Project)),
                Url = $"https://www.nuget.org/packages/{g.Key.Name}/{g.Key.Version}",
                License = licenseMap.TryGetValue((g.Key.Name, g.Key.Version), out var license) ? license : null
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

            // Ensure the directory exists for the file path
            var csvDirectory = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(csvDirectory))
            {
                Directory.CreateDirectory(csvDirectory);
            }

            using var fileStream = File.Open(csvPath, FileMode.Create);
            using var streamWriter = new StreamWriter(fileStream);
            foreach (var package in packageGroups)
            {
                if (ignoredPackages.Any(package.Name.StartsWith))
                {
                    continue;
                }

                var licenseValue = package.License ?? "";
                streamWriter.WriteLine($"{package.Name},{package.Version},\"{licenseValue}\",\"{package.Url}\",\"{package.Projects}\"");
            }
        }

        // Generate HTML file if requested
        if (arguments.ReportTypes.Contains("html"))
        {
            var htmlPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.html"
                : Path.Combine(arguments.ReportDirectory, "packages.html");

            // Ensure the directory exists for the file path
            var htmlDirectory = Path.GetDirectoryName(htmlPath);
            if (!string.IsNullOrEmpty(htmlDirectory))
            {
                Directory.CreateDirectory(htmlDirectory);
            }

            using var fileStream = File.Open(htmlPath, FileMode.Create);
            using var streamWriter = new StreamWriter(fileStream);
            streamWriter.WriteLine("<!DOCTYPE html>");
            streamWriter.WriteLine("<html>");
            streamWriter.WriteLine("<head>");
            streamWriter.WriteLine("    <title>NuGet Packages Report</title>");
            streamWriter.WriteLine("    <style>");
            streamWriter.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            streamWriter.WriteLine("        table { border-collapse: collapse; width: 100%; }");
            streamWriter.WriteLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            streamWriter.WriteLine("        th { background-color: #f2f2f2; font-weight: bold; }");
            streamWriter.WriteLine("        tr:nth-child(even) { background-color: #f9f9f9; }");
            streamWriter.WriteLine("        a { color: #0366d6; text-decoration: none; }");
            streamWriter.WriteLine("        a:hover { text-decoration: underline; }");
            streamWriter.WriteLine("        .package-name { font-weight: bold; }");
            streamWriter.WriteLine("        .version { font-family: monospace; }");
            streamWriter.WriteLine("        .version a { color: #0366d6; font-family: monospace; }");
            streamWriter.WriteLine("        .license { font-size: 0.9em; }");
            streamWriter.WriteLine("        .projects { font-size: 0.9em; color: #666; }");
            streamWriter.WriteLine("    </style>");
            streamWriter.WriteLine("</head>");
            streamWriter.WriteLine("<body>");
            streamWriter.WriteLine("    <h1>NuGet Packages Report</h1>");
            streamWriter.WriteLine("    <p>Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + "</p>");
            streamWriter.WriteLine("    <table>");
            streamWriter.WriteLine("        <thead>");
            streamWriter.WriteLine("            <tr>");
            streamWriter.WriteLine("                <th>Name</th>");
            streamWriter.WriteLine("                <th>Version</th>");
            streamWriter.WriteLine("                <th>License</th>");
            streamWriter.WriteLine("                <th>Projects</th>");
            streamWriter.WriteLine("            </tr>");
            streamWriter.WriteLine("        </thead>");
            streamWriter.WriteLine("        <tbody>");

            foreach (var package in packageGroups)
            {
                if (ignoredPackages.Any(package.Name.StartsWith))
                {
                    continue;
                }

                var licenseHtml = FormatLicenseHtml(package.License);

                streamWriter.WriteLine("            <tr>");
                streamWriter.WriteLine($"                <td class=\"package-name\">{System.Net.WebUtility.HtmlEncode(package.Name)}</td>");
                streamWriter.WriteLine($"                <td class=\"version\"><a href=\"{package.Url}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(package.Version ?? "N/A")}</a></td>");
                streamWriter.WriteLine($"                <td class=\"license\">{licenseHtml}</td>");
                streamWriter.WriteLine($"                <td class=\"projects\">{System.Net.WebUtility.HtmlEncode(package.Projects)}</td>");
                streamWriter.WriteLine("            </tr>");
            }

            streamWriter.WriteLine("        </tbody>");
            streamWriter.WriteLine("    </table>");
            streamWriter.WriteLine("</body>");
            streamWriter.WriteLine("</html>");
        }

        //Console.ReadLine();

        static string FormatLicenseHtml(string? license)
        {
            if (string.IsNullOrWhiteSpace(license))
                return "N/A";

            if (license.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                license.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return $"<a href=\"{System.Net.WebUtility.HtmlEncode(license)}\" target=\"_blank\">View License</a>";
            }

            return System.Net.WebUtility.HtmlEncode(license);
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
