namespace CheckNpmPackages;

public static class MarkdownReportGenerator
{
    public static void Generate(string filePath, string reportTitle, List<PackageEntry> packages, List<string> ignoredPackages)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = File.Open(filePath, FileMode.Create);
        using var streamWriter = new StreamWriter(fileStream);
        streamWriter.WriteLine($"# {reportTitle}");
        streamWriter.WriteLine();
        streamWriter.WriteLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
        streamWriter.WriteLine();
        streamWriter.WriteLine("| Name | Version | Resolved Version | License | Published Date | Deprecated | Vulnerabilities | Latest Version | Latest License | Latest Published Date | Latest Deprecated | Latest Vulnerabilities | Projects |");
        streamWriter.WriteLine("| ---- | ------- | ---------------- | ------- | -------------- | ---------- | --------------- | -------------- | -------------- | --------------------- | ----------------- | ---------------------- | -------- |");

        foreach (var package in packages)
        {
            if (ignoredPackages.Any(package.Name.StartsWith))
            {
                continue;
            }

            var licenseMd = FormatLicenseMarkdown(package.ResolvedVersion.License);
            var versionMd = EscapeMarkdown(package.Version ?? "N/A");
            var resolvedVersionMd = $"[{EscapeMarkdown(package.ResolvedVersion.Version ?? "N/A")}]({package.ResolvedVersion.Url})";
            var publishedDateMd = EscapeMarkdown(package.ResolvedVersion.PublishedDate ?? "N/A");
            var deprecatedMd = EscapeMarkdown(package.ResolvedVersion.Deprecated ?? "");
            var vulnerabilitiesMd = EscapeMarkdown(package.ResolvedVersion.Vulnerabilities ?? "");
            var latestVersionMd = package.LatestVersion.Url != null
                ? $"[{EscapeMarkdown(package.LatestVersion.Version ?? "N/A")}]({package.LatestVersion.Url})"
                : EscapeMarkdown(package.LatestVersion.Version ?? "N/A");
            var latestLicenseMd = FormatLicenseMarkdown(package.LatestVersion.License);
            var latestPublishedDateMd = EscapeMarkdown(package.LatestVersion.PublishedDate ?? "N/A");
            var latestDeprecatedMd = EscapeMarkdown(package.LatestVersion.Deprecated ?? "");
            var latestVulnerabilitiesMd = EscapeMarkdown(package.LatestVersion.Vulnerabilities ?? "");

            streamWriter.WriteLine($"| {EscapeMarkdown(package.Name)} | {versionMd} | {resolvedVersionMd} | {licenseMd} | {publishedDateMd} | {deprecatedMd} | {vulnerabilitiesMd} | {latestVersionMd} | {latestLicenseMd} | {latestPublishedDateMd} | {latestDeprecatedMd} | {latestVulnerabilitiesMd} | {EscapeMarkdown(package.Projects)} |");
        }
    }

    private static string FormatLicenseMarkdown(string? license)
    {
        if (string.IsNullOrWhiteSpace(license))
            return "N/A";

        if (license.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            license.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return $"[View License]({license})";
        }

        return EscapeMarkdown(license);
    }

    private static string EscapeMarkdown(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace("|", "\\|");
    }
}
