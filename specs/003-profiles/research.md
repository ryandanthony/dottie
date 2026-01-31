# Research: Configuration Profiles

**Feature**: 003-profiles  
**Date**: 2026-01-30  
**Purpose**: Analyze existing profile implementation and identify gaps

---

## 1. Existing Implementation Analysis

### Decision: **Extend current ProfileMerger and ProfileResolver**

### Current State

The 001-yaml-configuration feature already implemented significant profile infrastructure:

| Component | Location | Status |
|-----------|----------|--------|
| `ConfigProfile` model | `src/Dottie.Configuration/Models/ConfigProfile.cs` | ✅ Complete - has `Extends` property |
| `ProfileMerger` | `src/Dottie.Configuration/Inheritance/ProfileMerger.cs` | ✅ Complete - handles inheritance chains |
| `ProfileResolver` | `src/Dottie.Configuration/ProfileResolver.cs` | ⚠️ Partial - lists profiles but no inheritance info |
| `ValidateCommand` | `src/Dottie.Cli/Commands/ValidateCommand.cs` | ⚠️ Partial - validates profiles but positional arg |
| Circular detection | `ProfileMerger.BuildInheritanceChain()` | ✅ Complete - detects and reports cycles |

### Rationale

The core profile inheritance logic already exists and is well-tested. This feature focuses on:
1. Adding `--profile` flag to commands (FR-002)
2. Improving profile listing with inheritance visualization (FR-012)
3. Adding profile name validation (FR-011)
4. Ensuring "default" profile fallback behavior (FR-003)

---

## 2. CLI Profile Flag Pattern

### Decision: **Global option using Spectre.Console.Cli interceptors**

### Rationale

- Profile selection should apply to multiple commands (validate, link, install, apply)
- Using a global option avoids duplication across command settings classes
- Spectre.Console.Cli supports command interceptors for cross-cutting concerns

### Alternatives Considered

| Approach | Rejected Because |
|----------|------------------|
| Per-command `--profile` option | Duplicates option in every command settings class |
| Environment variable `DOTTIE_PROFILE` | Less discoverable, but could be added later |
| Profile in config file | Conflicts with multi-machine use case |

### Implementation Pattern

```csharp
// Base settings class all commands inherit
public abstract class ProfileAwareSettings : CommandSettings
{
    [Description("Profile to use (default: 'default')")]
    [CommandOption("-p|--profile")]
    public string? ProfileName { get; set; }
}
```

---

## 3. Default Profile Behavior

### Decision: **Implicit empty default when not defined**

### Rationale

- FR-003 requires "default" profile when none specified
- Users shouldn't be forced to define an empty default profile
- Aligns with principle of explicitness: if default is needed, it works; if custom behavior is needed, user defines it

### Implementation

```csharp
// In ProfileResolver.GetProfile()
if (string.IsNullOrEmpty(profileName))
{
    profileName = "default";
}

if (!_configuration.Profiles.ContainsKey(profileName))
{
    if (profileName == "default")
    {
        // Return empty implicit default
        return ProfileResolveResult.Success(new ConfigProfile());
    }
    // Other profiles must exist
    return ProfileResolveResult.Failure(...);
}
```

---

## 4. Profile Name Validation

### Decision: **Regex validation during configuration loading**

### Rationale

- FR-011 requires alphanumeric, hyphens, and underscores only
- Validation should happen early (during config load) not late (during profile resolution)
- Prevents confusing errors from invalid profile names in YAML keys

### Pattern

```csharp
private static readonly Regex ProfileNamePattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

public static bool IsValidProfileName(string name) => ProfileNamePattern.IsMatch(name);
```

---

## 5. Profile Listing with Inheritance Hierarchy

### Decision: **Tree visualization using Spectre.Console.Tree**

### Rationale

- FR-012 requires showing inheritance relationships
- Spectre.Console has built-in tree rendering
- Aligns with constitution: "Observable, Supportable Tooling"

### Example Output

```
Available profiles:
├── default
├── minimal
│   └── extends: (none)
├── full
│   └── extends: minimal
└── work
    └── extends: full
```

---

## 6. Deduplication by Target Path

### Decision: **Use existing MergeByKey pattern for dotfiles**

### Current Behavior

The `ProfileMerger.MergeDotfiles()` currently appends lists without deduplication:

```csharp
internal static List<DotfileEntry> MergeDotfiles(IList<DotfileEntry> parent, IList<DotfileEntry> child)
{
    var result = new List<DotfileEntry>(parent);
    result.AddRange(child);  // Simple append
    return result;
}
```

### Required Behavior (FR-010)

Deduplicate by target path with child taking precedence:

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
        merged[entry.Target] = entry;  // Child overrides
    }
    
    return merged.Values.ToList();
}
```

---

## 7. Test Strategy

### Decision: **Extend existing test fixtures and add profile-specific scenarios**

### Test Categories

| Category | Purpose | Location |
|----------|---------|----------|
| Unit tests | ProfileMerger deduplication | `tests/Dottie.Configuration.Tests/Inheritance/` |
| Unit tests | ProfileResolver default handling | `tests/Dottie.Configuration.Tests/` |
| Unit tests | Profile name validation | `tests/Dottie.Configuration.Tests/Validation/` |
| Integration | CLI `--profile` flag | `tests/Dottie.Cli.Tests/Commands/` |

### New Test Fixtures Needed

- `profile-with-invalid-name.yaml` - Profile key with special characters
- `profile-inheritance-dedup.yaml` - Parent/child with same target paths
- `profile-deep-chain.yaml` - A → B → C → D inheritance

---

## Summary: Gaps to Address

| FR | Description | Gap | Action |
|----|-------------|-----|--------|
| FR-002 | `--profile` flag | Validate command uses positional | Convert to `--profile` option |
| FR-003 | Default profile fallback | No implicit default | Add implicit empty default |
| FR-010 | Target deduplication | Simple append | Implement MergeByKey for dotfiles |
| FR-011 | Profile name validation | Not implemented | Add regex validation in ConfigurationValidator |
| FR-012 | List with inheritance | Basic list only | Add tree visualization |
