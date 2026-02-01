# Feature Specification: Installation Sources

**Feature Branch**: `006-install-sources`  
**Created**: January 31, 2026  
**Status**: Draft  
**Input**: User description: "Dottie installs tools from multiple sources including GitHub Releases, APT Packages, Private APT Repositories, Shell Scripts, Fonts, and Snap Packages"

## Clarifications

### Session 2026-01-31

- Q: When a GitHub release asset glob pattern matches multiple files, how should Dottie behave? → A: Use first match (alphabetically sorted) and warn the user
- Q: How should Dottie handle network failures during downloads? → A: Retry up to 3 times with exponential backoff, then warn for failed item, continue with remaining installations, report all failures at end
- Q: When the user lacks sudo privileges for APT/snap operations, how should Dottie behave? → A: Check upfront, warn which sources will be skipped, proceed with non-sudo sources
- Q: How should Dottie provide progress feedback during installation? → A: Source-level progress showing each source type and item name as processing
- Q: How are installation source conflicts handled (same tool from multiple sources)? → A: Install both; different sources install to different locations (e.g., ~/bin/ vs /usr/bin/), user manages PATH priority
- Q: What happens when a script modifies the shell environment (e.g., adds to PATH)? → A: Silent; scripts run normally, Dottie doesn't detect or warn about shell environment changes
- Q: How does the system handle partially completed installations after interruption? → A: Detect and skip; each source checks if items are already installed and skips them naturally (no state file needed, aligns with idempotency principle)
- Q: Should --dry-run be a required feature? → A: Yes; --dry-run must preview all installation actions AND perform full validation (check GitHub releases exist, scripts exist, package names valid, URLs reachable) without making any changes

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Install Tools from GitHub Releases (Priority: P1)

As a developer setting up a new Linux workstation, I want Dottie to automatically download and install binary tools from GitHub releases so that I can get the latest versions of my favorite CLI tools without manually downloading them.

**Why this priority**: GitHub Releases is the most common source for modern CLI tools (e.g., fzf, ripgrep, bat). This covers the majority of tool installation use cases and provides the highest value for developers.

**Independent Test**: Can be fully tested by configuring a GitHub release item in the config file and running the install command. Delivers value by automatically downloading, extracting, and placing binaries in the user's path.

**Acceptance Scenarios**:

1. **Given** a configuration with a GitHub release entry specifying repo, asset pattern, and binary name, **When** the user runs the install command, **Then** the binary is downloaded from the latest release, extracted, and placed in `~/bin/` with executable permissions.

2. **Given** a configuration with a pinned version for a GitHub release, **When** the user runs the install command, **Then** the specific version is downloaded instead of the latest release.

3. **Given** a GitHub release asset in `.tar.gz`, `.zip`, or `.tgz` format, **When** the asset is downloaded, **Then** the archive is automatically extracted and the specified binary is located within it.

4. **Given** the `~/bin/` directory does not exist, **When** a GitHub release binary is installed, **Then** the directory is created automatically.

---

### User Story 2 - Install Standard APT Packages (Priority: P2)

As a developer, I want Dottie to install standard Ubuntu packages from the official repositories so that I can declaratively manage my system packages alongside my dotfiles.

**Why this priority**: APT packages are fundamental to Ubuntu systems and cover a wide range of commonly needed tools. This is the simplest installation source with well-understood behavior.

**Independent Test**: Can be fully tested by listing package names in the config and running the install command. Delivers value by automating `apt-get install` for declared packages.

**Acceptance Scenarios**:

1. **Given** a configuration with a list of APT package names, **When** the user runs the install command, **Then** all specified packages are installed via `apt-get install -y`.

2. **Given** multiple APT packages are configured, **When** the install command runs, **Then** `apt-get update` is executed only once before installing all packages.

3. **Given** a package is already installed, **When** the install command runs, **Then** the package is skipped or updated without error.

---

### User Story 3 - Install from Private APT Repositories (Priority: P3)

As a developer, I want Dottie to add third-party APT repositories with their GPG keys and install packages from them so that I can get tools like Docker, VS Code, or other vendor-provided packages.

