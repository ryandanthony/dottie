#!/bin/bash
# Integration Test Scenario Runner
# Purpose: Execute test scenarios and validate outcomes
#
# Environment variables:
#   TEST_NAME - Run only this specific scenario (optional)
#   VERBOSITY - Output level: Quiet, Normal (default), Verbose (optional)
#
# Exit codes:
#   0 - All scenarios passed
#   1 - One or more scenarios failed

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCENARIOS_DIR="/home/testuser/scenarios"
PASSED=0
FAILED=0

# Get environment settings
TEST_NAME="${TEST_NAME:-}"
VERBOSITY="${VERBOSITY:-Normal}"

# Helper function for conditional output
log() {
    if [ "$VERBOSITY" != "Quiet" ]; then
        echo "$@"
    fi
}

verbose() {
    if [ "$VERBOSITY" = "Verbose" ]; then
        echo "$@"
    fi
}

quiet_log() {
    # Always show errors in any verbosity mode
    echo "$@"
}

# Header output
if [ "$VERBOSITY" != "Quiet" ]; then
    echo "=========================================="
    echo "Dottie Integration Test Runner"
    if [ -n "$TEST_NAME" ]; then
        echo "Test: $TEST_NAME"
    fi
    echo "=========================================="
    echo ""
fi

# Check dottie is available
if ! command -v dottie &> /dev/null; then
    quiet_log "ERROR: dottie binary not found in PATH"
    exit 1
fi

verbose "dottie version: $(dottie --version 2>/dev/null || echo 'unknown')"
verbose ""

# Collect scenarios to run
scenarios_to_run=()
for scenario_dir in "$SCENARIOS_DIR"/*/; do
    if [ -d "$scenario_dir" ]; then
        scenario_name=$(basename "$scenario_dir")
        
        # If TEST_NAME is specified, only run that scenario
        if [ -n "$TEST_NAME" ] && [ "$scenario_name" != "$TEST_NAME" ]; then
            continue
        fi
        
        scenarios_to_run+=("$scenario_dir")
    fi
done

# Validate scenario exists if TEST_NAME was specified
if [ -n "$TEST_NAME" ] && [ ${#scenarios_to_run[@]} -eq 0 ]; then
    quiet_log "ERROR: Scenario '$TEST_NAME' not found"
    quiet_log "Available scenarios:"
    ls -1 "$SCENARIOS_DIR"
    exit 1
fi

# Run each scenario
for scenario_dir in "${scenarios_to_run[@]}"; do
    scenario_name=$(basename "$scenario_dir")
    
    if [ "$VERBOSITY" != "Quiet" ]; then
        echo "------------------------------------------"
        echo "Running scenario: $scenario_name"
        echo "------------------------------------------"
    fi

    # Check for scenario config
    if [ ! -f "$scenario_dir/dottie.yml" ]; then
        log "  SKIP: No dottie.yml found"
        continue
    fi

    # Create isolated test directory
    test_dir="/tmp/test-$scenario_name-$$"
    rm -rf "$test_dir"
    mkdir -p "$test_dir"
    cd "$test_dir"

    # Copy scenario files
    verbose "  Copying scenario files from $scenario_dir to $test_dir"
    cp -r "$scenario_dir"/* "$test_dir/"

    # Run dottie validate
    log "  Running dottie validate..."
    verbose "  Command: dottie validate --config $test_dir/dottie.yml"
    
    if dottie validate --config "$test_dir/dottie.yml" 2>&1 | while IFS= read -r line; do
        verbose "    $line"
    done; then
        log "  ✓ Validation passed"

        # Run custom validation if exists
        if [ -f "$scenario_dir/validate.sh" ]; then
            log "  Running custom validation..."
            verbose "  Command: bash $scenario_dir/validate.sh $test_dir"
            if bash "$scenario_dir/validate.sh" "$test_dir" 2>&1 | while IFS= read -r line; do
                verbose "    $line"
            done; then
                log "  ✓ Custom validation passed"
                PASSED=$((PASSED + 1))
            else
                quiet_log "  ✗ Custom validation failed"
                FAILED=$((FAILED + 1))
            fi
        else
            PASSED=$((PASSED + 1))
        fi
    else
        quiet_log "  ✗ Validation failed"
        FAILED=$((FAILED + 1))
    fi

    # Cleanup
    cd /home/testuser
    rm -rf "$test_dir"
    
    if [ "$VERBOSITY" != "Quiet" ]; then
        echo ""
    fi
done

# Summary
if [ "$VERBOSITY" != "Quiet" ]; then
    echo "=========================================="
    echo "Results: $PASSED passed, $FAILED failed"
    echo "=========================================="
fi

if [ $FAILED -gt 0 ]; then
    exit 1
fi

exit 0
