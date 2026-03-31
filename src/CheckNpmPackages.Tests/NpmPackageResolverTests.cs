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
        var packages = new List<(string Name, string Version)>();
        var results = await NpmPackgeResolver.GetLicensesAsync(packages);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetLicensesAsync_CaretRange_ResolvesToMatchingVersion()
    {
        // ^4.17.0 should resolve to the highest 4.x.x version >= 4.17.0
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "^4.17.0"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "^4.17.0")];
        Assert.NotNull(info.ResolvedVersion);
        Assert.StartsWith("4.", info.ResolvedVersion);
        Assert.NotNull(info.License);
        Assert.Equal("MIT", info.License);
    }

    [Fact]
    public async Task GetLicensesAsync_TildeRange_ResolvesToMatchingVersion()
    {
        // ~4.17.0 should resolve to the highest 4.17.x version
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "~4.17.0"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "~4.17.0")];
        Assert.NotNull(info.ResolvedVersion);
        Assert.StartsWith("4.17.", info.ResolvedVersion);
        Assert.NotNull(info.License);
    }

    [Fact]
    public void ResolveVersion_ExactVersion_ReturnsExactMatch()
    {
        var versions = ParseVersions("1.0.0", "1.1.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("1.1.0", versions);
        Assert.Equal("1.1.0", result);
    }

    [Fact]
    public void ResolveVersion_CaretRange_ReturnsHighestCompatible()
    {
        var versions = ParseVersions("1.0.0", "1.2.0", "1.9.9", "2.0.0", "2.1.0");
        var result = NpmPackgeResolver.ResolveVersion("^1.0.0", versions);
        Assert.Equal("1.9.9", result);
    }

    [Fact]
    public void ResolveVersion_CaretZeroMajor_ReturnsHighestMinorCompatible()
    {
        // ^0.2.3 := >=0.2.3 <0.3.0
        var versions = ParseVersions("0.2.3", "0.2.5", "0.3.0", "1.0.0");
        var result = NpmPackgeResolver.ResolveVersion("^0.2.3", versions);
        Assert.Equal("0.2.5", result);
    }

    [Fact]
    public void ResolveVersion_CaretZeroZero_ReturnsExactPatch()
    {
        // ^0.0.3 := >=0.0.3 <0.0.4
        var versions = ParseVersions("0.0.2", "0.0.3", "0.0.4", "0.1.0");
        var result = NpmPackgeResolver.ResolveVersion("^0.0.3", versions);
        Assert.Equal("0.0.3", result);
    }

    [Fact]
    public void ResolveVersion_TildeRange_ReturnsHighestPatch()
    {
        // ~1.2.3 := >=1.2.3 <1.3.0
        var versions = ParseVersions("1.2.3", "1.2.5", "1.3.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("~1.2.3", versions);
        Assert.Equal("1.2.5", result);
    }

    [Fact]
    public void ResolveVersion_TildeMajorOnly_ReturnsHighestInMajor()
    {
        // ~1 := >=1.0.0 <2.0.0
        var versions = ParseVersions("1.0.0", "1.5.0", "1.9.9", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("~1", versions);
        Assert.Equal("1.9.9", result);
    }

    [Fact]
    public void ResolveVersion_GteAndLt_ReturnsHighestInRange()
    {
        var versions = ParseVersions("1.0.0", "1.5.0", "2.0.0", "3.0.0");
        var result = NpmPackgeResolver.ResolveVersion(">=1.0.0 <2.0.0", versions);
        Assert.Equal("1.5.0", result);
    }

    [Fact]
    public void ResolveVersion_OrRange_ReturnsHighestFromAnySet()
    {
        var versions = ParseVersions("1.0.0", "1.5.0", "2.0.0", "2.5.0", "3.0.0");
        var result = NpmPackgeResolver.ResolveVersion("^1.0.0 || ^2.0.0", versions);
        Assert.Equal("2.5.0", result);
    }

    [Fact]
    public void ResolveVersion_HyphenRange_ReturnsHighestInRange()
    {
        // 1.0.0 - 2.0.0 := >=1.0.0 <=2.0.0
        var versions = ParseVersions("0.9.0", "1.0.0", "1.5.0", "2.0.0", "2.1.0");
        var result = NpmPackgeResolver.ResolveVersion("1.0.0 - 2.0.0", versions);
        Assert.Equal("2.0.0", result);
    }

    [Fact]
    public void ResolveVersion_Star_ReturnsHighestNonPrerelease()
    {
        var versions = ParseVersions("1.0.0", "2.0.0", "3.0.0");
        var result = NpmPackgeResolver.ResolveVersion("*", versions);
        Assert.Equal("3.0.0", result);
    }

    [Fact]
    public void ResolveVersion_XRange_ReturnsHighestInMajor()
    {
        // 1.x := >=1.0.0 <2.0.0
        var versions = ParseVersions("1.0.0", "1.5.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("1.x", versions);
        Assert.Equal("1.5.0", result);
    }

    [Fact]
    public void ResolveVersion_XRangeMinor_ReturnsHighestInMinor()
    {
        // 1.2.x := >=1.2.0 <1.3.0
        var versions = ParseVersions("1.2.0", "1.2.5", "1.3.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("1.2.x", versions);
        Assert.Equal("1.2.5", result);
    }

    [Fact]
    public void ResolveVersion_PartialMajor_ReturnsHighestInMajor()
    {
        // "1" := >=1.0.0 <2.0.0
        var versions = ParseVersions("1.0.0", "1.9.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("1", versions);
        Assert.Equal("1.9.0", result);
    }

    [Fact]
    public void ResolveVersion_PartialMajorMinor_ReturnsHighestInMinor()
    {
        // "1.2" := >=1.2.0 <1.3.0
        var versions = ParseVersions("1.2.0", "1.2.9", "1.3.0");
        var result = NpmPackgeResolver.ResolveVersion("1.2", versions);
        Assert.Equal("1.2.9", result);
    }

    [Fact]
    public void ResolveVersion_GreaterThan_ReturnsHighestAbove()
    {
        var versions = ParseVersions("1.0.0", "1.5.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion(">1.0.0", versions);
        Assert.Equal("2.0.0", result);
    }

    [Fact]
    public void ResolveVersion_LessThan_ReturnsHighestBelow()
    {
        var versions = ParseVersions("1.0.0", "1.5.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("<2.0.0", versions);
        Assert.Equal("1.5.0", result);
    }

    [Fact]
    public void ResolveVersion_ExcludesPrerelease_ByDefault()
    {
        var versions = ParseVersions("1.0.0", "1.1.0", "2.0.0-alpha.1", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("^1.0.0", versions);
        Assert.Equal("1.1.0", result);
    }

    [Fact]
    public void ResolveVersion_IncludesPrerelease_WhenExplicitlyReferenced()
    {
        // ^2.0.0-alpha.0 should include prereleases on the same [major, minor, patch]
        var versions = ParseVersions("1.0.0", "2.0.0-alpha.1", "2.0.0-beta.1", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("^2.0.0-alpha.0", versions);
        Assert.Equal("2.0.0", result);
    }

    [Fact]
    public void ResolveVersion_NoMatch_ReturnsNull()
    {
        var versions = ParseVersions("1.0.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("^3.0.0", versions);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveVersion_NullInput_ReturnsNull()
    {
        var versions = ParseVersions("1.0.0");
        var result = NpmPackgeResolver.ResolveVersion(null, versions);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveVersion_EmptyInput_ReturnsNull()
    {
        var versions = ParseVersions("1.0.0");
        var result = NpmPackgeResolver.ResolveVersion("", versions);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveVersion_LeadingV_ResolvesCorrectly()
    {
        var versions = ParseVersions("1.0.0", "1.5.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("v1.0.0", versions);
        Assert.Equal("1.0.0", result);
    }

    [Fact]
    public void ResolveVersion_LeadingEquals_ResolvesCorrectly()
    {
        var versions = ParseVersions("1.0.0", "1.5.0", "2.0.0");
        var result = NpmPackgeResolver.ResolveVersion("=1.5.0", versions);
        Assert.Equal("1.5.0", result);
    }

    private static List<SemVer> ParseVersions(params string[] versions)
    {
        var result = new List<SemVer>();
        foreach (var v in versions)
        {
            if (SemVer.TryParse(v, out var sv))
                result.Add(sv);
        }
        return result;
    }

    [Fact]
    public async Task GetLicensesAsync_DeprecatedPackage_ReturnsDeprecatedInfo()
    {
        // "request" is a well-known deprecated npm package
        var packages = new List<(string Name, string Version)>
        {
            ("request", "2.88.2"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("request", "2.88.2")];
        Assert.NotNull(info.Deprecated);
    }

    [Fact]
    public async Task GetLicensesAsync_NonDeprecatedPackage_ReturnsNullDeprecated()
    {
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "4.17.21"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.21")];
        Assert.Null(info.Deprecated);
    }

    [Fact]
    public async Task GetLicensesAsync_VulnerablePackage_ReturnsVulnerabilityInfo()
    {
        // lodash 4.17.20 has known vulnerabilities (prototype pollution)
        var packages = new List<(string Name, string Version)>
        {
            ("lodash", "4.17.20"),
        };

        var results = await NpmPackgeResolver.GetLicensesAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.20")];
        Assert.NotNull(info.Vulnerabilities);
    }
}
