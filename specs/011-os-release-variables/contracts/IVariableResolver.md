# Contract: IVariableResolver

**Feature**: 011-os-release-variables
**Date**: February 7, 2026

## Overview

Defines the contract for the variable substitution engine that resolves `${KEY_NAME}` patterns in configuration string values.

## Interface

### `VariableResolver` (static utility class)

The resolver is implemented as a static utility class (consistent with `ArchitectureDetector` and `PathExpander` patterns in the codebase). It does not require instance state.

---

### Method: `ResolveString`

**Purpose**: Resolve all `${...}` variable references in a single string.

**Signature**:
```
ResolveString(input: string, variables: IReadOnlyDictionary<string, string>, deferredVariables: IReadOnlySet<string>?) → VariableResolutionResult
```

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `input` | `string` | The string containing zero or more `${KEY_NAME}` references |
| `variables` | `IReadOnlyDictionary<string, string>` | Available variable name → value mappings |
| `deferredVariables` | `IReadOnlySet<string>?` | Variable names allowed to remain unresolved (nullable, default: none) |

**Returns**: `VariableResolutionResult` with resolved string and any errors.

**Behavior**:
1. If `input` contains no `${...}` patterns, returns input unchanged with no errors.
2. For each `${KEY_NAME}` match:
   - If `KEY_NAME` is in `variables`, substitutes the value.
   - If `KEY_NAME` is in `deferredVariables`, leaves `${KEY_NAME}` as-is (no error).
   - Otherwise, adds `KEY_NAME` to unresolved list (error).
3. Returns the resolved string and the list of unresolvable variable names.

---

### Method: `ResolveConfiguration`

**Purpose**: Resolve all variables across an entire `DottieConfiguration` object.

**Signature**:
```
ResolveConfiguration(configuration: DottieConfiguration, variables: IReadOnlyDictionary<string, string>) → ConfigurationResolutionResult
```

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `configuration` | `DottieConfiguration` | The parsed configuration with unresolved variable references |
| `variables` | `IReadOnlyDictionary<string, string>` | Global variables (OS release + architecture) |

**Returns**: `ConfigurationResolutionResult` with a new `DottieConfiguration` (resolved) and any errors.

**Behavior**:
1. Iterates all profiles in the configuration.
2. For each profile, resolves variables in:
   - `DotfileEntry.Source` and `DotfileEntry.Target` — no deferred variables
   - `AptRepoItem.Repo`, `AptRepoItem.KeyUrl`, and each package in `AptRepoItem.Packages` — no deferred variables
   - `GithubReleaseItem.Asset` and `GithubReleaseItem.Binary` — `RELEASE_VERSION` is deferred
3. Collects all errors (unresolvable non-deferred variables) with context: profile name, entry identifier, and field name.
4. Returns a new configuration with resolved values, plus the error list.

**Important**: Model records use `init` properties. Resolution creates new record instances using `with` expressions to preserve immutability.

---

### Method: `BuildVariableSet`

**Purpose**: Construct the global variable dictionary from system information sources.

**Signature**:
```
BuildVariableSet(osReleaseVariables: IReadOnlyDictionary<string, string>, architectureDetector: ArchitectureDetector) → IReadOnlyDictionary<string, string>
```

**Behavior**:
1. Starts with OS release variables.
2. Adds `ARCH` → `ArchitectureDetector.RawArchitecture`.
3. Adds `MS_ARCH` → `ArchitectureDetector.CurrentArchitecture` (existing property).
4. Architecture variables override any conflicting OS release keys.

---

## Contract: `OsReleaseParser`

### Method: `Parse`

**Purpose**: Parse `/etc/os-release` content into a variable dictionary.

**Signature**:
```
Parse(content: string) → IReadOnlyDictionary<string, string>
```

**Behavior**:
1. Splits content into lines.
2. Skips empty lines and lines starting with `#`.
3. For each remaining line, splits on the first `=`.
4. Strips surrounding double quotes or single quotes from the value.
5. Returns a dictionary of key-value pairs.

### Method: `TryReadFromSystem`

**Purpose**: Attempt to read and parse `/etc/os-release` from the filesystem.

**Signature**:
```
TryReadFromSystem(filePath: string = "/etc/os-release") → (IReadOnlyDictionary<string, string> variables, bool isAvailable)
```

**Behavior**:
1. If the file exists, reads content and calls `Parse`.
2. If the file does not exist, returns an empty dictionary with `isAvailable = false`.

---

## Contract: `ArchitectureDetector` Extensions

### New Property: `RawArchitecture`

**Purpose**: Provide the architecture string matching `uname -m` output.

**Signature**:
```
static string RawArchitecture { get; }
```

**Returns**: `x86_64`, `aarch64`, `armv7l`, or `unknown`.

### Existing Property: `CurrentArchitecture` → serves as `MS_ARCH`

Already returns `amd64`, `arm64`, `armhf` (note: existing code returns `arm` for Arm — update to `armhf` per spec).

---

## Error Contract

### `VariableResolutionError`

| Field | Type | Description |
|-------|------|-------------|
| ProfileName | `string` | Which profile contains the error |
| EntryIdentifier | `string` | Which entry (e.g., aptrepo name, github repo, dotfile source) |
| FieldName | `string` | Which field contains the unresolvable variable |
| VariableName | `string` | The unresolvable variable name |
| Message | `string` | Human-readable error message |

**Format**: `"Unresolvable variable '${VARIABLE_NAME}' in profile 'default', aptrepo 'microsoft-prod', field 'repo'"`

---

## Integration Points

### ConfigurationLoader Enhancement

The existing `ConfigurationLoader.LoadFromString` method is enhanced to:

1. Deserialize YAML → `DottieConfiguration` (existing)
2. **NEW**: Build variable set from `OsReleaseParser` + `ArchitectureDetector`
3. **NEW**: Call `VariableResolver.ResolveConfiguration(config, variables)`
4. **NEW**: If resolution errors exist, add them to the `LoadResult.Errors`
5. Validate profiles (existing)
6. Return `LoadResult`

### GithubReleaseInstaller Enhancement

The existing `GithubReleaseInstaller` is enhanced to:

1. Determine version (existing: from `item.Version` or API latest)
2. **NEW**: Resolve `${RELEASE_VERSION}` in `item.Asset` and `item.Binary` using `VariableResolver.ResolveString`
3. Match asset against release assets (existing)
4. Download and install (existing)
