using System.Text.Json;
using System.Xml.Linq;

namespace CheckNugetPackages;

public class PackageScanner
{
    public static async Task<List<PackageEntry>> RunAsync(ParsedArguments arguments)
    {
        var packages = new List<(string Name, string Version, string Project)>();

        foreach (var directory in arguments.Directories)
        {
            var packagesInPackagesConfigureFiles = ScanPackagesInPackagesConfigureFiles(directory);
            var packagesInCsProjectFiles = ScanPackagesInCsProjectFiles(directory, arguments.IncludeTransitive);
            packages.AddRange(packagesInPackagesConfigureFiles);
            packages.AddRange(packagesInCsProjectFiles);
        }

        // Fetch license information from NuGet API
        Console.WriteLine("Fetching license information from NuGet API...");
        var packageInfoMap = await NugetPackageResolver.GetPackagesInfoAsync(
            packages.Select(p => (p.Name, p.Version)).Distinct());
        Console.WriteLine("License information fetched.");

        var packageGroups = packages.GroupBy(x => new { x.Name, x.Version })
            .Select(g =>
            {
                var info = packageInfoMap.TryGetValue((g.Key.Name, g.Key.Version), out var pi) ? pi : null;
                var resolvedVersion = info?.ResolvedVersion.Version;
                var latestVersion = info?.LatestVersion.Version;
                return new PackageEntry(
                    g.Key.Name,
                    g.Key.Version,
                    string.Join(", ", g.Select(x => x.Project)),
                    new VersionEntry(
                        resolvedVersion,
                        info?.ResolvedVersion.Url,
                        info?.ResolvedVersion.License,
                        info?.ResolvedVersion.PublishedDate,
                        info?.ResolvedVersion.Deprecated,
                        info?.ResolvedVersion.Vulnerabilities),
                    new VersionEntry(
                        latestVersion,
                        info?.LatestVersion.Url,
                        info?.LatestVersion.License,
                        info?.LatestVersion.PublishedDate,
                        info?.LatestVersion.Deprecated,
                        info?.LatestVersion.Vulnerabilities));
            })
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Version).ToList();

        return packageGroups;

        static List<(string Name, string Version, string Project)> ScanPackagesInPackagesConfigureFiles(string directory)
        {
            IEnumerable<string> files;

            if (File.Exists(directory))
            {
                if (!Path.GetFileName(directory).Equals("packages.config", StringComparison.OrdinalIgnoreCase))
                    return [];

                files = [directory];
            }
            else
            {
                files = Directory.EnumerateFiles(directory, "packages.config", SearchOption.AllDirectories);
            }

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

        static List<(string Name, string Version, string Project)> ScanPackagesInCsProjectFiles(string directory, bool includeTransitive)
        {
            IEnumerable<string> files;

            if (File.Exists(directory))
            {
                if (!Path.GetExtension(directory).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                    return [];

                files = [directory];
            }
            else
            {
                files = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories);
            }

            var packages = new List<(string Name, string Version, string Project)>();

            foreach (var file in files)
            {
                var projectName = new DirectoryInfo(Path.GetDirectoryName(file)).Name;

                if (includeTransitive)
                {
                    var assetsPath = Path.Combine(Path.GetDirectoryName(file), "obj", "project.assets.json");
                    if (File.Exists(assetsPath))
                    {
                        // Scan all direct and transitive packages from project.assets.json
                        var allPackagesMap = LoadProjectAssetsVersionMap(file);
                        foreach (var kvp in allPackagesMap)
                        {
                            foreach (var version in kvp.Value)
                            {
                                packages.Add((kvp.Key, version, projectName));
                            }
                        }
                        continue; // Skip csproj scan for this project
                    }
                }

                // Scan csproj as normal
                XDocument xdoc = XDocument.Load(file);
                var ItemGroupNodes = xdoc.Descendants("ItemGroup");

                Dictionary<string, List<string>>? assetsVersionMap = null;

                foreach (var ItemGroupNode in ItemGroupNodes)
                {
                    var packageNodes = ItemGroupNode.Descendants("PackageReference");
                    foreach (var node in packageNodes)
                    {
                        var packageName = node.Attribute("Include")?.Value;
                        var packageVersion = node.Attribute("Version")?.Value
                            ?? node.Element("Version")?.Value;

                        if (string.IsNullOrWhiteSpace(packageName))
                            continue;

                        if (string.IsNullOrWhiteSpace(packageVersion))
                        {
                            assetsVersionMap ??= LoadProjectAssetsVersionMap(file);
                            if (assetsVersionMap.TryGetValue(packageName, out var versions))
                            {
                                foreach (var version in versions)
                                {
                                    packages.Add((packageName, version, projectName));
                                }
                            }
                            else
                            {
                                packages.Add((packageName, packageVersion, projectName));
                            }

                            continue;
                        }

                        packages.Add((packageName, packageVersion, projectName));
                    }
                }

            }

            return packages;
        }
    }

    internal static Dictionary<string, List<string>> LoadProjectAssetsVersionMap(string csprojFilePath)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var projectDir = Path.GetDirectoryName(csprojFilePath);
        var assetsPath = Path.Combine(projectDir, "obj", "project.assets.json");

        if (!File.Exists(assetsPath))
            return map;

        var json = File.ReadAllText(assetsPath);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("targets", out var targets))
        {
            foreach (var targetFramework in targets.EnumerateObject())
            {
                foreach (var package in targetFramework.Value.EnumerateObject())
                {
                    var slashIndex = package.Name.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        var name = package.Name[..slashIndex];
                        var version = package.Name[(slashIndex + 1)..];

                        if (!map.TryGetValue(name, out var versions))
                        {
                            versions = [];
                            map[name] = versions;
                        }

                        if (!versions.Contains(version))
                        {
                            versions.Add(version);
                        }
                    }
                }
            }
        }

        return map;
    }
}
