#!/bin/bash
# Integration test validation script for snap package installation
# Verifies that snap packages were successfully installed

# Configuration
TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating snap package installation in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 1: Check if snapd is available
if ! command -v snap &> /dev/null; then
    echo "  ⊘ snapd not available in test environment (expected in container)"
    echo "  ⊘ Skipping snap installation test"
    exit 0
fi

# Step 2: Run dottie install (capture output for debugging)
echo "  Running dottie install..."
dottie install -c "$TEST_DIR/dottie.yml" --profile default > install_output.txt 2>&1
EXIT_CODE=$?
cat install_output.txt
echo ""

if [ $EXIT_CODE -ne 0 ]; then
    echo "    ✗ dottie install failed with exit code $EXIT_CODE"
    exit 1
fi
echo "    ✓ dottie install completed"

# Step 3: Verify snap was installed
echo "  Checking installed snaps..."
if snap list 2>/dev/null | grep -q "hello-world"; then
    echo "    ✓ Snap package 'hello-world' successfully installed"
    snap list | grep hello-world
else
    echo "    ✗ Snap package 'hello-world' not found"
    snap list
    exit 1
fi

echo ""
echo "  ✓ All snap installation tests passed!"
exit 0
