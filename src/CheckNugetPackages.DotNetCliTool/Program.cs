using CheckNugetPackages;

var parsedArgs = CommandLineParser.ParseParameters(args);
PackageScanner.Run(parsedArgs);