---
title: Overview
sidebar_position: 1
description: High-level architecture of dottie
---

# Architecture Overview

dottie is built as a modular .NET CLI application with clear separation between configuration parsing, profile resolution, and command execution.

## Component Architecture

```mermaid
flowchart TD
    subgraph CLI["CLI Layer"]
        Commands[Commands]
        Output[Output Formatting]
    end
    
    subgraph Core["Configuration Library"]
        Loader[ConfigurationLoader]
        Resolver[ProfileResolver]
        Validator[ConfigurationValidator]
    end
    
    subgraph Installers["Install Handlers"]
        Apt[AptPackageInstaller]
        GitHub[GithubReleaseInstaller]
        Scripts[ScriptRunner]
        Fonts[FontInstaller]
        Snaps[SnapInstaller]
        AptRepos[AptRepoInstaller]
    end
    
    Commands --> Loader
    Loader --> Resolver
    Resolver --> Validator
    Commands --> Installers
```

## Data Flow

```mermaid
sequenceDiagram
    participant User
    participant CLI
    participant Loader
    participant Resolver
    participant Validator
    participant Installer
    
    User->>CLI: dottie install --profile work
    CLI->>Loader: Load dottie.yaml
    Loader->>Resolver: Resolve profile inheritance
    Resolver->>Validator: Validate merged configuration
    Validator-->>CLI: Configuration valid
    CLI->>Installer: Execute install blocks
    Installer-->>User: Installation complete
```

## Key Design Principles

### Declarative Configuration

Configuration is treated as desired state. dottie compares the current system state against the declared configuration and only applies necessary changes.

### Idempotent Operations

All operations are designed to be safely repeatable. Running `dottie install` multiple times produces the same result as running it once.

### Safe by Default

- Dry-run mode for previewing changes
- Explicit confirmation for destructive operations
- Automatic backups when overwriting files

### Profile Composition

Profiles can extend other profiles, enabling reuse and customization without duplication.

## Next Steps

- [Project Structure](./project-structure) - Source code organization
- [Design Decisions](./design-decisions) - Key architectural choices
