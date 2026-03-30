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
        var packageInfoMap = await NpmLicenseResolver.GetLicensesAsync(
            packages.Select(p => (p.Name, p.Version)).Distinct());
        Console.WriteLine("License information fetched.");

        var packageGroups = packages.GroupBy(x => new { x.Name, x.Version })
            .Select(g =>
            {
                var info = packageInfoMap.TryGetValue((g.Key.Name, g.Key.Version), out var pi) ? pi : null;
                return new
                {
                    g.Key.Name,
                    g.Key.Version,
                    Projects = string.Join(", ", g.Select(x => x.Project)),
                    Url = $"https://www.npmjs.com/package/{g.Key.Name}/v/{NpmLicenseResolver.FormatVersion(g.Key.Version)}",
                    License = info?.License,
                    PublishedDate = info?.PublishedDate
                };
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
                var publishedDateValue = package.PublishedDate ?? "";
                streamWriter.WriteLine($"{package.Name},{package.Version},\"{licenseValue}\",\"{publishedDateValue}\",\"{package.Url}\",\"{package.Projects}\"");
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
            streamWriter.WriteLine("    <title>npm Packages Report</title>");
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
            streamWriter.WriteLine("        .published-date { font-size: 0.9em; font-family: monospace; }");
            streamWriter.WriteLine("        .projects { font-size: 0.9em; color: #666; }");
            streamWriter.WriteLine("    </style>");
            streamWriter.WriteLine("</head>");
            streamWriter.WriteLine("<body>");
            streamWriter.WriteLine("    <h1>npm Packages Report</h1>");
            streamWriter.WriteLine("    <p>Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + "</p>");
            streamWriter.WriteLine("    <table>");
            streamWriter.WriteLine("        <thead>");
            streamWriter.WriteLine("            <tr>");
            streamWriter.WriteLine("                <th>Name</th>");
            streamWriter.WriteLine("                <th>Version</th>");
            streamWriter.WriteLine("                <th>License</th>");
            streamWriter.WriteLine("                <th>Published Date</th>");
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
                var publishedDateHtml = System.Net.WebUtility.HtmlEncode(package.PublishedDate ?? "N/A");

                streamWriter.WriteLine("            <tr>");
                streamWriter.WriteLine($"                <td class=\"package-name\">{System.Net.WebUtility.HtmlEncode(package.Name)}</td>");
                streamWriter.WriteLine($"                <td class=\"version\"><a href=\"{package.Url}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(package.Version ?? "N/A")}</a></td>");
                streamWriter.WriteLine($"                <td class=\"license\">{licenseHtml}</td>");
                streamWriter.WriteLine($"                <td class=\"published-date\">{publishedDateHtml}</td>");
                streamWriter.WriteLine($"                <td class=\"projects\">{System.Net.WebUtility.HtmlEncode(package.Projects)}</td>");
                streamWriter.WriteLine("            </tr>");
            }

            streamWriter.WriteLine("        </tbody>");
            streamWriter.WriteLine("    </table>");
            streamWriter.WriteLine("</body>");
            streamWriter.WriteLine("</html>");
        }

        // Generate Markdown file if requested
        if (arguments.ReportTypes.Contains("md"))
        {
            var mdPath = string.IsNullOrEmpty(arguments.ReportDirectory)
                ? "packages.md"
                : Path.Combine(arguments.ReportDirectory, "packages.md");

            // Ensure the directory exists for the file path
            var mdDirectory = Path.GetDirectoryName(mdPath);
            if (!string.IsNullOrEmpty(mdDirectory))
            {
                Directory.CreateDirectory(mdDirectory);
            }

            using var fileStream = File.Open(mdPath, FileMode.Create);
            using var streamWriter = new StreamWriter(fileStream);
            streamWriter.WriteLine("# npm Packages Report");
            streamWriter.WriteLine();
            streamWriter.WriteLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            streamWriter.WriteLine();
            streamWriter.WriteLine("| Name | Version | License | Published Date | Projects |");
            streamWriter.WriteLine("| ---- | ------- | ------- | -------------- | -------- |");

            foreach (var package in packageGroups)
            {
                if (ignoredPackages.Any(package.Name.StartsWith))
                {
                    continue;
                }

                var licenseMd = FormatLicenseMarkdown(package.License);
                var versionMd = $"[{EscapeMarkdown(package.Version ?? "N/A")}]({package.Url})";
                var publishedDateMd = EscapeMarkdown(package.PublishedDate ?? "N/A");

                streamWriter.WriteLine($"| {EscapeMarkdown(package.Name)} | {versionMd} | {licenseMd} | {publishedDateMd} | {EscapeMarkdown(package.Projects)} |");
            }
        }

        static string FormatLicenseMarkdown(string? license)
        {
            if (string.IsNullOrWhiteSpace(license))
                return "N/A";

            if (license.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                license.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return $"[View License]({license})";
            }

            return EscapeMarkdown(license);
        }

        static string EscapeMarkdown(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("|", "\\|");
        }

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
