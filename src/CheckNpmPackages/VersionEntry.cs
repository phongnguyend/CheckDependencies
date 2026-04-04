namespace CheckNpmPackages;

public record VersionEntry(string? Version, string? Url, string? License, string? PublishedDate, string? Deprecated, string? Vulnerabilities);
