namespace CheckNpmPackages.Tests;

public class NpmPackageResolverTests
{
    [Fact]
    public async Task GetLicensesAsync_KnownPackage_ReturnsLicenseAndPublishedDate()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "4.17.21"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.21")];
        Assert.Equal("MIT", info.License);
        Assert.False(string.IsNullOrEmpty(info.PublishedDate));
    }

    [Fact]
    public async Task GetLicensesAsync_KnownPackage_ReturnsLatestVersionInfo()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "4.17.21"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.21")];
        Assert.NotNull(info.LatestVersion);
        Assert.NotNull(info.LatestLicense);
        Assert.NotNull(info.LatestPublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_NonExistentPackage_ReturnsNullInfo()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("nonexistent-pkg-xyz-99999", "0.0.1"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("nonexistent-pkg-xyz-99999", "0.0.1")];
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
            ("lodash", "4.17.21"),
            ("express", "4.18.2"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Equal(2, results.Count);
        Assert.NotNull(results[("lodash", "4.17.21")].License);
        Assert.NotNull(results[("express", "4.18.2")].License);
    }

    [Fact]
    public async Task GetLicensesAsync_DuplicatePackages_ReturnsDistinctResults()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "4.17.21"),
            ("lodash", "4.17.21"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetLicensesAsync_PublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "4.17.21"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        var info = results[("lodash", "4.17.21")];
        Assert.NotNull(info.PublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.PublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_LatestPublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "4.17.21"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        var info = results[("lodash", "4.17.21")];
        Assert.NotNull(info.LatestPublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.LatestPublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        var results = await NpmPackgeResolver.GetLicensesAsync([]);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("^1.0.0", "1.0.0")]
    [InlineData("~1.0.0", "1.0.0")]
    [InlineData("v1.0.0", "1.0.0")]
    [InlineData("=1.0.0", "1.0.0")]
    [InlineData("^^1.0.0", "1.0.0")]
    [InlineData(">=1.0.0 <2.0.0", "1.0.0")]
    [InlineData("^1.0.0 || ^2.0.0", "1.0.0")]
    [InlineData("1.0.0 - 2.0.0", "1.0.0")]
    [InlineData(">1.0.0", "1.0.0")]
    [InlineData(">=1.0.0", "1.0.0")]
    [InlineData("<2.0.0", "2.0.0")]
    [InlineData("<=2.0.0", "2.0.0")]
    public void FormatVersion_StripsAndFormatsCorrectly(string? input, string expected)
    {
        var result = NpmPackgeResolver.FormatVersion(input);
        Assert.Equal(expected, result);
    }
}
