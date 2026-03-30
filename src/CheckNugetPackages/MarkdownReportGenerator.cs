namespace CheckNugetPackages;

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
        streamWriter.WriteLine("| Name | Version | License | Published Date | Latest Version | Latest License | Latest Published Date | Projects |");
        streamWriter.WriteLine("| ---- | ------- | ------- | -------------- | -------------- | -------------- | --------------------- | -------- |");

        foreach (var package in packages)
        {
            if (ignoredPackages.Any(package.Name.StartsWith))
            {
                continue;
            }

            var licenseMd = FormatLicenseMarkdown(package.License);
            var versionMd = $"[{EscapeMarkdown(package.Version ?? "N/A")}]({package.Url})";
            var publishedDateMd = EscapeMarkdown(package.PublishedDate ?? "N/A");
            var latestVersionMd = package.LatestUrl != null
                ? $"[{EscapeMarkdown(package.LatestVersion ?? "N/A")}]({package.LatestUrl})"
                : EscapeMarkdown(package.LatestVersion ?? "N/A");
            var latestLicenseMd = FormatLicenseMarkdown(package.LatestLicense);
            var latestPublishedDateMd = EscapeMarkdown(package.LatestPublishedDate ?? "N/A");

            streamWriter.WriteLine($"| {EscapeMarkdown(package.Name)} | {versionMd} | {licenseMd} | {publishedDateMd} | {latestVersionMd} | {latestLicenseMd} | {latestPublishedDateMd} | {EscapeMarkdown(package.Projects)} |");
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
