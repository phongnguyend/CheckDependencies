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

            var licenseValue = package.License ?? "";
            var publishedDateValue = package.PublishedDate ?? "";
            var deprecatedValue = package.Deprecated ?? "";
            var vulnerabilitiesValue = package.Vulnerabilities ?? "";
            var latestVersionValue = package.LatestVersion ?? "";
            var latestLicenseValue = package.LatestLicense ?? "";
            var latestPublishedDateValue = package.LatestPublishedDate ?? "";
            var latestDeprecatedValue = package.LatestDeprecated ?? "";
            var latestVulnerabilitiesValue = package.LatestVulnerabilities ?? "";
            var latestUrlValue = package.LatestUrl ?? "";
            var resolvedVersionValue = package.ResolvedVersion ?? "";
            streamWriter.WriteLine($"{package.Name},{package.Version},\"{resolvedVersionValue}\",\"{licenseValue}\",\"{publishedDateValue}\",\"{deprecatedValue}\",\"{vulnerabilitiesValue}\",\"{latestVersionValue}\",\"{latestLicenseValue}\",\"{latestPublishedDateValue}\",\"{latestDeprecatedValue}\",\"{latestVulnerabilitiesValue}\",\"{package.Url}\",\"{latestUrlValue}\",\"{package.Projects}\"");
        }
    }
}
