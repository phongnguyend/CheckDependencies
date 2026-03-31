namespace CheckNugetPackages.Tests;

public class HtmlReportGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public HtmlReportGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"HtmlReportTests_{Guid.NewGuid():N}");
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
        var html = GenerateAndRead("NuGet Packages Report", [], []);
        Assert.Contains("<title>NuGet Packages Report</title>", html);
        Assert.Contains("<h1>NuGet Packages Report</h1>", html);
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
        Assert.Contains("<th>Resolved Version</th>", html);
        Assert.Contains("<th>License</th>", html);
        Assert.Contains("<th>Published Date</th>", html);
        Assert.Contains("<th>Deprecated</th>", html);
        Assert.Contains("<th>Vulnerabilities</th>", html);
        Assert.Contains("<th>Latest Version</th>", html);
        Assert.Contains("<th>Latest License</th>", html);
        Assert.Contains("<th>Latest Published Date</th>", html);
        Assert.Contains("<th>Latest Deprecated</th>", html);
        Assert.Contains("<th>Latest Vulnerabilities</th>", html);
        Assert.Contains("<th>Projects</th>", html);
    }

    [Fact]
    public void Generate_RendersPackageRow()
    {
        var packages = new List<PackageEntry>
        {
            new("Newtonsoft.Json", "13.0.3", "13.0.3", "ProjectA", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null, "13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("Newtonsoft.Json", html);
        Assert.Contains("13.0.3", html);
        Assert.Contains("MIT", html);
        Assert.Contains("2023-03-08", html);
        Assert.Contains("ProjectA", html);
        Assert.Contains("https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", html);
    }

    [Fact]
    public void Generate_ExcludesIgnoredPackages()
    {
        var packages = new List<PackageEntry>
        {
            new("System.Text.Json", "8.0.0", "8.0.0", "ProjectA", "https://example.com", "MIT", "2023-11-14", null, null, "8.0.0", "https://example.com/latest", "MIT", "2023-11-14", null, null),
            new("Newtonsoft.Json", "13.0.3", "13.0.3", "ProjectA", "https://example.com", "MIT", "2023-03-08", null, null, "13.0.3", "https://example.com/latest", "MIT", "2023-03-08", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, ["System."]);

        Assert.DoesNotContain("System.Text.Json", html);
        Assert.Contains("Newtonsoft.Json", html);
    }

    [Fact]
    public void Generate_NullLicense_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", null, "2024-01-01", null, null, "1.0.0", null, null, null, null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"license\">N/A</td>", html);
    }

    [Fact]
    public void Generate_NullPublishedDate_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", null, null, null, "1.0.0", null, "MIT", null, null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<td class=\"published-date\">N/A</td>", html);
    }

    [Fact]
    public void Generate_NullVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", null, null, "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "1.0.0", null, "MIT", "2024-01-01", null, null),
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
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "https://licenses.nuget.org/MIT", "2024-01-01", null, null, "1.0.0", null, "MIT", "2024-01-01", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<a href=\"https://licenses.nuget.org/MIT\" target=\"_blank\">View License</a>", html);
    }

    [Fact]
    public void Generate_HtmlEncodesSpecialCharacters()
    {
        var packages = new List<PackageEntry>
        {
            new("Pkg<Name>", "1.0.0", "1.0.0", "Project&A", "https://example.com", "License&Co", "2024-01-01", null, null, "1.0.0", null, "License&Co", "2024-01-01", null, null),
        };

        var html = GenerateAndRead("Title<>&", packages, []);

        Assert.Contains("Pkg&lt;Name&gt;", html);
        Assert.Contains("Project&amp;A", html);
        Assert.Contains("License&amp;Co", html);
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
            new("Newtonsoft.Json", "12.0.3", "12.0.3", "ProjectA", "https://www.nuget.org/packages/Newtonsoft.Json/12.0.3", "MIT", "2019-11-09", null, null, "13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<strong><a href=\"https://www.nuget.org/packages/Newtonsoft.Json/13.0.3\" target=\"_blank\">13.0.3</a></strong>", html);
        Assert.Contains("<strong>2023-03-08</strong>", html);
    }

    [Fact]
    public void Generate_NullLatestVersion_RendersNA()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, null, null, null, null, null, null),
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
            new("Newtonsoft.Json", "13.0.3", "13.0.3", "ProjectA", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null, "13.0.3", "https://www.nuget.org/packages/Newtonsoft.Json/13.0.3", "MIT", "2023-03-08", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.DoesNotContain("<strong>", html);
    }

    [Fact]
    public void Generate_LatestLicenseDiffers_RendersBold()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "1.0.0", "https://example.com/latest", "Apache-2.0", "2024-01-01", null, null),
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
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "1.0.0", "https://example.com/latest", "MIT", "2024-06-15", null, null),
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
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "2.0.0", "https://example.com/latest", "Apache-2.0", "2024-06-15", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<strong><a href=\"https://example.com/latest\" target=\"_blank\">2.0.0</a></strong>", html);
        Assert.Contains("<strong>Apache-2.0</strong>", html);
        Assert.Contains("<strong>2024-06-15</strong>", html);
    }

    [Fact]
    public void Generate_VersionDiffersComparedByResolvedVersion()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.DoesNotContain("<strong>", html);
    }

    [Fact]
    public void Generate_DeprecatedPackage_RendersWarningIconWithTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("OldPackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", "This package is deprecated", null, "1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-deprecated\" title=\"This package is deprecated\">&#9888;&#65039;</span>", html);
    }

    [Fact]
    public void Generate_VulnerablePackage_RendersAlertIconWithTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("VulnPackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, "CVE-2024-1234 High", "1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-vulnerable\" title=\"CVE-2024-1234 High\">&#128680;</span>", html);
    }

    [Fact]
    public void Generate_NullDeprecated_RendersEmpty()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null),
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
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null),
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
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "2.0.0", "https://example.com/latest", "MIT", "2024-06-15", "Legacy package", null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-deprecated\" title=\"Legacy package\">&#9888;&#65039;</span>", html);
    }

    [Fact]
    public void Generate_LatestVulnerabilities_RendersAlertIconWithTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", null, null, "2.0.0", "https://example.com/latest", "MIT", "2024-06-15", null, "CVE-2024-5678 Critical"),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("<span class=\"icon-vulnerable\" title=\"CVE-2024-5678 Critical\">&#128680;</span>", html);
    }

    [Fact]
    public void Generate_DeprecatedWithSpecialChars_HtmlEncodesTooltip()
    {
        var packages = new List<PackageEntry>
        {
            new("SomePackage", "1.0.0", "1.0.0", "ProjectA", "https://example.com", "MIT", "2024-01-01", "Use <NewPackage> instead", null, "1.0.0", "https://example.com/latest", "MIT", "2024-01-01", null, null),
        };

        var html = GenerateAndRead("Test Report", packages, []);

        Assert.Contains("title=\"Use &lt;NewPackage&gt; instead\"", html);
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
