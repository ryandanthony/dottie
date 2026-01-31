# Feature Specification: Configuration Profiles

**Feature Branch**: `003-profiles`  
**Created**: January 30, 2026  
**Status**: Draft  
**Input**: User description: "Profiles allow different configurations for different machines/use cases with inheritance support"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Select a Named Profile (Priority: P1)

As a user with multiple machines, I want to select a specific profile when running Dottie commands so that I can apply the appropriate configuration for my current machine or use case.

**Why this priority**: Profile selection is the fundamental capability that enables all profile-based workflows. Without the ability to select a profile, no other profile features can function.

**Independent Test**: Can be tested by creating a configuration with multiple profiles and using the `--profile` flag to select one. Delivers the core value of machine-specific configurations.

**Acceptance Scenarios**:

1. **Given** a configuration file with profiles named "work" and "personal", **When** user runs a command with `--profile work`, **Then** the "work" profile configuration is applied
2. **Given** a configuration file with profiles, **When** user runs a command without specifying a profile, **Then** the "default" profile configuration is applied
3. **Given** a configuration file, **When** user specifies a non-existent profile name, **Then** the system displays a clear error message listing available profiles

---

### User Story 2 - Profile Inheritance (Priority: P2)

As a user maintaining multiple similar configurations, I want to define a base profile and have other profiles inherit from it so that I can avoid duplicating common settings across profiles.

**Why this priority**: Inheritance reduces configuration duplication and maintenance burden. It builds on basic profile selection but adds significant value for users with complex setups.

**Independent Test**: Can be tested by creating a base profile and a child profile that extends it, then verifying the child inherits base settings while applying its own overrides.

**Acceptance Scenarios**:

1. **Given** a profile "minimal" and a profile "full" that extends "minimal", **When** the "full" profile is resolved, **Then** it contains all settings from "minimal" plus any additional or overridden settings
2. **Given** a profile that extends another profile, **When** both profiles define the same setting, **Then** the child profile's value takes precedence
3. **Given** a profile chain (A extends B extends C), **When** resolving profile A, **Then** settings are merged in order: C → B → A with later values taking precedence

---

### User Story 3 - List Management in Profiles (Priority: P3)

As a user with profile-specific software needs, I want to add or override lists of items (dotfiles, packages) in child profiles so that I can customize what gets installed on each machine.

**Why this priority**: List management is essential for practical profile usage where different machines need different packages or dotfiles. Depends on inheritance being functional first.

**Independent Test**: Can be tested by creating profiles with dotfile or install lists and verifying items are correctly merged or overridden.

**Acceptance Scenarios**:

1. **Given** a base profile with dotfiles [A, B] and a child profile that extends it with dotfiles [C], **When** the profile is resolved, **Then** the effective list is [A, B, C]
2. **Given** a base profile with packages [X, Y] and a child profile that extends it with packages [Z], **When** the profile is resolved, **Then** the effective list is [X, Y, Z]
3. **Given** a profile configuration, **When** a list item in a child profile references the same target as a base profile item, **Then** the child's definition takes precedence (deduplication by target)

---

### User Story 4 - View Available Profiles (Priority: P4)

As a user, I want to see a list of available profiles and their relationships so that I can understand my configuration options before running commands.

**Why this priority**: Discoverability improves user experience but is not essential for core functionality. Users can work with profiles without this feature.

**Independent Test**: Can be tested by running a command to list profiles and verifying the output shows all defined profiles with their inheritance relationships.

**Acceptance Scenarios**:

1. **Given** a configuration with multiple profiles, **When** user requests the profile list, **Then** all profile names are displayed with their inheritance relationships
2. **Given** a profile that extends another, **When** viewing profile details, **Then** the parent profile name is shown

---

### Edge Cases

- What happens when a profile extends a non-existent profile? System should display a clear error identifying the missing parent profile
- What happens when there's a circular inheritance (A extends B, B extends A)? System should detect and report the cycle with the chain of profiles involved
- What happens when the "default" profile is not defined? System should use an implicit empty default profile
- What happens when a profile name contains special characters? Profile names should be validated to contain only alphanumeric characters, hyphens, and underscores
- What happens when multiple levels of inheritance produce conflicting list items? The deepest child profile's definition wins

## Clarifications

### Session 2026-01-30

- Q: How does a user declare override mode vs additive mode for lists? → A: There is no override mode; lists are always merged additively via `extends`

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support defining multiple named profiles within a configuration file
- **FR-002**: System MUST accept a `--profile <name>` flag on commands to select which profile to use
- **FR-003**: System MUST use a profile named "default" when no profile is explicitly specified
- **FR-004**: System MUST support the `extends: <profile-name>` syntax to declare profile inheritance
- **FR-005**: System MUST merge inherited profile settings with child profile settings, giving precedence to child values
- **FR-006**: System MUST support multi-level inheritance (profile chains) with correct precedence ordering
- **FR-007**: System MUST detect and report circular inheritance as a configuration error
- **FR-008**: System MUST validate that referenced parent profiles exist and report clear errors for missing profiles
- **FR-009**: System MUST merge list properties (dotfiles, packages) additively, appending child items to inherited parent items
- **FR-010**: System MUST deduplicate list items by target path, with child profile definitions taking precedence over parent definitions for the same target
- **FR-011**: System MUST validate profile names to contain only alphanumeric characters, hyphens, and underscores
- **FR-012**: System MUST provide a way to list available profiles and show their inheritance hierarchy

### Key Entities

- **Profile**: A named collection of configuration settings that can be selected at runtime. Contains optional parent reference, dotfiles list, install blocks, and other configuration options.
- **Default Profile**: A special profile name ("default") that is automatically used when no profile is specified. May be explicitly defined or implicitly empty.
- **Inheritance Chain**: The ordered sequence of profiles from a leaf profile back to its ultimate ancestor, used to determine setting merge order.

## Assumptions

- Profile configuration will be stored within the existing YAML configuration file structure
- The `--profile` flag will be available on all commands that apply configuration (link, install, apply, etc.)
- List merging is always additive (concatenation) with deduplication by target
- Profile names are case-sensitive
- There is no limit on inheritance depth, though deep chains may impact readability

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can switch between profiles in under 5 seconds by specifying the `--profile` flag
- **SC-002**: Configuration files with inheritance result in 50% less duplicated settings compared to fully duplicated profile definitions
- **SC-003**: 100% of circular inheritance cases are detected and reported before any configuration is applied
- **SC-004**: Users can view all available profiles and understand inheritance relationships with a single command
- **SC-005**: Profile selection errors (invalid names, missing profiles) are reported with actionable messages that include available profile names
