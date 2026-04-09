namespace CheckNugetPackages;

public static class UpgradeCommandsGenerator
{
    public static List<string> Generate(List<PackageEntry> packageGroups, ParsedArguments arguments, string? reportDirectory)
    {
        var generatedFiles = new List<string>();

        if (arguments.CheckLatestPatch)
        {
            var commands = GenerateCommands(packageGroups, p => p.LatestPatchVersion, "patch");
            if (commands.Count > 0)
            {
                var filePath = string.IsNullOrEmpty(reportDirectory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "upgrade-to-latest-patch-commands.txt")
                    : Path.Combine(reportDirectory, "upgrade-to-latest-patch-commands.txt");
                
                WriteCommandsFile(filePath, commands);
                generatedFiles.Add(filePath);
            }
        }

        if (arguments.CheckLatestMinor)
        {
            var commands = GenerateCommands(packageGroups, p => p.LatestMinorVersion, "minor");
            if (commands.Count > 0)
            {
                var filePath = string.IsNullOrEmpty(reportDirectory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "upgrade-to-latest-minor-commands.txt")
                    : Path.Combine(reportDirectory, "upgrade-to-latest-minor-commands.txt");
                
                WriteCommandsFile(filePath, commands);
                generatedFiles.Add(filePath);
            }
        }

        if (arguments.CheckLatest)
        {
            var commands = GenerateCommands(packageGroups, p => new VersionEntry(p.LatestVersion.Version, p.LatestVersion.Url, p.LatestVersion.License, p.LatestVersion.PublishedDate, p.LatestVersion.Deprecated, p.LatestVersion.Vulnerabilities), "latest");
            if (commands.Count > 0)
            {
                var filePath = string.IsNullOrEmpty(reportDirectory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "upgrade-to-latest-commands.txt")
                    : Path.Combine(reportDirectory, "upgrade-to-latest-commands.txt");
                
                WriteCommandsFile(filePath, commands);
                generatedFiles.Add(filePath);
            }
        }

        return generatedFiles;
    }

    private static List<string> GenerateCommands(List<PackageEntry> packageGroups, Func<PackageEntry, VersionEntry?> targetVersionSelector, string versionType)
    {
        var commands = new List<string>();

        foreach (var package in packageGroups)
        {
            var targetVersion = targetVersionSelector(package);
            
            if (targetVersion?.Version == null)
                continue;

            // Only include if version is different from resolved version
            if (package.ResolvedVersion.Version == targetVersion.Version)
                continue;

            var command = $"dotnet add package {package.Name} --version {targetVersion.Version}";
            commands.Add(command);
        }

        return commands;
    }

    private static void WriteCommandsFile(string filePath, List<string> commands)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(filePath, commands);
    }
}
