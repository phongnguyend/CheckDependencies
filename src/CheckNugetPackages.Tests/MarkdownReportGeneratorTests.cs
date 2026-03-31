namespace CheckNugetPackages.Tests;

public class MarkdownReportGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public MarkdownReportGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"MdReportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string GenerateAndRead(string reportTitle, List<PackageEntry> packages, List<string> ignoredPackages)
    {
        var filePath = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.md");
        MarkdownReportGenerator.Generate(filePath, reportTitle, packages, ignoredPackages);
        return File.ReadAllText(filePath);
    }

    [Fact]
    public void Generate_ContainsReportTitle()
    {
        var md = GenerateAndRead("NuGet Packages Report", [], []);
        Assert.Contains("# NuGet Packages Report", md);
    }

    [Fact]
    public void Generate_ContainsGeneratedOnLine()
    {
        var md = GenerateAndRead("Test Report", [], []);
        Assert.Contains("Generated on: ", md);
    }

    [Fact]
    public void Generate_ContainsTableHeaders()
    {
        var md = GenerateAndRead("Test Report", [], []);
        Assert.Contains("| Name | Version | Resolved Version | License | Published Date | Latest Version | Latest License | Latest Published Date | Projects |", md);
        Assert.Contains("| ---- | ------- | ---------------- | ------- | -------------- | -------------- | -------------- | --------------------- | -------- |", md);
    }

    [Fact]
    public void Generate_RendersPackageRow()
    {
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "13.0.3", "13.0.3", "ProjectA", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", "13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("Newtonsoft.Json", md);
        Assert.Contains("[13.0.3](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3)", md);
        Assert.Contains("MIT", md);
        Assert.Contains("2023-03-08", md);
        Assert.Contains("ProjectA", md);
    }

    [Fact]
    public void Generate_ExcludesIgnoredPackages()
    {
        var packages = new List<PackageEntry>
        {
            new("System.Text.Json", "8.0.0", "8.0.0", "ProjectA", "https://example.com", "MIT", "2023-11-14", "8.0.0", "https://example.com/latest", "MIT", "2023-11-14"),
            new("Newtonsoft.Json", "13.0.3", "13.0.3", "ProjectA", "https://example.com", "MIT", "2023-03-08", "13.0.3", "https://example.com/latest", "MIT", "2023-03-08"),
        };

        var md = GenerateAndRead("Test Report", packages, ["System."]);

        Assert.DoesNotContain("System.Text.Json", md);
        Assert.Contains("Newtonsoft.Json", md);
    }

    [Fact]
    public void Generate_NullLicense_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", null, "2024-01-01", "1.0.0", null, null, null),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("| N/A |", md);
    }

    [Fact]
    public void Generate_NullPublishedDate_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", null, "1.0.0", null, "MIT", null),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        // Published date N/A should appear between two pipes
        var lines = md.Split('\n');
        var dataLine = lines.First(l => l.Contains("SomePackage"));
        Assert.Contains("| N/A |", dataLine);
    }

    [Fact]
    public void Generate_NullVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", null, null, "ProjectA", "https://example.com", "MIT", "2024-01-01", "1.0.0", null, "MIT", "2024-01-01"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("| N/A | [N/A](https://example.com) |", md);
    }

    [Fact]
    public void Generate_LicenseUrl_RendersAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "https://licenses.nuget.org/MIT", "2024-01-01", "1.0.0", null, "MIT", "2024-01-01"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("[View License](https://licenses.nuget.org/MIT)", md);
    }

    [Fact]
    public void Generate_EscapesPipeInValues()
    {
        var packages = new List<PackageEntry>
        {
            new("Pkg|Name", "1.0.0", "1.0.0", "Project|A", "https://example.com", "MIT|BSD", "2024-01-01", "1.0.0", null, "MIT|BSD", "2024-01-01"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("Pkg\\|Name", md);
        Assert.Contains("Project\\|A", md);
        Assert.Contains("MIT\\|BSD", md);
    }

    [Fact]
    public void Generate_CreatesDirectoryIfNotExists()
    {
        var filePath = Path.Combine(_tempDir, "subdir", "report.md");
        MarkdownReportGenerator.Generate(filePath, "Test", [], []);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Generate_EmptyPackageList_OnlyContainsHeaderAndTableStructure()
    {
        var md = GenerateAndRead("Test Report", [], []);
        var lines = md.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Title, Generated on, table header, table separator = 4 lines
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public void Generate_RendersLatestVersionAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "12.0.3", "12.0.3", "ProjectA", "https://www.nuget.org/packages/Newtonsoft.Json/12.0.3", "MIT", "2019-11-09", "13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("[13.0.3](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3)", md);
    }
}
