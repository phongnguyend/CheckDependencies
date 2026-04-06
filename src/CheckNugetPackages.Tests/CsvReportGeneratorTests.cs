namespace CheckNugetPackages.Tests;

public class CsvReportGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public CsvReportGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CsvReportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Generate_WritesHeader()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var args = new ParsedArguments(["./"], ["csv"], null, CheckLatest: true);

        CsvReportGenerator.Generate(filePath, [], [], args);

        var lines = File.ReadAllLines(filePath);
        var expectedHeader = "Name,Version,Resolved Version,Resolved License,Resolved Published Date,Resolved Deprecated,Resolved Vulnerabilities,Resolved Url,Latest Version,Latest License,Latest Published Date,Latest Deprecated,Latest Vulnerabilities,Latest Url,Projects";
        Assert.Equal(expectedHeader, lines[0]);
    }

    [Fact]
    public void Generate_WritesAllPackageRows()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "13.0.3", "ProjectA", new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null), new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null)),
            new("Serilog", "3.1.1", "ProjectB", new VersionEntry("3.1.1", "https://www.nuget.org/packages/Serilog/3.1.1", "Apache-2.0", "2023-11-09", null, null), new VersionEntry("4.0.0", "https://www.nuget.org/packages/Serilog/4.0.0", "Apache-2.0", "2024-06-01", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null, CheckLatest: true);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var lines = File.ReadAllLines(filePath);
        Assert.Equal(3, lines.Length); // header + 2 data rows
        Assert.Contains("Newtonsoft.Json", lines[1]);
        Assert.Contains("13.0.3", lines[1]);
        Assert.Contains("MIT", lines[1]);
        Assert.Contains("2023-03-08", lines[1]);
        Assert.Contains("ProjectA", lines[1]);
        Assert.Contains("Serilog", lines[2]);
    }

    [Fact]
    public void Generate_ExcludesIgnoredPackages()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("System.Text.Json", "8.0.0", "ProjectA", new VersionEntry("8.0.0", "https://www.nuget.org/packages/System.Text.Json/8.0.0", "MIT", "2023-11-14", null, null), new VersionEntry("8.0.0", "https://www.nuget.org/packages/System.Text.Json/8.0.0", "MIT", "2023-11-14", null, null)),
            new("Newtonsoft.Json", "13.0.3", "ProjectA", new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null), new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null, CheckLatest: true);

        CsvReportGenerator.Generate(filePath, packages, ["System."], args);

        var lines = File.ReadAllLines(filePath);
        Assert.Equal(2, lines.Length); // header + 1 data row
        Assert.Contains("Newtonsoft.Json", lines[1]);
    }

    [Fact]
    public void Generate_HandlesNullLicenseAndPublishedDate()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "ProjectA", new VersionEntry("1.0.0", "https://www.nuget.org/packages/SomePackage/1.0.0", null, null, null, null), new VersionEntry(null, null, null, null, null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null, CheckLatest: true);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var lines = File.ReadAllLines(filePath);
        Assert.Equal(2, lines.Length); // header + 1 data row
        Assert.Contains("\"\"", lines[1]);
    }

    [Fact]
    public void Generate_EmptyPackageList_CreatesFileWithHeader()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var args = new ParsedArguments(["./"], ["csv"], null);

        CsvReportGenerator.Generate(filePath, [], [], args);

        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.NotEmpty(content);
        Assert.Contains("Name,Version,Resolved Version", content);
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
            new("TestPkg", "2.0.0", "ProjX", new VersionEntry("2.0.0", "https://example.com", "Apache-2.0", "2024-01-15", null, null), new VersionEntry("3.0.0", "https://example.com/latest", "Apache-2.0", "2024-06-01", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null, CheckLatest: true);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var lines = File.ReadAllLines(filePath);
        var line = lines[1]; // second line is the data row (first line is header)
        Assert.Equal("TestPkg,2.0.0,\"2.0.0\",\"Apache-2.0\",\"2024-01-15\",\"\",\"\",\"https://example.com\",\"3.0.0\",\"Apache-2.0\",\"2024-06-01\",\"\",\"\",\"https://example.com/latest\",\"ProjX\"", line);
    }

    [Fact]
    public void Generate_WithoutCheckLatest_ExcludesLatestVersionColumns()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("TestPkg", "2.0.0", "ProjX", new VersionEntry("2.0.0", "https://example.com", "Apache-2.0", "2024-01-15", null, null), new VersionEntry("3.0.0", "https://example.com/latest", "Apache-2.0", "2024-06-01", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null, CheckLatest: false);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var lines = File.ReadAllLines(filePath);
        var header = lines[0];
        Assert.DoesNotContain("Latest Version", header);
        Assert.DoesNotContain("Latest License", header);
        Assert.Contains("Resolved Url", header);
        Assert.Contains("Resolved Version", header);
    }

    [Fact]
    public void Generate_WithCheckLatest_IncludesLatestVersionColumns()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("TestPkg", "2.0.0", "ProjX", new VersionEntry("2.0.0", "https://example.com", "Apache-2.0", "2024-01-15", null, null), new VersionEntry("3.0.0", "https://example.com/latest", "Apache-2.0", "2024-06-01", null, null)),
        };
        var args = new ParsedArguments(["./"], ["csv"], null, CheckLatest: true);

        CsvReportGenerator.Generate(filePath, packages, [], args);

        var lines = File.ReadAllLines(filePath);
        var header = lines[0];
        Assert.Contains("Latest Version", header);
        Assert.Contains("Latest License", header);
        Assert.Contains("Latest Url", header);
        Assert.Contains("Resolved Url", header);
    }
}
