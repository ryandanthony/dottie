# Research: GitHub Release Asset Type

**Feature**: 012-github-release-type
**Date**: 2026-02-08

## Research Tasks

### R1: How should the `type` field be modeled?

**Decision**: Add a `GithubReleaseAssetType` enum with values `Binary` and `Deb`. Add an optional `Type` property to `GithubReleaseItem` that defaults to `Binary`.

**Rationale**:
- An enum is type-safe and provides compile-time validation of allowed values.
- YamlDotNet with `CamelCaseNamingConvention` maps YAML `binary`/`deb` to C# `Binary`/`Deb` automatically for enums.
- The existing codebase already uses `IgnoreUnmatchedProperties()` so adding a new property won't break deserialization of old configs.
- Using a nullable property with default in the model (rather than a non-required property) lets us distinguish "user explicitly set binary" from "user omitted type" if needed later, but for this feature the behavior is identical.

**Alternatives considered**:
- **String field with validation**: Simpler YAML mapping, but loses compile-time type safety and requires string comparisons throughout the installer. Rejected because enum is the standard pattern in this codebase (see `InstallSourceType`, `InstallStatus`).
- **Separate installer class per type**: Would require a new `IInstallSource` implementation and registration. Rejected because the download, API lookup, asset matching, and variable substitution logic is shared — only the final install step differs.

### R2: How should `binary` field validation work conditionally?

**Decision**: Modify `InstallBlockValidator.ValidateGithubRelease()` to only require `binary` when `Type` is `Binary` (or omitted/defaulted to `Binary`).

**Rationale**:
- The current validator unconditionally requires `binary`. With `type: deb`, the binary field is meaningless — `dpkg` handles installation to system paths.
- The validator already has access to the full `GithubReleaseItem` record, so checking `item.Type` is trivial.
- This is a backward-compatible change: all existing configs have `binary` set, so they continue to pass validation.

**Alternatives considered**:
- **Separate validator for deb type**: Unnecessary complexity. A single conditional check is clearer.
- **Make `binary` always optional**: Would weaken validation for `type: binary` users who accidentally omit it. Rejected.

### R3: How should the `GithubReleaseInstaller` be extended?

**Decision**: Add a private method `InstallDebPackageAsync()` alongside the existing binary installation logic. The existing `InstallSingleItemAsync()` method dispatches to either the binary path or the deb path based on `item.Type`.

**Rationale**:
- The download, API lookup, asset matching, and variable resolution logic is 100% shared between binary and deb types. Only the post-download installation step differs.
- The existing installer already has an internal branching pattern for archives vs. standalone binaries in `ExtractOrUseBinary()`. Adding another branch for deb is consistent.
- Keeps all GitHub release logic in one installer rather than splitting across two.

**Alternatives considered**:
- **Strategy pattern with separate classes**: Would require extracting the shared download logic into a base class or shared service. Over-engineered for two types; can be introduced later if more types are added (rpm, appimage, etc.).
- **New `DebPackageInstaller : IInstallSource`**: Would duplicate the GitHub API interaction, asset matching, and download logic. Rejected per DRY and constitution ("avoid unnecessary abstraction").

### R4: How should deb idempotency work?

**Decision**: Use `dpkg -s <package-name>` to check if a package is already installed. Extract the package name from the `.deb` filename using the standard Debian naming convention (`<package>_<version>_<arch>.deb`) or from `dpkg-deb --showformat='${Package}' -W <file>` after download.

**Rationale**:
- `dpkg -s` is the standard way to check if a Debian package is installed. Exit code 0 = installed; non-zero = not installed.
- For the pre-download check (avoiding unnecessary downloads), we can derive the package name from the repo name or the asset filename pattern. Most `.deb` files follow the `<package>_<version>_<arch>.deb` convention.
- For a robust post-download check, `dpkg-deb -W` can extract exact package metadata from the downloaded file.

