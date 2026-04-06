namespace CheckNpmPackages;

public static class CsvReportGenerator
{
    public static void Generate(string filePath, List<PackageEntry> packages, List<string> ignoredPackages, ParsedArguments arguments)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = File.Open(filePath, FileMode.Create);
        using var streamWriter = new StreamWriter(fileStream);
        
        var headerLine = "Name,Version,Resolved Version,Resolved License,Resolved Published Date,Resolved Deprecated,Resolved Vulnerabilities";
        if (arguments.CheckLatestPatch)
            headerLine += ",Latest Patch Version,Latest Patch License,Latest Patch Published Date,Latest Patch Deprecated,Latest Patch Vulnerabilities,Latest Patch Url";
        if (arguments.CheckLatestMinor)
            headerLine += ",Latest Minor Version,Latest Minor License,Latest Minor Published Date,Latest Minor Deprecated,Latest Minor Vulnerabilities,Latest Minor Url";
        headerLine += ",Latest Version,Latest License,Latest Published Date,Latest Deprecated,Latest Vulnerabilities,Resolved Url,Latest Url,Projects";
        
        streamWriter.WriteLine(headerLine);
        
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
            
            var line = $"{package.Name},{package.Version},\"{resolvedVersionValue}\",\"{licenseValue}\",\"{publishedDateValue}\",\"{deprecatedValue}\",\"{vulnerabilitiesValue}\"";
            
            if (arguments.CheckLatestPatch && package.LatestPatchVersion != null)
            {
                var latestPatchVersionValue = package.LatestPatchVersion.Version ?? "";
                var latestPatchLicenseValue = package.LatestPatchVersion.License ?? "";
                var latestPatchPublishedDateValue = package.LatestPatchVersion.PublishedDate ?? "";
                var latestPatchDeprecatedValue = package.LatestPatchVersion.Deprecated ?? "";
                var latestPatchVulnerabilitiesValue = package.LatestPatchVersion.Vulnerabilities ?? "";
                var latestPatchUrlValue = package.LatestPatchVersion.Url ?? "";
                line += $",\"{latestPatchVersionValue}\",\"{latestPatchLicenseValue}\",\"{latestPatchPublishedDateValue}\",\"{latestPatchDeprecatedValue}\",\"{latestPatchVulnerabilitiesValue}\",\"{latestPatchUrlValue}\"";
            }
            
            if (arguments.CheckLatestMinor && package.LatestMinorVersion != null)
            {
                var latestMinorVersionValue = package.LatestMinorVersion.Version ?? "";
                var latestMinorLicenseValue = package.LatestMinorVersion.License ?? "";
                var latestMinorPublishedDateValue = package.LatestMinorVersion.PublishedDate ?? "";
                var latestMinorDeprecatedValue = package.LatestMinorVersion.Deprecated ?? "";
                var latestMinorVulnerabilitiesValue = package.LatestMinorVersion.Vulnerabilities ?? "";
                var latestMinorUrlValue = package.LatestMinorVersion.Url ?? "";
                line += $",\"{latestMinorVersionValue}\",\"{latestMinorLicenseValue}\",\"{latestMinorPublishedDateValue}\",\"{latestMinorDeprecatedValue}\",\"{latestMinorVulnerabilitiesValue}\",\"{latestMinorUrlValue}\"";
            }
            
            line += $",\"{latestVersionValue}\",\"{latestLicenseValue}\",\"{latestPublishedDateValue}\",\"{latestDeprecatedValue}\",\"{latestVulnerabilitiesValue}\",\"{package.ResolvedVersion.Url}\",\"{latestUrlValue}\",\"{package.Projects}\"";
            
            streamWriter.WriteLine(line);
        }
    }
}
