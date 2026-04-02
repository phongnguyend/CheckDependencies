using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CheckNugetPackages;

public record PackageInfo(string? ResolvedVersion, string? License, string? PublishedDate, string? Deprecated, string? Vulnerabilities, string? LatestVersion, string? LatestLicense, string? LatestPublishedDate, string? LatestDeprecated, string? LatestVulnerabilities);

public static class NugetPackageResolver
{
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string RegistrationBaseUrl = "https://api.nuget.org/v3/registration5-gz-semver2";

    private static readonly ConcurrentDictionary<string, Task<RegistrationIndex?>> RegistrationCache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<Dictionary<(string Name, string Version), PackageInfo>> GetLicensesAsync(
        IEnumerable<(string Name, string Version)> packages)
    {
        var distinct = packages.Distinct().ToList();
        var results = new Dictionary<(string Name, string Version), PackageInfo>();

        if (distinct.Count == 0)
            return results;

        // Use SemaphoreSlim to limit concurrent requests
        using var semaphore = new SemaphoreSlim(10);
        var tasks = distinct.Select(async package =>
        {
            await semaphore.WaitAsync();
            try
            {
                var info = await GetPackageInfoAsync(package.Name, package.Version);
                lock (results)
                {
                    results[package] = info;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return results;
    }

    private static async Task<PackageInfo> GetPackageInfoAsync(string packageName, string? version)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return new PackageInfo(null, null, null, null, null, null, null, null, null, null);

        try
        {
            var registration = await GetRegistrationAsync(packageName);
            if (registration?.Items == null)
                return new PackageInfo(null, null, null, null, null, null, null, null, null, null);

            string? license = null;
            string? publishedDate = null;
            string? deprecated = null;
            string? vulnerabilities = null;
            CatalogEntry? latestEntry = null;
            RegistrationLeaf? latestLeaf = null;

            // Search through pages for the matching version and track the latest version
            // Collect all entries and track the latest (non-prerelease) version
            var allEntries = new List<(CatalogEntry Entry, RegistrationLeaf Leaf)>();

            foreach (var page in registration.Items)
            {
                if (page.Items == null)
                    continue;

                foreach (var item in page.Items)
                {
                    var catalogEntry = item.CatalogEntry;
                    if (catalogEntry == null)
                        continue;

                    // Check for the requested version
                    if (string.Equals(catalogEntry.Version, version, StringComparison.OrdinalIgnoreCase))
                    {
                        license = !string.IsNullOrEmpty(catalogEntry.LicenseExpression) ? catalogEntry.LicenseExpression : catalogEntry.LicenseUrl;
                        publishedDate = catalogEntry.Published?.ToString("yyyy-MM-dd");
                        deprecated = FormatDeprecation(catalogEntry.Deprecation);
                        vulnerabilities = FormatVulnerabilities(catalogEntry.Vulnerabilities);
                    }
                    allEntries.Add((catalogEntry, item));

                    // Track the latest (non-prerelease) version
                    if (catalogEntry.Listed != false && !IsPrerelease(catalogEntry.Version))
                    {
                        if (latestEntry == null || CompareVersions(catalogEntry.Version, latestEntry.Version) > 0)
                        {
                            latestEntry = catalogEntry;
                            latestLeaf = item;
                        }
                    }
                }
            }

            // Build a list of available version strings
            var availableVersions = allEntries.Select(e => e.Entry.Version ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();

            // Resolve the requested version (support NuGet version range syntax)
            string? resolvedVersion = null;
            if (!string.IsNullOrWhiteSpace(version))
            {
                // If exact version exists, prefer exact
                if (availableVersions.Contains(version, StringComparer.OrdinalIgnoreCase))
                {
                    resolvedVersion = availableVersions.First(v => string.Equals(v, version, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    // Try to resolve range/wildcard
                    resolvedVersion = ResolveNugetVersion(version, availableVersions);
                }
            }

            // If not resolved, pick latest (non-prerelease)
            if (string.IsNullOrWhiteSpace(resolvedVersion))
            {
                resolvedVersion = latestEntry?.Version;
            }

            // Extract info for resolved version
            if (!string.IsNullOrWhiteSpace(resolvedVersion))
            {
                var match = allEntries.FirstOrDefault(e => string.Equals(e.Entry.Version, resolvedVersion, StringComparison.OrdinalIgnoreCase));
                if (match.Entry != null)
                {
                    var catalogEntry = match.Entry;
                    license = !string.IsNullOrEmpty(catalogEntry.LicenseExpression) ? catalogEntry.LicenseExpression : catalogEntry.LicenseUrl;
                    publishedDate = catalogEntry.Published?.ToString("yyyy-MM-dd");
                    deprecated = FormatDeprecation(catalogEntry.Deprecation);
                    vulnerabilities = FormatVulnerabilities(catalogEntry.Vulnerabilities);
                }
            }

            string? latestVersion = latestEntry?.Version;
            string? latestLicense = null;
            string? latestPublishedDate = null;
            string? latestDeprecated = null;
            string? latestVulnerabilities = null;

            if (latestEntry != null)
            {
                latestLicense = !string.IsNullOrEmpty(latestEntry.LicenseExpression) ? latestEntry.LicenseExpression : latestEntry.LicenseUrl;
                latestPublishedDate = latestEntry.Published?.ToString("yyyy-MM-dd");
                latestDeprecated = FormatDeprecation(latestEntry.Deprecation);
                latestVulnerabilities = FormatVulnerabilities(latestEntry.Vulnerabilities);
            }

            return new PackageInfo(resolvedVersion, license, publishedDate, deprecated, vulnerabilities, latestVersion, latestLicense, latestPublishedDate, latestDeprecated, latestVulnerabilities);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return new PackageInfo(null, null, null, null, null, null, null, null, null, null);
    }

    internal static string? FormatDeprecation(DeprecationInfo? deprecation)
    {
        if (deprecation == null)
            return null;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(deprecation.Message))
            parts.Add(deprecation.Message);

        if (deprecation.Reasons != null && deprecation.Reasons.Count > 0)
            parts.Add(string.Join(", ", deprecation.Reasons));

        return parts.Count > 0 ? string.Join(" - ", parts) : "Deprecated";
    }

    internal static string? FormatVulnerabilities(List<VulnerabilityInfo>? vulnerabilities)
    {
        if (vulnerabilities == null || vulnerabilities.Count == 0)
            return null;

        var descriptions = vulnerabilities.Select(v =>
        {
            var severity = v.Severity ?? "Unknown";
            var advisoryUrl = v.AdvisoryUrl;
            return !string.IsNullOrWhiteSpace(advisoryUrl) ? $"{severity} ({advisoryUrl})" : severity;
        });

        return string.Join("; ", descriptions);
    }

    private static Task<RegistrationIndex?> GetRegistrationAsync(string packageName)
    {
        return RegistrationCache.GetOrAdd(packageName, static (name, client) => FetchRegistrationAsync(name, client), HttpClient);
    }

    private static async Task<RegistrationIndex?> FetchRegistrationAsync(string packageName, HttpClient httpClient)
    {
        var url = $"{RegistrationBaseUrl}/{packageName.ToLowerInvariant()}/index.json";
        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var registration = await response.Content.ReadFromJsonAsync<RegistrationIndex>();
        if (registration?.Items == null)
            return registration;

        // Pre-fetch any pages that don't have inlined items so the data is fully resolved in the cache
        for (var i = 0; i < registration.Items.Count; i++)
        {
            var page = registration.Items[i];
            if (page.Items == null && page.Id != null)
            {
                var pageResponse = await httpClient.GetAsync(page.Id);
                if (pageResponse.IsSuccessStatusCode)
                {
                    var pageData = await pageResponse.Content.ReadFromJsonAsync<RegistrationPage>();
                    if (pageData != null)
                    {
                        registration.Items[i] = pageData;
                    }
                }
            }
        }

        return registration;
    }

    private static bool IsPrerelease(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        return version.Contains('-');
    }

    // Compare numeric parts and treat prerelease as lower precedence than release
    private static int CompareNugetVersions(string? a, string? b)
    {
        var numCmp = CompareVersions(a, b);
        if (numCmp != 0)
            return numCmp;

        var aIsPre = !string.IsNullOrEmpty(a) && a.Contains('-');
        var bIsPre = !string.IsNullOrEmpty(b) && b.Contains('-');

        if (aIsPre == bIsPre) // both pre or both not pre
        {
            if (!aIsPre) return 0;
            // both prerelease: compare prerelease identifiers lexically
            var aPre = a!.Split('-', 2)[1];
            var bPre = b!.Split('-', 2)[1];
            return string.Compare(aPre, bPre, StringComparison.Ordinal);
        }

        // non-prerelease is greater
        return aIsPre ? -1 : 1;
    }

    // Resolve NuGet version range or floating versions to the highest matching available version
    internal static string? ResolveNugetVersion(string? range, List<string> availableVersions)
    {
        if (string.IsNullOrWhiteSpace(range))
            return null;

        var trimmed = range.Trim();

        // If it's a bracketed range like [1.0,2.0) or (1.0,)
        if ((trimmed.StartsWith('[') || trimmed.StartsWith('(')) && (trimmed.EndsWith(']') || trimmed.EndsWith(')')))
        {
            var inclusiveLower = trimmed.StartsWith('[');
            var inclusiveUpper = trimmed.EndsWith(']');
            var inner = trimmed[1..^1];
            var parts = inner.Split(',', 2);
            var lower = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            var upper = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            var allowPrerelease = (lower.Contains('-') || upper.Contains('-'));

            var candidates = availableVersions.Where(v =>
            {
                if (string.IsNullOrEmpty(v)) return false;
                // lower bound
                if (!string.IsNullOrEmpty(lower))
                {
                    var cmp = CompareNugetVersions(v, lower);
                    if (cmp < 0 || (!inclusiveLower && cmp == 0))
                        return false;
                }
                // upper bound
                if (!string.IsNullOrEmpty(upper))
                {
                    var cmp = CompareNugetVersions(v, upper);
                    if (cmp > 0 || (!inclusiveUpper && cmp == 0))
                        return false;
                }

                if (!allowPrerelease && IsPrerelease(v))
                    return false;

                return true;
            }).ToList();

            if (!candidates.Any()) return null;

            return candidates.OrderByDescending(v => v, Comparer<string>.Create(CompareNugetVersions)).First();
        }

        // Wildcard/floating versions like 1.*, 1.2.*
        if (trimmed.Contains('*') || trimmed.EndsWith(".x", StringComparison.OrdinalIgnoreCase))
        {
            var norm = trimmed.Replace("*", "x");
            var parts = norm.Split('.');
            if (parts.Length >= 1 && int.TryParse(parts[0], out var major))
            {
                if (parts.Length == 1 || (parts.Length >= 2 && (parts[1].Equals("x", StringComparison.OrdinalIgnoreCase))))
                {
                    // 1 or 1.x => >=1.0.0 <2.0.0
                    var candidates = availableVersions.Where(v =>
                    {
                        var cmpLow = CompareNugetVersions(v, $"{major}.0.0");
                        var cmpHigh = CompareNugetVersions(v, $"{major + 1}.0.0");
                        return cmpLow >= 0 && cmpHigh < 0 && !IsPrerelease(v);
                    }).ToList();

                    if (candidates.Any())
                        return candidates.OrderByDescending(v => v, Comparer<string>.Create(CompareNugetVersions)).First();
                }
                else if (parts.Length >= 2 && int.TryParse(parts[1], out var minor))
                {
                    // 1.2.x => >=1.2.0 <1.3.0
                    var candidates = availableVersions.Where(v =>
                    {
                        var cmpLow = CompareNugetVersions(v, $"{major}.{minor}.0");
                        var cmpHigh = CompareNugetVersions(v, $"{major}.{minor + 1}.0");
                        return cmpLow >= 0 && cmpHigh < 0 && !IsPrerelease(v);
                    }).ToList();

                    if (candidates.Any())
                        return candidates.OrderByDescending(v => v, Comparer<string>.Create(CompareNugetVersions)).First();
                }
            }

            return null;
        }

        // If it looks like an exact version (including prerelease), just return it if available
        if (!trimmed.Contains(',') && !trimmed.Contains(' ') && !trimmed.StartsWith('^') && !trimmed.StartsWith('~'))
        {
            return availableVersions.FirstOrDefault(v => string.Equals(v, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        // Fallback: unsupported complex range syntax - try to find exact match
        return availableVersions.FirstOrDefault(v => string.Equals(v, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static int CompareVersions(string? a, string? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // Strip prerelease suffix for comparison
        var aParts = a.Split('-')[0].Split('.');
        var bParts = b.Split('-')[0].Split('.');

        var maxLen = Math.Max(aParts.Length, bParts.Length);
        for (var i = 0; i < maxLen; i++)
        {
            var aNum = i < aParts.Length && int.TryParse(aParts[i], out var av) ? av : 0;
            var bNum = i < bParts.Length && int.TryParse(bParts[i], out var bv) ? bv : 0;
            if (aNum != bNum)
                return aNum.CompareTo(bNum);
        }

        return 0;
    }

    private class RegistrationIndex
    {
        [JsonPropertyName("items")]
        public List<RegistrationPage>? Items { get; set; }
    }

    private class RegistrationPage
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("items")]
        public List<RegistrationLeaf>? Items { get; set; }
    }

    private class RegistrationLeaf
    {
        [JsonPropertyName("catalogEntry")]
        public CatalogEntry? CatalogEntry { get; set; }
    }

    private class CatalogEntry
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("licenseExpression")]
        public string? LicenseExpression { get; set; }

        [JsonPropertyName("licenseUrl")]
        public string? LicenseUrl { get; set; }

        [JsonPropertyName("published")]
        public DateTimeOffset? Published { get; set; }

        [JsonPropertyName("listed")]
        public bool? Listed { get; set; }

        [JsonPropertyName("deprecation")]
        public DeprecationInfo? Deprecation { get; set; }

        [JsonPropertyName("vulnerabilities")]
        public List<VulnerabilityInfo>? Vulnerabilities { get; set; }
    }

    internal class DeprecationInfo
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("reasons")]
        public List<string>? Reasons { get; set; }
    }

    internal class VulnerabilityInfo
    {
        [JsonPropertyName("advisoryUrl")]
        public string? AdvisoryUrl { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }
    }
}
