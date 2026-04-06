namespace CheckNugetPackages;

public record PackageEntry(
    string Name,
    string? Version,
    string Projects,
    VersionEntry ResolvedVersion,
    VersionEntry LatestVersion,
    VersionEntry? LatestPatchVersion = null,
    VersionEntry? LatestMinorVersion = null);
