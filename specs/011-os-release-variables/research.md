# Research: OS Release Variable Substitution

**Feature**: 011-os-release-variables
**Date**: February 7, 2026

## Research Task 1: `/etc/os-release` Parsing Approach

### Decision: Custom line parser reading `KEY=VALUE` or `KEY="VALUE"` pairs

### Rationale
- `/etc/os-release` follows a simple, well-defined format (freedesktop.org specification): one `KEY=VALUE` pair per line, values optionally double-quoted.
- No .NET built-in API reads this file format.
- The file is small (typically 8–15 lines), so a simple `File.ReadAllLines` + split approach is optimal.
- No YAML/JSON/INI parser is needed — the format is simpler than any of those.

### Alternatives Considered
1. **Shell out to `source /etc/os-release && printenv`**: Rejected — unnecessary process spawn, shell dependency, and security concerns (executing arbitrary file content).
2. **Use a third-party Linux system info library**: Rejected — adds a dependency for a trivial parse operation. No well-maintained .NET library exists for this specific file.
3. **Use `Microsoft.Extensions.Configuration` with a key-value provider**: Rejected — overkill dependency for a 10-line file; the format doesn't match any built-in provider exactly.

### Format Specification
```
KEY=value
KEY="quoted value"
KEY='single-quoted value'
# comment lines
```
- Lines starting with `#` are comments.
- Empty lines are ignored.
- Values may be unquoted, double-quoted, or single-quoted.
- Only the first `=` separates key from value.

---

## Research Task 2: Architecture Detection — Raw vs. MS-Style

### Decision: Extend existing `ArchitectureDetector` with two new properties

### Rationale
- The existing `ArchitectureDetector.CurrentArchitecture` already maps `RuntimeInformation.OSArchitecture` to Microsoft-style strings (`amd64`, `arm64`). This maps cleanly to `${MS_ARCH}`.
- For `${ARCH}` (raw `uname -m` equivalent), .NET's `RuntimeInformation.OSArchitecture` enum can be mapped to the corresponding uname strings: `X64` → `x86_64`, `Arm64` → `aarch64`, `Arm` → `armv7l`.
- No need to shell out to `uname -m` since `RuntimeInformation` provides the same information cross-platform.

### Alternatives Considered
1. **Shell out to `uname -m`**: Rejected — not cross-platform for development/testing, and `RuntimeInformation` provides equivalent data.
2. **Create a separate `RawArchitectureDetector` class**: Rejected — duplicates concerns. Better to extend the existing class with additional properties.

### Mapping Table
| `RuntimeInformation.OSArchitecture` | `${ARCH}` (raw) | `${MS_ARCH}` (Microsoft-style) |
|--------------------------------------|------------------|---------------------------------|
| `X64`                                | `x86_64`         | `amd64`                         |
| `Arm64`                              | `aarch64`        | `arm64`                         |
| `Arm`                                | `armv7l`         | `armhf`                         |
| `X86` / Other                        | `unknown`        | ERROR if referenced              |

---

## Research Task 3: Variable Substitution Engine

### Decision: Regex-based single-pass replacement with `Regex.Replace` and a variable dictionary

