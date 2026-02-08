# Data Model: OS Release Variable Substitution

**Feature**: 011-os-release-variables
**Date**: February 7, 2026

## Entities

### 1. OsReleaseInfo *(conceptual — represented as return tuple, not a named type)*

**Description**: Parsed representation of the `/etc/os-release` file contents. Provides key-value pairs representing OS identification data. Implemented via `OsReleaseParser.TryReadFromSystem` returning `(IReadOnlyDictionary<string, string> variables, bool isAvailable)`.

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| Variables | `IReadOnlyDictionary<string, string>` | All key-value pairs from the file | `{"VERSION_CODENAME": "noble", "ID": "ubuntu", ...}` |
| IsAvailable | `bool` | Whether the OS release file was successfully read | `true` |

**Source**: `/etc/os-release` file (freedesktop.org standard)

**Parsing Rules**:
- Lines starting with `#` are comments — ignored.
- Empty/whitespace-only lines are ignored.
- Format: `KEY=VALUE` where value may be unquoted, double-quoted (`"value"`), or single-quoted (`'value'`).
- Only the first `=` on a line separates key from value.
- Key names contain only `[A-Z0-9_]` (uppercase letters, digits, underscores).

---

### 2. ArchitectureInfo *(conceptual — represented by static properties on ArchitectureDetector)*

**Description**: System CPU architecture information in two formats: raw (matching `uname -m` output) and Microsoft-style (matching Debian/Microsoft package naming). Accessed via `ArchitectureDetector.RawArchitecture` and `ArchitectureDetector.CurrentArchitecture` static properties.

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| RawArchitecture | `string` | Architecture as `uname -m` would report | `x86_64`, `aarch64`, `armv7l` |
| MsArchitecture | `string` | Microsoft/Debian-style architecture name | `amd64`, `arm64`, `armhf` |
| IsSupported | `bool` | Whether the architecture has a known MS mapping | `true` |

**Mapping Table**:

| RuntimeInformation.OSArchitecture | RawArchitecture | MsArchitecture |
|-----------------------------------|-----------------|----------------|
| X64 | `x86_64` | `amd64` |
| Arm64 | `aarch64` | `arm64` |
| Arm | `armv7l` | `armhf` |
| X86 / Other | `unknown` | (unsupported) |

---

### 3. VariableSet *(conceptual — represented as `IReadOnlyDictionary<string, string>`)*

**Description**: The complete set of variables available for substitution, built from multiple sources. Constructed by `VariableResolver.BuildVariableSet` and returned as a plain dictionary.

| Field | Type | Description |
|-------|------|-------------|
| Variables | `IReadOnlyDictionary<string, string>` | All available variables keyed by name |
| Source | Composed from | `OsReleaseInfo.Variables` + `ARCH` + `MS_ARCH` |

**Variable Precedence** (in case of conflicts):
1. Architecture variables (`ARCH`, `MS_ARCH`) — highest priority, cannot be overridden
2. OS release variables — from file

**Reserved Variable Names** (cannot come from `/etc/os-release`):
- `ARCH`
- `MS_ARCH`
- `RELEASE_VERSION` (deferred, resolved per-item)

---

### 4. VariableReference *(conceptual — exists as regex match groups during resolution, not a named type)*

**Description**: A `${KEY_NAME}` pattern found in a configuration string value during resolution.

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| VariableName | `string` | The name inside `${...}` | `VERSION_CODENAME` |
| FullMatch | `string` | The complete `${...}` token | `${VERSION_CODENAME}` |
| IsResolvable | `bool` | Whether the variable exists in the VariableSet | `true` |
| IsDeferred | `bool` | Whether this variable is deferred (e.g., `RELEASE_VERSION` in github context) | `false` |

**Pattern**: `\$\{([A-Za-z_][A-Za-z0-9_]*)\}`

---

### 5. VariableResolutionResult *(concrete record)*

**Description**: The outcome of resolving variables in a single string value.

| Field | Type | Description |
|-------|------|-------------|
| ResolvedValue | `string` | The string with all resolvable variables substituted |
| UnresolvedVariables | `IReadOnlyList<string>` | Variable names that could not be resolved |
| HasErrors | `bool` | Whether any non-deferred variables were unresolvable |

---

### 6. VariableResolutionError *(concrete record)*

**Description**: A single error produced when a non-deferred variable cannot be resolved, with full context for actionable error reporting (FR-011).

| Field | Type | Description | Example |
|-------|------|-------------|--------|
| ProfileName | `string` | Which profile contains the error | `"default"` |
| EntryIdentifier | `string` | Which entry (e.g., aptrepo name, github repo) | `"microsoft-prod"` |
| FieldName | `string` | Which field contains the unresolvable variable | `"repo"` |
| VariableName | `string` | The unresolvable variable name | `"VERSION_CODENAME"` |
| Message | `string` | Human-readable error message | `"Unresolvable variable '${VERSION_CODENAME}' in profile 'default', aptrepo 'microsoft-prod', field 'repo'"` |

---

### 7. ConfigurationResolutionResult *(concrete record)*

**Description**: The outcome of resolving all variables across an entire `DottieConfiguration`. Wraps the resolved configuration and any accumulated errors.

| Field | Type | Description |
|-------|------|-------------|
| Configuration | `DottieConfiguration` | The configuration with all resolvable variables substituted |
| Errors | `IReadOnlyList<VariableResolutionError>` | All errors from unresolvable non-deferred variables |
| HasErrors | `bool` | Whether any resolution errors occurred |

---

## State Transitions

### Variable Resolution Pipeline

```
┌─────────────────┐
│ YAML Deserialized│
│ (raw strings)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Build VariableSet│ ← OsReleaseParser + ArchitectureDetector
│ (global vars)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Resolve globals  │ ← VariableResolver processes all config string fields
│ in config        │   (except github asset/binary get RELEASE_VERSION deferred)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Validation       │ ← Existing validators run on resolved values
└────────┬────────┘
         │
         ▼
┌─────────────────┐    ┌─────────────────────┐
│ Installation     │───▶│ GithubReleaseInstaller│
│ (per source)     │    │ resolves             │
└─────────────────┘    │ ${RELEASE_VERSION}   │
                       │ per-item             │
                       └─────────────────────┘
```

## Relationships

```
DottieConfiguration
  └── ConfigProfile (1..N)
        ├── DotfileEntry (0..N)         ← source, target get variable substitution
        └── InstallBlock (0..1)
              ├── AptRepoItem (0..N)    ← repo, key_url, packages get variable substitution
              ├── GithubReleaseItem (0..N) ← asset, binary get variable substitution
              │                              (RELEASE_VERSION deferred)
              ├── FontItem (0..N)       ← no variable substitution (future)
              ├── SnapItem (0..N)       ← no variable substitution (future)
              └── apt (0..N strings)    ← no variable substitution (future)

OsReleaseParser ──reads──▶ /etc/os-release ──produces──▶ OsReleaseInfo
ArchitectureDetector ──queries──▶ RuntimeInformation ──produces──▶ ArchitectureInfo
VariableResolver ──uses──▶ VariableSet ──transforms──▶ DottieConfiguration fields
```
