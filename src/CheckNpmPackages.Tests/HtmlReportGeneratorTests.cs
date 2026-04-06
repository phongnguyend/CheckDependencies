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
        var args = new ParsedArguments(["./"], ["html"], null);
        HtmlReportGenerator.Generate(filePath, reportTitle, packages, ignoredPackages, args);
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
        Assert.Contains("<th rowspan=\"2\">Name</th>", html);
        Assert.Contains("<th rowspan=\"2\">Version</th>", html);
        Assert.Contains("<th colspan=\"5\">Current Resolved Version</th>", html);
        Assert.Contains("<th colspan=\"5\">Latest Version</th>", html);
        Assert.Contains("<th rowspan=\"2\">Projects</th>", html);
        Assert.Contains("<th>Version</th>", html);
        Assert.Contains("<th>License</th>", html);
        Assert.Contains("<th>Published Date</th>", html);
        Assert.Contains("<th>Deprecated</th>", html);
        Assert.Contains("<th>Vulnerable</th>", html);
    }

    [Fact]
    public void Generate_RendersPackageRow()
    {
        var packages = new List<PackageEntry>
        {
            new("lodash", "4.17.21", "my-app", new VersionEntry("4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", null, null), new VersionEntry("4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", null, null)),
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
            new("@types/node", "20.0.0", "my-app", new VersionEntry("20.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("20.1.0", "https://example.com/latest", "MIT", "2024-02-01", null, null)),
            new("lodash", "4.17.21", "my-app", new VersionEntry("4.17.21", "https://example.com", "MIT", "2021-02-20", null, null), new VersionEntry("4.17.21", "https://example.com/latest", "MIT", "2021-02-20", null, null)),
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
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", null, "2024-01-01", null, null), new VersionEntry("1.0.0", null, null, null, null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"license\">N/A</td>", html);
    }

    [Fact]
    public void Generate_NullPublishedDate_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", null, null, null), new VersionEntry("1.0.0", null, "MIT", null, null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"published-date\">N/A</td>", html);
    }

    [Fact]
    public void Generate_NullVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", null, "my-app", new VersionEntry(null, "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", null, "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version\">N/A</td>", html);
        Assert.Contains(">N/A</a>", html);
    }

    [Fact]
    public void Generate_LicenseUrl_RendersAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "https://opensource.org/licenses/MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", null, "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<a href=\"https://opensource.org/licenses/MIT\" target=\"_blank\">View License</a>", html);
    }

    [Fact]
    public void Generate_HtmlEncodesSpecialCharacters()
    {
        var packages = new List<PackageEntry>
        {
            new("pkg<name>", "1.0.0", "project&a", new VersionEntry("1.0.0", "https://example.com", "license&co", "2024-01-01", null, null), new VersionEntry("1.0.0", null, "license&co", "2024-01-01", null, null)),
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
        Assert.Contains(".different", html);
        Assert.Contains(".deprecated", html);
        Assert.Contains(".vulnerable", html);
        Assert.Contains(".icon-deprecated", html);
        Assert.Contains(".icon-vulnerable", html);
        Assert.Contains(".version-deprecated", html);
        Assert.Contains(".version-vulnerable", html);
    }

    [Fact]
    public void Generate_CreatesDirectoryIfNotExists()
    {
        var filePath = Path.Combine(_tempDir, "subdir", "report.html");
        var args = new ParsedArguments(["./"], ["html"], null);
        HtmlReportGenerator.Generate(filePath, "Test", [], [], args);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Generate_RendersLatestVersionAsLink()
    {
        var packages = new List<PackageEntry>
        {
            new("lodash", "4.17.20", "my-app", new VersionEntry("4.17.20", "https://www.npmjs.com/package/lodash/v/4.17.20", "MIT", "2020-10-27", null, null), new VersionEntry("4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<strong><a href=\"https://www.npmjs.com/package/lodash/v/4.17.21\" target=\"_blank\">4.17.21</a></strong>", html);
        Assert.Contains("<strong>2021-02-20</strong>", html);
    }

    [Fact]
    public void Generate_NullLatestVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry(null, null, null, null, null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        var latestVersionCount = CountOccurrences(html, "N/A");
        Assert.True(latestVersionCount >= 3);
    }

    [Fact]
    public void Generate_LatestVersionSameAsCurrent_NoBold()
    {
        var packages = new List<PackageEntry>
        {
            new("lodash", "4.17.21", "my-app", new VersionEntry("4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", null, null), new VersionEntry("4.17.21", "https://www.npmjs.com/package/lodash/v/4.17.21", "MIT", "2021-02-20", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.DoesNotContain("<strong>", html);
    }

    [Fact]
    public void Generate_LatestLicenseDiffers_RendersBold()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", "https://example.com/latest", "Apache-2.0", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<strong>Apache-2.0</strong>", html);
        Assert.DoesNotContain("<strong>1.0.0", html);
        Assert.DoesNotContain("<strong>2024-01-01</strong>", html);
    }

    [Fact]
    public void Generate_LatestPublishedDateDiffers_RendersBold()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-06-15", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<strong>2024-06-15</strong>", html);
        Assert.DoesNotContain("<strong>MIT</strong>", html);
    }

    [Fact]
    public void Generate_AllLatestFieldsDiffer_AllRenderBold()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("2.0.0", "https://example.com/latest", "Apache-2.0", "2024-06-15", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<strong><a href=\"https://example.com/latest\" target=\"_blank\">2.0.0</a></strong>", html);
        Assert.Contains("<strong>Apache-2.0</strong>", html);
        Assert.Contains("<strong>2024-06-15</strong>", html);
    }

    [Fact]
    public void Generate_ResolvedVersionDiffersFromVersion_BothRendered()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "^1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version\">^1.0.0</td>", html);
        Assert.Contains("<a href=\"https://example.com\" target=\"_blank\">1.0.0</a>", html);
    }

    [Fact]
    public void Generate_DeprecatedPackage_RendersWarningIconWithTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("old-package", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", "This package is deprecated", null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-deprecated\" title=\"This package is deprecated\">&#9888;&#65039;</span>", html);
    }

    [Fact]
    public void Generate_VulnerablePackage_RendersAlertIconWithTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("vuln-package", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, "CVE-2024-1234 High"), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-vulnerable\" title=\"CVE-2024-1234 High\">&#128680;</span>", html);
    }

    [Fact]
    public void Generate_NullDeprecated_RendersEmpty()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"deprecated\"></td>", html);
        Assert.DoesNotContain("&#9888;", html);
    }

    [Fact]
    public void Generate_NullVulnerabilities_RendersEmpty()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"vulnerable\"></td>", html);
        Assert.DoesNotContain("&#128680;", html);
    }

    [Fact]
    public void Generate_LatestDeprecated_RendersWarningIconWithTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("2.0.0", "https://example.com/latest", "MIT", "2024-06-15", "Legacy package", null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-deprecated\" title=\"Legacy package\">&#9888;&#65039;</span>", html);
    }

    [Fact]
    public void Generate_LatestVulnerabilities_RendersAlertIconWithTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("2.0.0", "https://example.com/latest", "MIT", "2024-06-15", null, "CVE-2024-5678 Critical")),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-vulnerable\" title=\"CVE-2024-5678 Critical\">&#128680;</span>", html);
    }

    [Fact]
    public void Generate_DeprecatedWithSpecialChars_HtmlEncodesTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", "Use <new-package> ??????", null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("title=\"Use &lt;new-package&gt; ??????\"", html);
    }

    [Fact]
    public void Generate_DeprecatedPackage_RowHasOrangeBackground()
    {
        var packages = new List<PackageEntry>
        {
            new("old-package", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", "This package is deprecated", null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version version-deprecated\">", html);
        Assert.DoesNotContain("<td class=\"version version-vulnerable\">", html);
    }

    [Fact]
    public void Generate_VulnerablePackage_RowHasRedBackground()
    {
        var packages = new List<PackageEntry>
        {
            new("vuln-package", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, "CVE-2024-1234 High"), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version version-vulnerable\">", html);
        Assert.DoesNotContain("<td class=\"version version-deprecated\">", html);
    }

    [Fact]
    public void Generate_DeprecatedAndVulnerablePackage_RowHasRedBackground()
    {
        var packages = new List<PackageEntry>
        {
            new("bad-package", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", "Deprecated", "CVE-2024-1234 High"), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version version-vulnerable\">", html);
        Assert.DoesNotContain("<td class=\"version version-deprecated\">", html);
    }

    [Fact]
    public void Generate_LatestDeprecatedOnly_RowHasOrangeBackground()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("2.0.0", "https://example.com/latest", "MIT", "2024-06-15", "Legacy package", null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version version-deprecated\">", html);
    }

    [Fact]
    public void Generate_LatestVulnerableOnly_RowHasRedBackground()
    {
        var packages = new List<PackageEntry>
        {
            new("some-pkg", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("2.0.0", "https://example.com/latest", "MIT", "2024-06-15", null, "CVE-2024-5678 Critical")),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version version-vulnerable\">", html);
    }

    [Fact]
    public void Generate_NoDeprecatedOrVulnerable_RowHasNoColorClass()
    {
        var packages = new List<PackageEntry>
        {
            new("good-package", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", null, null), new VersionEntry("1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null)),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.DoesNotContain("<td class=\"version version-deprecated\">", html);
        Assert.DoesNotContain("<td class=\"version version-vulnerable\">", html);
    }

    [Fact]
    public void Generate_CurrentDeprecatedLatestVulnerable_SeparateColors()
    {
        var packages = new List<PackageEntry>
        {
            new("mixed-package", "1.0.0", "my-app", new VersionEntry("1.0.0", "https://example.com", "MIT", "2024-01-01", "Deprecated", null), new VersionEntry("2.0.0", "https://example.com/latest", "MIT", "2024-06-15", null, "CVE-2024-5678 Critical")),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"version version-deprecated\"><a href=\"https://example.com\"", html);
        Assert.Contains("<td class=\"version version-vulnerable\">", html);
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
