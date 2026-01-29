# Research: YAML Configuration System

**Feature**: 001-yaml-configuration  
**Date**: 2026-01-28  
**Purpose**: Resolve technical unknowns before Phase 1 design

---

## 1. YAML Parsing Library for .NET 10 Trimmed/AOT

### Decision: **YamlDotNet** with source generators

### Rationale

- YamlDotNet is the most mature YAML library for .NET
- Version 13+ supports source generators for AOT/trimming compatibility
- Attribute-based mapping aligns with C# record patterns
- Active maintenance and broad community adoption

### Alternatives Considered

| Library                         | Rejected Because                            |
| ------------------------------- | ------------------------------------------- |
| SharpYaml                       | Less active, fewer AOT considerations       |
| NetEscapades.Configuration.Yaml | Focused on IConfiguration, not general YAML |
| Manual parsing                  | Excessive effort for well-solved problem    |

### Implementation Notes

```csharp
// Use YamlDotNet.Serialization.IDeserializer with StaticContext for AOT
[YamlSerializable(typeof(DottieConfiguration))]
public partial class DottieYamlContext : StaticContext { }
```

---

## 2. Validation Strategy

### Decision: **Fluent validation with custom validator classes**

### Rationale

- FluentValidation is heavyweight and has trimming issues
- Custom validators give full control over error messages with line numbers
- Validation logic is simple enough to not need a framework
- Aligns with constitution principle: "simplest acceptable approach"

### Alternatives Considered

| Approach                              | Rejected Because                                |
| ------------------------------------- | ----------------------------------------------- |
| FluentValidation                      | Trimming complications, overkill for this scope |
| Data Annotations                      | Poor error messages, no line number support     |
| System.ComponentModel.DataAnnotations | Same issues as above                            |

### Implementation Pattern

```csharp
public record ValidationError(string Path, string Message, int? Line = null);

public interface IConfigValidator
{
    IEnumerable<ValidationError> Validate(DottieConfiguration config);
}
```

---

## 3. Error Reporting with Line Numbers

### Decision: **Wrap YamlDotNet parsing with position tracking**

### Rationale

- YamlDotNet's `YamlException` includes `Start` and `End` marks with line/column
- For validation errors post-parsing, store original YAML text and map paths to positions
- Spectre.Console markup can highlight errors with colors and context

### Implementation Notes

- Parse YAML twice: once to DOM (for position mapping), once to typed objects
- Or: use `IYamlTypeConverter` to capture positions during deserialization
- Store `Dictionary<string, (int Line, int Column)>` for key paths

---

## 4. Profile Inheritance & Merging

### Decision: **Eager resolution at load time**

### Rationale

- Resolve inheritance graph during configuration loading
- Produces fully-merged Profile objects for consumers
- Detect cycles using visited set during traversal
- Constitution: "prefer declarative approaches when they improve clarity"

### Merge Rules (from spec clarifications)

| Item Type  | Merge Behavior                              |
| ---------- | ------------------------------------------- |
| `dotfiles` | Append child after parent                   |
| `apt`      | Append child after parent                   |
| `github`   | Merge by `repo` key; child overrides parent |
| `snap`     | Merge by `name` key; child overrides parent |
| `apt-repo` | Merge by `name` key; child overrides parent |
| `fonts`    | Merge by `name` key; child overrides parent |
| `scripts`  | Append child after parent                   |

### Cycle Detection

```csharp
HashSet<string> visited = new();
HashSet<string> inProgress = new();

void ResolveProfile(string name) {
    if (inProgress.Contains(name)) throw new CircularInheritanceException(...);
    if (visited.Contains(name)) return;
    inProgress.Add(name);
    // resolve parent first
    inProgress.Remove(name);
    visited.Add(name);
}
```

---

## 5. Trimming & Native AOT Considerations

### Decision: **Design for trimming from day one**

### Key Constraints

- No reflection-based serialization (use source generators)
- No dynamic type loading
- Mark assemblies with `[assembly: JsonSerializable]` or YAML equivalent
- Test with `PublishTrimmed=true` early and often

### YamlDotNet AOT Setup

```xml
<PackageReference Include="YamlDotNet" Version="16.*" />
```