**Why this priority**: Many essential development tools (Docker, VS Code, Kubernetes tools) are distributed through private APT repositories. This enables access to vendor-maintained packages with proper authentication.

**Independent Test**: Can be fully tested by configuring a private repo entry with key URL, repository line, and package list. Delivers value by automating the entire process of adding trusted repositories and installing packages.

**Acceptance Scenarios**:

1. **Given** a configuration with a private APT repo entry including name, key_url, repo line, and packages, **When** the user runs the install command, **Then** the GPG key is downloaded, added to the system, the repository is configured, and packages are installed.

2. **Given** a private APT repo configuration, **When** the GPG key is added, **Then** it is stored securely using `signed-by` or in `/etc/apt/trusted.gpg.d/`.

3. **Given** the repository source is added, **When** packages are installed, **Then** `apt-get update` is run to refresh the package list before installation.

---

### User Story 4 - Run Custom Installation Scripts (Priority: P4)

As a developer, I want Dottie to run custom shell scripts from my dotfiles repository so that I can handle complex installation scenarios that don't fit other sources (e.g., installing nvm, sdkman, or custom configurations).

**Why this priority**: Scripts provide flexibility for edge cases and complex installations. They must be from the local repository for security (no external URLs).

**Independent Test**: Can be fully tested by placing a script in the repository and referencing it in the config. Delivers value by executing custom installation logic.

**Acceptance Scenarios**:

1. **Given** a configuration listing script paths relative to the repository root, **When** the user runs the install command, **Then** each script is executed with bash from the repository root directory.

2. **Given** a script path that does not exist in the repository, **When** the install command runs, **Then** an error is reported indicating the missing script.

3. **Given** a script fails during execution, **When** the error occurs, **Then** the failure is reported clearly with the script name and exit code.

---

### User Story 5 - Install Fonts (Priority: P5)

As a developer, I want Dottie to install fonts to my user font directory so that I can have my preferred coding fonts (e.g., Nerd Fonts) available without manual installation.

**Why this priority**: Fonts are important for terminal and editor customization but are lower priority than core tool installation. Installation is straightforward but requires cache refresh.

**Independent Test**: Can be fully tested by configuring a font entry with name and download URL. Delivers value by downloading, extracting, and registering fonts.

**Acceptance Scenarios**:

1. **Given** a configuration with a font entry including name and URL, **When** the user runs the install command, **Then** the font archive is downloaded and extracted to `~/.local/share/fonts/<name>/`.

2. **Given** fonts are installed, **When** the installation completes, **Then** `fc-cache -fv` is run to refresh the system font cache.

3. **Given** the font directory does not exist, **When** fonts are installed, **Then** the directory structure is created automatically.

---

### User Story 6 - Install Snap Packages (Priority: P6)

As a developer, I want Dottie to install snap packages so that I can use tools distributed via the Snap store, including those requiring classic confinement.

**Why this priority**: Snap is an alternative package format with some unique applications. Lower priority as it overlaps with APT for many tools, but necessary for certain applications.

**Independent Test**: Can be fully tested by configuring a snap entry with name and optional classic flag. Delivers value by automating snap package installation.

**Acceptance Scenarios**:

1. **Given** a configuration with a snap package entry, **When** the user runs the install command, **Then** the package is installed via `snap install <name>`.

2. **Given** a snap package entry with `classic: true`, **When** the package is installed, **Then** the `--classic` flag is passed to the snap install command.

3. **Given** a snap package is already installed, **When** the install command runs, **Then** the package is refreshed or skipped without error.

---

### Edge Cases

- When a GitHub release asset pattern matches multiple files, use the first match (alphabetically sorted) and emit a warning to the user
- Network failures during downloads: retry up to 3 times with exponential backoff, then warn for the failed item, continue with remaining installations, and report all failures at end
- Missing sudo privileges: check upfront, warn which sources (APT, snap) will be skipped, proceed with non-sudo sources (GitHub releases, fonts, scripts)
- Installation source conflicts (same tool from multiple sources): install both; user manages PATH priority
- Script modifies shell environment: silent operation; Dottie doesn't track or warn about shell config changes
- Partially completed installations: idempotent behavior; each source detects already-installed items and skips them

