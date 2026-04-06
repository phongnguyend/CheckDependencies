using NuGet.Versioning;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CheckNugetPackages;

public record PackageInfo(
    VersionEntry ResolvedVersion,
    VersionEntry LatestVersion,
    VersionEntry? LatestPatchVersion = null,
    VersionEntry? LatestMinorVersion = null);

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

    public static async Task<Dictionary<(string Name, string Version), PackageInfo>> GetPackagesInfoAsync(
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

    public static async Task<PackageInfo> GetPackageInfoAsync(string packageName, string? version)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return new PackageInfo(new VersionEntry(null, null, null, null, null, null), new VersionEntry(null, null, null, null, null, null));

        try
        {
            var registration = await GetRegistrationAsync(packageName);
            if (registration?.Items == null)
                return new PackageInfo(new VersionEntry(null, null, null, null, null, null), new VersionEntry(null, null, null, null, null, null));

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
                    resolvedVersion = availableVersions.First(v => CompareVersions(v, version) == 0);
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
                resolvedVersion = NormalizeVersion(latestEntry?.Version);
            }

            // Extract info for resolved version
            if (!string.IsNullOrWhiteSpace(resolvedVersion))
            {
                var match = allEntries.FirstOrDefault(e => CompareVersions(e.Entry.Version, resolvedVersion) == 0);
                if (match.Entry != null)
                {
                    var catalogEntry = match.Entry;
                    license = !string.IsNullOrEmpty(catalogEntry.LicenseExpression) ? catalogEntry.LicenseExpression : catalogEntry.LicenseUrl;
                    publishedDate = catalogEntry.Published?.ToString("yyyy-MM-dd");
                    deprecated = FormatDeprecation(catalogEntry.Deprecation);
                    vulnerabilities = FormatVulnerabilities(catalogEntry.Vulnerabilities);
                }
            }

            string? latestVersion = NormalizeVersion(latestEntry?.Version);
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

            // Determine the latest patch and minor versions
            VersionEntry? latestPatchVersionEntry = null;
            VersionEntry? latestMinorVersionEntry = null;

            if (!string.IsNullOrWhiteSpace(resolvedVersion) && NuGetVersion.TryParse(resolvedVersion, out var resolvedNugetVersion))
            {
                // Find latest patch version (same major.minor, highest patch)
                var patchCandidate = allEntries
                    .Where(e => !IsPrerelease(e.Entry.Version) &&
                                NuGetVersion.TryParse(e.Entry.Version, out var v) &&
                                v.Major == resolvedNugetVersion.Major &&
                                v.Minor == resolvedNugetVersion.Minor &&
                                e.Entry.Listed != false)
                    .OrderByDescending(e => NuGetVersion.Parse(e.Entry.Version))
                    .FirstOrDefault();

                if (patchCandidate.Entry != null)
                {
                    var patchVersionStr = NormalizeVersion(patchCandidate.Entry.Version);
                    var patchLicense = !string.IsNullOrEmpty(patchCandidate.Entry.LicenseExpression) ? patchCandidate.Entry.LicenseExpression : patchCandidate.Entry.LicenseUrl;
                    var patchPublishedDate = patchCandidate.Entry.Published?.ToString("yyyy-MM-dd");
                    var patchDeprecated = FormatDeprecation(patchCandidate.Entry.Deprecation);
                    var patchVulnerabilities = FormatVulnerabilities(patchCandidate.Entry.Vulnerabilities);
                    latestPatchVersionEntry = new VersionEntry(patchVersionStr, BuildPackageUrl(packageName, patchVersionStr), patchLicense, patchPublishedDate, patchDeprecated, patchVulnerabilities);
                }

                // Find latest minor version (same major, highest minor.patch)
                var minorCandidate = allEntries
                    .Where(e => !IsPrerelease(e.Entry.Version) &&
                                NuGetVersion.TryParse(e.Entry.Version, out var v) &&
                                v.Major == resolvedNugetVersion.Major &&
                                e.Entry.Listed != false)
                    .OrderByDescending(e => NuGetVersion.Parse(e.Entry.Version))
                    .FirstOrDefault();

                if (minorCandidate.Entry != null)
                {
                    var minorVersionStr = NormalizeVersion(minorCandidate.Entry.Version);
                    var minorLicense = !string.IsNullOrEmpty(minorCandidate.Entry.LicenseExpression) ? minorCandidate.Entry.LicenseExpression : minorCandidate.Entry.LicenseUrl;
                    var minorPublishedDate = minorCandidate.Entry.Published?.ToString("yyyy-MM-dd");
                    var minorDeprecated = FormatDeprecation(minorCandidate.Entry.Deprecation);
                    var minorVulnerabilities = FormatVulnerabilities(minorCandidate.Entry.Vulnerabilities);
                    latestMinorVersionEntry = new VersionEntry(minorVersionStr, BuildPackageUrl(packageName, minorVersionStr), minorLicense, minorPublishedDate, minorDeprecated, minorVulnerabilities);
                }
            }

            return new PackageInfo(
                new VersionEntry(resolvedVersion, BuildPackageUrl(packageName, resolvedVersion), license, publishedDate, deprecated, vulnerabilities),
                new VersionEntry(latestVersion, BuildPackageUrl(packageName, latestVersion), latestLicense, latestPublishedDate, latestDeprecated, latestVulnerabilities),
                latestPatchVersionEntry,
                latestMinorVersionEntry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return new PackageInfo(new VersionEntry(null, null, null, null, null, null), new VersionEntry(null, null, null, null, null, null));
    }

    private static string? NormalizeVersion(string? version)
    {
        return !string.IsNullOrEmpty(version) ? NuGetVersion.Parse(version).ToNormalizedString() : version;
    }

    private static string? BuildPackageUrl(string packageName, string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;
        return $"https://www.nuget.org/packages/{packageName}/{version}";
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

    // Resolve NuGet version range or floating versions to the highest matching available version
    public static string? ResolveNugetVersion(string range, IEnumerable<string> versions)
    {
        if (string.IsNullOrWhiteSpace(range))
            return null;

        var parsedVersions = versions
            .Select(v => NuGetVersion.TryParse(v, out var nv) ? nv : null)
            .Where(v => v != null)
            .Cast<NuGetVersion>()
            .ToList();

        if (parsedVersions.Count == 0)
            return null;

        // Exact version (like 1.2.3-beta)
        if (NuGetVersion.TryParse(range, out var exact))
        {
            var match = parsedVersions
                .FirstOrDefault(v => v == exact);

            return match?.ToNormalizedString();
        }

        // Range or floating version
        if (!VersionRange.TryParse(range, out var versionRange))
            return null;

        // Floating version
        if (versionRange.Float != null)
        {
            var best = versionRange.FindBestMatch(parsedVersions);
            return best?.ToNormalizedString();
        }

        // Normal range
        return parsedVersions
            .Where(v => versionRange.Satisfies(v))
            .Max()
            ?.ToNormalizedString();
    }

    private static int CompareVersions(string? a, string? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        return NuGetVersion.Parse(a).CompareTo(NuGetVersion.Parse(b));
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
