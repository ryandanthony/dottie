#!/bin/bash
# Validate link-command scenario
# Tests the dottie link command functionality

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating link-command scenario in $TEST_DIR"

# Test 1: Dry-run should succeed without creating symlinks
echo "  - Testing dry-run mode..."
dottie link --dry-run -c "$TEST_DIR/dottie.yaml" > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "  FAIL: dry-run mode failed"
    exit 1
fi
echo "  PASS: dry-run mode works"

# Test 2: Default profile should resolve
echo "  - Testing default profile resolution..."
output=$(dottie link --dry-run -c "$TEST_DIR/dottie.yaml" 2>&1)
if ! echo "$output" | grep -q "Would create"; then
    echo "  FAIL: dry-run output missing 'Would create'"
    exit 1
fi
echo "  PASS: default profile resolves correctly"

# Test 3: Work profile should resolve
echo "  - Testing work profile resolution..."
output=$(dottie link --dry-run -p work -c "$TEST_DIR/dottie.yaml" 2>&1)
if ! echo "$output" | grep -q "Would create"; then
    echo "  FAIL: work profile dry-run output missing 'Would create'"
    exit 1
fi
echo "  PASS: work profile resolves correctly"

echo "  All link-command tests passed!"
exit 0
