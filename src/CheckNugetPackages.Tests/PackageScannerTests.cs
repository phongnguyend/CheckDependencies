namespace CheckNugetPackages.Tests;

public class PackageScannerTests
{
    private readonly string _samplesDir;

    public PackageScannerTests()
    {
        _samplesDir = Path.Combine(AppContext.BaseDirectory, "samples");
    }

    [Fact]
    public async Task RunAsync_Samples_Monolith()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath
        );

        await PackageScanner.RunAsync(arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_Monolith_IncludeTransivtie()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath,
            true
        );

        await PackageScanner.RunAsync(arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_Monolith_CPM()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith-CPM");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath
        );

        await PackageScanner.RunAsync(arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_Monolith_CPM_IncludeTransivtie()
    {
        var samplePath = Path.Combine(_samplesDir, "Monolith-CPM");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath,
            true
        );

        await PackageScanner.RunAsync(arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }
}
