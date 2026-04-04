using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CheckNpmPackages;

public record PackageInfo(VersionEntry ResolvedVersion, VersionEntry LatestVersion);

public static class NpmPackgeResolver
{
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string RegistryBaseUrl = "https://registry.npmjs.org";
    private const string AuditBaseUrl = "https://registry.npmjs.org/-/npm/v1/security/advisories/bulk";

    private static readonly ConcurrentDictionary<string, Task<JsonElement?>> PackageDocCache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<Dictionary<(string Name, string Version), PackageInfo>> GetLicensesAsync(
        IEnumerable<(string Name, string Version)> packages)
    {
        var result = await GetLicensesAsync(packages.Select(p => (p.Name, p.Version, (string?)null)));
        return result.ToDictionary(kvp => (kvp.Key.Name, kvp.Key.Version), kvp => kvp.Value);
    }

    public static async Task<Dictionary<(string Name, string Version, string? ResolvedVersion), PackageInfo>> GetLicensesAsync(
        IEnumerable<(string Name, string Version, string? ResolvedVersion)> packages)
    {
        var distinct = packages.Distinct().ToList();
        var results = new Dictionary<(string Name, string Version, string? ResolvedVersion), PackageInfo>();

        if (distinct.Count == 0)
            return results;

        // Use SemaphoreSlim to limit concurrent requests
        using var semaphore = new SemaphoreSlim(10);
        var tasks = distinct.Select(async package =>
        {
            await semaphore.WaitAsync();
            try
            {
                var info = await GetPackageInfoAsync(package.Name, package.Version, package.ResolvedVersion);
                lock (results)
                {
                    results[(package.Name, package.Version, package.ResolvedVersion)] = info;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Fetch vulnerabilities in bulk to avoid many requests
        var bulkVulnMap = await FetchBulkVulnerabilitiesAsync(results);
        if (bulkVulnMap != null)
        {
            foreach (var key in results.Keys.ToList())
            {
                var info = results[key];
                string? vulnerabilities = null;
                string? latestVulnerabilities = null;

                if (!string.IsNullOrWhiteSpace(info.ResolvedVersion.Version) &&
                    bulkVulnMap.TryGetValue((key.Name, info.ResolvedVersion.Version), out var rv))
                    vulnerabilities = rv;

                if (!string.IsNullOrWhiteSpace(info.LatestVersion.Version) &&
                    bulkVulnMap.TryGetValue((key.Name, info.LatestVersion.Version), out var lv))
                    latestVulnerabilities = lv;

                if (vulnerabilities != null || latestVulnerabilities != null)
                {
                    results[key] = info with
                    {
                        ResolvedVersion = info.ResolvedVersion with { Vulnerabilities = vulnerabilities ?? info.ResolvedVersion.Vulnerabilities },
                        LatestVersion = info.LatestVersion with { Vulnerabilities = latestVulnerabilities ?? info.LatestVersion.Vulnerabilities }
                    };
                }
            }
        }

        return results;
    }

    public static async Task<PackageInfo> GetPackageInfoAsync(string packageName, string? version, string? resolvedVersionHint = null)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return new PackageInfo(new VersionEntry(null, null, null, null, null, null), new VersionEntry(null, null, null, null, null, null));

        try
        {
            var doc = await GetPackageDocAsync(packageName);
            if (doc == null)
                return new PackageInfo(new VersionEntry(null, null, null, null, null, null), new VersionEntry(null, null, null, null, null, null));

            var docValue = doc.Value;

            // Determine the latest version from dist-tags
            string? latestVersion = null;
            if (docValue.TryGetProperty("dist-tags", out var distTags) &&
                distTags.TryGetProperty("latest", out var latestProp) &&
                latestProp.ValueKind == JsonValueKind.String)
            {
                latestVersion = latestProp.GetString();
            }

            // Use the resolved version hint from package-lock.json if available
            string? resolvedVersion = null;
            if (!string.IsNullOrWhiteSpace(resolvedVersionHint))
            {
                resolvedVersion = resolvedVersionHint;
            }
            else
            {
                // Collect all available versions
                var availableVersions = new List<SemVer>();
                if (docValue.TryGetProperty("versions", out var versionsProp))
                {
                    foreach (var prop in versionsProp.EnumerateObject())
                    {
                        if (SemVer.TryParse(prop.Name, out var sv))
                        {
                            availableVersions.Add(sv);
                        }
                    }
                }

                // Resolve the version range to an actual version like npm install does
                resolvedVersion = ResolveVersion(version, availableVersions, distTags);
            }

            if (string.IsNullOrWhiteSpace(resolvedVersion))
            {
                resolvedVersion = latestVersion;
            }

            // Extract license and deprecated from the specific version
            string? license = null;
            string? deprecated = null;
            if (!string.IsNullOrWhiteSpace(resolvedVersion) &&
                docValue.TryGetProperty("versions", out var versionsForResolved) &&
                versionsForResolved.TryGetProperty(resolvedVersion, out var versionDoc))
            {
                if (versionDoc.TryGetProperty("license", out var licenseProp) && licenseProp.ValueKind == JsonValueKind.String)
                {
                    license = licenseProp.GetString();
                }

                deprecated = ExtractDeprecated(versionDoc);
            }

            // Extract published date from the "time" object
            string? publishedDate = null;
            if (!string.IsNullOrWhiteSpace(resolvedVersion) &&
                docValue.TryGetProperty("time", out var timeProp) &&
                timeProp.TryGetProperty(resolvedVersion, out var versionTimeProp) &&
                versionTimeProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(versionTimeProp.GetString(), out var dto))
                {
                    publishedDate = dto.ToString("yyyy-MM-dd");
                }
            }

            // Extract latest version license, deprecated, and published date
            string? latestLicense = null;
            string? latestPublishedDate = null;
            string? latestDeprecated = null;
            if (!string.IsNullOrWhiteSpace(latestVersion) &&
                docValue.TryGetProperty("versions", out var versionsForLatest) &&
                versionsForLatest.TryGetProperty(latestVersion, out var latestDoc))
            {
                if (latestDoc.TryGetProperty("license", out var latestLicenseProp) && latestLicenseProp.ValueKind == JsonValueKind.String)
                {
                    latestLicense = latestLicenseProp.GetString();
                }

                latestDeprecated = ExtractDeprecated(latestDoc);
            }

            if (!string.IsNullOrWhiteSpace(latestVersion) &&
                docValue.TryGetProperty("time", out var timeForLatest) &&
                timeForLatest.TryGetProperty(latestVersion, out var latestTimeProp) &&
                latestTimeProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(latestTimeProp.GetString(), out var dto))
                {
                    latestPublishedDate = dto.ToString("yyyy-MM-dd");
                }
            }

            return new PackageInfo(
                new VersionEntry(
                    !string.IsNullOrEmpty(resolvedVersion) ? resolvedVersion : null,
                    null,
                    !string.IsNullOrEmpty(license) ? license : null,
                    publishedDate,
                    deprecated,
                    null),
                new VersionEntry(
                    latestVersion,
                    null,
                    !string.IsNullOrEmpty(latestLicense) ? latestLicense : null,
                    latestPublishedDate,
                    latestDeprecated,
                    null));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return new PackageInfo(new VersionEntry(null, null, null, null, null, null), new VersionEntry(null, null, null, null, null, null));
    }

    private static string? ExtractDeprecated(JsonElement versionDoc)
    {
        if (versionDoc.TryGetProperty("deprecated", out var depProp))
        {
            if (depProp.ValueKind == JsonValueKind.String)
            {
                var value = depProp.GetString();
                return !string.IsNullOrWhiteSpace(value) ? value : null;
            }
            if (depProp.ValueKind == JsonValueKind.True)
            {
                return "Deprecated";
            }
        }

        return null;
    }

    /// <summary>
    /// Fetches vulnerabilities for all packages in a single bulk request to reduce HTTP calls.
    /// Returns a map of (packageName, version) -> vulnerability description string.
    /// </summary>
    private static async Task<Dictionary<(string Name, string Version), string>?> FetchBulkVulnerabilitiesAsync(
        Dictionary<(string Name, string Version, string? ResolvedVersion), PackageInfo> results)
    {
        try
        {
            // Build a payload mapping each package name to all its distinct versions
            var payload = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (key, info) in results)
            {
                var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(info.ResolvedVersion.Version))
                    versions.Add(info.ResolvedVersion.Version);
                if (!string.IsNullOrWhiteSpace(info.LatestVersion.Version))
                    versions.Add(info.LatestVersion.Version);

                if (versions.Count == 0)
                    continue;

                if (!payload.TryGetValue(key.Name, out var existing))
                {
                    existing = [];
                    payload[key.Name] = existing;
                }

                foreach (var v in versions)
                {
                    if (!existing.Contains(v, StringComparer.OrdinalIgnoreCase))
                        existing.Add(v);
                }
            }

            if (payload.Count == 0)
                return null;

            var response = await HttpClient.PostAsJsonAsync(AuditBaseUrl, payload);
            if (!response.IsSuccessStatusCode)
                return null;

            var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (doc.ValueKind != JsonValueKind.Object)
                return null;

            var result = new Dictionary<(string Name, string Version), string>();

            foreach (var (packageName, versionsList) in payload)
            {
                if (!doc.TryGetProperty(packageName, out var advisories) || advisories.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var version in versionsList)
                {
                    var matchingAdvisories = new List<string>();
                    foreach (var advisory in advisories.EnumerateArray())
                    {
                        var vulnerableVersions = advisory.TryGetProperty("vulnerable_versions", out var vv) && vv.ValueKind == JsonValueKind.String
                            ? vv.GetString()
                            : null;

                        if (!string.IsNullOrWhiteSpace(vulnerableVersions) && SemVer.TryParse(version, out var sv))
                        {
                            var comparatorSets = ParseRange(vulnerableVersions);
                            if (comparatorSets.Count > 0 && SatisfiesRange(sv, comparatorSets))
                            {
                                var severity = advisory.TryGetProperty("severity", out var sev) && sev.ValueKind == JsonValueKind.String
                                    ? sev.GetString() ?? "Unknown"
                                    : "Unknown";
                                var url = advisory.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String
                                    ? u.GetString()
                                    : null;
                                var title = advisory.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String
                                    ? t.GetString()
                                    : null;

                                var desc = !string.IsNullOrWhiteSpace(title) ? $"{severity}: {title}" : severity;
                                if (!string.IsNullOrWhiteSpace(url))
                                    desc += $" ({url})";
                                matchingAdvisories.Add(desc);
                            }
                        }
                    }

                    if (matchingAdvisories.Count > 0)
                        result[(packageName, version)] = string.Join("; ", matchingAdvisories);
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private static Task<JsonElement?> GetPackageDocAsync(string packageName)
    {
        return PackageDocCache.GetOrAdd(packageName, static (name, client) => FetchPackageDocAsync(name, client), HttpClient);
    }

    private static async Task<JsonElement?> FetchPackageDocAsync(string packageName, HttpClient httpClient)
    {
        var url = $"{RegistryBaseUrl}/{Uri.EscapeDataString(packageName)}";
        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// Resolves a version range string to the highest matching version from the available versions,
    /// following npm semver resolution rules.
    /// </summary>
    internal static string? ResolveVersion(string? range, List<SemVer> availableVersions, JsonElement distTags = default)
    {
        if (string.IsNullOrWhiteSpace(range))
            return null;

        var trimmed = range.Trim();

        // Check if it's a dist-tag (e.g., "latest", "next")
        if (distTags.ValueKind == JsonValueKind.Object &&
            distTags.TryGetProperty(trimmed, out var tagVersion) &&
            tagVersion.ValueKind == JsonValueKind.String)
        {
            return tagVersion.GetString();
        }

        // Parse the range and find the best match
        var comparatorSets = ParseRange(trimmed);
        if (comparatorSets.Count == 0)
            return null;

        // Filter to non-prerelease versions unless the range explicitly references a prerelease
        var candidates = availableVersions
            .Where(v => SatisfiesRange(v, comparatorSets))
            .OrderByDescending(v => v)
            .ToList();

        return candidates.FirstOrDefault()?.ToString();
    }

    /// <summary>
    /// Parses a semver range string into a list of comparator sets (OR-joined by ||).
    /// Each comparator set is a list of comparators (AND-joined).
    /// </summary>
    internal static List<List<Comparator>> ParseRange(string range)
    {
        var sets = range.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var result = new List<List<Comparator>>();

        foreach (var set in sets)
        {
            var comparators = ParseComparatorSet(set.Trim());
            if (comparators.Count > 0)
                result.Add(comparators);
        }

        return result;
    }

    private static List<Comparator> ParseComparatorSet(string set)
    {
        // Handle hyphen ranges: X.Y.Z - A.B.C
        var hyphenMatch = Regex.Match(set, @"^\s*(\S+)\s+-\s+(\S+)\s*$");
        if (hyphenMatch.Success)
        {
            return ParseHyphenRange(hyphenMatch.Groups[1].Value, hyphenMatch.Groups[2].Value);
        }

        var comparators = new List<Comparator>();
        // Split by whitespace to get individual comparators
        var parts = Regex.Split(set.Trim(), @"\s+");

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            comparators.AddRange(ParseComparator(part.Trim()));
        }

        return comparators;
    }

    private static List<Comparator> ParseHyphenRange(string low, string high)
    {
        var comparators = new List<Comparator>();

        // Low end: >=low
        var lowParsed = ParsePartialVersion(low);
        comparators.Add(new Comparator(ComparatorOp.Gte, lowParsed.Major, lowParsed.Minor ?? 0, lowParsed.Patch ?? 0, lowParsed.Prerelease));

        // High end depends on whether it's a partial version
        var highParsed = ParsePartialVersion(high);
        if (highParsed.Minor == null)
        {
            // 1 - 2  =>  >=1.0.0 <3.0.0
            comparators.Add(new Comparator(ComparatorOp.Lt, highParsed.Major + 1, 0, 0, null));
        }
        else if (highParsed.Patch == null)
        {
            // 1.0 - 2.1  =>  >=1.0.0 <2.2.0
            comparators.Add(new Comparator(ComparatorOp.Lt, highParsed.Major, highParsed.Minor.Value + 1, 0, null));
        }
        else
        {
            // 1.0.0 - 2.1.3  =>  >=1.0.0 <=2.1.3
            comparators.Add(new Comparator(ComparatorOp.Lte, highParsed.Major, highParsed.Minor.Value, highParsed.Patch.Value, highParsed.Prerelease));
        }

        return comparators;
    }

    private static List<Comparator> ParseComparator(string comp)
    {
        // Handle ^, ~, >=, <=, >, <, = prefixes
        if (comp.StartsWith('^'))
            return ParseCaretRange(comp[1..]);
        if (comp.StartsWith('~'))
            return ParseTildeRange(comp[1..]);

        string op;
        string versionStr;
        if (comp.StartsWith(">="))
        {
            op = ">="; versionStr = comp[2..];
        }
        else if (comp.StartsWith("<="))
        {
            op = "<="; versionStr = comp[2..];
        }
        else if (comp.StartsWith('>'))
        {
            op = ">"; versionStr = comp[1..];
        }
        else if (comp.StartsWith('<'))
        {
            op = "<"; versionStr = comp[1..];
        }
        else if (comp.StartsWith('='))
        {
            op = "="; versionStr = comp[1..];
        }
        else
        {
            op = ""; versionStr = comp;
        }

        versionStr = versionStr.TrimStart('v', '=');

        // Handle X-ranges: *, x, X, 1.x, 1.*, 1.2.x, 1.2.*
        var parsed = ParsePartialVersion(versionStr);

        if (parsed.IsAny)
        {
            // * matches everything
            return [new Comparator(ComparatorOp.Gte, 0, 0, 0, null)];
        }

        if (parsed.Minor == null)
        {
            // e.g., "1" or "1.x" => >=1.0.0 <2.0.0
            if (string.IsNullOrEmpty(op))
            {
                return
                [
                    new Comparator(ComparatorOp.Gte, parsed.Major, 0, 0, null),
                    new Comparator(ComparatorOp.Lt, parsed.Major + 1, 0, 0, null)
                ];
            }
        }

        if (parsed.Patch == null)
        {
            // e.g., "1.2" or "1.2.x" => >=1.2.0 <1.3.0
            if (string.IsNullOrEmpty(op))
            {
                return
                [
                    new Comparator(ComparatorOp.Gte, parsed.Major, parsed.Minor!.Value, 0, null),
                    new Comparator(ComparatorOp.Lt, parsed.Major, parsed.Minor.Value + 1, 0, null)
                ];
            }
        }

        var major = parsed.Major;
        var minor = parsed.Minor ?? 0;
        var patch = parsed.Patch ?? 0;
        var pre = parsed.Prerelease;

        var comparatorOp = op switch
        {
            ">=" => ComparatorOp.Gte,
            "<=" => ComparatorOp.Lte,
            ">" => ComparatorOp.Gt,
            "<" => ComparatorOp.Lt,
            _ => ComparatorOp.Eq,
        };

        return [new Comparator(comparatorOp, major, minor, patch, pre)];
    }

    private static List<Comparator> ParseCaretRange(string version)
    {
        var v = version.TrimStart('v', '=');
        var parsed = ParsePartialVersion(v);

        if (parsed.IsAny)
            return [new Comparator(ComparatorOp.Gte, 0, 0, 0, null)];

        var major = parsed.Major;
        var minor = parsed.Minor ?? 0;
        var patch = parsed.Patch ?? 0;
        var pre = parsed.Prerelease;

        // ^1.2.3 := >=1.2.3 <2.0.0
        // ^0.2.3 := >=0.2.3 <0.3.0
        // ^0.0.3 := >=0.0.3 <0.0.4
        // ^1.x   := >=1.0.0 <2.0.0
        // ^0.x   := >=0.0.0 <1.0.0
        // ^0.0.x := >=0.0.0 <0.1.0

        Comparator upper;
        if (major != 0)
        {
            upper = new Comparator(ComparatorOp.Lt, major + 1, 0, 0, null);
        }
        else if (parsed.Minor == null)
        {
            // ^0.x => >=0.0.0 <1.0.0
            upper = new Comparator(ComparatorOp.Lt, major + 1, 0, 0, null);
        }
        else if (minor != 0)
        {
            upper = new Comparator(ComparatorOp.Lt, major, minor + 1, 0, null);
        }
        else if (parsed.Patch == null)
        {
            // ^0.0.x => >=0.0.0 <0.1.0
            upper = new Comparator(ComparatorOp.Lt, major, minor + 1, 0, null);
        }
        else
        {
            upper = new Comparator(ComparatorOp.Lt, major, minor, patch + 1, null);
        }

        return
        [
            new Comparator(ComparatorOp.Gte, major, minor, patch, pre),
            upper
        ];
    }

    private static List<Comparator> ParseTildeRange(string version)
    {
        var v = version.TrimStart('v', '=');
        var parsed = ParsePartialVersion(v);

        if (parsed.IsAny)
            return [new Comparator(ComparatorOp.Gte, 0, 0, 0, null)];

        var major = parsed.Major;
        var minor = parsed.Minor ?? 0;
        var patch = parsed.Patch ?? 0;
        var pre = parsed.Prerelease;

        // ~1.2.3 := >=1.2.3 <1.3.0
        // ~1.2   := >=1.2.0 <1.3.0
        // ~1     := >=1.0.0 <2.0.0
        // ~0.2.3 := >=0.2.3 <0.3.0

        Comparator upper;
        if (parsed.Minor == null)
        {
            // ~1 => >=1.0.0 <2.0.0
            upper = new Comparator(ComparatorOp.Lt, major + 1, 0, 0, null);
        }
        else
        {
            // ~1.2.3 => >=1.2.3 <1.3.0
            upper = new Comparator(ComparatorOp.Lt, major, minor + 1, 0, null);
        }

        return
        [
            new Comparator(ComparatorOp.Gte, major, minor, patch, pre),
            upper
        ];
    }

    internal static bool SatisfiesRange(SemVer version, List<List<Comparator>> comparatorSets)
    {
        // A version satisfies the range if it satisfies ANY of the comparator sets (OR logic)
        foreach (var set in comparatorSets)
        {
            if (SatisfiesComparatorSet(version, set))
                return true;
        }

        return false;
    }

    private static bool SatisfiesComparatorSet(SemVer version, List<Comparator> comparators)
    {
        // A version satisfies a comparator set if it satisfies ALL comparators (AND logic)
        // npm excludes prereleases unless a comparator in the set explicitly includes a prerelease on the same [major, minor, patch] tuple
        if (version.Prerelease != null)
        {
            var allowPrerelease = comparators.Any(c =>
                c.Prerelease != null &&
                c.Major == version.Major &&
                c.Minor == version.Minor &&
                c.Patch == version.Patch);

            if (!allowPrerelease)
                return false;
        }

        foreach (var comparator in comparators)
        {
            if (!comparator.Test(version))
                return false;
        }

        return true;
    }

    internal static PartialVersion ParsePartialVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version) || version == "*" || version.Equals("x", StringComparison.OrdinalIgnoreCase))
            return new PartialVersion(0, null, null, null, true);

        // Match versions like: 1, 1.2, 1.2.3, 1.2.3-beta.1
        var match = Regex.Match(version, @"^(\d+|[xX*])(?:\.(\d+|[xX*])(?:\.(\d+|[xX*])(?:-([a-zA-Z0-9._-]+))?)?)?$");
        if (!match.Success)
            return new PartialVersion(0, null, null, null, true);

        var majorStr = match.Groups[1].Value;
        if (IsWildcard(majorStr))
            return new PartialVersion(0, null, null, null, true);

        var major = int.Parse(majorStr);

        int? minor = null;
        if (match.Groups[2].Success && !IsWildcard(match.Groups[2].Value))
            minor = int.Parse(match.Groups[2].Value);
        else if (match.Groups[2].Success)
            return new PartialVersion(major, null, null, null, false); // e.g., 1.x

        int? patch = null;
        if (match.Groups[3].Success && !IsWildcard(match.Groups[3].Value))
            patch = int.Parse(match.Groups[3].Value);
        else if (match.Groups[3].Success)
            return new PartialVersion(major, minor, null, null, false); // e.g., 1.2.x

        string? prerelease = match.Groups[4].Success ? match.Groups[4].Value : null;

        return new PartialVersion(major, minor, patch, prerelease, false);
    }

    private static bool IsWildcard(string value) => value is "*" or "x" or "X";
}

internal record PartialVersion(int Major, int? Minor, int? Patch, string? Prerelease, bool IsAny);

internal enum ComparatorOp
{
    Eq,
    Gt,
    Gte,
    Lt,
    Lte,
}

internal record Comparator(ComparatorOp Op, int Major, int Minor, int Patch, string? Prerelease)
{
    public bool Test(SemVer version)
    {
        var cmp = version.CompareTo(Major, Minor, Patch, Prerelease);
        return Op switch
        {
            ComparatorOp.Eq => cmp == 0,
            ComparatorOp.Gt => cmp > 0,
            ComparatorOp.Gte => cmp >= 0,
            ComparatorOp.Lt => cmp < 0,
            ComparatorOp.Lte => cmp <= 0,
            _ => false,
        };
    }
}

internal class SemVer : IComparable<SemVer>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? Prerelease { get; }

