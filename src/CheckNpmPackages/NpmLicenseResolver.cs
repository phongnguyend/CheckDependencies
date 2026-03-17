using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckNpmPackages;

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

            // Update cache with new results and save only if there were new fetches
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
            // npm registry API: GET /{package}/{version}
            var formattedVersion = FormatVersion(version);
            var url = string.IsNullOrWhiteSpace(formattedVersion)
                ? $"{RegistryBaseUrl}/{Uri.EscapeDataString(packageName)}/latest"
                : $"{RegistryBaseUrl}/{Uri.EscapeDataString(packageName)}/{Uri.EscapeDataString(formattedVersion)}";

            var response = await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var packageInfo = await response.Content.ReadFromJsonAsync<NpmPackageVersion>();
            if (packageInfo == null)
                return null;

            // Try license field first (string)
            if (!string.IsNullOrEmpty(packageInfo.License))
                return packageInfo.License;

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch license for {packageName} {version}: {ex.Message}");
        }

        return null;
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

    private class NpmPackageVersion
    {
        [JsonPropertyName("license")]
        public string? License { get; set; }
    }
}
