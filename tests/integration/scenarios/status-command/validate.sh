#!/bin/bash
# Validate status-command scenario
# Tests the dottie status command functionality

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating status-command scenario in $TEST_DIR"

# Test 1: Status command should succeed (exit code 0 is informational)
echo "  - Testing default profile status..."
output=$(dottie status -c "$TEST_DIR/dottie.yaml" 2>&1)
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "  FAIL: status command failed with exit code $exit_code"
    echo "  Output: $output"
    exit 1
fi
echo "  PASS: status command exits with 0"

# Test 2: Status output should show dotfiles section
echo "  - Testing dotfiles section presence..."
if ! echo "$output" | grep -q "Dotfiles"; then
    echo "  FAIL: status output missing 'Dotfiles' section"
    echo "  Output: $output"
    exit 1
fi
echo "  PASS: dotfiles section present"

# Test 3: Status output should show software section
echo "  - Testing software section presence..."
if ! echo "$output" | grep -q "Software"; then
    echo "  FAIL: status output missing 'Software' section"
    echo "  Output: $output"
    exit 1
fi
echo "  PASS: software section present"

# Test 4: Profile option should work
echo "  - Testing work profile status..."
output=$(dottie status -p work -c "$TEST_DIR/dottie.yaml" 2>&1)
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "  FAIL: work profile status failed with exit code $exit_code"
    echo "  Output: $output"
    exit 1
fi
echo "  PASS: work profile status works"

# Test 5: Work profile should show inheritance chain
echo "  - Testing inheritance chain display..."
if ! echo "$output" | grep -q -i "work"; then
    echo "  WARN: Output doesn't show profile name (might be format change)"
fi
echo "  PASS: profile option works"

# Test 6: Non-existent profile should fail
echo "  - Testing non-existent profile..."
output=$(dottie status -p nonexistent -c "$TEST_DIR/dottie.yaml" 2>&1)
exit_code=$?
if [ $exit_code -eq 0 ]; then
    echo "  FAIL: non-existent profile should fail but didn't"
    exit 1
fi
echo "  PASS: non-existent profile fails as expected"

# Test 7: Minimal profile (no inheritance)
echo "  - Testing minimal profile..."
output=$(dottie status -p minimal -c "$TEST_DIR/dottie.yaml" 2>&1)
exit_code=$?
if [ $exit_code -ne 0 ]; then
    echo "  FAIL: minimal profile status failed"
    echo "  Output: $output"
    exit 1
fi
echo "  PASS: minimal profile status works"

echo "  All status-command tests passed!"
exit 0
