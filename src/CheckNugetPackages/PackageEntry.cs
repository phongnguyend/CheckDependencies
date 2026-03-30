namespace CheckNugetPackages;

public record PackageEntry(string Name, string? Version, string Projects, string Url, string? License, string? PublishedDate);