```csharp
// In Dottie.Configuration project
[YamlStaticContext]
[YamlSerializable(typeof(DottieConfiguration))]
[YamlSerializable(typeof(Profile))]
// ... all model types
public partial class ConfigurationYamlContext : StaticContext { }
```

### Trimming-Safe Patterns

- Use `required` properties instead of nullable with validation
- Avoid `Activator.CreateInstance`
- Prefer records with primary constructors
- Test: `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishTrimmed=true`

---

## 6. Architecture Detection for GitHub Assets

### Decision: **Use `RuntimeInformation.OSArchitecture`**

### Rationale

- Built-in .NET API, no external dependencies
- Works correctly under AOT
- Maps to common asset naming patterns

### Implementation

```csharp
string GetArchitecturePattern() => RuntimeInformation.OSArchitecture switch
{
    Architecture.X64 => "amd64|x86_64|x64",
    Architecture.Arm64 => "arm64|aarch64",
    _ => throw new PlatformNotSupportedException(...)
};
```

### Asset Matching Algorithm

1. Apply glob pattern to all release assets
2. If single match → use it
3. If multiple matches → filter by architecture regex
4. If still multiple → error with list
5. If zero matches → error

---

## 7. Tilde Expansion in Paths

### Decision: **Expand `~` to `Environment.GetFolderPath(UserProfile)`**

### Rationale

- Standard Unix convention
- .NET provides cross-platform home directory API
- Only expand at path resolution time, not during parsing

### Implementation

```csharp
string ExpandPath(string path) =>
    path.StartsWith("~/")
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..])
        : path;
```

---

## 8. Script Path Security Validation

### Decision: **Normalize and check path is under repo root**

### Rationale

- FR-019 requires scripts stay within repository
- Prevent `../../../etc/passwd` style traversal
- Use `Path.GetFullPath` to resolve and compare

### Implementation

```csharp
bool IsPathWithinRepo(string scriptPath, string repoRoot)
{
    var fullPath = Path.GetFullPath(Path.Combine(repoRoot, scriptPath));
    var normalizedRoot = Path.GetFullPath(repoRoot);
    return fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar);
}
```

---

## 9. Starter Template Generation

### Decision: **Embedded resource with string interpolation**

### Rationale

- Template is static, no need for templating engine
- Embed as `.yaml` resource file
- Simple string replacement for any dynamic values

### Template Structure

```yaml
# dottie.yaml - Generated by dottie init
# Documentation: https://github.com/ryandanthony/dottie

profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
      # - source: dotfiles/vimrc
      #   target: ~/.vimrc

    install:
      apt:
        - git
        - curl
      # github:
      #   - repo: owner/repo
      #     asset: "*-linux-amd64.tar.gz"
      #     binary: tool-name
      # snap:
      #   - name: code
      #     classic: true
```

---

## 10. LibGit2Sharp Usage

### Decision: **Optional dependency for repo root discovery only**

### Rationale

- Primary use: find repository root when running from subdirectory
- Fallback: walk up directory tree looking for `.git` folder
- Keep LibGit2Sharp optional to reduce binary size if not needed

### Implementation

```csharp
string? FindRepoRoot(string startPath)
{
    // Try LibGit2Sharp first if available
    try {
        return Repository.Discover(startPath);
    } catch {
        // Fallback: manual traversal
        var dir = new DirectoryInfo(startPath);
        while (dir != null) {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
```

### Trimming Note

LibGit2Sharp has native dependencies. If trimming is problematic:

- Use fallback-only approach
- Or: make LibGit2Sharp a separate optional package

---

## Summary of Decisions

| Topic                  | Decision                                   |
| ---------------------- | ------------------------------------------ |
| YAML Parsing           | YamlDotNet with source generators          |
| Validation             | Custom validators (no FluentValidation)    |
| Error Line Numbers     | Position tracking during/after parse       |
| Inheritance            | Eager resolution with cycle detection      |
| AOT/Trimming           | Source generators, no reflection           |
| Architecture Detection | `RuntimeInformation.OSArchitecture`        |
| Tilde Expansion        | `Environment.SpecialFolder.UserProfile`    |
| Script Security        | Path normalization and prefix check        |
| Template Generation    | Embedded resource                          |
| Git Integration        | Optional LibGit2Sharp with manual fallback |

---

## Open Questions (None)

All technical unknowns have been resolved.
