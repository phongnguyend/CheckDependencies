namespace CheckNpmPackages.Tests;

public class CsvReportGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public CsvReportGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"NpmCsvReportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Generate_WritesAllPackageRows()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("lodash", "4.17.21", "my-app", new VersionEntry("4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", null, null), new VersionEntry("4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", null, null)),
            new("express", "4.18.2", "my-api", new VersionEntry("4.18.2", "https://www.npmjs.com/package/express/v/4.18.2", "MIT", "2023-10-11", null, null), new VersionEntry("4.21.0", "https://www.npmjs.com/package/express/v/4.21.0", "MIT", "2024-09-11", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var lines = File.ReadAllLines(filePath);
        Assert.Equal(2, lines.Length);
        Assert.Contains("lodash", lines[0]);
        Assert.Contains("4.17.21", lines[0]);
        Assert.Contains("MIT", lines[0]);
        Assert.Contains("2021-02-20", lines[0]);
        Assert.Contains("express", lines[1]);
    }

    [Fact]
    public void Generate_ExcludesIgnoredPackages()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("@types/node", "20.0.0", "my-app", new VersionEntry("20.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("20.1.0", "https://example.com/latest", "MIT", "2024-02-01", null, null)),
            new("lodash", "4.17.21", "my-app", new VersionEntry("4.17.21", "https://example.com", "MIT", "2021-02-20", null, null), new VersionEntry("4.17.21", "https://example.com/latest", "MIT", "2021-02-20", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null);

        CsvReportGenerator.Generate(filePath, packages, ["@types/"], args);

        var lines = File.ReadAllLines(filePath);
        Assert.Single(lines);
        Assert.Contains("lodash", lines[0]);
    }

    [Fact]
    public void Generate_HandlesNullLicenseAndPublishedDate()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", null, null, null, null), new VersionEntry(null, null, null, null, null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var lines = File.ReadAllLines(filePath);
        Assert.Single(lines);
        Assert.Contains("\"\"", lines[0]);
    }

    [Fact]
    public void Generate_EmptyPackageList_CreatesEmptyFile()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var args = new ParsedArguments(["./"], ["csv"], null);

        CsvReportGenerator.Generate(filePath, [], [], args);

        Assert.True(File.Exists(filePath));
        Assert.Empty(File.ReadAllText(filePath));
    }

    [Fact]
    public void Generate_CreatesDirectoryIfNotExists()
    {
        var filePath = Path.Combine(_tempDir, "subdir", "packages.csv");
        var args = new ParsedArguments(["./"], ["csv"], null);

        CsvReportGenerator.Generate(filePath, [], [], args);

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Generate_CsvRowFormat()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("test-pkg", "2.0.0", "proj-x", new VersionEntry("2.0.0", "https://example.com", "Apache-2.0", "2024-01-15", null, null), new VersionEntry("3.0.0", "https://example.com/latest", "Apache-2.0", "2024-06-01", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var line = File.ReadAllLines(filePath)[0];
        Assert.Equal("test-pkg,2.0.0,\"2.0.0\",\"Apache-2.0\",\"2024-01-15\",\"\",\"\",\"3.0.0\",\"Apache-2.0\",\"2024-06-01\",\"\",\"\",\"https://example.com\",\"https://example.com/latest\",\"proj-x\"", line);
    }
}
