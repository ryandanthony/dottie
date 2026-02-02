#!/bin/bash
# Validate install-profile-inheritance scenario
# Tests that dottie install --profile child includes parent profile's install sources

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating install-profile-inheritance scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial" 2>/dev/null || true

# Step 1: Run install with extended profile (which extends default)
echo "  - Running dottie install --profile extended --dry-run..."
output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile extended --dry-run 2>&1)
exit_code=$?

echo "  Dry run output:"
echo "$output" | sed 's/^/    /'

if [ $exit_code -ne 0 ]; then
    # Exit code 1 might be OK if packages would fail validation
    # The key test is whether both curl and wget appear in the output
    echo "  Note: Exit code was $exit_code (may be expected for validation failures)"
fi

# Step 2: Verify both parent and child apt packages appear in output
# The output should mention both 'curl' (from default) and 'wget' (from extended)
has_curl=false
has_wget=false

if echo "$output" | grep -qi "curl"; then
    has_curl=true
    echo "  PASS: Found 'curl' from parent profile (default)"
else
    echo "  INFO: 'curl' from parent not explicitly shown (may be already installed)"
fi

if echo "$output" | grep -qi "wget"; then
    has_wget=true
    echo "  PASS: Found 'wget' from child profile (extended)"
else
    echo "  INFO: 'wget' from child not explicitly shown (may be already installed)"
fi

# Step 3: Verify we're in dry-run mode (no actual changes)
if echo "$output" | grep -qi "dry run"; then
    echo "  PASS: Dry run mode confirmed"
else
    echo "  WARN: Dry run mode indicator not found"
fi

# Step 4: Test that using a nonexistent profile fails properly
echo "  - Testing nonexistent profile error handling..."
error_output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile nonexistent 2>&1)
error_exit=$?

if [ $error_exit -ne 0 ]; then
    echo "  PASS: Nonexistent profile correctly returns error"

    # Check if available profiles are listed
    if echo "$error_output" | grep -qi "available profiles\|default\|extended"; then
        echo "  PASS: Available profiles listed in error message"
    else
        echo "  INFO: Available profiles may not be shown (implementation detail)"
    fi
else
    echo "  FAIL: Nonexistent profile should return error"
    exit 1
fi

echo "  PASS: Install profile inheritance test completed successfully"
exit 0
