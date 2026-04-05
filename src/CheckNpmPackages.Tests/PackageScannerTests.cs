namespace CheckNpmPackages.Tests;

public class PackageScannerTests
{
    private readonly string _samplesDir;

    public PackageScannerTests()
    {
        _samplesDir = Path.Combine(AppContext.BaseDirectory, "samples");
    }

    [Fact]
    public async Task RunAsync_Samples_ReactJs()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_ReactJs_IncludeTransitive()
    {
        var samplePath = Path.Combine(_samplesDir, "reactjs");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath,
            true
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_VueJs()
    {
        var samplePath = Path.Combine(_samplesDir, "vuejs");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_VueJs_IncludeTransitive()
    {
        var samplePath = Path.Combine(_samplesDir, "vuejs");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath,
            true
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_Angular()
    {
        var samplePath = Path.Combine(_samplesDir, "angular");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_Angular_IncludeTransitive()
    {
        var samplePath = Path.Combine(_samplesDir, "angular");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath,
            true
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_NextJs()
    {
        var samplePath = Path.Combine(_samplesDir, "nextjs");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }

    [Fact]
    public async Task RunAsync_Samples_NextJs_IncludeTransitive()
    {
        var samplePath = Path.Combine(_samplesDir, "nextjs");

        var arguments = new ParsedArguments(
            [samplePath],
            ["csv", "html", "md"],
            samplePath,
            true
        );

        var packageGroups = await PackageScanner.RunAsync(arguments);
        ReportsWriter.Write(packageGroups, arguments);

        Assert.True(File.Exists(Path.Combine(samplePath, "packages.csv")), "CSV report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.html")), "HTML report not generated");
        Assert.True(File.Exists(Path.Combine(samplePath, "packages.md")), "Markdown report not generated");
    }
}