## Requirements *(mandatory)*

### Functional Requirements

#### GitHub Releases

- **FR-001**: System MUST download assets from GitHub releases matching the configured glob pattern
- **FR-002**: System MUST extract archives in `.tar.gz`, `.zip`, and `.tgz` formats
- **FR-003**: System MUST copy the specified binary to `~/bin/` and set executable permissions
- **FR-004**: System MUST support pinning to a specific version or defaulting to latest release
- **FR-005**: System MUST create the `~/bin/` directory if it does not exist

#### APT Packages

- **FR-006**: System MUST install packages using `apt-get install -y`
- **FR-007**: System MUST run `apt-get update` once per installation session before installing packages

#### Private APT Repositories

- **FR-008**: System MUST download GPG keys from the configured URL
- **FR-009**: System MUST add GPG keys securely using `signed-by` or `/etc/apt/trusted.gpg.d/`
- **FR-010**: System MUST add repository sources to `/etc/apt/sources.list.d/<name>.list`
- **FR-011**: System MUST run `apt-get update` after adding repository sources
- **FR-012**: System MUST install specified packages from the configured repository

#### Shell Scripts

- **FR-013**: System MUST execute scripts using bash from the repository root directory
- **FR-014**: System MUST only allow scripts that exist within the repository (no external URLs)
- **FR-015**: System MUST report script execution failures with script name and exit code

#### Fonts

- **FR-016**: System MUST download font archives from the configured URL
- **FR-017**: System MUST extract fonts to `~/.local/share/fonts/<name>/`
- **FR-018**: System MUST run `fc-cache -fv` after font installation to refresh the cache

#### Snap Packages

- **FR-019**: System MUST install snap packages using `snap install <name>`
- **FR-020**: System MUST pass `--classic` flag when configured

#### Installation Order

- **FR-021**: System MUST process installation sources in priority order: GitHub Releases → APT Packages → Private APT Repos → Shell Scripts → Fonts → Snap Packages

#### Progress & Observability

- **FR-022**: System MUST display source-level progress showing each source type and item name as it processes
- **FR-023**: System MUST display a summary at the end listing successful installations, warnings, and failures

#### Dry Run Mode

- **FR-024**: System MUST support a --dry-run flag that previews all installation actions without executing them
- **FR-025**: In --dry-run mode, system MUST validate that GitHub releases exist and assets match the configured pattern
- **FR-026**: In --dry-run mode, system MUST validate that configured scripts exist within the repository
- **FR-027**: In --dry-run mode, system MUST validate that font and GPG key URLs are reachable

### Key Entities

- **GitHubReleaseItem**: Represents a tool to be installed from GitHub releases. Key attributes: repository (owner/repo), asset pattern (glob), binary name, optional version pin.
- **AptPackage**: Represents a standard APT package to install. Key attribute: package name.
- **AptRepoItem**: Represents a private APT repository configuration. Key attributes: name, GPG key URL, repository source line, list of packages.
- **ScriptItem**: Represents a custom installation script. Key attribute: relative path within repository.
- **FontItem**: Represents a font to install. Key attributes: display name, download URL.
- **SnapItem**: Represents a snap package. Key attributes: package name, classic confinement flag.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can install a GitHub release binary with a single configuration entry in under 30 seconds
- **SC-002**: Users can install 10+ APT packages in a single configuration block
- **SC-003**: Users can add and install from a private APT repository with one configuration entry
- **SC-004**: All installation sources execute in the documented priority order
- **SC-005**: 100% of supported archive formats (.tar.gz, .zip, .tgz) are correctly extracted
- **SC-006**: Users can set up a complete development environment with all installation sources in under 10 minutes
- **SC-007**: Failed installations provide clear error messages identifying the source, item, and failure reason
- **SC-008**: Installed binaries are immediately usable from `~/bin/` without manual PATH configuration
