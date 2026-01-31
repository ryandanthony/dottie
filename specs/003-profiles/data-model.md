# Data Model: Configuration Profiles

**Feature**: 003-profiles  
**Date**: 2026-01-30  
**Purpose**: Document changes to existing entities for profile enhancements

---

## Overview

This feature primarily extends existing types from 001-yaml-configuration. The core data model (`ConfigProfile`, `DottieConfiguration`, `ProfileMerger`) already supports inheritance via the `Extends` property. This document captures the minimal additions needed.

---

## Entity Relationship (Changes Highlighted)

```
┌─────────────────────────────────────────────────────────────────┐
│                     DottieConfiguration                          │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Profiles: Dictionary<string, ConfigProfile>              │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ 1..*
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        ConfigProfile                             │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Extends: string?           ← EXISTING (inheritance)      │    │
│  │ Dotfiles: IList<DotfileEntry>                           │    │
│  │ Install: InstallBlock?                                   │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ resolution
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     ResolvedProfile (existing)                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Name: string                                             │    │
│  │ Dotfiles: List<DotfileEntry>   ← merged & deduplicated  │    │
│  │ Install: InstallBlock?          ← merged                 │    │
│  │ InheritanceChain: IReadOnlyList<string>                  │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Existing Types (No Changes Needed)

### ConfigProfile

```csharp
// src/Dottie.Configuration/Models/ConfigProfile.cs
// Already has Extends property - NO CHANGES NEEDED

public sealed record ConfigProfile
{
    public string? Extends { get; init; }
    public IList<DotfileEntry> Dotfiles { get; init; } = [];
    public InstallBlock? Install { get; init; }
}
```

### ResolvedProfile

```csharp
// src/Dottie.Configuration/Inheritance/ResolvedProfile.cs
// Already captures merged result - NO CHANGES NEEDED

public sealed record ResolvedProfile
{
    public required string Name { get; init; }
    public required List<DotfileEntry> Dotfiles { get; init; }
    public InstallBlock? Install { get; init; }
    public required IReadOnlyList<string> InheritanceChain { get; init; }
}
```

---

## New Types

### ProfileInfo (for enhanced listing)

```csharp
// src/Dottie.Configuration/ProfileInfo.cs

namespace Dottie.Configuration;

/// <summary>
/// Summary information about a profile for display purposes.
/// </summary>
public sealed record ProfileInfo
{
    /// <summary>
    /// Gets the profile name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the name of the profile this extends, if any.
    /// </summary>
    public string? Extends { get; init; }

    /// <summary>
    /// Gets the count of dotfile entries defined directly in this profile.
    /// </summary>
    public int DotfileCount { get; init; }

    /// <summary>
    /// Gets whether this profile has an install block.
    /// </summary>
    public bool HasInstallBlock { get; init; }
}
```

### ProfileAwareSettings (CLI base class)

```csharp
// src/Dottie.Cli/Commands/ProfileAwareSettings.cs

namespace Dottie.Cli.Commands;

using System.ComponentModel;
using Spectre.Console.Cli;

/// <summary>
/// Base settings class for commands that accept a profile option.
/// </summary>
public abstract class ProfileAwareSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the profile name to use.
    /// </summary>
    [Description("Profile to use (default: 'default')")]
    [CommandOption("-p|--profile")]
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    [Description("Path to the configuration file (default: dottie.yaml in repo root)")]
    [CommandOption("-c|--config")]
    public string? ConfigPath { get; set; }
}
```

---

## Behavioral Changes

### ProfileMerger.MergeDotfiles() - Deduplication

**Before** (simple append):
```csharp
internal static List<DotfileEntry> MergeDotfiles(IList<DotfileEntry> parent, IList<DotfileEntry> child)
{
    var result = new List<DotfileEntry>(parent);
    result.AddRange(child);
    return result;
}
```

**After** (deduplicate by target):
```csharp
internal static List<DotfileEntry> MergeDotfiles(IList<DotfileEntry> parent, IList<DotfileEntry> child)
{
    var merged = new Dictionary<string, DotfileEntry>(StringComparer.Ordinal);
    
    foreach (var entry in parent)
    {
        merged[entry.Target] = entry;
    }
    
    foreach (var entry in child)
    {
        merged[entry.Target] = entry;  // Child overrides parent
    }
    
    return merged.Values.ToList();
}
```

### ProfileResolver.GetProfile() - Implicit Default

**Before**:
```csharp
if (string.IsNullOrEmpty(profileName))
{
    return ProfileResolveResult.Failure(
        "No profile specified. Please specify a profile name.",
        availableProfiles);
}
```

**After**:
```csharp
if (string.IsNullOrEmpty(profileName))
{
    profileName = "default";
}

if (!_configuration.Profiles.ContainsKey(profileName))
{
    if (profileName == "default")
    {
        // Implicit empty default profile
        return ProfileResolveResult.Success(new ConfigProfile());
    }
    
    return ProfileResolveResult.Failure(
        $"Profile '{profileName}' not found.",
        availableProfiles);
}
```

---

## Validation Rules

### Profile Name Validation (NEW)

```csharp
// In ConfigurationValidator

private static readonly Regex ValidProfileNamePattern = 
    new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

public IEnumerable<ValidationError> ValidateProfileNames(DottieConfiguration config)
{
    foreach (var profileName in config.Profiles.Keys)
    {
        if (!ValidProfileNamePattern.IsMatch(profileName))
        {
            yield return new ValidationError(
                $"profiles.{profileName}",
                $"Profile name '{profileName}' contains invalid characters. Use only letters, numbers, hyphens, and underscores.");
        }
    }
}
```

---

## Summary of Model Changes

| Type | Change Type | Description |
|------|-------------|-------------|
| `ConfigProfile` | None | Existing `Extends` property sufficient |
| `ResolvedProfile` | None | Existing structure sufficient |
| `ProfileInfo` | **New** | Summary info for enhanced listing |
| `ProfileAwareSettings` | **New** | CLI base class with `--profile` option |
| `ProfileMerger.MergeDotfiles()` | **Modified** | Add target deduplication |
| `ProfileResolver.GetProfile()` | **Modified** | Add implicit default support |
| `ConfigurationValidator` | **Modified** | Add profile name validation |
