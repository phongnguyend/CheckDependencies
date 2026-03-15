using System.Net;
using System.Net.Http.Json;
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

    public static async Task<Dictionary<(string Name, string Version), string?>> GetLicensesAsync(
        IEnumerable<(string Name, string Version)> packages)
    {
        var distinct = packages.Distinct().ToList();
        var results = new Dictionary<(string Name, string Version), string?>();

        // Use SemaphoreSlim to limit concurrent requests
        using var semaphore = new SemaphoreSlim(10);
        var tasks = distinct.Select(async package =>
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
        return results;
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
