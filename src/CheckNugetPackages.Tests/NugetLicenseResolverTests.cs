namespace CheckNugetPackages.Tests;

public class NugetLicenseResolverTests
{
    [Fact]
    public async Task GetLicensesAsync_KnownPackage_ReturnsLicenseAndPublishedDate()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
        };

        var results = await NugetLicenseResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.Equal("MIT", info.License);
        Assert.False(string.IsNullOrEmpty(info.PublishedDate));
    }

    [Fact]
    public async Task GetLicensesAsync_NonExistentPackage_ReturnsNullInfo()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("NonExistentPackage_XYZ_12345", "0.0.1"),
        };

        var results = await NugetLicenseResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("NonExistentPackage_XYZ_12345", "0.0.1")];
        Assert.Null(info.License);
        Assert.Null(info.PublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_MultiplePackages_ReturnsAllResults()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("Newtonsoft.Json", "13.0.3"),
            ("Serilog", "4.0.0"),
        };

        var results = await NugetLicenseResolver.GetLicensesAsync(packages);

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

        var results = await NugetLicenseResolver.GetLicensesAsync(packages);

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

        var results = await NugetLicenseResolver.GetLicensesAsync(packages);

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

        var results = await NugetLicenseResolver.GetLicensesAsync(packages);

        var info = results[("Newtonsoft.Json", "13.0.3")];
        Assert.NotNull(info.PublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.PublishedDate);
    }

    [Fact]
    public async Task GetLicensesAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        var results = await NugetLicenseResolver.GetLicensesAsync([]);
        Assert.Empty(results);
    }
}
