namespace CheckNpmPackages.Tests;

public class NpmPackageResolverTests
{
    [Fact]
    public async Task GetPackagesInfoAsync_KnownPackage_ReturnsLicenseAndPublishedDate()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.21", null)];
        Assert.Equal("MIT", info.ResolvedVersion.License);
        Assert.False(string.IsNullOrEmpty(info.ResolvedVersion.PublishedDate));
    }

    [Fact]
    public async Task GetPackagesInfoAsync_KnownPackage_ReturnsPackageUrls()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.21", null)];
        Assert.NotNull(info.ResolvedVersion.Url);
        Assert.Contains("https://www.npmjs.com/package/lodash", info.ResolvedVersion.Url);
        Assert.NotNull(info.LatestVersion.Url);
        Assert.Contains("https://www.npmjs.com/package/lodash", info.LatestVersion.Url);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_KnownPackage_ReturnsLatestVersionInfo()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.21", null)];
        Assert.NotNull(info.LatestVersion.Version);
        Assert.NotNull(info.LatestVersion.License);
        Assert.NotNull(info.LatestVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_NonExistentPackage_ReturnsNullInfo()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("nonexistent-pkg-xyz-99999", "0.0.1", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("nonexistent-pkg-xyz-99999", "0.0.1", null)];
        Assert.Null(info.ResolvedVersion.License);
        Assert.Null(info.ResolvedVersion.PublishedDate);
        Assert.Null(info.LatestVersion.Version);
        Assert.Null(info.LatestVersion.License);
        Assert.Null(info.LatestVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_MultiplePackages_ReturnsAllResults()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
            ("express", "4.18.2", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Equal(2, results.Count);
        Assert.NotNull(results[("lodash", "4.17.21", null)].ResolvedVersion.License);
        Assert.NotNull(results[("express", "4.18.2", null)].ResolvedVersion.License);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_DuplicatePackages_ReturnsDistinctResults()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
            ("lodash", "4.17.21", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_PublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        var info = results[("lodash", "4.17.21", null)];
        Assert.NotNull(info.ResolvedVersion.PublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.ResolvedVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_LatestPublishedDate_IsFormattedCorrectly()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        var info = results[("lodash", "4.17.21", null)];
        Assert.NotNull(info.LatestVersion.PublishedDate);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", info.LatestVersion.PublishedDate);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>();
        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_CaretRange_ResolvesToMatchingVersion()
    {
        // ^4.17.0 should resolve to the highest 4.x.x version >= 4.17.0
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "^4.17.0", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "^4.17.0", null)];
        Assert.NotNull(info.ResolvedVersion.Version);
        Assert.StartsWith("4.", info.ResolvedVersion.Version);
        Assert.NotNull(info.ResolvedVersion.License);
        Assert.Equal("MIT", info.ResolvedVersion.License);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_TildeRange_ResolvesToMatchingVersion()
    {
        // ~4.17.0 should resolve to the highest 4.17.x version
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "~4.17.0", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "~4.17.0", null)];
        Assert.NotNull(info.ResolvedVersion.Version);
        Assert.StartsWith("4.17.", info.ResolvedVersion.Version);
        Assert.NotNull(info.ResolvedVersion.License);
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
    public async Task GetPackagesInfoAsync_DeprecatedPackage_ReturnsDeprecatedInfo()
    {
        // "request" is a well-known deprecated npm package
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("request", "2.88.2", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("request", "2.88.2", null)];
        Assert.NotNull(info.ResolvedVersion.Deprecated);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_NonDeprecatedPackage_ReturnsNullDeprecated()
    {
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.21", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.21", null)];
        Assert.Null(info.ResolvedVersion.Deprecated);
    }

    [Fact]
    public async Task GetPackagesInfoAsync_VulnerablePackage_ReturnsVulnerabilityInfo()
    {
        // lodash 4.17.20 has known vulnerabilities (prototype pollution)
        var packages = new List<(string Name, string Version, string? ResolvedVersion)>
        {
            ("lodash", "4.17.20", null),
        };

        var results = await NpmPackgeResolver.GetPackagesInfoAsync(packages);

        Assert.Single(results);
        var info = results[("lodash", "4.17.20", null)];
        Assert.NotNull(info.ResolvedVersion.Vulnerabilities);
    }
}
