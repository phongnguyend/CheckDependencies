namespace CheckNpmPackages.Tests;

public class HtmlReportGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public HtmlReportGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"NpmHtmlReportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string GenerateAndRead(string reportTitle, List<PackageEntry> packages, List<string> ignoredPackages)
    {
        var filePath = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.html");
        HtmlReportGenerator.Generate(filePath, reportTitle, packages, ignoredPackages);
        return File.ReadAllText(filePath);
    }

    [Fact]
    public void Generate_ContainsDoctype()
    {
        var html = GenerateAndRead("Test Report", [], []);
        Assert.StartsWith("<!DOCTYPE html>", html);
    }

    [Fact]
    public void Generate_ContainsReportTitle()
    {
        var html = GenerateAndRead("npm Packages Report", [], []);
        Assert.Contains("<title>npm Packages Report</title>", html);
        Assert.Contains("<h1>npm Packages Report</h1>", html);
    }

    [Fact]
    public void Generate_ContainsGeneratedOnLine()
    {
        var html = GenerateAndRead("Test Report", [], []);
        Assert.Contains("<p>Generated on: ", html);
    }

    [Fact]
    public void Generate_ContainsTableHeaders()
    {
        var html = GenerateAndRead("Test Report", [], []);
        Assert.Contains("<th>Name</th>", html);
        Assert.Contains("<th>Version</th>", html);
        Assert.Contains("<th>License</th>", html);
        Assert.Contains("<th>Published Date</th>", html);
        Assert.Contains("<th>Latest Version</th>", html);
        Assert.Contains("<th>Latest License</th>", html);
        Assert.Contains("<th>Latest Published Date</th>", html);
        Assert.Contains("<th>Projects</th>", html);
    }

    [Fact]
    public void Generate_RendersPackageRow()
    {
        var packages = new List<PackageEntry>
        {
            new("lodash", "4.17.21", "my-app", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", "4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20"),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("lodash", html);
        Assert.Contains("4.17.21", html);
        Assert.Contains("MIT", html);
        Assert.Contains("2021-02-20", html);
        Assert.Contains("my-app", html);
        Assert.Contains("https://www.npmjs.com/package/lodash/v/4.17.21", html);
    }

    [Fact]
    public void Generate_ExcludesIgnoredPackages()
    {
        var packages = new List<PackageEntry>
        {
            new("@types/node", "20.0.0", "my-app", "https://example.com", "MIT", "2024-01-01", "20.1.0", "https://example.com/latest", "MIT", "2024-02-01"),
            new("lodash", "4.17.21", "my-app", "https://example.com", "MIT", "2021-02-20", "4.17.21", "https://example.com/latest", "MIT", "2021-02-20"),
        };

        var html = GenerateAndRead("Test Report", packages, ["@types/"]);

        Assert.DoesNotContain("@types/node", html);
        Assert.Contains("lodash", html);
    }

    [Fact]
    public void Generate_NullLicense_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", "https://example.com", null, "2024-01-01", "1.0.0", null, null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"license\">N/A</td>", html);
    }

    [Fact]
    public void Generate_NullPublishedDate_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", "https://example.com", "MIT", null, "1.0.0", null, "MIT", null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"published-date\">N/A</td>", html);
    }

    [Fact]
    public void Generate_NullVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", null, "my-app", "https://example.com", "MIT", "2024-01-01", "1.0.0", null, "MIT", "2024-01-01"),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains(">N/A</a>", html);
    }

    [Fact]
    public void Generate_LicenseUrl_RendersAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", "https://example.com", "https://opensource.org/licenses/MIT", "2024-01-01", "1.0.0", null, "MIT", "2024-01-01"),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<a href=\"https://opensource.org/licenses/MIT\" target=\"_blank\">View License</a>", html);
    }

    [Fact]
    public void Generate_HtmlEncodesSpecialCharacters()
    {
        var packages = new List<PackageEntry>
        {
            new("pkg<name>", "1.0.0", "project&a", "https://example.com", "license&co", "2024-01-01", "1.0.0", null, "license&co", "2024-01-01"),
        };

        var html = GenerateAndRead("Title<>&", packages, []);

        Assert.Contains("pkg&lt;name&gt;", html);
        Assert.Contains("project&amp;a", html);
        Assert.Contains("license&amp;co", html);
        Assert.Contains("Title&lt;&gt;&amp;", html);
    }

    [Fact]
    public void Generate_ContainsStyleBlock()
    {
        var html = GenerateAndRead("Test Report", [], []);
        Assert.Contains("<style>", html);
        Assert.Contains(".package-name", html);
        Assert.Contains(".published-date", html);
    }

    [Fact]
    public void Generate_CreatesDirectoryIfNotExists()
    {
        var filePath = Path.Combine(_tempDir, "subdir", "report.html");
        HtmlReportGenerator.Generate(filePath, "Test", [], []);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Generate_RendersLatestVersionAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("lodash", "4.17.20", "my-app", "https://www.npmjs.com/package/lodash/v/4.17.20", "MIT", "2020-10-27", "4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20"),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<a href=\"https://www.npmjs.com/package/lodash/v/4.17.21\" target=\"_blank\">4.17.21</a>", html);
        Assert.Contains("2021-02-20", html);
    }

    [Fact]
    public void Generate_NullLatestVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", "https://example.com", "MIT", "2024-01-01", null, null, null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        // Latest version N/A, latest license N/A, latest published date N/A
        var latestVersionCount = CountOccurrences(html, "N/A");
        Assert.True(latestVersionCount >= 3);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
