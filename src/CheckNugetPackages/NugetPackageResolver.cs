using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckNugetPackages;

public record PackageInfo(string? License, string? PublishedDate);

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
    private const string CacheFileName = ".CheckNugetPackagesCache";

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
            if (cache.TryGetValue(package.Name, out var versions) &&
                versions.TryGetValue(package.Version, out var cachedInfo))
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
        }

        // Update cache with new results and save only if there were new fetches
        if (toFetch.Count > 0)
        {
            foreach (var (key, info) in results)
            {
                if (!cache.TryGetValue(key.Name, out var versions))
                {
                    versions = new Dictionary<string, CacheEntry>();
                    cache[key.Name] = versions;
                }

                versions[key.Version] = new CacheEntry { License = info.License, PublishedDate = info.PublishedDate };
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
            var url = $"{RegistrationBaseUrl}/{packageName.ToLowerInvariant()}/index.json";
            var response = await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new PackageInfo(null, null);

            var registration = await response.Content.ReadFromJsonAsync<RegistrationIndex>();
            if (registration?.Items == null)
                return new PackageInfo(null, null);

            // Search through pages for the matching version
            foreach (var page in registration.Items)
            {
                var items = page.Items;

                // If items are not inlined, fetch the page
                if (items == null && page.Id != null)
                {
                    var pageResponse = await HttpClient.GetAsync(page.Id);
                    if (!pageResponse.IsSuccessStatusCode)
                        continue;

                    var pageData = await pageResponse.Content.ReadFromJsonAsync<RegistrationPage>();
                    items = pageData?.Items;
                }

                if (items == null)
                    continue;

                foreach (var item in items)
                {
                    var catalogEntry = item.CatalogEntry;
                    if (catalogEntry == null)
                        continue;

                    if (string.Equals(catalogEntry.Version, version, StringComparison.OrdinalIgnoreCase))
                    {
                        var license = !string.IsNullOrEmpty(catalogEntry.LicenseExpression) ? catalogEntry.LicenseExpression : catalogEntry.LicenseUrl;
                        var publishedDate = catalogEntry.Published?.ToString("yyyy-MM-dd");
                        return new PackageInfo(license, publishedDate);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return new PackageInfo(null, null);
    }

    private class CacheEntry
    {
        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; set; }
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
    }
}
