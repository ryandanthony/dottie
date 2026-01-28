<!--
Sync Impact Report

- Version change: 1.3.0 → 1.4.0
- Modified principles: None
- Added sections:
	- Testing Philosophy
- Removed sections: None
- Templates requiring updates:
	- ✅ .specify/templates/plan-template.md
	- ⚠️ .specify/templates/spec-template.md (no change required)
  - ⚠️ .specify/templates/tasks-template.md (no change required)
	- ⚠️ .specify/templates/checklist-template.md (no change required)
- Follow-up TODOs:
	- TODO(RATIFICATION_DATE): Set the original adoption date for this constitution.
-->

# dottie Constitution

## Core Principles

### Safety & Explicitness (Non-Negotiable)

dottie MUST not make irreversible changes without explicit user intent. Default to
previewing actions (e.g., dry-run), clearly show what will be changed, and require
an explicit apply/confirm step for writes. Destructive operations (delete, purge,
overwrite) MUST require an additional explicit flag and MUST offer a safe rollback
path (e.g., backups) where feasible.

### Idempotent, Declarative State

Repeated runs with the same inputs MUST converge to the same system state.
Configuration MUST be treated as declarative desired state (what to install, what
to link, what to configure), and the tool MUST detect and skip already-satisfied
steps. Where idempotency is not possible (e.g., interactive prompts), the tool
MUST provide a non-interactive mode and document any remaining limitations.

### Secure-by-Default Installations

Installers and bootstrap steps MUST minimize risk:

- Prefer OS package managers (e.g., apt) over ad-hoc installers.
- Avoid unsafe patterns like piping remote scripts directly to a shell.
- Verify provenance where possible (checksums/signatures, pinned versions).
- Never log secrets, tokens, or credentials.
- Changes requiring elevated privileges MUST be clearly explained and limited in
  scope.

### Ubuntu-First Compatibility

dottie targets Linux with Ubuntu as the primary supported distribution. Any
feature MUST be designed to work on Ubuntu LTS versions the project claims to
support. If behavior is distro-specific, it MUST be detected and handled
explicitly (feature flags, adapters, clear error messages) rather than failing in
surprising ways.

### Observable, Supportable Tooling

The tool MUST be debuggable by users:

- Emit clear, actionable errors (what failed, why, and how to fix).
- Provide a verbose mode suitable for bug reports.
- Log what actions were taken (and what was skipped) without leaking secrets.
- Prefer small, composable steps over clever automation.

## Product Scope & Compatibility

- The primary use case is managing dotfiles and installing software on Ubuntu.
- Prefer standards and common conventions (e.g., XDG base directories) over
  bespoke locations.
- Any file/link operations MUST be explicit about source/target and MUST not
  silently overwrite user files.
- Non-interactive operation MUST be supported for automation (CI, provisioning).

## Development Workflow

- Changes that affect user-facing behavior MUST be documented and reviewable via
  a feature spec and plan (using `.specify/templates/*`).
- PRs MUST include a clear description of behavior changes and a rollback story
  if the change is risky.
- Breaking changes MUST be called out explicitly and versioned appropriately.
- Tests are expected for logic that can be tested deterministically (parsing,
  decision-making, state resolution). If tests are not added, the PR MUST justify
  why and describe alternative validation.

## Decision-Making Rules

- When multiple approaches satisfy the requirement, the implementation SHOULD use
  the simplest one.
- Code SHOULD optimize for clarity and maintainability by default. Performance
  optimizations MUST be driven by explicit requirements or measurements.
- Changes MUST NOT introduce speculative optimization without evidence (a stated
  requirement, profiling results, or a concrete performance failure).
- Composition SHOULD be the default reuse mechanism. Inheritance SHOULD be used
  only when it models a true “is-a” relationship and reduces complexity.
- Prefer declarative approaches when they improve clarity, testability, or
  idempotency.

## Security & Safety Values

- Security is a first-class requirement for user-facing behavior.
- Secrets MUST NOT be committed to the repository (code, config, docs, examples).
- The system MUST fail closed rather than open when security-relevant state is
  unknown or invalid.
- Trust boundaries SHOULD be minimized and made explicit (e.g., privilege
  escalation, network downloads, executing external code).
- All external inputs MUST be validated (CLI args, environment, file contents,
  network responses).
- Data retention MUST be minimized: do not persist data unless required, and
  store the smallest amount needed for the shortest time.

## Guiding Principles

- Implementations SHOULD favor simplicity over cleverness.
- Changes SHOULD optimize for long-term maintainability.
- Prefer explicitness over implicit behavior (“magic”), especially in
  user-facing commands.
- Consistency SHOULD be preferred over novelty in UX, CLI flags, and config.
- Design SHOULD anticipate change; avoid over-fitting to a hypothetical “perfect”
  future design.
- Functionality SHOULD be built from small, composable units.
- Avoid unnecessary abstraction; introduce indirection only when it reduces
  real duplication or improves testability.

## Testing Philosophy

- Testing is a first-class requirement; it MUST be planned alongside behavior
  changes, not deferred.
- Tests MUST validate behavior and contracts rather than internal implementation
  details.
- Integration tests SHOULD be the primary mechanism for verifying end-to-end
  correctness and real workflows.
- Tests SHOULD reflect realistic workflows and data flows.
- Tests MUST be deterministic and isolated (no hidden dependencies on network,
  time, global state, or order).
- Every test SHOULD increase confidence and reduce ambiguity; remove or rewrite
  tests that are flaky or unclear.

## Governance

This constitution defines non-negotiable project rules and supersedes templates
and conventions when there is a conflict.

- Amendments MUST be made via PR with:
  - Rationale
  - Migration/rollout plan if behavior expectations change
  - Version bump per the policy below
- Versioning policy:
  - MAJOR: Removes/weakens a principle, or materially changes governance.
  - MINOR: Adds a new principle/section, or adds new non-trivial constraints.
  - PATCH: Clarifications, wording improvements, and non-semantic refinements.
- Compliance review expectation: PR authors and reviewers MUST explicitly verify
  compliance with the Core Principles for any user-facing change.

**Version**: 1.4.0 | **Ratified**: TODO(RATIFICATION_DATE): Unknown | **Last Amended**: 2026-01-28
