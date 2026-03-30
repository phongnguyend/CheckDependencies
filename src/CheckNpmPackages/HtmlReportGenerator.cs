namespace CheckNpmPackages;

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
        streamWriter.WriteLine("                <th>License</th>");
        streamWriter.WriteLine("                <th>Published Date</th>");
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

            streamWriter.WriteLine("            <tr>");
            streamWriter.WriteLine($"                <td class=\"package-name\">{System.Net.WebUtility.HtmlEncode(package.Name)}</td>");
            streamWriter.WriteLine($"                <td class=\"version\"><a href=\"{package.Url}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(package.Version ?? "N/A")}</a></td>");
            streamWriter.WriteLine($"                <td class=\"license\">{licenseHtml}</td>");
            streamWriter.WriteLine($"                <td class=\"published-date\">{publishedDateHtml}</td>");
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
}
