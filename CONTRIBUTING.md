# Contributing to PSProxmoxVE

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing to PSProxmoxVE.

## Development Setup

### Prerequisites

- [.NET SDK 9.0+](https://dotnet.microsoft.com/download)
- [PowerShell 7.2+](https://github.com/PowerShell/PowerShell) (for running Pester tests)
- [Pester 5](https://pester.dev/) (`Install-Module Pester -MinimumVersion 5.0 -Force`)
- An IDE with C# support (Visual Studio, VS Code with C# Dev Kit, Rider)

### Building

```bash
# Clone the repository
git clone https://github.com/goodolclint/PSProxmoxVE.git
cd PSProxmoxVE

# Restore and build
dotnet build

# Publish for local testing
dotnet publish src/PSProxmoxVE/PSProxmoxVE.csproj --framework netstandard2.0 --output ./publish/netstandard2.0
```

### Running Tests

```bash
# xUnit unit tests (C# core library)
dotnet test tests/PSProxmoxVE.Core.Tests

# Pester cmdlet tests (requires built module)
pwsh -Command "Invoke-Pester -Path tests/PSProxmoxVE.Tests -ExcludeTag Integration"

# Integration tests (requires live PVE node -- see tests/infrastructure/README.md)
pwsh -Command "Invoke-Pester -Path tests/PSProxmoxVE.Tests/Integration -Tag Integration"
```

## Coding Standards

### C# Style

- Use C# 10.0 language features.
- Enable nullable reference types (`#nullable enable`).
- Follow standard .NET naming conventions (PascalCase for public members, camelCase for locals).
- Use 4-space indentation (see `.editorconfig`).

### Cmdlet Design

- **Verb-Noun naming**: Use approved PowerShell verbs (`Get-Verb` to see the list). Noun prefix is always `Pve`.
- **ShouldProcess**: All mutating cmdlets must implement `SupportsShouldProcess = true` and call `ShouldProcess()`.
- **ConfirmImpact**: Set `ConfirmImpact.High` on destructive operations (Remove, Stop, Reset).
- **OutputType**: Every cmdlet must have an `[OutputType]` attribute.
- **Pipeline support**: Use `ValueFromPipelineByPropertyName = true` on `Node`, `VmId`, and similar parameters.
- **WriteVerbose**: Add a `WriteVerbose` call describing the API operation before making API calls.
- **HelpMessage**: All `[Parameter]` attributes should include a `HelpMessage`.
- **ValidateRange**: VmId parameters should have `[ValidateRange(100, 999999999)]`.

### Commit Convention

This project uses [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` -- new feature
- `fix:` -- bug fix
- `test:` -- test additions or changes
- `ci:` -- CI/CD changes
- `docs:` -- documentation changes
- `refactor:` -- code refactoring
- `chore:` -- maintenance tasks

## Pull Request Process

1. Fork the repository and create a feature branch from `main`.
2. Make your changes following the coding standards above.
3. Add or update tests for your changes.
4. Ensure `dotnet build` succeeds with zero warnings.
5. Ensure all existing tests pass.
6. Update `CHANGELOG.md` under `[Unreleased]` with a description of your change.
7. Submit a pull request with a clear description of the change and its motivation.

## Adding a New Cmdlet

1. Create the cmdlet class in the appropriate `Cmdlets/` subdirectory.
2. Create or extend a service class in `PSProxmoxVE.Core/Services/`.
3. Add model classes in `PSProxmoxVE.Core/Models/` if needed.
4. Add the cmdlet name to `CmdletsToExport` in `PSProxmoxVE.psd1`.
5. Add Pester unit tests in `tests/PSProxmoxVE.Tests/`.
6. Add integration test coverage in `Integration.Tests.ps1` if applicable.
7. Update `README.md` cmdlet reference table.

## Reporting Issues

- Use [GitHub Issues](https://github.com/goodolclint/PSProxmoxVE/issues) for bug reports and feature requests.
- For security vulnerabilities, see [SECURITY.md](SECURITY.md).
