namespace CheckNugetPackages.Tests;

public class NugetPackageResolverTests
{
    [Fact]
    public async Task GetPackagesInfoAsync_KnownPackage_ReturnsLicenseAndPublishedDate()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.Equal("MIT", info.ResolvedVersion.License);
        Assert.False(string.IsNullOrEmpty(info.ResolvedVersion.PublishedDate));
    }

    [Fact]
    public async Task GetPackagesInfoAsync_KnownPackage_ReturnsLatestVersionInfo()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.NotNull(info.LatestVersion.Version);
        Assert.NotNull(info.LatestVersion.License);
        Assert.NotNull(info.LatestVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_NonExistentPackage_ReturnsNullInfo()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("NonExistentPackage_XYZ_12345", "0.0.1"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("NonExistentPackage_XYZ_12345", "0.0.1")];
        Assert.Null(info.ResolvedVersion.License);
        Assert.Null(info.ResolvedVersion.PublishedDate);
        Assert.Null(info.LatestVersion.Version);
        Assert.Null(info.LatestVersion.License);
        Assert.Null(info.LatestVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_MultiplePackages_ReturnsAllResults()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
            ("Serilog", "4.0.0"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Equal(2, results.Count);
        Assert.NotNull(results[("Newtonsoft.Json", "13.0.3")].ResolvedVersion.License);
        Assert.NotNull(results[("Serilog", "4.0.0")].ResolvedVersion.License);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_DuplicatePackages_ReturnsDistinctResults()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_PackageWithLicenseUrl_ReturnsUrl()
    {
        // Castle.Core 4.4.0 uses licenseUrl (not licenseExpression)
        var packages = new List<(string Name, string Version)>
        {
            ("Castle.Core", "4.4.0"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("Castle.Core", "4.4.0")];
        Assert.NotNull(info.ResolvedVersion.License);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_PublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.NotNull(info.ResolvedVersion.PublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.ResolvedVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_LatestPublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.NotNull(info.LatestVersion.PublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.LatestVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        var results = await NugetPackageResolver.GetPackagesInfoAsync([]);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_DeprecatedPackage_ReturnsDeprecatedInfo()
    {
        // Microsoft.Azure.EventHubs is deprecated in favor of Azure.Messaging.EventHubs
        var packages = new List<(string Name, string Version)>
        {
            ("Microsoft.Azure.EventHubs", "4.3.2"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("Microsoft.Azure.EventHubs", "4.3.2")];
        Assert.NotNull(info.ResolvedVersion.Deprecated);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_VulnerablePackage_ReturnsVulnerabilityInfo()
    {
        // System.Text.RegularExpressions 4.3.0 has known vulnerabilities
        var packages = new List<(string Name, string Version)>
        {
            ("System.Text.RegularExpressions", "4.3.0"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("System.Text.RegularExpressions", "4.3.0")];
        Assert.NotNull(info.ResolvedVersion.Vulnerabilities);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_NonDeprecatedPackage_ReturnsNullDeprecated()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetPackageResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.Null(info.ResolvedVersion.Deprecated);
        Assert.Null(info.ResolvedVersion.Vulnerabilities);
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