    private SemVer(int major, int minor, int patch, string? prerelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
    }

    public static bool TryParse(string version, out SemVer result)
    {
        result = null!;
        var match = Regex.Match(version, @"^v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9._-]+))?(?:\+.*)?$");
        if (!match.Success)
            return false;

        result = new SemVer(
            int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value),
            match.Groups[4].Success ? match.Groups[4].Value : null);
        return true;
    }

    public int CompareTo(int major, int minor, int patch, string? prerelease)
    {
        var cmp = Major.CompareTo(major);
        if (cmp != 0) return cmp;
        cmp = Minor.CompareTo(minor);
        if (cmp != 0) return cmp;
        cmp = Patch.CompareTo(patch);
        if (cmp != 0) return cmp;

        return ComparePrerelease(Prerelease, prerelease);
    }

    public int CompareTo(SemVer? other)
    {
        if (other is null) return 1;
        return CompareTo(other.Major, other.Minor, other.Patch, other.Prerelease);
    }

    private static int ComparePrerelease(string? a, string? b)
    {
        // No prerelease > prerelease (1.0.0 > 1.0.0-alpha)
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        var aParts = a.Split('.');
        var bParts = b.Split('.');

        for (var i = 0; i < Math.Max(aParts.Length, bParts.Length); i++)
        {
            if (i >= aParts.Length) return -1; // fewer fields = lower precedence
            if (i >= bParts.Length) return 1;

            var aIsNum = int.TryParse(aParts[i], out var aNum);
            var bIsNum = int.TryParse(bParts[i], out var bNum);

            if (aIsNum && bIsNum)
            {
                var cmp = aNum.CompareTo(bNum);
                if (cmp != 0) return cmp;
            }
            else if (aIsNum)
            {
                return -1; // numeric < string
            }
            else if (bIsNum)
            {
                return 1;
            }
            else
            {
                var cmp = string.Compare(aParts[i], bParts[i], StringComparison.Ordinal);
                if (cmp != 0) return cmp;
            }
        }

        return 0;
    }

    public override string ToString()
    {
        return Prerelease != null ? $"{Major}.{Minor}.{Patch}-{Prerelease}" : $"{Major}.{Minor}.{Patch}";
    }
}
