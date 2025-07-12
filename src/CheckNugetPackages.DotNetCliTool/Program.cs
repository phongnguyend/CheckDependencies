using System.Xml.Linq;

var packages = new List<(string Name, string Version, string Project)>();

var directories = args;

foreach (var directory in directories)
{
    var packagesInPackagesConfigureFiles = ScanPackagesInPackagesConfigureFiles(directory);
    var packagesInCsProjectFiles = ScanPackagesInCsProjectFiles(directory);
    packages.AddRange(packagesInPackagesConfigureFiles);
    packages.AddRange(packagesInCsProjectFiles);
}

var packageGroups = packages.GroupBy(x => new { x.Name, x.Version })
    .Select(g => new
    {
        g.Key.Name,
        g.Key.Version,
        Projects = string.Join(", ", g.Select(x => x.Project)),
        Url = $"https://www.nuget.org/packages/{g.Key.Name}/{g.Key.Version}"
    })
    .OrderBy(x => x.Name)
    .ThenBy(x => x.Version).ToList();

var ignoredPackages = new List<string>
{
    //"System.",
    //"Microsoft."
};

using (var fileStream = File.Open("packages.csv", FileMode.Create))
{
    using (var streamWriter = new StreamWriter(fileStream))
    {
        foreach (var package in packageGroups)
        {
            if (ignoredPackages.Any(x => package.Name.StartsWith(x)))
            {
                continue;
            }

            streamWriter.WriteLine($"{package.Name},{package.Version}, ,\"{package.Url}\",\"{package.Projects}\"");
        }
    }
}

static List<(string Name, string Version, string Project)> ScanPackagesInPackagesConfigureFiles(string directory)
{
    var files = Directory.EnumerateFiles(directory, "packages.config", SearchOption.AllDirectories);
    var packages = new List<(string Name, string Version, string Project)>();

    foreach (var file in files)
    {
        var projectName = new DirectoryInfo(Path.GetDirectoryName(file)).Name;
        XDocument xdoc = XDocument.Load(file);
        var packagesNode = xdoc.Descendants("packages").First();
        var packageNodes = packagesNode.Descendants("package");
        foreach (var node in packageNodes)
        {
            var packageName = node.Attribute("id")?.Value;
            var packageVersion = node.Attribute("version")?.Value;

            packages.Add((packageName, packageVersion, projectName));
        }
    }

    return packages;
}

static List<(string Name, string Version, string Project)> ScanPackagesInCsProjectFiles(string directory)
{
    var files = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories);
    var packages = new List<(string Name, string Version, string Project)>();

    foreach (var file in files)
    {
        var projectName = new DirectoryInfo(Path.GetDirectoryName(file)).Name;
        XDocument xdoc = XDocument.Load(file);
        var ItemGroupNodes = xdoc.Descendants("ItemGroup");
        foreach (var ItemGroupNode in ItemGroupNodes)
        {
            var packageNodes = ItemGroupNode.Descendants("PackageReference");
            foreach (var node in packageNodes)
            {
                var packageName = node.Attribute("Include")?.Value;
                var packageVersion = node.Attribute("Version")?.Value;

                if (string.IsNullOrWhiteSpace(packageName))
                    continue;

                packages.Add((packageName, packageVersion, projectName));
            }
        }

    }

    return packages;
}