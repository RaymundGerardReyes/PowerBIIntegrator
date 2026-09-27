# Invariant
All CLI instructions, automation scripts, and command recommendations must use cross-shell compatible path formatting and option syntax across Windows PowerShell, CMD, and Git Bash (MINGW64).

## Rules

### Path Formatting
- ALWAYS use forward slashes (`/`) for directory separators in commands intended for or executed within Bash environments (Git Bash, WSL, MINGW64).
- NEVER mix forward slashes and backslashes in a single path argument (e.g., `src/Dir\File.ext`), as Bash treats `\` as an escape character and strips it.
- When referencing .NET projects with `--project`, prefer directory paths over explicit `.csproj` file paths whenever a single project file exists in that folder:
  - Good: `dotnet run --project backend/src/AnalyticsPlatform.Api`
  - Good: `dotnet test backend/AnalyticsPlatform.slnx`
  - Avoid: `dotnet run --project backend/src/AnalyticsPlatform.Api\AnalyticsPlatform.Api.csproj`

### Option Syntax
- NEVER insert a space between dashes and the option name (e.g., write `--project`, NEVER `-- project`).
- For passing runtime arguments through to the underlying application, use `--` only AFTER the CLI command options:
  - Example: `dotnet run --project backend/src/AnalyticsPlatform.McpServer -- --stdio`
