using CheckNugetPackages;

var parsedArgs = CommandLineParser.ParseParameters(args);
var packageGroups = await PackageScanner.RunAsync(parsedArgs);
ReportsWriter.Write(packageGroups, parsedArgs);