namespace CheckNugetPackages;

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
            streamWriter.WriteLine($"{package.Name},{package.Version},\"{licenseValue}\",\"{publishedDateValue}\",\"{package.Url}\",\"{package.Projects}\"");
        }
    }
}
