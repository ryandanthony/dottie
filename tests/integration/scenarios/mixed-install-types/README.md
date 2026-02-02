# Mixed Install Types Scenario

## Purpose
Test that dottie handles multiple install types in a single configuration and profile.

## What is tested
1. **APT packages** - APT package installation
2. **Scripts** - Script installation
3. **Mixed execution** - All install types run correctly in one command
4. **Error handling** - Partial failures are handled appropriately
5. **Completeness** - All install types complete even if one fails

## Expected behavior
1. Configuration with APT, scripts, and other install types validates
2. `dottie install` executes all install types
3. Each install type is processed in correct order
4. If one type fails, others still attempt to execute
5. Output clearly indicates which install types succeeded/failed

## Rationale
Real-world dotfile configs typically combine multiple installation methods.
This ensures they work correctly together without conflicts.
