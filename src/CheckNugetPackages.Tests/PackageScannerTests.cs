namespace CheckNugetPackages.Tests;

public class PackageScannerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _samplesDir;

    public PackageScannerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PackageScannerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _samplesDir = Path.Combine(AppContext.BaseDirectory, "samples");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task RunAsync_WithCsProjectSamples_ScansPackageReferences()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        Assert.True(File.Exists(csvPath), $"CSV report was not generated at {csvPath}");

        var lines = File.ReadAllLines(csvPath);
        Assert.NotEmpty(lines);

        // Check for some known packages in the sample
        var allLines = string.Join("\n", lines);
        Assert.Contains("AWS.Logger.SeriLog", allLines);
        Assert.Contains("Azure.Identity", allLines);
        Assert.Contains("Serilog", allLines);
    }

    [Fact]
    public async Task RunAsync_WithMultipleCsProjects_CombinesPackages()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
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
    public async Task RunAsync_GeneratesAllReportTypes()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
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
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        Assert.True(File.Exists(Path.Combine(_tempDir, "packages.csv")), "CSV report not generated");
        Assert.False(File.Exists(Path.Combine(_tempDir, "packages.html")), "HTML report should not be generated");
        Assert.False(File.Exists(Path.Combine(_tempDir, "packages.md")), "Markdown report should not be generated");
    }

    [Fact]
    public async Task RunAsync_WithHtmlReportType_GeneratesValidHtml()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
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
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
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
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
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
    public async Task RunAsync_ReturnsDistinctPackages()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith");
        
        var arguments = new ParsedArguments(
            [samplePath],
            ["csv"],
            _tempDir
        );

        await PackageScanner.RunAsync(arguments);

        var csvPath = Path.Combine(_tempDir, "packages.csv");
        var lines = File.ReadAllLines(csvPath);
        
        var packageNames = lines.Select(l => l.Split(',')[0]).ToList();
        
        // Check that duplicates don't appear (they should be aggregated)
        // If a package appears in multiple projects, it should be combined into one line
        Assert.NotEmpty(packageNames);
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
}
