using System.Text.Json;

namespace CheckNpmPackages;

public class PackageScanner
{
    public static async Task RunAsync(ParsedArguments arguments)
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion, string Project)>();

        foreach (var directory in arguments.Directories)
        {
            var scannedPackages = ScanPackagesInPackageJsonFiles(directory);
            packages.AddRange(scannedPackages);
        }

        // Fetch license information from npm registry
        Console.WriteLine("Fetching license information from npm registry...");
        var packageInfoMap = await NpmPackgeResolver.GetLicensesAsync(
            packages.Select(p => (p.Name, p.Version, p.ResolvedVersion)).Distinct());
        Console.WriteLine("License information fetched.");

        var packageGroups = packages.GroupBy(x => new { x.Name, x.Version, x.ResolvedVersion })
            .Select(g =>
            {
                var info = packageInfoMap.TryGetValue((g.Key.Name, g.Key.Version, g.Key.ResolvedVersion), out var pi) ? pi : null;
                return new PackageEntry(
                    g.Key.Name,
                    g.Key.Version,
                    info?.ResolvedVersion,
                    string.Join(", ", g.Select(x => x.Project)),
                    $"https://www.npmjs.com/package/{g.Key.Name}/v/{info?.ResolvedVersion}",
                    info?.License,
                    info?.PublishedDate,
                    info?.LatestVersion,
                    info?.LatestVersion != null ? $"https://www.npmjs.com/package/{g.Key.Name}/v/{info.LatestVersion}" : null,
                    info?.LatestLicense,
                    info?.LatestPublishedDate);
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

        static List<(string Name, string Version, string? ResolvedVersion, string Project)> ScanPackagesInPackageJsonFiles(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "package.json", SearchOption.AllDirectories);
            var packages = new List<(string Name, string Version, string? ResolvedVersion, string Project)>();

            foreach (var file in files)
            {
                if (file.Replace("\\", "/").Contains("/node_modules/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var package = GetPackage(file);
                var projectName = new DirectoryInfo(Path.GetDirectoryName(file)!).Name;

                // Try to load package-lock.json from the same directory for resolved versions
                var lockFilePath = Path.Combine(Path.GetDirectoryName(file)!, "package-lock.json");
                var lockedVersions = GetLockedVersions(lockFilePath);

                if (package?.Dependencies != null)
                {
                    foreach (var node in package.Dependencies)
                    {
                        if (node.Value.StartsWith("file:"))
                            continue;

                        lockedVersions.TryGetValue(node.Key, out var resolvedVersion);
                        packages.Add((node.Key, node.Value, resolvedVersion, projectName));
                    }
                }

                if (package?.DevDependencies != null)
                {
                    foreach (var node in package.DevDependencies)
                    {
                        if (node.Value.StartsWith("file:"))
                            continue;

                        lockedVersions.TryGetValue(node.Key, out var resolvedVersion);
                        packages.Add((node.Key, node.Value, resolvedVersion, projectName));
                    }
                }
            }

            return packages;
        }

        static Dictionary<string, string> GetLockedVersions(string lockFilePath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(lockFilePath))
                return result;

            try
            {
                var lockFileContent = File.ReadAllText(lockFilePath);
                using var doc = JsonDocument.Parse(lockFileContent);
                var root = doc.RootElement;

                // lockfileVersion 2 and 3 use "packages" with "node_modules/<name>" keys
                if (root.TryGetProperty("packages", out var packages))
                {
                    foreach (var entry in packages.EnumerateObject())
                    {
                        // Skip the root package (empty key "")
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        // Keys are like "node_modules/lodash" or "node_modules/@scope/name"
                        var lastNodeModules = entry.Name.LastIndexOf("node_modules/", StringComparison.Ordinal);
                        if (lastNodeModules < 0)
                            continue;

                        var packageName = entry.Name[(lastNodeModules + "node_modules/".Length)..];

                        // Only take top-level dependencies (node_modules/<name>, not nested)
                        if (entry.Name.IndexOf("node_modules/", StringComparison.Ordinal) != lastNodeModules)
                            continue;

                        if (entry.Value.TryGetProperty("version", out var versionProp) &&
                            versionProp.ValueKind == JsonValueKind.String)
                        {
                            var version = versionProp.GetString();
                            if (!string.IsNullOrWhiteSpace(version))
                            {
                                result.TryAdd(packageName, version);
                            }
                        }
                    }
                }

                // lockfileVersion 1 uses "dependencies" with direct package name keys
                if (result.Count == 0 && root.TryGetProperty("dependencies", out var dependencies))
                {
                    foreach (var entry in dependencies.EnumerateObject())
                    {
                        if (entry.Value.TryGetProperty("version", out var versionProp) &&
                            versionProp.ValueKind == JsonValueKind.String)
                        {
                            var version = versionProp.GetString();
                            if (!string.IsNullOrWhiteSpace(version))
                            {
                                result.TryAdd(entry.Name, version);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Warning: Failed to parse {lockFilePath}");
            }

            return result;
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
