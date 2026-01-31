#!/bin/bash
# Integration Test Scenario Runner
# Purpose: Execute all test scenarios and validate outcomes
#
# Exit codes:
#   0 - All scenarios passed
#   1 - One or more scenarios failed

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCENARIOS_DIR="/home/testuser/scenarios"
PASSED=0
FAILED=0

echo "=========================================="
echo "Dottie Integration Test Runner"
echo "=========================================="
echo ""

# Check dottie is available
if ! command -v dottie &> /dev/null; then
    echo "ERROR: dottie binary not found in PATH"
    exit 1
fi

echo "dottie version: $(dottie --version 2>/dev/null || echo 'unknown')"
echo ""

# Run each scenario
for scenario_dir in "$SCENARIOS_DIR"/*/; do
    if [ -d "$scenario_dir" ]; then
        scenario_name=$(basename "$scenario_dir")
        echo "------------------------------------------"
        echo "Running scenario: $scenario_name"
        echo "------------------------------------------"

        # Check for scenario config
        if [ ! -f "$scenario_dir/dottie.yml" ]; then
            echo "  SKIP: No dottie.yml found"
            continue
        fi

        # Create isolated test directory
        test_dir="/tmp/test-$scenario_name"
        rm -rf "$test_dir"
        mkdir -p "$test_dir"
        cd "$test_dir"

        # Copy scenario files
        cp -r "$scenario_dir"/* "$test_dir/"

        # Run dottie (dry-run first, then apply if validate script exists)
        echo "  Running dottie validate..."
        if dottie validate --config "$test_dir/dottie.yml"; then
            echo "  ✓ Validation passed"

            # Run custom validation if exists
            if [ -f "$scenario_dir/validate.sh" ]; then
                echo "  Running custom validation..."
                if bash "$scenario_dir/validate.sh" "$test_dir"; then
                    echo "  ✓ Custom validation passed"
                    PASSED=$((PASSED + 1))
                else
                    echo "  ✗ Custom validation failed"
                    FAILED=$((FAILED + 1))
                fi
            else
                PASSED=$((PASSED + 1))
            fi
        else
            echo "  ✗ Validation failed"
            FAILED=$((FAILED + 1))
        fi

        # Cleanup
        cd /home/testuser
        rm -rf "$test_dir"
        echo ""
    fi
done

echo "=========================================="
echo "Results: $PASSED passed, $FAILED failed"
echo "=========================================="

if [ $FAILED -gt 0 ]; then
    exit 1
fi

exit 0