### Rationale
- The `${KEY_NAME}` pattern is simple and well-defined.
- A single regex `\$\{([A-Za-z_][A-Za-z0-9_]*)\}` matches all variable references.
- `Regex.Replace` with a `MatchEvaluator` callback allows looking up each variable name in a dictionary and replacing it, or collecting unresolved references.
- No recursive expansion needed (variables don't reference other variables).
- No escape mechanism in this iteration — simplest approach.

### Alternatives Considered
1. **String.Replace in a loop for each known variable**: Rejected — O(n*m) where n = string length, m = variable count. Regex single-pass is cleaner and handles the "detect unresolved" requirement naturally.
2. **Template engine library (Scriban, Handlebars.NET)**: Rejected — massive overkill for simple `${...}` substitution. Adds unnecessary dependency.
3. **Custom parser scanning for `${`**: Rejected — reimplements what `Regex.Replace` already provides, with more code and more potential for bugs.

### Design
```
Input:  "deb [arch=${MS_ARCH}] https://example.com/${VERSION_CODENAME}/prod"
Regex:  \$\{([A-Za-z_][A-Za-z0-9_]*)\}
Match1: ${MS_ARCH} → lookup "MS_ARCH" → "amd64"
Match2: ${VERSION_CODENAME} → lookup "VERSION_CODENAME" → "noble"
Output: "deb [arch=amd64] https://example.com/noble/prod"
```

If a variable is not found in the dictionary, it is collected as an error (not left as-is), per FR-011.

---

## Research Task 4: Integration Point — Where to Hook Variable Resolution

### Decision: Post-YAML-parse, pre-validation. New step in `ConfigurationLoader` pipeline.

### Rationale
- Variables must be resolved before validation runs (FR-012: "resolve all variables before performing any installation actions").
- YAML parsing must happen first — variables are in string values, not YAML structure.
- The `ConfigurationLoader.LoadFromString` method returns a `LoadResult` with a `DottieConfiguration`. After deserialization, a new `VariableResolver.ResolveConfiguration(config)` call transforms all string fields that contain `${...}` patterns.
- This keeps the resolver independent of YAML parsing and independent of installation.

### Alternatives Considered
1. **Resolve at installation time (in each installer)**: Rejected — duplicates logic across 3+ installers, violates FR-012 (which requires pre-resolution), and makes validation impossible before install.
2. **Custom YAML deserializer that resolves during parse**: Rejected — couples variable resolution to YAML library internals, making it harder to test and maintain.
3. **Resolve in a middleware/decorator around `ConfigurationLoader`**: Considered but unnecessary indirection. A direct call in `LoadFromString` is simpler.

### Flow
```
YAML string → YamlDotNet deserialize → DottieConfiguration
→ VariableResolver.Resolve(config, variables) → DottieConfiguration (resolved)
→ Validation → LoadResult
```

---

## Research Task 5: `${RELEASE_VERSION}` Scoping

### Decision: `${RELEASE_VERSION}` is NOT resolved during configuration loading. It is resolved per-item at installation time within `GithubReleaseInstaller`.

### Rationale
- `${RELEASE_VERSION}` is item-specific: each `github` entry may have a different version (explicit or latest from API). It cannot be a single global variable.
- Resolving it requires GitHub API calls, which should only happen during installation (not during config loading/validation).
- The existing `GithubReleaseInstaller` already resolves version and fetches release data. Variable substitution for `${RELEASE_VERSION}` in `asset` and `binary` fields happens after the version is known, within the installer.
- Other variables (`${ARCH}`, `${MS_ARCH}`, `${VERSION_CODENAME}`, etc.) are global and resolved at config load time.

### Alternatives Considered
1. **Resolve all variables including `${RELEASE_VERSION}` at config load time**: Rejected — would require GitHub API calls during parsing, which is slow, may fail, and mixes concerns.
2. **Pass unresolved `${RELEASE_VERSION}` through and let the installer handle all `${...}` patterns**: Rejected — this would mean the installer needs to know about the full variable system. Better to resolve global variables first, then handle the item-specific one in the installer.

### Design
- Global variables (`${ARCH}`, `${MS_ARCH}`, all `/etc/os-release` keys) → resolved by `VariableResolver` at config load time.
- `${RELEASE_VERSION}` → left as `${RELEASE_VERSION}` after global resolution → resolved by `GithubReleaseInstaller` per-item after version is determined from API or `version` field.
- If `${RELEASE_VERSION}` appears in a non-`github` context, the global resolver reports it as unresolvable (per spec edge case).

### Scoping Rule
The `VariableResolver` accepts a set of "deferred variables" — variable names that are allowed to remain unresolved without error. For `github` entry fields (`asset`, `binary`), `RELEASE_VERSION` is deferred. For all other fields, it is not deferred and produces an error if referenced.

---

## Research Task 6: Testing Strategy for OS-Dependent Behavior

### Decision: Abstracted file system reading via injectable interfaces for unit tests; real file for integration tests

### Rationale
- `OsReleaseParser` needs to be testable without a real `/etc/os-release` file.
- The parser takes a `string` content parameter (not a file path) for unit testing. A separate method or wrapper reads the file.
- Architecture detection uses `RuntimeInformation` which is a static API — for tests that need a specific architecture, use the `VariableResolver` with injected dictionaries rather than mocking the static.
- Integration tests on CI (Ubuntu) can test real `/etc/os-release` parsing end-to-end.

### Alternatives Considered
1. **Mock `File.ReadAllText` via a filesystem abstraction**: Rejected for the parser itself — simpler to pass content string. The "read file" concern is a thin wrapper tested separately.
2. **Use `Xunit.SkippableFact` for OS-specific tests**: Already available in the test project. Use for integration-level tests that require a real Linux system.

---

## Summary of Decisions

| # | Decision | Key File(s) |
|---|----------|-------------|
| 1 | Custom line parser for `/etc/os-release` | `OsReleaseParser.cs` |
| 2 | Extend `ArchitectureDetector` with `RawArchitecture` and existing `CurrentArchitecture` as MS-style | `ArchitectureDetector.cs` |
| 3 | Regex single-pass `${...}` substitution | `VariableResolver.cs` |
| 4 | Resolve global variables post-YAML-parse in `ConfigurationLoader` pipeline | `ConfigurationLoader.cs`, `VariableResolver.cs` |
| 5 | `${RELEASE_VERSION}` deferred — resolved per-item in `GithubReleaseInstaller` | `GithubReleaseInstaller.cs`, `VariableResolver.cs` |
| 6 | Parser takes string content for unit tests; integration tests use real file | `OsReleaseParserTests.cs` |
