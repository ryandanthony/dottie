#!/bin/bash
# Integration test validation script for GitHub release deb dry-run
# Verifies that --dry-run reports intended actions without downloading or installing

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

EXPECTED_PACKAGE="gh"

echo "  Validating GitHub release deb dry-run in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 1: Ensure the package is NOT installed before the test
echo "  Ensuring $EXPECTED_PACKAGE is not pre-installed..."
if dpkg -s "$EXPECTED_PACKAGE" > /dev/null 2>&1; then
    sudo dpkg -r "$EXPECTED_PACKAGE" > /dev/null 2>&1 || true
fi
echo "    ✓ Package not installed"

# Step 2: Run dottie install with --dry-run
echo "  Running dottie install --dry-run..."
dry_run_output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile default --dry-run 2>&1)
EXIT_CODE=$?
echo "$dry_run_output"
echo ""

if [ $EXIT_CODE -ne 0 ]; then
    echo "    ✗ Dry-run failed with exit code $EXIT_CODE"
    exit 1
fi
echo "    ✓ Dry-run command succeeded"

# Step 3: Verify the output mentions "would be installed via dpkg"
echo "  Checking dry-run output message..."
if echo "$dry_run_output" | grep -qi "would be installed via dpkg"; then
    echo "    ✓ Output reports package would be installed via dpkg"
else
    echo "    ✗ Expected 'would be installed via dpkg' in output"
    echo "    Actual output: $dry_run_output"
    exit 1
fi

# Step 4: Verify the package was NOT actually installed
echo "  Verifying package was NOT installed..."
if dpkg -s "$EXPECTED_PACKAGE" > /dev/null 2>&1; then
    echo "    ✗ Package $EXPECTED_PACKAGE was installed — dry-run should not install!"
    sudo dpkg -r "$EXPECTED_PACKAGE" > /dev/null 2>&1 || true
    exit 1
fi
echo "    ✓ Package was not installed (dry-run respected)"

# Step 5: Verify no temp .deb files were left behind
echo "  Checking for leftover temp files..."
leftover_debs=$(find /tmp -maxdepth 1 -name "*.deb" -newer "$TEST_DIR/dottie.yml" 2>/dev/null | wc -l)
if [ "$leftover_debs" -gt 0 ]; then
    echo "    ✗ Found leftover .deb files in /tmp"
    find /tmp -maxdepth 1 -name "*.deb" -newer "$TEST_DIR/dottie.yml" 2>/dev/null
    exit 1
fi
echo "    ✓ No leftover temp files"

echo "  ✓ All GitHub release deb dry-run tests passed!"
exit 0
