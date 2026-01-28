# Quickstart: YAML Configuration System

**Feature**: 001-yaml-configuration  
**Date**: 2026-01-28  
**Purpose**: Project setup and build instructions for developers

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Linux x64 or arm64 (for runtime testing)
- Git

## Project Initialization

### 1. Create Solution Structure

```bash
# From repository root
mkdir -p src/Dottie.Cli src/Dottie.Configuration
mkdir -p tests/Dottie.Configuration.Tests tests/Dottie.Cli.Tests

# Create solution
dotnet new sln -n Dottie

# Create projects
dotnet new console -n Dottie.Cli -o src/Dottie.Cli
dotnet new classlib -n Dottie.Configuration -o src/Dottie.Configuration
dotnet new xunit -n Dottie.Configuration.Tests -o tests/Dottie.Configuration.Tests
dotnet new xunit -n Dottie.Cli.Tests -o tests/Dottie.Cli.Tests

# Add to solution
dotnet sln add src/Dottie.Cli/Dottie.Cli.csproj
dotnet sln add src/Dottie.Configuration/Dottie.Configuration.csproj
dotnet sln add tests/Dottie.Configuration.Tests/Dottie.Configuration.Tests.csproj
dotnet sln add tests/Dottie.Cli.Tests/Dottie.Cli.Tests.csproj

# Add project references
dotnet add src/Dottie.Cli/Dottie.Cli.csproj reference src/Dottie.Configuration/Dottie.Configuration.csproj
dotnet add tests/Dottie.Configuration.Tests/Dottie.Configuration.Tests.csproj reference src/Dottie.Configuration/Dottie.Configuration.csproj
dotnet add tests/Dottie.Cli.Tests/Dottie.Cli.Tests.csproj reference src/Dottie.Cli/Dottie.Cli.csproj
```

### 2. Add NuGet Packages

```bash
# CLI project
dotnet add src/Dottie.Cli/Dottie.Cli.csproj package Spectre.Console
dotnet add src/Dottie.Cli/Dottie.Cli.csproj package Spectre.Console.Cli

# Configuration library
dotnet add src/Dottie.Configuration/Dottie.Configuration.csproj package YamlDotNet

# Test projects
dotnet add tests/Dottie.Configuration.Tests/Dottie.Configuration.Tests.csproj package FluentAssertions
dotnet add tests/Dottie.Cli.Tests/Dottie.Cli.Tests.csproj package FluentAssertions
```

### 3. Configure for AOT/Trimming

Edit `src/Dottie.Cli/Dottie.Cli.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- AOT/Trimming configuration -->
    <PublishAot>true</PublishAot>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
    <InvariantGlobalization>true</InvariantGlobalization>
    
    <!-- Single file output -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    
    <!-- Assembly info -->
    <AssemblyName>dottie</AssemblyName>
  </PropertyGroup>
</Project>
```

Edit `src/Dottie.Configuration/Dottie.Configuration.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Trimming compatibility -->
    <IsTrimmable>true</IsTrimmable>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  </PropertyGroup>
</Project>
```

---

## Build Commands

### Debug Build

```bash
dotnet build
```

### Release Build

```bash
dotnet build -c Release
```

### Run Tests

```bash
dotnet test
```

### Publish Trimmed Binary

```bash
# Linux x64
dotnet publish src/Dottie.Cli -c Release -r linux-x64 -o dist/linux-x64

# Linux arm64
dotnet publish src/Dottie.Cli -c Release -r linux-arm64 -o dist/linux-arm64
```

---

## Development Workflow

### 1. Create Model Types

Start with the data model types in `src/Dottie.Configuration/Models/`:

```csharp
// DottieConfiguration.cs
namespace Dottie.Configuration.Models;

public sealed record DottieConfiguration
{
    public required Dictionary<string, Profile> Profiles { get; init; }
}
```

### 2. Add YAML Deserialization

Create the YAML context for AOT in `src/Dottie.Configuration/Parsing/`:

```csharp
// ConfigurationYamlContext.cs
using YamlDotNet.Serialization;

namespace Dottie.Configuration.Parsing;

[YamlStaticContext]
[YamlSerializable(typeof(DottieConfiguration))]
[YamlSerializable(typeof(Profile))]
// ... add all model types
public partial class ConfigurationYamlContext : StaticContext { }
```

### 3. Implement Validation

Create validators in `src/Dottie.Configuration/Validation/`:

```csharp
// ConfigurationValidator.cs
namespace Dottie.Configuration.Validation;

public class ConfigurationValidator
{
    public ValidationResult Validate(DottieConfiguration config, string repoRoot)
    {
        var errors = new List<ValidationError>();
        // Validation logic here
        return new ValidationResult { Errors = errors };
    }
}
```

### 4. Write Tests First

Create test fixtures in `tests/Dottie.Configuration.Tests/Fixtures/`:

```yaml
# valid-minimal.yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
```

Write tests:

```csharp
// ConfigurationLoaderTests.cs
public class ConfigurationLoaderTests
{
    [Fact]
    public void Load_ValidMinimalConfig_ReturnsConfiguration()
    {
        // Arrange
        var yaml = File.ReadAllText("Fixtures/valid-minimal.yaml");
        var loader = new ConfigurationLoader();
        
        // Act
        var result = loader.Load(yaml);
        
        // Assert
        result.Should().NotBeNull();
        result.Profiles.Should().ContainKey("default");
    }
}
```

---

## CLI Structure

### Entry Point

```csharp
// Program.cs
using Spectre.Console.Cli;
using Dottie.Cli.Commands;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<ValidateCommand>("validate")
        .WithDescription("Validate a dottie.yaml configuration file");
});

return app.Run(args);
```

### Validate Command

```csharp
// Commands/ValidateCommand.cs
using Spectre.Console;
using Spectre.Console.Cli;

public class ValidateCommand : Command<ValidateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<profile>")]
        public required string Profile { get; init; }
        
        [CommandOption("-c|--config")]
        public string? ConfigPath { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        // Implementation
        return 0;
    }
}
```

---

## Testing Strategy

### Unit Tests (Dottie.Configuration.Tests)

| Area | Test Focus |
|------|------------|
| Parsing | Valid YAML → typed objects |
| Parsing | Invalid YAML → clear errors with line numbers |
| Validation | Missing required fields detected |
| Validation | Script path security (no traversal) |
| Inheritance | Merge rules applied correctly |
| Inheritance | Circular dependency detected |

### Integration Tests (Dottie.Cli.Tests)

| Area | Test Focus |
|------|------------|
| CLI | `dottie validate default` with valid config |
| CLI | `dottie validate missing` shows available profiles |
| CLI | Error output is formatted correctly |

---

## Verification Checklist

Before marking implementation complete:

- [ ] `dotnet build` succeeds with no warnings
- [ ] `dotnet test` passes all tests
- [ ] `dotnet publish -c Release -r linux-x64` produces working binary
- [ ] Binary size < 50MB
- [ ] `./dottie validate default` works with example config
- [ ] Error messages include line numbers for YAML errors
- [ ] Profile inheritance merges correctly

---

## References

- [Spec](spec.md) — Functional requirements
- [Data Model](data-model.md) — C# type definitions
- [Research](research.md) — Technical decisions
- [Contracts](contracts/) — JSON Schema and examples
