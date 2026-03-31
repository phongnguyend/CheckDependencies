namespace CheckNugetPackages;

public static class HtmlReportGenerator
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
        streamWriter.WriteLine("<!DOCTYPE html>");
        streamWriter.WriteLine("<html>");
        streamWriter.WriteLine("<head>");
        streamWriter.WriteLine($"    <title>{System.Net.WebUtility.HtmlEncode(reportTitle)}</title>");
        streamWriter.WriteLine("    <style>");
        streamWriter.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
        streamWriter.WriteLine("        table { border-collapse: collapse; width: 100%; }");
        streamWriter.WriteLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        streamWriter.WriteLine("        th { background-color: #f2f2f2; font-weight: bold; }");
        streamWriter.WriteLine("        tr:nth-child(even) { background-color: #f9f9f9; }");
        streamWriter.WriteLine("        a { color: #0366d6; text-decoration: none; }");
        streamWriter.WriteLine("        a:hover { text-decoration: underline; }");
        streamWriter.WriteLine("        .package-name { font-weight: bold; }");
        streamWriter.WriteLine("        .version { font-family: monospace; }");
        streamWriter.WriteLine("        .version a { color: #0366d6; font-family: monospace; }");
        streamWriter.WriteLine("        .license { font-size: 0.9em; }");
        streamWriter.WriteLine("        .published-date { font-size: 0.9em; font-family: monospace; }");
        streamWriter.WriteLine("        .projects { font-size: 0.9em; color: #666; }");
        streamWriter.WriteLine("        .different { font-weight: bold; }");
        streamWriter.WriteLine("        .deprecated { color: #b08800; }");
        streamWriter.WriteLine("        .vulnerable { color: #d73a49; }");
        streamWriter.WriteLine("        .icon-deprecated { cursor: help; font-size: 1.2em; }");
        streamWriter.WriteLine("        .icon-vulnerable { cursor: help; font-size: 1.2em; }");
        streamWriter.WriteLine("    </style>");
        streamWriter.WriteLine("</head>");
        streamWriter.WriteLine("<body>");
        streamWriter.WriteLine($"    <h1>{System.Net.WebUtility.HtmlEncode(reportTitle)}</h1>");
        streamWriter.WriteLine("    <p>Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + "</p>");
        streamWriter.WriteLine("    <table>");
        streamWriter.WriteLine("        <thead>");
        streamWriter.WriteLine("            <tr>");
        streamWriter.WriteLine("                <th>Name</th>");
        streamWriter.WriteLine("                <th>Version</th>");
        streamWriter.WriteLine("                <th>Resolved Version</th>");
        streamWriter.WriteLine("                <th>License</th>");
        streamWriter.WriteLine("                <th>Published Date</th>");
        streamWriter.WriteLine("                <th>Deprecated</th>");
        streamWriter.WriteLine("                <th>Vulnerabilities</th>");
        streamWriter.WriteLine("                <th>Latest Version</th>");
        streamWriter.WriteLine("                <th>Latest License</th>");
        streamWriter.WriteLine("                <th>Latest Published Date</th>");
        streamWriter.WriteLine("                <th>Latest Deprecated</th>");
        streamWriter.WriteLine("                <th>Latest Vulnerabilities</th>");
        streamWriter.WriteLine("                <th>Projects</th>");
        streamWriter.WriteLine("            </tr>");
        streamWriter.WriteLine("        </thead>");
        streamWriter.WriteLine("        <tbody>");

        foreach (var package in packages)
        {
            if (ignoredPackages.Any(package.Name.StartsWith))
            {
                continue;
            }

            var licenseHtml = FormatLicenseHtml(package.License);
            var publishedDateHtml = System.Net.WebUtility.HtmlEncode(package.PublishedDate ?? "N/A");
            var deprecatedHtml = FormatDeprecatedHtml(package.Deprecated);
            var vulnerabilitiesHtml = FormatVulnerabilitiesHtml(package.Vulnerabilities);
            var latestVersionHtml = package.LatestUrl != null
                ? $"<a href=\"{package.LatestUrl}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(package.LatestVersion ?? "N/A")}</a>"
                : System.Net.WebUtility.HtmlEncode(package.LatestVersion ?? "N/A");
            var latestLicenseHtml = FormatLicenseHtml(package.LatestLicense);
            var latestPublishedDateHtml = System.Net.WebUtility.HtmlEncode(package.LatestPublishedDate ?? "N/A");
            var latestDeprecatedHtml = FormatDeprecatedHtml(package.LatestDeprecated);
            var latestVulnerabilitiesHtml = FormatVulnerabilitiesHtml(package.LatestVulnerabilities);

            var versionDiffers = !string.Equals(package.ResolvedVersion, package.LatestVersion, StringComparison.OrdinalIgnoreCase);
            var licenseDiffers = !string.Equals(package.License, package.LatestLicense, StringComparison.OrdinalIgnoreCase);
            var publishedDateDiffers = !string.Equals(package.PublishedDate, package.LatestPublishedDate, StringComparison.OrdinalIgnoreCase);

            if (versionDiffers)
                latestVersionHtml = $"<strong>{latestVersionHtml}</strong>";
            if (licenseDiffers)
                latestLicenseHtml = $"<strong>{latestLicenseHtml}</strong>";
            if (publishedDateDiffers)
                latestPublishedDateHtml = $"<strong>{latestPublishedDateHtml}</strong>";

            streamWriter.WriteLine("            <tr>");
            streamWriter.WriteLine($"                <td class=\"package-name\">{System.Net.WebUtility.HtmlEncode(package.Name)}</td>");
            streamWriter.WriteLine($"                <td class=\"version\">{System.Net.WebUtility.HtmlEncode(package.Version ?? "N/A")}</td>");
            streamWriter.WriteLine($"                <td class=\"version\"><a href=\"{package.Url}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(package.ResolvedVersion ?? "N/A")}</a></td>");
            streamWriter.WriteLine($"                <td class=\"license\">{licenseHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"published-date\">{publishedDateHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"deprecated\">{deprecatedHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"vulnerable\">{vulnerabilitiesHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"version\">{latestVersionHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"license\">{latestLicenseHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"published-date\">{latestPublishedDateHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"deprecated\">{latestDeprecatedHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"vulnerable\">{latestVulnerabilitiesHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"projects\">{System.Net.WebUtility.HtmlEncode(package.Projects)}</td>");
            streamWriter.WriteLine("            </tr>");
        }

        streamWriter.WriteLine("        </tbody>");
        streamWriter.WriteLine("    </table>");
        streamWriter.WriteLine("</body>");
        streamWriter.WriteLine("</html>");
    }

    private static string FormatLicenseHtml(string? license)
    {
        if (string.IsNullOrWhiteSpace(license))
            return "N/A";

        if (license.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            license.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return $"<a href=\"{System.Net.WebUtility.HtmlEncode(license)}\" target=\"_blank\">View License</a>";
        }

        return System.Net.WebUtility.HtmlEncode(license);
    }

    private static string FormatDeprecatedHtml(string? deprecated)
    {
        if (string.IsNullOrWhiteSpace(deprecated))
            return "";

        var encoded = System.Net.WebUtility.HtmlEncode(deprecated);
        return $"<span class=\"icon-deprecated\" title=\"{encoded}\">&#9888;&#65039;</span>";
    }

    private static string FormatVulnerabilitiesHtml(string? vulnerabilities)
    {
        if (string.IsNullOrWhiteSpace(vulnerabilities))
            return "";

        var encoded = System.Net.WebUtility.HtmlEncode(vulnerabilities);
        return $"<span class=\"icon-vulnerable\" title=\"{encoded}\">&#128680;</span>";
    }
}
