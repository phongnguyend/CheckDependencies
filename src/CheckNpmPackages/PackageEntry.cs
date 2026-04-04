namespace CheckNpmPackages;

public record PackageEntry(string Name, string? Version, string Projects, VersionEntry ResolvedVersion, VersionEntry LatestVersion);
