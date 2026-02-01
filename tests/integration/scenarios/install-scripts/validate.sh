#!/bin/bash
# Integration test validation script for shell script installation
# Verifies that the installation script was executed successfully

# Configuration
TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

EXPECTED_FILE="$HOME/.local/bin/script-test.txt"

echo "  Validating shell script installation in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 1: Ensure test script is executable
chmod +x "$TEST_DIR/scripts/test-script.sh"
echo "  ✓ Test script made executable"

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

# Step 3: Verify script was executed (check for marker file)
echo "  Checking if script was executed..."
if [ -f "$EXPECTED_FILE" ]; then
    echo "    ✓ Script execution marker found at $EXPECTED_FILE"
    echo "    Content: $(cat $EXPECTED_FILE)"
else
    echo "    ✗ Script execution marker not found at $EXPECTED_FILE"
    echo "    Checking home directory contents:"
    ls -la "$HOME/.local/bin/" 2>/dev/null || echo "    (.local/bin directory does not exist)"
    exit 1
fi

echo ""
echo "  ✓ All shell script installation tests passed!"
exit 0
