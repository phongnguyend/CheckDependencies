namespace CheckNpmPackages;

public static class CsvReportGenerator
{
    public static void Generate(string filePath, List<PackageEntry> packages, List<string> ignoredPackages)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = File.Open(filePath, FileMode.Create);
        using var streamWriter = new StreamWriter(fileStream);
        foreach (var package in packages)
        {
            if (ignoredPackages.Any(package.Name.StartsWith))
            {
                continue;
            }

            var licenseValue = package.ResolvedVersion.License ?? "";
            var publishedDateValue = package.ResolvedVersion.PublishedDate ?? "";
            var deprecatedValue = package.ResolvedVersion.Deprecated ?? "";
            var vulnerabilitiesValue = package.ResolvedVersion.Vulnerabilities ?? "";
            var latestVersionValue = package.LatestVersion.Version ?? "";
            var latestLicenseValue = package.LatestVersion.License ?? "";
            var latestPublishedDateValue = package.LatestVersion.PublishedDate ?? "";
            var latestDeprecatedValue = package.LatestVersion.Deprecated ?? "";
            var latestVulnerabilitiesValue = package.LatestVersion.Vulnerabilities ?? "";
            var latestUrlValue = package.LatestVersion.Url ?? "";
            var resolvedVersionValue = package.ResolvedVersion.Version ?? "";
            streamWriter.WriteLine($"{package.Name},{package.Version},\"{resolvedVersionValue}\",\"{licenseValue}\",\"{publishedDateValue}\",\"{deprecatedValue}\",\"{vulnerabilitiesValue}\",\"{latestVersionValue}\",\"{latestLicenseValue}\",\"{latestPublishedDateValue}\",\"{latestDeprecatedValue}\",\"{latestVulnerabilitiesValue}\",\"{package.ResolvedVersion.Url}\",\"{latestUrlValue}\",\"{package.Projects}\"");
        }
    }
}
