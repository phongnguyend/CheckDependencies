using CheckNugetPackages;

var parsedArgs = CommandLineParser.ParseParameters(args);
await PackageScanner.RunAsync(parsedArgs);