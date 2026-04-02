namespace CheckNugetPackages.Tests;

public class NugetPackageResolverTests
{
    [Fact]
    public async Task GetLicensesAsync_KnownPackage_ReturnsLicenseAndPublishedDate()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.Equal("MIT", info.License);
        Assert.False(string.IsNullOrEmpty(info.PublishedDate));
    }

    [Fact]
    public async Task GetLicensesAsync_KnownPackage_ReturnsLatestVersionInfo()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.NotNull(info.LatestVersion);
        Assert.NotNull(info.LatestLicense);
        Assert.NotNull(info.LatestPublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_NonExistentPackage_ReturnsNullInfo()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("NonExistentPackage_XYZ_12345", "0.0.1"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("NonExistentPackage_XYZ_12345", "0.0.1")];
        Assert.Null(info.License);
        Assert.Null(info.PublishedDate);
        Assert.Null(info.LatestVersion);
        Assert.Null(info.LatestLicense);
        Assert.Null(info.LatestPublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_MultiplePackages_ReturnsAllResults()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
            ("Serilog", "4.0.0"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Equal(2, results.Count);
        Assert.NotNull(results[("Newtonsoft.Json", "13.0.3")].License);
        Assert.NotNull(results[("Serilog", "4.0.0")].License);
    }

    [Fact]
    public async Task GetLicensesAsync_DuplicatePackages_ReturnsDistinctResults()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetLicensesAsync_PackageWithLicenseUrl_ReturnsUrl()
    {
        // Castle.Core 4.4.0 uses licenseUrl (not licenseExpression)
        var packages = new List<(string Name, string Version)>
        {
            ("Castle.Core", "4.4.0"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("Castle.Core", "4.4.0")];
        Assert.NotNull(info.License);
    }

    [Fact]
    public async Task GetLicensesAsync_PublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.NotNull(info.PublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.PublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_LatestPublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.NotNull(info.LatestPublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.LatestPublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        var results = await NugetPackageResolver.GetLicensesAsync([]);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetLicensesAsync_DeprecatedPackage_ReturnsDeprecatedInfo()
    {
        // Microsoft.Azure.EventHubs is deprecated in favor of Azure.Messaging.EventHubs
        var packages = new List<(string Name, string Version)>
        {
            ("Microsoft.Azure.EventHubs", "4.3.2"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("Microsoft.Azure.EventHubs", "4.3.2")];
        Assert.NotNull(info.Deprecated);
    }

    [Fact]
    public async Task GetLicensesAsync_VulnerablePackage_ReturnsVulnerabilityInfo()
    {
        // System.Text.RegularExpressions 4.3.0 has known vulnerabilities
        var packages = new List<(string Name, string Version)>
        {
            ("System.Text.RegularExpressions", "4.3.0"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("System.Text.RegularExpressions", "4.3.0")];
        Assert.NotNull(info.Vulnerabilities);
    }

    [Fact]
    public async Task GetLicensesAsync_NonDeprecatedPackage_ReturnsNullDeprecated()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.Null(info.Deprecated);
        Assert.Null(info.Vulnerabilities);
    }

    [Fact]
    public void FormatDeprecation_NullInput_ReturnsNull()
    {
        Assert.Null(NugetPackageResolver.FormatDeprecation(null));
    }

    [Fact]
    public void FormatVulnerabilities_NullInput_ReturnsNull()
    {
        Assert.Null(NugetPackageResolver.FormatVulnerabilities(null));
    }

    [Fact]
    public void FormatVulnerabilities_EmptyList_ReturnsNull()
    {
        Assert.Null(NugetPackageResolver.FormatVulnerabilities([]));
    }

    [Theory]
    [InlineData("1.0.0", new[] { "1.0.0" }, "1.0.0")]
    [InlineData("[1.0.0,2.0.0)", new[] { "1.0.0", "1.5.0", "2.0.0" }, "1.5.0")]
    [InlineData("(,1.0.0]", new[] { "0.9.0", "1.0.0", "1.0.1" }, "1.0.0")]
    [InlineData("[1.0.0,)", new[] { "1.0.0", "1.1.0", "2.0.0" }, "2.0.0")]
    [InlineData("1.*", new[] { "1.0.0", "1.5.0", "2.0.0" }, "1.5.0")]
    [InlineData("1.2.*", new[] { "1.2.0", "1.2.5", "1.3.0" }, "1.2.5")]
    [InlineData("1.2.3-beta", new[] { "1.2.3-alpha", "1.2.3-beta", "1.2.3" }, "1.2.3-beta")]
    [InlineData("[1.0.0,1.0.0]", new[] { "1.0.0" }, "1.0.0")]
    public void ResolveNugetVersion_BasicCases(string range, string[] available, string expected)
    {
        var result = NugetPackageResolver.ResolveNugetVersion(range, new List<string>(available));
        Assert.Equal(expected, result);
    }
}
