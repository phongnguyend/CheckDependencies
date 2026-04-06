namespace CheckNpmPackages.Tests;

public class CommandLineParserTests
{
    [Fact]
    public void ParseParameters_NoArgs_ReturnsDefaults()
    {
        var result = CommandLineParser.ParseParameters([]);

        Assert.Single(result.Directories);
        Assert.Equal(Directory.GetCurrentDirectory(), result.Directories[0]);
        Assert.Equal(["csv", "html", "md"], result.ReportTypes);
        Assert.Null(result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_SingleDirectory()
    {
        var result = CommandLineParser.ParseParameters(["C:\\Projects"]);

        Assert.Single(result.Directories);
        Assert.Equal("C:\\Projects", result.Directories[0]);
    }

    [Fact]
    public void ParseParameters_MultipleDirectories()
    {
        var result = CommandLineParser.ParseParameters(["C:\\ProjectA", "C:\\ProjectB"]);

        Assert.Equal(2, result.Directories.Count);
        Assert.Equal("C:\\ProjectA", result.Directories[0]);
        Assert.Equal("C:\\ProjectB", result.Directories[1]);
    }

    [Fact]
    public void ParseParameters_ReportTypeCsv()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "csv"]);

        Assert.Single(result.ReportTypes);
        Assert.Contains("csv", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_ReportTypeHtml()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "html"]);

        Assert.Single(result.ReportTypes);
        Assert.Contains("html", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_ReportTypeMd()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "md"]);

        Assert.Single(result.ReportTypes);
        Assert.Contains("md", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_MultipleReportTypes()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "csv", "html"]);

        Assert.Equal(2, result.ReportTypes.Count);
        Assert.Contains("csv", result.ReportTypes);
        Assert.Contains("html", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_ReportTypeCaseInsensitive()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "CSV", "HTML", "MD"]);

        Assert.Equal(3, result.ReportTypes.Count);
        Assert.Contains("csv", result.ReportTypes);
        Assert.Contains("html", result.ReportTypes);
        Assert.Contains("md", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_DuplicateReportTypes_DeduplicatesResult()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "csv", "csv"]);

        Assert.Single(result.ReportTypes);
        Assert.Contains("csv", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_InvalidReportType_UsesDefaults()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "pdf"]);

        Assert.Equal(["csv", "html", "md"], result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_ReportDirectory()
    {
        var result = CommandLineParser.ParseParameters(["--report-directory", "C:\\Output"]);

        Assert.Equal("C:\\Output", result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_ReportDirectoryWithoutValue_RemainsNull()
    {
        var result = CommandLineParser.ParseParameters(["--report-directory"]);

        Assert.Null(result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_ReportDirectoryFollowedByAnotherParam_RemainsNull()
    {
        var result = CommandLineParser.ParseParameters(["--report-directory", "--report-type", "csv"]);

        Assert.Null(result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_DirectoryAndReportType()
    {
        var result = CommandLineParser.ParseParameters(["C:\\Projects", "--report-type", "csv"]);

        Assert.Single(result.Directories);
        Assert.Equal("C:\\Projects", result.Directories[0]);
        Assert.Single(result.ReportTypes);
        Assert.Contains("csv", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_DirectoryAndReportDirectory()
    {
        var result = CommandLineParser.ParseParameters(["C:\\Projects", "--report-directory", "C:\\Output"]);

        Assert.Single(result.Directories);
        Assert.Equal("C:\\Projects", result.Directories[0]);
        Assert.Equal("C:\\Output", result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_AllOptions()
    {
        var result = CommandLineParser.ParseParameters(
            ["C:\\ProjectA", "C:\\ProjectB", "--report-type", "csv", "md", "--report-directory", "C:\\Output"]);

        Assert.Equal(2, result.Directories.Count);
        Assert.Equal("C:\\ProjectA", result.Directories[0]);
        Assert.Equal("C:\\ProjectB", result.Directories[1]);
        Assert.Equal(2, result.ReportTypes.Count);
        Assert.Contains("csv", result.ReportTypes);
        Assert.Contains("md", result.ReportTypes);
        Assert.Equal("C:\\Output", result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_UnknownParameter_IgnoredAndUsesDefaults()
    {
        var result = CommandLineParser.ParseParameters(["--unknown-param"]);

        Assert.Single(result.Directories);
        Assert.Equal(Directory.GetCurrentDirectory(), result.Directories[0]);
        Assert.Equal(["csv", "html", "md"], result.ReportTypes);
        Assert.Null(result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_NoDirectories_WithParams_UsesDefaultDirectory()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "html"]);

        Assert.Single(result.Directories);
        Assert.Equal(Directory.GetCurrentDirectory(), result.Directories[0]);
    }

    [Fact]
    public void ParseParameters_NoReportType_UsesAllDefaults()
    {
        var result = CommandLineParser.ParseParameters(["C:\\Projects"]);

        Assert.Equal(["csv", "html", "md"], result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_ReportDirectoryBeforeReportType()
    {
        var result = CommandLineParser.ParseParameters(
            ["--report-directory", "C:\\Output", "--report-type", "html"]);

        Assert.Equal("C:\\Output", result.ReportDirectory);
        Assert.Single(result.ReportTypes);
        Assert.Contains("html", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_MixOfValidAndInvalidReportTypes()
    {
        var result = CommandLineParser.ParseParameters(["--report-type", "csv", "pdf", "html"]);

        Assert.Equal(2, result.ReportTypes.Count);
        Assert.Contains("csv", result.ReportTypes);
        Assert.Contains("html", result.ReportTypes);
    }

    [Fact]
    public void ParseParameters_IncludeTransitive_DefaultsFalse()
    {
        var result = CommandLineParser.ParseParameters([]);

        Assert.False(result.IncludeTransitive);
    }

    [Fact]
    public void ParseParameters_IncludeTransitive_SetToTrue()
    {
        var result = CommandLineParser.ParseParameters(["--include-transitive"]);

        Assert.True(result.IncludeTransitive);
    }

    [Fact]
    public void ParseParameters_IncludeTransitive_WithOtherOptions()
    {
        var result = CommandLineParser.ParseParameters(
            ["C:\\Projects", "--include-transitive", "--report-type", "csv", "--report-directory", "C:\\Output"]);

        Assert.True(result.IncludeTransitive);
        Assert.Single(result.Directories);
        Assert.Equal("C:\\Projects", result.Directories[0]);
        Assert.Single(result.ReportTypes);
        Assert.Contains("csv", result.ReportTypes);
        Assert.Equal("C:\\Output", result.ReportDirectory);
    }

    [Fact]
    public void ParseParameters_CheckLatestPatch_DefaultsFalse()
    {
        var result = CommandLineParser.ParseParameters([]);

        Assert.False(result.CheckLatestPatch);
    }

    [Fact]
    public void ParseParameters_CheckLatestPatch_SetToTrue()
    {
        var result = CommandLineParser.ParseParameters(["--check-latest-patch"]);

        Assert.True(result.CheckLatestPatch);
    }

    [Fact]
    public void ParseParameters_CheckLatestMinor_DefaultsFalse()
    {
        var result = CommandLineParser.ParseParameters([]);

        Assert.False(result.CheckLatestMinor);
    }

    [Fact]
    public void ParseParameters_CheckLatestMinor_SetToTrue()
    {
        var result = CommandLineParser.ParseParameters(["--check-latest-minor"]);

        Assert.True(result.CheckLatestMinor);
    }

    [Fact]
    public void ParseParameters_BothCheckLatestOptions()
    {
        var result = CommandLineParser.ParseParameters(["--check-latest-patch", "--check-latest-minor"]);

        Assert.True(result.CheckLatestPatch);
        Assert.True(result.CheckLatestMinor);
    }

    [Fact]
    public void ParseParameters_CheckLatestWithOtherOptions()
    {
        var result = CommandLineParser.ParseParameters(
            ["C:\\Projects", "--check-latest-patch", "--report-type", "csv", "--check-latest-minor"]);

        Assert.True(result.CheckLatestPatch);
        Assert.True(result.CheckLatestMinor);
        Assert.Single(result.Directories);
        Assert.Equal("C:\\Projects", result.Directories[0]);
        Assert.Single(result.ReportTypes);
        Assert.Contains("csv", result.ReportTypes);
    }
}