**Two-phase approach**:
1. **Pre-download**: Try to derive package name from repo name (e.g., `jgraph/drawio-desktop` → `drawio`) or asset pattern. Run `dpkg -s <name>` to check. If installed, skip the download entirely.
2. **Post-download fallback**: If pre-download check is inconclusive (couldn't derive name), download the file, extract the package name with `dpkg-deb`, then check `dpkg -s`. If already installed, clean up and skip.

**Simplification for MVP**: For the initial implementation, always download the asset first, then extract the package name with `dpkg-deb --showformat='${Package}' -W <file>`, then check `dpkg -s`. This is simpler and more reliable. Pre-download optimization can be added later as a separate enhancement.

**Alternatives considered**:
- **Optional `package` field in config**: Adds config complexity for the user. Rejected for MVP but noted as a future option.
- **Derive package name from repo name only**: Too unreliable — repo names don't always match package names (e.g., `drawio-desktop` repo ships `drawio` package).

### R5: How should deb dry-run work?

**Decision**: Follow the existing dry-run pattern — validate the GitHub release and asset exist via API (same as binary dry-run), then report "would be installed via dpkg" without downloading or installing.

**Rationale**:
- The existing dry-run for binary type performs a HEAD/GET to the GitHub API to verify the release and asset exist. This same check applies to deb type.
- No need to download the actual `.deb` file during dry-run — the purpose is to validate the configuration points to a real artifact.

**Alternatives considered**:
- **Download and inspect but don't install**: Wasteful for dry-run. Rejected.

### R6: How should sudo detection work for deb type?

**Decision**: Reuse the existing `context.HasSudo` flag, following the exact pattern from `AptPackageInstaller`.

**Rationale**:
- `HasSudo` is already set in `InstallContext` and checked by `AptPackageInstaller` and `AptRepoInstaller`.
- Consistent pattern: if `!context.HasSudo`, return `InstallResult.Warning()` with "Sudo required to install .deb packages".
- This is checked before any download attempt to avoid wasting bandwidth.

**Alternatives considered**: None — this is the established pattern with no reason to deviate.

### R7: How should dpkg availability be checked?

**Decision**: Use `IProcessRunner` to run `which dpkg` (or `command -v dpkg`). If it fails (exit code non-zero), return `InstallResult.Failed()` with "dpkg is not available on this system."

**Rationale**:
- `IProcessRunner` is already injected into `GithubReleaseInstaller` and used for `chmod +x` and `which`/`where` checks.
- Checking `dpkg` availability before attempting installation provides a clear error rather than a confusing process execution failure.
- This check can be cached for the duration of the install run (dpkg won't appear/disappear mid-run).

**Alternatives considered**:
- **Check once at startup**: Would require changing the installer interface. Rejected for simplicity.
- **Just let dpkg fail**: Poor UX — the error from a missing command is confusing. Rejected per constitution ("actionable errors").

### R8: Asset validation for deb type

**Decision**: After download, validate the asset has a `.deb` file extension. Optionally, check the first bytes for the Debian package magic (`!<arch>\n` header followed by `debian-binary`).

**Rationale**:
- Extension check is the simplest validation and catches obvious misconfiguration (e.g., user configured `type: deb` but the asset is a `.tar.gz`).
- Magic byte check adds defense-in-depth but is not strictly necessary for MVP since `dpkg -i` will fail with a clear error on non-deb files anyway.

**Decision for MVP**: Extension check only. If the asset pattern doesn't end in `.deb`, fail before download. If the downloaded file is corrupt, let `dpkg -i` report the error naturally.

**Alternatives considered**:
- **No validation, let dpkg handle it**: dpkg errors can be cryptic. A pre-check provides a better user experience. Rejected as-is but dpkg's error is an acceptable fallback.

### R9: Temp file cleanup strategy

**Decision**: Use a `try/finally` pattern around the download and install steps. Create a temp directory, download the `.deb` file into it, install, then delete the temp directory in the `finally` block.

**Rationale**:
- The existing binary installer already creates temp directories for archive extraction using this pattern.
- Ensures cleanup happens even on failure (dpkg error, cancellation, etc.).
- Constitution requires minimizing data retention.

**Alternatives considered**: None — this is the established pattern.

### R10: Profile inheritance and MergeKey for type field

**Decision**: No changes needed. The `MergeKey` for `GithubReleaseItem` is `Repo`. When profiles are merged, child entries override parent entries with the same `Repo`. The `type` field will naturally carry over as part of the record.

**Rationale**:
- `ProfileMerger.MergeByKey()` uses `g => g.Repo` as the key selector. A child profile can override a parent's GitHub release entry (including changing the `type`) by specifying the same repo.
- No changes to the merge infrastructure are needed.

**Alternatives considered**: None — the existing system handles this correctly.
