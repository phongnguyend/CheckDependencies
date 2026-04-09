namespace CheckNugetPackages.Tests;

public class UpgradeCommandsGeneratorTests
{
    [Fact]
    public void Generate_CheckLatestPatch_GeneratesCommandsForDifferentVersions()
    {
        // Arrange
        var packages = new List<PackageEntry>
        {
            new(
                "Newtonsoft.Json",
                "13.0.0",
                "Project1",
                new VersionEntry("13.0.1", null, null, null, null, null),
                new VersionEntry("13.0.1", null, null, null, null, null),
                new VersionEntry("13.0.2", null, null, null, null, null),
                null)
        };

        var arguments = new ParsedArguments(
            new List<string> { "." },
            new List<string> { "csv" },
            null,
            false,
            true,
            false,
            false);

        var tempDir = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = UpgradeCommandsGenerator.Generate(packages, arguments, tempDir);

            // Assert
            Assert.Single(result);
            Assert.Contains("upgrade-to-latest-patch-commands.txt", result[0]);
            Assert.True(File.Exists(result[0]));

            var commands = File.ReadAllLines(result[0]);
            Assert.Single(commands);
            Assert.Equal("dotnet add package Newtonsoft.Json --version 13.0.2", commands[0]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Generate_CheckLatestMinor_GeneratesCommandsForDifferentVersions()
    {
        // Arrange
        var packages = new List<PackageEntry>
        {
            new(
                "Microsoft.AspNetCore.App",
                "6.0.0",
                "Project1",
                new VersionEntry("6.0.2", null, null, null, null, null),
                new VersionEntry("8.0.0", null, null, null, null, null),
                null,
                new VersionEntry("6.0.5", null, null, null, null, null))
        };

        var arguments = new ParsedArguments(
            new List<string> { "." },
            new List<string> { "csv" },
            null,
            false,
            false,
            true,
            false);

        var tempDir = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = UpgradeCommandsGenerator.Generate(packages, arguments, tempDir);

            // Assert
            Assert.Single(result);
            Assert.Contains("upgrade-to-latest-minor-commands.txt", result[0]);
            Assert.True(File.Exists(result[0]));

            var commands = File.ReadAllLines(result[0]);
            Assert.Single(commands);
            Assert.Equal("dotnet add package Microsoft.AspNetCore.App --version 6.0.5", commands[0]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Generate_CheckLatest_GeneratesCommandsForDifferentVersions()
    {
        // Arrange
        var packages = new List<PackageEntry>
        {
            new(
                "System.Text.Json",
                "6.0.0",
                "Project1",
                new VersionEntry("6.0.1", null, null, null, null, null),
                new VersionEntry("8.0.0", null, null, null, null, null),
                null,
                null)
        };

        var arguments = new ParsedArguments(
            new List<string> { "." },
            new List<string> { "csv" },
            null,
            false,
            false,
            false,
            true);

        var tempDir = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = UpgradeCommandsGenerator.Generate(packages, arguments, tempDir);

            // Assert
            Assert.Single(result);
            Assert.Contains("upgrade-to-latest-commands.txt", result[0]);
            Assert.True(File.Exists(result[0]));

            var commands = File.ReadAllLines(result[0]);
            Assert.Single(commands);
            Assert.Equal("dotnet add package System.Text.Json --version 8.0.0", commands[0]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Generate_SkipsPackagesWithSameVersion()
    {
        // Arrange
        var packages = new List<PackageEntry>
        {
            new(
                "Newtonsoft.Json",
                "13.0.1",
                "Project1",
                new VersionEntry("13.0.1", null, null, null, null, null),
                new VersionEntry("13.0.1", null, null, null, null, null),
                new VersionEntry("13.0.1", null, null, null, null, null),
                null)
        };

        var arguments = new ParsedArguments(
            new List<string> { "." },
            new List<string> { "csv" },
            null,
            false,
            true,
            false,
            false);

        var tempDir = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = UpgradeCommandsGenerator.Generate(packages, arguments, tempDir);

            // Assert
            Assert.Empty(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Generate_MultipleOptions_GeneratesMultipleFiles()
    {
        // Arrange
        var packages = new List<PackageEntry>
        {
            new(
                "Newtonsoft.Json",
                "13.0.0",
                "Project1",
                new VersionEntry("13.0.0", null, null, null, null, null),
                new VersionEntry("13.0.3", null, null, null, null, null),
                new VersionEntry("13.0.2", null, null, null, null, null),
                new VersionEntry("13.0.2", null, null, null, null, null))
        };

        var arguments = new ParsedArguments(
            new List<string> { "." },
            new List<string> { "csv" },
            null,
            false,
            true,
            true,
            true);

        var tempDir = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = UpgradeCommandsGenerator.Generate(packages, arguments, tempDir);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("upgrade-to-latest-patch-commands.txt", result[0]);
            Assert.Contains("upgrade-to-latest-minor-commands.txt", result[1]);
            Assert.Contains("upgrade-to-latest-commands.txt", result[2]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Generate_NoReportDirectory_SavesInCurrentDirectory()
    {
        // Arrange
        var packages = new List<PackageEntry>
        {
            new(
                "Newtonsoft.Json",
                "13.0.1",
                "Project1",
                new VersionEntry("13.0.1", null, null, null, null, null),
                new VersionEntry("13.0.2", null, null, null, null, null),
                new VersionEntry("13.0.2", null, null, null, null, null),
                null)
        };

        var arguments = new ParsedArguments(
            new List<string> { "." },
            new List<string> { "csv" },
            null,
            false,
            true,
            false,
            false);

        var currentDir = Directory.GetCurrentDirectory();
        var testFile = Path.Combine(currentDir, "upgrade-to-latest-patch-commands.txt");

        try
        {
            // Act
            var result = UpgradeCommandsGenerator.Generate(packages, arguments, null);

            // Assert
            Assert.Single(result);
            Assert.Equal(testFile, result[0]);
            Assert.True(File.Exists(testFile));
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }
}
