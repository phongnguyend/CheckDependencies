namespace CheckNpmPackages;

public record PackageEntry(string Name, string? Version, string Projects, string Url, string? License, string? PublishedDate, string? LatestVersion, string? LatestUrl, string? LatestLicense, string? LatestPublishedDate);
