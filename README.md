# Check Dependencies

A collection of .NET CLI tools to scan and generate reports for package dependencies in your projects.

## Tools

- **CheckNugetPackages** - Scans .NET projects for NuGet package dependencies.
- **CheckNpmPackages** - Scans Node.js projects for npm package dependencies.

## Installation

### As .NET Global Tool

Install the tools globally using the .NET CLI:

```bash
# Install CheckNugetPackages
dotnet tool install --global CheckNugetPackages.DotNetCliTool

# Install CheckNpmPackages
dotnet tool install --global CheckNpmPackages.DotNetCliTool
```

Or visit the NuGet package pages:
- [CheckNugetPackages.DotNetCliTool](https://www.nuget.org/packages/CheckNugetPackages.DotNetCliTool)
- [CheckNpmPackages.DotNetCliTool](https://www.nuget.org/packages/CheckNpmPackages.DotNetCliTool)

### Update Existing Installation

```bash
# Update CheckNugetPackages
dotnet tool update --global CheckNugetPackages.DotNetCliTool

# Update CheckNpmPackages
dotnet tool update --global CheckNpmPackages.DotNetCliTool
```

### Uninstall

```bash
# Uninstall CheckNugetPackages
dotnet tool uninstall --global CheckNugetPackages.DotNetCliTool

# Uninstall CheckNpmPackages
dotnet tool uninstall --global CheckNpmPackages.DotNetCliTool
```

## Usage

### CheckNugetPackages

Scans directories for NuGet packages used in .NET projects (both `packages.config` and SDK-style `.csproj` files).

#### Basic Usage

```bash
# Scan a single directory and generate both CSV and HTML reports
CheckNugetPackages "C:\MyProject\API"

# Scan multiple directories
CheckNugetPackages "C:\Project1\API" "C:\Project2\API"
```

#### Command Line Options

```bash
CheckNugetPackages [directories...] [options]
```

**Arguments:**
- `directories` - One or more directory paths to scan (all arguments before first `--` parameter)

**Options:**
- `--report-type <types>` - Report types to generate (default: `csv html md`)
  - Valid values: `csv`, `html`, `md`
  - Can specify multiple types separated by spaces
- `--report-directory <path>` - Directory where reports will be saved (default: current directory)

#### Examples

```bash
# Generate only CSV report
CheckNugetPackages "C:\MyProject" --report-type csv

# Generate only HTML report
CheckNugetPackages "C:\MyProject" --report-type html

# Generate only Markdown report
CheckNugetPackages "C:\MyProject" --report-type md

# Specify output directory
CheckNugetPackages "C:\MyProject" --report-directory "C:\Reports"

# Scan multiple directories with custom output
CheckNugetPackages "C:\Project1" "C:\Project2" --report-type csv html md --report-directory "output"

# Generate CSV report in a specific directory
CheckNugetPackages "C:\MyProject\API" "C:\MyProject\Services" --report-type csv --report-directory "reports"
```

#### Output Files

The tool generates the following files:

- **packages.csv** - CSV format with columns: Name, Version, License, URL, Projects
- **packages.html** - HTML report with formatted table and links to NuGet.org
- **packages.md** - Markdown report with table and links to NuGet.org

#### Sample CSV Output

```csv
Newtonsoft.Json,13.0.3, ,"https://www.nuget.org/packages/Newtonsoft.Json/13.0.3","ProjectA, ProjectB"
Microsoft.EntityFrameworkCore,8.0.0, ,"https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/8.0.0","ProjectA"
```

#### Sample HTML Output

The HTML report includes:
- Styled table with package information
- Direct links to NuGet.org package pages
- List of projects using each package version
- Generation timestamp

#### Sample Markdown Output

The Markdown report includes:
- Header with generation timestamp
- Table with package name, version (linked to NuGet.org), license, and projects

### CheckNpmPackages

Scans directories for npm packages used in Node.js projects (`package.json` files).

#### Basic Usage

```bash
# Scan a single directory
CheckNpmPackages "C:\MyProject\UI"

# Scan multiple directories
CheckNpmPackages "C:\Project1\UI" "C:\Project2\UI"
```

#### Command Line Options

```bash
CheckNpmPackages [directories...] [options]
```

**Arguments:**
- `directories` - One or more directory paths to scan

**Options:**
- `--report-type <types>` - Report types to generate (default: `csv html md`)
  - Valid values: `csv`, `html`, `md`
  - Can specify multiple types separated by spaces
- `--report-directory <path>` - Directory where reports will be saved (default: current directory)

#### Examples

```bash
# Scan and generate report
CheckNpmPackages "C:\MyProject\ClientApp"

# Generate only Markdown report
CheckNpmPackages "C:\MyProject\ClientApp" --report-type md

# Specify output directory
CheckNpmPackages "C:\MyProject" --report-directory "C:\Reports"

# Scan multiple directories
CheckNpmPackages "C:\Project1\ClientApp" "C:\Project2\ClientApp" --report-directory "npm-reports"
```

#### Output Files

- **packages.csv** - CSV format with columns: Name, Version, License, URL, Projects
- **packages.html** - HTML report with formatted table and links to npmjs.com
- **packages.md** - Markdown report with table and links to npmjs.com

#### Features

- Scans both `dependencies` and `devDependencies`
- Automatically skips `node_modules` directories
- Ignores local file dependencies (starting with `file:`)
- Generates links to npmjs.com package pages

## Use Cases

### Audit Package Usage

Generate reports to understand which packages and versions are being used across multiple projects.

```bash
CheckNugetPackages "C:\Solutions\Project1" "C:\Solutions\Project2" --report-directory "C:\Audits\2024-01"
```

### Identify Version Inconsistencies

The reports group packages by name and version, making it easy to identify projects using different versions of the same package.

### Package Upgrade Planning

Use the HTML report with direct links to NuGet.org to quickly check for available updates and plan upgrade paths.

### Documentation

Generate reports as documentation of third-party dependencies for compliance or security reviews.

## Troubleshooting

### Tool Not Found After Installation

Ensure the .NET tools directory is in your PATH:

```bash
# Windows
%USERPROFILE%\.dotnet\tools

# macOS/Linux
$HOME/.dotnet/tools
```

### Permission Errors

Run the command prompt or terminal as administrator when installing global tools.

### Report Not Generated

- Verify the directory paths exist
- Check that you have write permissions to the output directory
- Ensure projects contain `packages.config`, `.csproj`, or `package.json` files

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the terms specified in the LICENSE file.

## Links

- [GitHub Repository](https://github.com/phongnguyend/CheckDependencies)
- [Report Issues](https://github.com/phongnguyend/CheckDependencies/issues)
- [CheckNugetPackages.DotNetCliTool on NuGet](https://www.nuget.org/packages/CheckNugetPackages.DotNetCliTool)
- [CheckNpmPackages.DotNetCliTool on NuGet](https://www.nuget.org/packages/CheckNpmPackages.DotNetCliTool)
- [NuGet Package Version Reference | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning?tabs=semver20sort)
- [Central Package Management | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
