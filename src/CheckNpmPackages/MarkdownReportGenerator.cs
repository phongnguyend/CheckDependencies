namespace CheckNpmPackages;

public static class MarkdownReportGenerator
{
    public static void Generate(string filePath, string reportTitle, List<PackageEntry> packages, List<string> ignoredPackages, ParsedArguments arguments)
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
        
        var headerLine = "| Name | Version | Resolved Version | License | Published Date | Deprecated | Vulnerabilities |";
        if (arguments.CheckLatestPatch)
            headerLine += " Latest Patch Version | Latest Patch License | Latest Patch Published Date | Latest Patch Deprecated | Latest Patch Vulnerabilities |";
        if (arguments.CheckLatestMinor)
            headerLine += " Latest Minor Version | Latest Minor License | Latest Minor Published Date | Latest Minor Deprecated | Latest Minor Vulnerabilities |";
        headerLine += " Latest Version | Latest License | Latest Published Date | Latest Deprecated | Latest Vulnerabilities | Projects |";
        
        streamWriter.WriteLine(headerLine);
        
        var separatorLine = "| ---- | ------- | ---------------- | ------- | -------------- | ---------- | --------------- |";
        if (arguments.CheckLatestPatch)
            separatorLine += " -------------------- | -------------------- | ----------------------------- | ----------------------- | ----------------------------- |";
        if (arguments.CheckLatestMinor)
            separatorLine += " -------------------- | -------------------- | ----------------------------- | ----------------------- | ----------------------------- |";
        separatorLine += " -------------- | -------------- | --------------------- | ----------------- | ---------------------- | -------- |";
        
        streamWriter.WriteLine(separatorLine);

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

            var line = $"| {EscapeMarkdown(package.Name)} | {versionMd} | {resolvedVersionMd} | {licenseMd} | {publishedDateMd} | {deprecatedMd} | {vulnerabilitiesMd} |";
            
            if (arguments.CheckLatestPatch)
            {
                var patchVersionMd = package.LatestPatchVersion?.Url != null
                    ? $"[{EscapeMarkdown(package.LatestPatchVersion.Version ?? "N/A")}]({package.LatestPatchVersion.Url})"
                    : EscapeMarkdown(package.LatestPatchVersion?.Version ?? "N/A");
                var patchLicenseMd = FormatLicenseMarkdown(package.LatestPatchVersion?.License);
                var patchPublishedDateMd = EscapeMarkdown(package.LatestPatchVersion?.PublishedDate ?? "N/A");
                var patchDeprecatedMd = EscapeMarkdown(package.LatestPatchVersion?.Deprecated ?? "");
                var patchVulnerabilitiesMd = EscapeMarkdown(package.LatestPatchVersion?.Vulnerabilities ?? "");
                line += $" {patchVersionMd} | {patchLicenseMd} | {patchPublishedDateMd} | {patchDeprecatedMd} | {patchVulnerabilitiesMd} |";
            }
            
            if (arguments.CheckLatestMinor)
            {
                var minorVersionMd = package.LatestMinorVersion?.Url != null
                    ? $"[{EscapeMarkdown(package.LatestMinorVersion.Version ?? "N/A")}]({package.LatestMinorVersion.Url})"
                    : EscapeMarkdown(package.LatestMinorVersion?.Version ?? "N/A");
                var minorLicenseMd = FormatLicenseMarkdown(package.LatestMinorVersion?.License);
                var minorPublishedDateMd = EscapeMarkdown(package.LatestMinorVersion?.PublishedDate ?? "N/A");
                var minorDeprecatedMd = EscapeMarkdown(package.LatestMinorVersion?.Deprecated ?? "");
                var minorVulnerabilitiesMd = EscapeMarkdown(package.LatestMinorVersion?.Vulnerabilities ?? "");
                line += $" {minorVersionMd} | {minorLicenseMd} | {minorPublishedDateMd} | {minorDeprecatedMd} | {minorVulnerabilitiesMd} |";
            }
            
            line += $" {latestVersionMd} | {latestLicenseMd} | {latestPublishedDateMd} | {latestDeprecatedMd} | {latestVulnerabilitiesMd} | {EscapeMarkdown(package.Projects)} |";
            
            streamWriter.WriteLine(line);
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
