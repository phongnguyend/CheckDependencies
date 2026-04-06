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

    private string GenerateAndRead(string reportTitle, List<PackageEntry> packages, List<string> ignoredPackages, bool checkLatest = true)
    {
        var filePath = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.md");
        var args = new ParsedArguments(["./"], ["md"], null, CheckLatest: checkLatest);
        MarkdownReportGenerator.Generate(filePath, reportTitle, packages, ignoredPackages, args);
        return File.ReadAllText(filePath);
    }

    [Fact]
    public void Generate_ContainsReportTitle()
    {
        var md = GenerateAndRead("NuGet Packages Report", [], [], checkLatest: true);
        Assert.Contains("# NuGet Packages Report", md);
    }

    [Fact]
    public void Generate_ContainsGeneratedOnLine()
    {
        var md = GenerateAndRead("Test Report", [], [], checkLatest: true);
        Assert.Contains("Generated on: ", md);
    }

    [Fact]
    public void Generate_ContainsTableHeaders()
    {
        var md = GenerateAndRead("Test Report", [], [], checkLatest: true);
        Assert.Contains("| Name | Version | Resolved Version | License | Published Date | Deprecated | Vulnerabilities | Latest Version | Latest License | Latest Published Date | Latest Deprecated | Latest Vulnerabilities | Projects |", md);
        Assert.Contains("| ---- | ------- | ---------------- | ------- | -------------- | ---------- | --------------- | -------------- | -------------- | --------------------- | ----------------- | ---------------------- | -------- |", md);
    }

    [Fact]
    public void Generate_RendersPackageRow()
    {
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "13.0.3", "ProjectA", new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null), new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

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
            new("System.Text.Json", "8.0.0", "ProjectA", new VersionEntry("8.0.0", "https://example.com", "MIT", "2023-11-14", null, null), new VersionEntry("8.0.0", "https://example.com/latest", "MIT", "2023-11-14", null, null)),
            new("Newtonsoft.Json", "13.0.3", "ProjectA", new VersionEntry("13.0.3", "https://example.com", "MIT", "2023-03-08", null, null), new VersionEntry("13.0.3", "https://example.com/latest", "MIT", "2023-03-08", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, ["System."], checkLatest: true);

        Assert.DoesNotContain("System.Text.Json", md);
        Assert.Contains("Newtonsoft.Json", md);
    }

    [Fact]
    public void Generate_NullLicense_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "ProjectA", new VersionEntry("1.0.0", "https://example.com", null, "2024-01-01", null, null), new VersionEntry("1.0.0", null, null, null, null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

        Assert.Contains("| N/A |", md);
    }

    [Fact]
    public void Generate_NullPublishedDate_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "ProjectA", new VersionEntry("1.0.0", "https://example.com", "MIT", null, null, null), new VersionEntry("1.0.0", null, "MIT", null, null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

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
            new("SomePackage", null, "ProjectA", new VersionEntry(null, "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", null, "MIT", "2024-01-01", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

        Assert.Contains("| N/A | [N/A](https://example.com) |", md);
    }

    [Fact]
    public void Generate_LicenseUrl_RendersAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "ProjectA", new VersionEntry("1.0.0", "https://example.com", "https://licenses.nuget.org/MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", null, "MIT", "2024-01-01", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

        Assert.Contains("[View License](https://licenses.nuget.org/MIT)", md);
    }

    [Fact]
    public void Generate_EscapesPipeInValues()
    {
        var packages = new List<PackageEntry>
        {
            new("Pkg|Name", "1.0.0", "Project|A", new VersionEntry("1.0.0", "https://example.com", "MIT|BSD", "2024-01-01", null, null), new VersionEntry("1.0.0", null, "MIT|BSD", "2024-01-01", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

        Assert.Contains("Pkg\\|Name", md);
        Assert.Contains("Project\\|A", md);
        Assert.Contains("MIT\\|BSD", md);
    }

    [Fact]
    public void Generate_CreatesDirectoryIfNotExists()
    {
        var filePath = Path.Combine(_tempDir, "subdir", "report.md");
        var args = new ParsedArguments(["./"], ["md"], null, CheckLatest: true);
        MarkdownReportGenerator.Generate(filePath, "Test", [], [], args);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Generate_EmptyPackageList_OnlyContainsHeaderAndTableStructure()
    {
        var md = GenerateAndRead("Test Report", [], [], checkLatest: true);
        var lines = md.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Title, Generated on, table header, table separator = 4 lines
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public void Generate_RendersLatestVersionAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "12.0.3", "ProjectA", new VersionEntry("12.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/12.0.3", "MIT", "2019-11-09", null, null), new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

        Assert.Contains("[13.0.3](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3)", md);
    }

    [Fact]
    public void Generate_WithoutCheckLatest_ExcludesLatestVersionColumns()
    {
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "12.0.3", "ProjectA", new VersionEntry("12.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/12.0.3", "MIT", "2019-11-09", null, null), new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: false);

        Assert.DoesNotContain("Latest Version", md);
        Assert.DoesNotContain("Latest License", md);
        Assert.Contains("Resolved Version", md);
    }

    [Fact]
    public void Generate_WithCheckLatest_IncludesLatestVersionColumns()
    {
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "12.0.3", "ProjectA", new VersionEntry("12.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/12.0.3", "MIT", "2019-11-09", null, null), new VersionEntry("13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null)),
        };

        var md = GenerateAndRead("Test Report", packages, [], checkLatest: true);

        Assert.Contains("Latest Version", md);
        Assert.Contains("Latest License", md);
        Assert.Contains("Resolved Version", md);
    }
}
