using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckNpmPackages;

public record PackageInfo(string? License, string? PublishedDate);

public static class NpmLicenseResolver
{
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string RegistryBaseUrl = "https://registry.npmjs.org";
    private const string CacheFileName = ".CheckNpmPackagesCache";

    private static readonly JsonSerializerOptions CacheJsonOptions = new()
    {
        WriteIndented = true
    };

    public static async Task<Dictionary<(string Name, string Version), PackageInfo>> GetLicensesAsync(
        IEnumerable<(string Name, string Version)> packages)
    {
        var distinct = packages.Distinct().ToList();
        var results = new Dictionary<(string Name, string Version), PackageInfo>();

        // Load cache
        var cache = LoadCache();

        // Determine which packages need fetching
        var toFetch = new List<(string Name, string Version)>();
        foreach (var package in distinct)
        {
            var formattedVersion = FormatVersion(package.Version);
            if (cache.TryGetValue(package.Name, out var versions) &&
                versions.TryGetValue(formattedVersion, out var cachedInfo))
            {
                results[package] = new PackageInfo(cachedInfo.License, cachedInfo.PublishedDate);
            }
            else
            {
                toFetch.Add(package);
            }
        }

        if (toFetch.Count > 0)
        {
            // Use SemaphoreSlim to limit concurrent requests
            using var semaphore = new SemaphoreSlim(10);
            var tasks = toFetch.Select(async package =>
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

            // Update cache with new results and save only if there were new fetches
            foreach (var (key, info) in results)
            {
                var formattedVersion = FormatVersion(key.Version);
                if (!cache.TryGetValue(key.Name, out var versions))
                {
                    versions = new Dictionary<string, CacheEntry>();
                    cache[key.Name] = versions;
                }

                versions[formattedVersion] = new CacheEntry { License = info.License, PublishedDate = info.PublishedDate };
            }

            SaveCache(cache);
        }

        return results;
    }

    private static Dictionary<string, Dictionary<string, CacheEntry>> LoadCache()
    {
        try
        {
            if (File.Exists(CacheFileName))
            {
                var json = File.ReadAllText(CacheFileName);
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, CacheEntry>>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load license cache: {ex.Message}");
        }

        return [];
    }

    private static void SaveCache(Dictionary<string, Dictionary<string, CacheEntry>> cache)
    {
        try
        {
            var ordered = cache
                .OrderBy(p => p.Key)
                .ToDictionary(
                    p => p.Key,
                    p => p.Value.OrderBy(v => v.Key).ToDictionary(v => v.Key, v => v.Value));

            var json = JsonSerializer.Serialize(ordered, CacheJsonOptions);
            File.WriteAllText(CacheFileName, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to save license cache: {ex.Message}");
        }
    }

    private static async Task<PackageInfo> GetPackageInfoAsync(string packageName, string? version)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return new PackageInfo(null, null);

        try
        {
            var formattedVersion = FormatVersion(version);

            // Fetch the full package document to get both version info and time metadata
            var url = $"{RegistryBaseUrl}/{Uri.EscapeDataString(packageName)}";
            var response = await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new PackageInfo(null, null);

            var doc = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Determine the resolved version
            var resolvedVersion = formattedVersion;
            if (string.IsNullOrWhiteSpace(resolvedVersion))
            {
                // Fall back to dist-tags.latest
                if (doc.TryGetProperty("dist-tags", out var distTags) &&
                    distTags.TryGetProperty("latest", out var latestProp) &&
                    latestProp.ValueKind == JsonValueKind.String)
                {
                    resolvedVersion = latestProp.GetString();
                }
            }

            // Extract license from the specific version
            string? license = null;
            if (!string.IsNullOrWhiteSpace(resolvedVersion) &&
                doc.TryGetProperty("versions", out var versionsProp) &&
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
                doc.TryGetProperty("time", out var timeProp) &&
                timeProp.TryGetProperty(resolvedVersion, out var versionTimeProp) &&
                versionTimeProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(versionTimeProp.GetString(), out var dto))
                {
                    publishedDate = dto.ToString("yyyy-MM-dd");
                }
            }

            return new PackageInfo(
                !string.IsNullOrEmpty(license) ? license : null,
                publishedDate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return new PackageInfo(null, null);
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

    private class CacheEntry
    {
        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; set; }
    }
}
