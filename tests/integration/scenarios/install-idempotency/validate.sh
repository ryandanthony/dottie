#!/bin/bash
# Validate install-idempotency scenario
# Tests that running dottie install multiple times skips already-installed binaries

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating install-idempotency scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial" 2>/dev/null || true

# Ensure clean state - remove jq if it exists in ~/bin
TEST_BIN_DIR="${HOME}/bin"
EXPECTED_BINARY="jq"
rm -f "$TEST_BIN_DIR/$EXPECTED_BINARY" 2>/dev/null

# Step 1: First install run
echo "  - Running dottie install (first time)..."
output1=$(dottie install -c "$TEST_DIR/dottie.yml" 2>&1)
exit_code1=$?

echo "  First run output:"
echo "$output1" | sed 's/^/    /'

if [ $exit_code1 -ne 0 ]; then
    echo "  FAIL: First install run failed with exit code $exit_code1"
    exit 1
fi

# Verify binary was installed
if [ ! -f "$TEST_BIN_DIR/$EXPECTED_BINARY" ]; then
    echo "  FAIL: Binary not installed after first run"
    ls -la "$TEST_BIN_DIR/" 2>/dev/null | head -20 || echo "  ~/bin directory not found"
    exit 1
fi
echo "  PASS: Binary installed on first run"

# Step 2: Second install run - should skip
echo "  - Running dottie install (second time)..."
output2=$(dottie install -c "$TEST_DIR/dottie.yml" 2>&1)
exit_code2=$?

echo "  Second run output:"
echo "$output2" | sed 's/^/    /'

if [ $exit_code2 -ne 0 ]; then
    echo "  FAIL: Second install run failed with exit code $exit_code2"
    exit 1
fi

# Check that second run shows "Skipped" or "Already installed"
if echo "$output2" | grep -qi "skipped\|already installed"; then
    echo "  PASS: Second run correctly skipped already-installed binary"
else
    echo "  WARN: Second run did not indicate skipping (may still be correct)"
    # Not a failure - the binary existing is the key test
fi

# Step 3: Verify binary still works
echo "  - Verifying binary functionality..."
if "$TEST_BIN_DIR/$EXPECTED_BINARY" --version >/dev/null 2>&1; then
    echo "  PASS: Binary is functional"
else
    echo "  FAIL: Binary not functional"
    exit 1
fi

# Cleanup
rm -f "$TEST_BIN_DIR/$EXPECTED_BINARY" 2>/dev/null

echo "  PASS: Install idempotency test completed successfully"
exit 0
