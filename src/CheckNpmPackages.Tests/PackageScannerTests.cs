namespace CheckNpmPackages.Tests;

public class PackageScannerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _samplesDir;

    public PackageScannerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"NpmPackageScannerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _samplesDir = Path.Combine(AppContext.BaseDirectory, "samples");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task RunAsync_WithReactJsSample_ScansPackagesFromPackageJson()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), "CSV report was not generated");

        var lines = File.ReadAllLines(csvPath);
        Assert.NotEmpty(lines);

        var allLines = string.Join("\n", lines);
        Assert.Contains("react", allLines.ToLowerInvariant());
    }

    [Fact]
    public async Task RunAsync_WithAngularSample_ScansPackagesFromPackageJson()
    {
        var samplePath = Path.Combine(_samplesDir, "angular");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), "CSV report was not generated");

        var lines = File.ReadAllLines(csvPath);
        Assert.NotEmpty(lines);
    }

    [Fact]
    public async Task RunAsync_WithMultipleSamples_CombinesPackagesFromAllDirectories()
    {
        var sample1 = Path.Combine(_samplesDir, "reactjs");
        var sample2 = Path.Combine(_samplesDir, "angular");

        var arguments = new ParsedArguments(
            [sample1, sample2],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), "CSV report was not generated");
        
        var lines = File.ReadAllLines(csvPath);
        Assert.NotEmpty(lines);
    }

    [Fact]
    public async Task RunAsync_GeneratesAllReportTypes()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        Assert.True(File.Exists(Path.Combine(_tempDir, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(_tempDir, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(_tempDir, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_GeneratesOnlyRequestedReportTypes()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["html"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        Assert.False(File.Exists(Path.Combine(_tempDir, "packages.csv")), "CSV report should not be generated");
        Assert.True(File.Exists(Path.Combine(_tempDir, "packages.html")), "HTML report not generated");
        Assert.False(File.Exists(Path.Combine(_tempDir, "packages.md")), "Markdown report should not be generated");
    }

    [Fact]
    public async Task RunAsync_WithHtmlReportType_GeneratesValidHtml()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["html"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var htmlPath = Path.Combine(_tempDir, "packages.html");
        Assert.True(File.Exists(htmlPath), "HTML report not generated");

        var html = File.ReadAllText(htmlPath);
        Assert.Contains("<html", html.ToLowerInvariant());
        Assert.Contains("</html>", html.ToLowerInvariant());
        Assert.Contains("<table", html.ToLowerInvariant());
    }

    [Fact]
    public async Task RunAsync_WithMarkdownReportType_GeneratesValidMarkdown()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["md"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var mdPath = Path.Combine(_tempDir, "packages.md");
        Assert.True(File.Exists(mdPath), "Markdown report not generated");

        var md = File.ReadAllText(mdPath);
        Assert.Contains("#", md);
        Assert.Contains("|", md);
        Assert.Contains("Name", md);
    }

    [Fact]
    public async Task RunAsync_WithoutReportDirectory_UsesCurrentDirectory()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");
        
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir);

            var arguments = new ParsedArguments(
                [samplePath],
                ["csv"],
                null
            );

            await PackageScanner.RunAsync(arguments);

            Assert.True(File.Exists(Path.Combine(_tempDir, "packages.csv")), 
                "CSV report not generated in current directory");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public async Task RunAsync_SkipsNodeModulesDirectory()
    {
        var testDir = Path.Combine(_tempDir, "test_project");
        Directory.CreateDirectory(testDir);
        Directory.CreateDirectory(Path.Combine(testDir, "node_modules", "some_package"));

        var packageJsonInNodeModules = Path.Combine(testDir, "node_modules", "package.json");
        File.WriteAllText(packageJsonInNodeModules, "{ \"dependencies\": {} }");

        var mainPackageJson = Path.Combine(testDir, "package.json");
        File.WriteAllText(mainPackageJson, """
        {
            "name": "test-project",
            "dependencies": {
                "lodash": "^4.17.21"
            }
        }
        """);

        var arguments = new ParsedArguments(
            [testDir],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), "CSV report not generated");

        var lines = File.ReadAllLines(csvPath);
        var allLines = string.Join("\n", lines);
        
        Assert.Contains("lodash", allLines.ToLowerInvariant());
    }

    [Fact]
    public async Task RunAsync_HandlesPackagesWithDevDependencies()
    {
        var testDir = Path.Combine(_tempDir, "test_with_devdeps");
        Directory.CreateDirectory(testDir);

        var packageJson = Path.Combine(testDir, "package.json");
        File.WriteAllText(packageJson, """
        {
            "name": "test-project",
            "dependencies": {
                "lodash": "^4.17.21"
            },
            "devDependencies": {
                "typescript": "^5.0.0",
                "eslint": "^8.0.0"
            }
        }
        """);

        var arguments = new ParsedArguments(
            [testDir],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), "CSV report not generated");

        var lines = File.ReadAllLines(csvPath);
        var allLines = string.Join("\n", lines);
        
        Assert.Contains("lodash", allLines.ToLowerInvariant());
        Assert.Contains("typescript", allLines.ToLowerInvariant());
        Assert.Contains("eslint", allLines.ToLowerInvariant());
    }

    [Fact]
    public async Task RunAsync_IgnoresFileDependencies()
    {
        var testDir = Path.Combine(_tempDir, "test_file_deps");
        Directory.CreateDirectory(testDir);

        var packageJson = Path.Combine(testDir, "package.json");
        File.WriteAllText(packageJson, """
        {
            "name": "test-project",
            "dependencies": {
                "lodash": "^4.17.21",
                "local-package": "file:../local-package"
            }
        }
        """);

        var arguments = new ParsedArguments(
            [testDir],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), "CSV report not generated");

        var lines = File.ReadAllLines(csvPath);
        var allLines = string.Join("\n", lines);
        
        Assert.Contains("lodash", allLines.ToLowerInvariant());
        Assert.DoesNotContain("local-package", allLines);
    }

    [Fact]
    public async Task RunAsync_EmptyDirectory_GeneratesEmptyReport()
    {
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var arguments = new ParsedArguments(
            [emptyDir],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), "CSV report not generated");
        
        var lines = File.ReadAllLines(csvPath);
        Assert.Empty(lines);
    }

    [Fact]
    public async Task RunAsync_ProjectsAreIdentifiedCorrectly()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        var lines = File.ReadAllLines(csvPath);
        
        var allLines = string.Join("\n", lines);
        
        Assert.Contains("reactjs", allLines);
    }
}
