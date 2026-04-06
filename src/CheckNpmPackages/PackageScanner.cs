using System.Text.Json;

namespace CheckNpmPackages;

public class PackageScanner
{
    public static async Task<List<PackageEntry>> RunAsync(ParsedArguments arguments)
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion, string Project)>();

        foreach (var directory in arguments.Directories)
        {
            var scannedPackages = arguments.IncludeTransitive
                ? ScanPackagesInPackageLockJsonFiles(directory)
                : ScanPackagesInPackageJsonFiles(directory);
            packages.AddRange(scannedPackages);
        }

        // Fetch license information from npm registry
        Console.WriteLine("Fetching license information from npm registry...");
        var packageInfoMap = await NpmPackgeResolver.GetPackagesInfoAsync(
            packages.Select(p => (p.Name, p.Version, p.ResolvedVersion)).Distinct(),
            arguments.IncludePrerelease);
        Console.WriteLine("License information fetched.");

        var packageGroups = packages.GroupBy(x => new { x.Name, x.Version, x.ResolvedVersion })
            .Select(g =>
            {
                var info = packageInfoMap.TryGetValue((g.Key.Name, g.Key.Version, g.Key.ResolvedVersion), out var pi) ? pi : null;
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
                        info?.LatestVersion.Vulnerabilities),
                    info?.LatestPatchVersion,
                    info?.LatestMinorVersion);
            })
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Version).ToList();

        return packageGroups;

        static List<(string Name, string Version, string? ResolvedVersion, string Project)> ScanPackagesInPackageLockJsonFiles(string directory)
        {
            IEnumerable<string> files;

            if (File.Exists(directory))
            {
                var fileName = Path.GetFileName(directory);
                if (!fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.Equals("package-lock.json", StringComparison.OrdinalIgnoreCase))
                {
                    return [];
                }

                files = [fileName];
            }
            else
            {
                var packageJsonFiles = Directory.EnumerateFiles(directory, "package.json", SearchOption.AllDirectories);
                var packageLockJsonFiles = Directory.EnumerateFiles(directory, "package-lock.json", SearchOption.AllDirectories);
                files = packageJsonFiles.Concat(packageLockJsonFiles);
            }

            var packages = new List<(string Name, string Version, string? ResolvedVersion, string Project)>();

            var processedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var normalizedPath = file.Replace("\\", "/");
                if (normalizedPath.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileDirectory = Path.GetDirectoryName(file)!;

                // Skip if this directory has already been processed
                if (!processedDirectories.Add(fileDirectory))
                {
                    continue;
                }

                var projectName = new DirectoryInfo(fileDirectory).Name;
                var lockFilePath = Path.Combine(fileDirectory, "package-lock.json");

                if (File.Exists(lockFilePath))
                {
                    // Scan all packages (direct and transitive) from package-lock.json
                    var lockPackages = ScanAllPackagesFromLockFile(lockFilePath);
                    foreach (var (name, version) in lockPackages)
                    {
                        packages.Add((name, version, version, projectName));
                    }
                }
                else
                {
                    // No package-lock.json, scan package.json as normal
                    var packageJsonPath = Path.Combine(fileDirectory, "package.json");
                    if (File.Exists(packageJsonPath))
                    {
                        var package = GetPackage(packageJsonPath);
                        if (package?.Dependencies != null)
                        {
                            foreach (var node in package.Dependencies)
                            {
                                if (node.Value.StartsWith("file:"))
                                    continue;

                                packages.Add((node.Key, node.Value, null, projectName));
                            }
                        }

                        if (package?.DevDependencies != null)
                        {
                            foreach (var node in package.DevDependencies)
                            {
                                if (node.Value.StartsWith("file:"))
                                    continue;

                                packages.Add((node.Key, node.Value, null, projectName));
                            }
                        }
                    }
                }
            }

            return packages;
        }

        static List<(string Name, string Version)> ScanAllPackagesFromLockFile(string lockFilePath)
        {
            var result = new List<(string Name, string Version)>();

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

                        if (entry.Value.TryGetProperty("version", out var versionProp) &&
                            versionProp.ValueKind == JsonValueKind.String)
                        {
                            var version = versionProp.GetString();
                            if (!string.IsNullOrWhiteSpace(version))
                            {
                                result.Add((packageName, version));
                            }
                        }
                    }
                }

                // lockfileVersion 1 uses "dependencies" with direct package name keys
                if (result.Count == 0 && root.TryGetProperty("dependencies", out var dependencies))
                {
                    ScanLockFileV1Dependencies(dependencies, result);
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Warning: Failed to parse {lockFilePath}");
            }

            return result;
        }

        static void ScanLockFileV1Dependencies(JsonElement dependencies, List<(string Name, string Version)> result)
        {
            foreach (var entry in dependencies.EnumerateObject())
            {
                if (entry.Value.TryGetProperty("version", out var versionProp) &&
                    versionProp.ValueKind == JsonValueKind.String)
                {
                    var version = versionProp.GetString();
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        result.Add((entry.Name, version));
                    }
                }

                // lockfileVersion 1 can have nested "dependencies" for transitive deps
                if (entry.Value.TryGetProperty("dependencies", out var nestedDeps))
                {
                    ScanLockFileV1Dependencies(nestedDeps, result);
                }
            }
        }

        static List<(string Name, string Version, string? ResolvedVersion, string Project)> ScanPackagesInPackageJsonFiles(string directory)
        {
            IEnumerable<string> files;

            if (File.Exists(directory))
            {
                if (!Path.GetFileName(directory).Equals("package.json", StringComparison.OrdinalIgnoreCase))
                    return [];

                files = [directory];
            }
            else
            {
                files = Directory.EnumerateFiles(directory, "package.json", SearchOption.AllDirectories);
            }

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
