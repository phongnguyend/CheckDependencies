using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckNugetPackages;

public static class NugetLicenseResolver
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

    public static async Task<Dictionary<(string Name, string Version), string?>> GetLicensesAsync(
        IEnumerable<(string Name, string Version)> packages)
    {
        var distinct = packages.Distinct().ToList();
        var results = new Dictionary<(string Name, string Version), string?>();

        // Load cache
        var cache = LoadCache();

        // Determine which packages need fetching
        var toFetch = new List<(string Name, string Version)>();
        foreach (var package in distinct)
        {
            if (cache.TryGetValue(package.Name, out var versions) &&
                versions.TryGetValue(package.Version, out var cachedLicense))
            {
                results[package] = cachedLicense;
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
                    var license = await GetLicenseAsync(package.Name, package.Version);
                    lock (results)
                    {
                        results[package] = license;
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
            foreach (var (key, license) in results)
            {
                if (!cache.TryGetValue(key.Name, out var versions))
                {
                    versions = new Dictionary<string, string?>();
                    cache[key.Name] = versions;
                }

                versions[key.Version] = license;
            }

            SaveCache(cache);
        }

        return results;
    }

    private static Dictionary<string, Dictionary<string, string?>> LoadCache()
    {
        try
        {
            if (File.Exists(CacheFileName))
            {
                var json = File.ReadAllText(CacheFileName);
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string?>>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load license cache: {ex.Message}");
        }

        return [];
    }

    private static void SaveCache(Dictionary<string, Dictionary<string, string?>> cache)
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

    private static async Task<string?> GetLicenseAsync(string packageName, string? version)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return null;

        try
        {
            var url = $"{RegistrationBaseUrl}/{packageName.ToLowerInvariant()}/index.json";
            var response = await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var registration = await response.Content.ReadFromJsonAsync<RegistrationIndex>();
            if (registration?.Items == null)
                return null;

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
                        return !string.IsNullOrEmpty(catalogEntry.LicenseExpression) ? catalogEntry.LicenseExpression : catalogEntry.LicenseUrl;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return null;
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
    }
}
