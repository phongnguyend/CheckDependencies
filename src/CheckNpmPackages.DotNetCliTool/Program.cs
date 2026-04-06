using CheckNpmPackages;

var parsedArgs = CommandLineParser.ParseParameters(args);
var packageGroups = await PackageScanner.RunAsync(parsedArgs);
var generatedReports = ReportsWriter.Write(packageGroups, parsedArgs);

foreach (var report in generatedReports)
{
    Console.WriteLine($"Report generated: {report}");
}