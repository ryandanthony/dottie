---
title: Project Structure
sidebar_position: 2
description: Source code organization and directory structure
---

# Project Structure

dottie is organized as a .NET solution with two main projects: the CLI application and the configuration library.

## Directory Layout

```
src/
├── Dottie.Cli/              # CLI application
│   ├── Commands/            # CLI commands (validate, link, install)
│   ├── Output/              # Console output formatting
│   └── Utilities/           # Helper utilities
│
└── Dottie.Configuration/    # Configuration parsing library
    ├── Inheritance/         # Profile inheritance logic
    ├── Models/              # Domain models
    ├── Parsing/             # YAML parsing
    ├── Templates/           # Starter templates
    ├── Utilities/           # Helper utilities
    └── Validation/          # Configuration validation

tests/
├── Dottie.Cli.Tests/        # CLI unit tests
└── Dottie.Configuration.Tests/  # Configuration library tests

scripts/                     # Build and installation scripts
specs/                       # Feature specifications
design-notes/                # Design documentation
```

## Dottie.Cli

The CLI project contains the user-facing commands and output formatting.

### Commands

Each CLI command is implemented as a Spectre.Console command:

- `ValidateCommand` - Configuration validation
- `LinkCommand` - Dotfile symlink creation
- `InstallCommand` - Software installation

### Output

Consistent console output with:

- Progress indicators
- Color-coded status messages
- Structured result summaries

## Dottie.Configuration

The configuration library handles all YAML parsing, validation, and profile resolution.

### Models

Domain models representing configuration entities:

- `DottieConfiguration` - Root configuration object
- `ConfigProfile` - Individual profile definition
- `DotfileEntry` - Single dotfile mapping
- Install block models (`GithubReleaseItem`, `FontItem`, etc.)

### Inheritance

Profile inheritance resolution:

- `ProfileResolver` - Merges extended profiles
- Handles multi-level inheritance chains
- Detects circular references

### Validation

Configuration validation:

- `ConfigurationValidator` - Schema validation
- `DotfileEntryValidator` - Path validation
- Comprehensive error messages

## Testing Strategy

- **Unit Tests**: Cover parsing, validation, and resolution logic
- **Integration Tests**: End-to-end command execution
- **Build Validation**: Docusaurus build validates documentation

## Build Artifacts

```
publish/
└── linux-x64/
    └── dottie              # Self-contained Linux binary
```

The published binary is self-contained and includes the .NET runtime.
