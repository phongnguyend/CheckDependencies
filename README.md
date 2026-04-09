# Check Dependencies

A collection of .NET CLI tools and MCP (Model Context Protocol) tools to scan and generate reports for package dependencies in your projects.

## Tools

- **CheckNugetPackages** - Scans .NET projects for NuGet package dependencies.
- **CheckNpmPackages** - Scans Node.js projects for npm package dependencies.

Each tool is available as both a **.NET CLI tool** and a **.NET MCP tool**:

| Tool | CLI Tool | MCP Tool |
| --- | --- | --- |
| CheckNugetPackages | [CheckNugetPackages.DotNetCliTool](https://www.nuget.org/packages/CheckNugetPackages.DotNetCliTool) | [CheckNugetPackages.DotNetMcpTool](https://www.nuget.org/packages/CheckNugetPackages.DotNetMcpTool) |
| CheckNpmPackages | [CheckNpmPackages.DotNetCliTool](https://www.nuget.org/packages/CheckNpmPackages.DotNetCliTool) | [CheckNpmPackages.DotNetMcpTool](https://www.nuget.org/packages/CheckNpmPackages.DotNetMcpTool) |

## Installation

### As .NET Global Tool (CLI)

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

### As .NET Global Tool (MCP)

Install the MCP tools globally using the .NET CLI:

```bash
# Install CheckNugetPackages MCP Tool
dotnet tool install --global CheckNugetPackages.DotNetMcpTool

# Install CheckNpmPackages MCP Tool
dotnet tool install --global CheckNpmPackages.DotNetMcpTool
```

Or visit the NuGet package pages:
- [CheckNugetPackages.DotNetMcpTool](https://www.nuget.org/packages/CheckNugetPackages.DotNetMcpTool)
- [CheckNpmPackages.DotNetMcpTool](https://www.nuget.org/packages/CheckNpmPackages.DotNetMcpTool)

### Update Existing Installation

```bash
# Update CheckNugetPackages
dotnet tool update --global CheckNugetPackages.DotNetCliTool

# Update CheckNpmPackages
dotnet tool update --global CheckNpmPackages.DotNetCliTool

# Update CheckNugetPackages MCP Tool
dotnet tool update --global CheckNugetPackages.DotNetMcpTool

# Update CheckNpmPackages MCP Tool
dotnet tool update --global CheckNpmPackages.DotNetMcpTool
```

### Uninstall

```bash
# Uninstall CheckNugetPackages
dotnet tool uninstall --global CheckNugetPackages.DotNetCliTool

# Uninstall CheckNpmPackages
dotnet tool uninstall --global CheckNpmPackages.DotNetCliTool

# Uninstall CheckNugetPackages MCP Tool
dotnet tool uninstall --global CheckNugetPackages.DotNetMcpTool

# Uninstall CheckNpmPackages MCP Tool
dotnet tool uninstall --global CheckNpmPackages.DotNetMcpTool
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
- `--include-transitive` - When specified, scans `project.assets.json` files for all direct and transitive dependencies instead of only scanning `.csproj` files. If `project.assets.json` does not exist for a project, it falls back to scanning the `.csproj` file as normal.
- `--check-latest-patch` - When specified, includes the latest patch version information in the reports. This shows the latest version with the same major and minor version numbers. Adds additional columns with license, published date, deprecated status, and vulnerability information for the patch version.
- `--check-latest-minor` - When specified, includes the latest minor version information in the reports. This shows the latest version with the same major version number. Adds additional columns with license, published date, deprecated status, and vulnerability information for the minor version.
- `--check-latest` - When specified, includes the latest version information in the reports. Adds additional columns with license, published date, deprecated status, and vulnerability information for the latest version.
- `--include-prerelease` - When specified, includes prerelease versions (alpha, beta, rc, etc.) when checking for the latest versions. By default, only stable releases are considered when using `--check-latest-patch`, `--check-latest-minor`, or `--check-latest`.

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

# Scan project.assets.json for all direct and transitive dependencies
CheckNugetPackages "C:\MyProject" --include-transitive

# Combine transitive scan with other options
CheckNugetPackages "C:\MyProject" --include-transitive --report-type csv html --report-directory "C:\Reports"

# Include latest patch version information
CheckNugetPackages "C:\MyProject" --check-latest-patch

# Include latest minor version information
CheckNugetPackages "C:\MyProject" --check-latest-minor

# Include both latest patch and minor versions
CheckNugetPackages "C:\MyProject" --check-latest-patch --check-latest-minor

# Include latest version information
CheckNugetPackages "C:\MyProject" --check-latest

# Combine all options
CheckNugetPackages "C:\MyProject" --include-transitive --check-latest-patch --check-latest-minor --check-latest --report-type csv html --report-directory "C:\Reports"
```

#### Output Files

The tool generates the following files:

- **packages.csv** - CSV format with columns: Name, Version, License, URL, Projects
- **packages.html** - HTML report with formatted table and links to NuGet.org
- **packages.md** - Markdown report with table and links to NuGet.org

**Upgrade Command Files** (when using `--check-latest-patch`, `--check-latest-minor`, or `--check-latest`):
- **upgrade-to-latest-patch-commands.txt** - Contains `dotnet add package` commands for upgrading to the latest patch versions
- **upgrade-to-latest-minor-commands.txt** - Contains `dotnet add package` commands for upgrading to the latest minor versions
- **upgrade-to-latest-commands.txt** - Contains `dotnet add package` commands for upgrading to the latest versions

These upgrade command files contain only packages that have a newer version available and can be executed to perform the upgrades.

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
- `--include-transitive` - When specified, scans `package-lock.json` files for all direct and transitive dependencies instead of only scanning `package.json` files. This provides a more complete view of all resolved packages, including nested dependencies.
- `--check-latest-patch` - When specified, includes the latest patch version information in the reports. This shows the latest version with the same major and minor version numbers. Adds additional columns with license, published date, deprecated status, and vulnerability information for the patch version.
- `--check-latest-minor` - When specified, includes the latest minor version information in the reports. This shows the latest version with the same major version number. Adds additional columns with license, published date, deprecated status, and vulnerability information for the minor version.
- `--check-latest` - When specified, includes the latest version information in the reports. Adds additional columns with license, published date, deprecated status, and vulnerability information for the latest version.
- `--include-prerelease` - When specified, includes prerelease versions (alpha, beta, rc, etc.) when checking for the latest versions. By default, only stable releases are considered when using `--check-latest-patch`, `--check-latest-minor`, or `--check-latest`.

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

# Scan package-lock.json for all direct and transitive dependencies
CheckNpmPackages "C:\MyProject\ClientApp" --include-transitive

# Combine transitive scan with other options
CheckNpmPackages "C:\MyProject" --include-transitive --report-type csv html --report-directory "C:\Reports"

# Include latest patch version information
CheckNpmPackages "C:\MyProject" --check-latest-patch

# Include latest minor version information
CheckNpmPackages "C:\MyProject" --check-latest-minor

# Include both latest patch and minor versions
CheckNpmPackages "C:\MyProject" --check-latest-patch --check-latest-minor

# Include latest version information
CheckNpmPackages "C:\MyProject" --check-latest

# Combine all options
CheckNpmPackages "C:\MyProject" --include-transitive --check-latest-patch --check-latest-minor --check-latest --report-type csv html --report-directory "C:\Reports"
```

#### Output Files

- **packages.csv** - CSV format with columns: Name, Version, License, URL, Projects
- **packages.html** - HTML report with formatted table and links to npmjs.com
- **packages.md** - Markdown report with table and links to npmjs.com

**Upgrade Command Files** (when using `--check-latest-patch`, `--check-latest-minor`, or `--check-latest`):
- **upgrade-to-latest-patch-commands.txt** - Contains `npm install` commands for upgrading to the latest patch versions
- **upgrade-to-latest-minor-commands.txt** - Contains `npm install` commands for upgrading to the latest minor versions
- **upgrade-to-latest-commands.txt** - Contains `npm install` commands for upgrading to the latest versions

These upgrade command files contain only packages that have a newer version available and can be executed to perform the upgrades.

#### Features

- Scans both `dependencies` and `devDependencies`
- Automatically skips `node_modules` directories
- Ignores local file dependencies (starting with `file:`)
- Generates links to npmjs.com package pages
- Supports `package-lock.json` scanning for complete transitive dependency analysis

## MCP Tools

The MCP (Model Context Protocol) tools provide the same functionality as the CLI tools but are designed to be used as MCP servers, enabling integration with AI assistants and other MCP-compatible clients.

### CheckNugetPackages MCP Tool

The `CheckNugetPackages.DotNetMcpTool` exposes a `CheckNugetPackages` MCP tool that scans directories for NuGet package dependencies and generates reports.

**Tool Name:** `CheckNugetPackages`

**Parameters:**
| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `directories` | `string[]` | No | Current directory | One or more directory paths to scan for NuGet packages. If not provided, scans current directory. |
| `reportTypes` | `string[]` | No | `["csv", "html", "md"]` | Report types to generate (valid values: `csv`, `html`, `md`). If not provided, generates CSV, HTML, and Markdown reports. |
| `reportDirectory` | `string` | No | Current directory | Directory where reports will be saved. If not provided, saves to current directory. |
| `includeTransitive` | `bool` | No | `false` | When `true`, scans `project.assets.json` for all direct and transitive dependencies instead of only scanning `.csproj` files. |
| `includePrerelease` | `bool` | No | `false` | When `true`, includes prerelease versions (alpha, beta, rc, etc.) in package analysis. When `false`, only analyzes stable versions. |
| `writeReports` | `bool` | No | `true` | When `true`, generates and writes report files. When `false`, only returns package data without writing files. |

**Return Type:** `GeneratedReports`
- `Packages` - List of scanned package entries
- `ReportPaths` - List of file paths where reports were generated (empty list if `writeReports` is `false`)

#### MCP Configuration Example

To configure the CheckNugetPackages MCP tool in your MCP client (e.g., VS Code), add the following to your MCP settings:

```json
{
  "servers": {
    "CheckNugetPackages": {
      "type": "stdio",
      "command": "CheckNugetPackagesMcp"
    }
  }
}
```

### CheckNpmPackages MCP Tool

The `CheckNpmPackages.DotNetMcpTool` exposes a `CheckNpmPackages` MCP tool that scans directories for npm package dependencies and generates reports.

**Tool Name:** `CheckNpmPackages`

**Parameters:**
| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `directories` | `string[]` | No | Current directory | One or more directory paths to scan for npm packages. If not provided, scans current directory. |
| `reportTypes` | `string[]` | No | `["csv", "html", "md"]` | Report types to generate (valid values: `csv`, `html`, `md`). If not provided, generates CSV, HTML, and Markdown reports. |
| `reportDirectory` | `string` | No | Current directory | Directory where reports will be saved. If not provided, saves to current directory. |
| `includeTransitive` | `bool` | No | `false` | When `true`, scans `package-lock.json` for all direct and transitive dependencies instead of only scanning `package.json`. |
| `includePrerelease` | `bool` | No | `false` | When `true`, includes prerelease versions (alpha, beta, rc, etc.) in package analysis. When `false`, only analyzes stable versions. |
| `writeReports` | `bool` | No | `true` | When `true`, generates and writes report files. When `false`, only returns package data without writing files. |

**Return Type:** `GeneratedReports`
- `Packages` - List of scanned package entries
- `ReportPaths` - List of file paths where reports were generated (empty list if `writeReports` is `false`)

#### MCP Configuration Example

To configure the CheckNpmPackages MCP tool in your MCP client (e.g., VS Code), add the following to your MCP settings:

```json
{
  "servers": {
    "CheckNpmPackages": {
      "type": "stdio",
      "command": "CheckNpmPackagesMcp"
    }
  }
}
```

#### Additional MCP Tools

Both CheckNugetPackages and CheckNpmPackages MCP tools also expose the following additional tools for retrieving package version information:

**GetNugetPackageVersion / GetNpmPackageVersion**
- Get information about a specific version of a package, including license, published date, deprecation, and vulnerability status.

**GetNugetPackageLatestVersion / GetNpmPackageLatestVersion**
- Get information about the latest version of a package.

**GetNugetPackageLatestPatchVersion / GetNpmPackageLatestPatchVersion**
- Get information about the latest patch version of a package (latest version with the same major and minor version numbers).

**GetNugetPackageLatestMinorVersion / GetNpmPackageLatestMinorVersion**
- Get information about the latest minor version of a package (latest version with the same major version number).

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

### Generate Upgrade Commands

Generate executable upgrade commands to streamline package updates:

#### For NuGet Packages

```bash
# Generate upgrade commands for latest patch versions
CheckNugetPackages "C:\MyProject" --check-latest-patch --report-directory "C:\Reports"

# Then execute the commands
# The file upgrade-to-latest-patch-commands.txt will contain:
# dotnet add package Newtonsoft.Json --version 13.0.2
# dotnet add package Microsoft.EntityFrameworkCore --version 8.0.1
# ... etc
```

#### For npm Packages

```bash
# Generate upgrade commands for latest minor versions
CheckNpmPackages "C:\MyProject\ClientApp" --check-latest-minor --report-directory "C:\Reports"

# Then execute the commands
# The file upgrade-to-latest-minor-commands.txt will contain:
# npm install react@18.2.0
# npm install lodash@4.17.21
# ... etc
```

The upgrade command files are generated only when they contain packages with newer versions available. You can execute these files to automatically upgrade your dependencies:

```bash
# Execute NuGet upgrade commands (Windows)
for /f %i in (upgrade-to-latest-patch-commands.txt) do %i

# Execute npm upgrade commands
cat upgrade-to-latest-minor-commands.txt | sh
```

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

- [Report Issues](https://github.com/phongnguyend/CheckDependencies/issues)
- [CheckNugetPackages.DotNetCliTool on NuGet](https://www.nuget.org/packages/CheckNugetPackages.DotNetCliTool)
- [CheckNpmPackages.DotNetCliTool on NuGet](https://www.nuget.org/packages/CheckNpmPackages.DotNetCliTool)
- [CheckNugetPackages.DotNetMcpTool on NuGet](https://www.nuget.org/packages/CheckNugetPackages.DotNetMcpTool)
- [CheckNpmPackages.DotNetMcpTool on NuGet](https://www.nuget.org/packages/CheckNpmPackages.DotNetMcpTool)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [NuGet Package Version Reference | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning?tabs=semver20sort)
- [Central Package Management | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
