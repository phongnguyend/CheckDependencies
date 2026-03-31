using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CheckNpmPackages;

public record PackageInfo(string? ResolvedVersion, string? License, string? PublishedDate, string? LatestVersion, string? LatestLicense, string? LatestPublishedDate);

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

    private static readonly ConcurrentDictionary<string, Task<JsonElement?>> PackageDocCache = new(StringComparer.OrdinalIgnoreCase);

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
            return new PackageInfo(null, null, null, null, null, null);

        try
        {
            var formattedVersion = FormatVersion(version);

            var doc = await GetPackageDocAsync(packageName);
            if (doc == null)
                return new PackageInfo(null, null, null, null, null, null);

            var docValue = doc.Value;

            // Determine the latest version from dist-tags
            string? latestVersion = null;
            if (docValue.TryGetProperty("dist-tags", out var distTags) &&
                distTags.TryGetProperty("latest", out var latestProp) &&
                latestProp.ValueKind == JsonValueKind.String)
            {
                latestVersion = latestProp.GetString();
            }

            // Determine the resolved version for the requested version
            var resolvedVersion = formattedVersion;
            if (string.IsNullOrWhiteSpace(resolvedVersion))
            {
                resolvedVersion = latestVersion;
            }

            // Extract license from the specific version
            string? license = null;
            if (!string.IsNullOrWhiteSpace(resolvedVersion) &&
                docValue.TryGetProperty("versions", out var versionsProp) &&
                versionsProp.TryGetProperty(resolvedVersion, out var versionDoc))
            {
                if (versionDoc.TryGetProperty("license", out var licenseProp) && licenseProp.ValueKind == JsonValueKind.String)
                {
                    license = licenseProp.GetString();
                }
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

            // Extract latest version license and published date
            string? latestLicense = null;
            string? latestPublishedDate = null;
            if (!string.IsNullOrWhiteSpace(latestVersion) &&
                docValue.TryGetProperty("versions", out var versionsForLatest) &&
                versionsForLatest.TryGetProperty(latestVersion, out var latestDoc))
            {
                if (latestDoc.TryGetProperty("license", out var latestLicenseProp) && latestLicenseProp.ValueKind == JsonValueKind.String)
                {
                    latestLicense = latestLicenseProp.GetString();
                }
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
                !string.IsNullOrEmpty(formattedVersion) ? formattedVersion : null,
                !string.IsNullOrEmpty(license) ? license : null,
                publishedDate,
                latestVersion,
                !string.IsNullOrEmpty(latestLicense) ? latestLicense : null,
                latestPublishedDate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return new PackageInfo(null, null, null, null, null, null);
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

    internal static string FormatVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "";

        // Strip leading ^, ~, v, = prefixes
        var trimmed = version.TrimStart('^', '~', 'v', '=');

        // Handle range operators like >=1.0.0 <2.0.0 — take the first version
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex > 0)
            trimmed = trimmed[..spaceIndex];

        // Handle || ranges — take the first part
        var pipeIndex = trimmed.IndexOf("||", StringComparison.Ordinal);
        if (pipeIndex > 0)
            trimmed = trimmed[..pipeIndex].Trim().TrimStart('^', '~', 'v', '=');

        // Handle hyphen ranges like 1.0.0 - 2.0.0 — take the first version
        var hyphenIndex = trimmed.IndexOf(" - ", StringComparison.Ordinal);
        if (hyphenIndex > 0)
            trimmed = trimmed[..hyphenIndex].Trim();

        // Strip any remaining leading comparison operators
        trimmed = trimmed.TrimStart('>', '<', '=');

        return trimmed;
    }
}
