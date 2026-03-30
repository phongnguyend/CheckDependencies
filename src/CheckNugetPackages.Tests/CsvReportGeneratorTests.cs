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
    public void Generate_WritesAllPackageRows()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "13.0.3", "ProjectA", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08"),
            new("Serilog", "3.1.1", "ProjectB", "https://www.nuget.org/packages/Serilog/3.1.1", "Apache-2.0", "2023-11-09"),
        };

        CsvReportGenerator.Generate(filePath, packages, []);

        var lines = File.ReadAllLines(filePath);
        Assert.Equal(2, lines.Length);
        Assert.Contains("Newtonsoft.Json", lines[0]);
        Assert.Contains("13.0.3", lines[0]);
        Assert.Contains("MIT", lines[0]);
        Assert.Contains("2023-03-08", lines[0]);
        Assert.Contains("ProjectA", lines[0]);
        Assert.Contains("Serilog", lines[1]);
    }

    [Fact]
    public void Generate_ExcludesIgnoredPackages()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("System.Text.Json", "8.0.0", "ProjectA", "https://www.nuget.org/packages/System.Text.Json/8.0.0", "MIT", "2023-11-14"),
            new("Newtonsoft.Json", "13.0.3", "ProjectA", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08"),
        };

        CsvReportGenerator.Generate(filePath, packages, ["System."]);

        var lines = File.ReadAllLines(filePath);
        Assert.Single(lines);
        Assert.Contains("Newtonsoft.Json", lines[0]);
    }

    [Fact]
    public void Generate_HandlesNullLicenseAndPublishedDate()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "ProjectA", "https://www.nuget.org/packages/SomePackage/1.0.0", null, null),
        };

        CsvReportGenerator.Generate(filePath, packages, []);

        var lines = File.ReadAllLines(filePath);
        Assert.Single(lines);
        Assert.Contains("\"\"", lines[0]);
    }

    [Fact]
    public void Generate_EmptyPackageList_CreatesEmptyFile()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");

        CsvReportGenerator.Generate(filePath, [], []);

        Assert.True(File.Exists(filePath));
        Assert.Empty(File.ReadAllText(filePath));
    }

    [Fact]
    public void Generate_CreatesDirectoryIfNotExists()
    {
        var filePath = Path.Combine(_tempDir, "subdir", "packages.csv");

        CsvReportGenerator.Generate(filePath, [], []);

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Generate_CsvRowFormat()
    {
        var filePath = Path.Combine(_tempDir, "packages.csv");
        var packages = new List<PackageEntry>
        {
            new("TestPkg", "2.0.0", "ProjX", "https://example.com", "Apache-2.0", "2024-01-15"),
        };

        CsvReportGenerator.Generate(filePath, packages, []);

        var line = File.ReadAllLines(filePath)[0];
        Assert.Equal("TestPkg,2.0.0,\"Apache-2.0\",\"2024-01-15\",\"https://example.com\",\"ProjX\"", line);
    }
}
