namespace CheckNugetPackages;

public record PackageEntry(string Name, string? Version, string? ResolvedVersion, string Projects, string Url, string? License, string? PublishedDate, string? Deprecated, string? Vulnerabilities, string? LatestVersion, string? LatestUrl, string? LatestLicense, string? LatestPublishedDate, string? LatestDeprecated, string? LatestVulnerabilities);
