namespace CheckNpmPackages.Tests;

public class MarkdownReportGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public MarkdownReportGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"NpmMdReportTests_{Guid.NewGuid():N}");
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
        var md = GenerateAndRead("npm Packages Report", [], []);
        Assert.Contains("# npm Packages Report", md);
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
        Assert.Contains("| Name | Version | License | Published Date | Projects |", md);
        Assert.Contains("| ---- | ------- | ------- | -------------- | -------- |", md);
    }

    [Fact]
    public void Generate_RendersPackageRow()
    {
        var packages = new List<PackageEntry>
        {
            new("lodash", "4.17.21", "my-app", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("lodash", md);
        Assert.Contains("[4.17.21](https://www.npmjs.com/package/lodash/v/4.17.21)", md);
        Assert.Contains("MIT", md);
        Assert.Contains("2021-02-20", md);
        Assert.Contains("my-app", md);
    }

    [Fact]
    public void Generate_ExcludesIgnoredPackages()
    {
        var packages = new List<PackageEntry>
        {
            new("@types/node", "20.0.0", "my-app", "https://example.com", "MIT", "2024-01-01"),
            new("lodash", "4.17.21", "my-app", "https://example.com", "MIT", "2021-02-20"),
        };

        var md = GenerateAndRead("Test Report", packages, ["@types/"]);

        Assert.DoesNotContain("@types/node", md);
        Assert.Contains("lodash", md);
    }

    [Fact]
    public void Generate_NullLicense_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", "https://example.com", null, "2024-01-01"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("| N/A |", md);
    }

    [Fact]
    public void Generate_NullPublishedDate_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", "https://example.com", "MIT", null),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        var lines = md.Split('\n');
        var dataLine = lines.First(l => l.Contains("some-pkg"));
        Assert.Contains("| N/A |", dataLine);
    }

    [Fact]
    public void Generate_NullVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", null, "my-app", "https://example.com", "MIT", "2024-01-01"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("[N/A](https://example.com)", md);
    }

    [Fact]
    public void Generate_LicenseUrl_RendersAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", "https://example.com", "https://opensource.org/licenses/MIT", "2024-01-01"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("[View License](https://opensource.org/licenses/MIT)", md);
    }

    [Fact]
    public void Generate_EscapesPipeInValues()
    {
        var packages = new List<PackageEntry>
        {
            new("pkg|name", "1.0.0", "project|a", "https://example.com", "MIT|BSD", "2024-01-01"),
        };

        var md = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("pkg\\|name", md);
        Assert.Contains("project\\|a", md);
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
}
