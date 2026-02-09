#!/bin/bash
# Integration test validation script for mixed GitHub release types
# Verifies that both binary and deb type entries install correctly in the same run

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

EXPECTED_BINARY="direnv"
EXPECTED_PACKAGE="gh"
TEST_BIN_DIR="${HOME}/bin"

echo "  Validating mixed GitHub release types in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 0: Clean up any prior state
echo "  Cleaning prior state..."
rm -f "$TEST_BIN_DIR/$EXPECTED_BINARY"
sudo dpkg -r "$EXPECTED_PACKAGE" > /dev/null 2>&1 || true
echo "    ✓ Clean state"

# Step 1: Run dottie install
echo "  Running dottie install with mixed types..."
dottie install -c "$TEST_DIR/dottie.yml" --profile default > install_output.txt 2>&1
EXIT_CODE=$?
cat install_output.txt
echo ""

if [ $EXIT_CODE -ne 0 ]; then
    echo "    ✗ dottie install failed with exit code $EXIT_CODE"
    exit 1
fi
echo "    ✓ dottie install completed"

# Step 2: Verify binary-type entry (direnv)
echo "  Verifying binary-type entry (direnv)..."

if [ ! -f "$TEST_BIN_DIR/$EXPECTED_BINARY" ]; then
    echo "    ✗ Binary $EXPECTED_BINARY not found in $TEST_BIN_DIR"
    echo "    Contents of $TEST_BIN_DIR:"
    ls -la "$TEST_BIN_DIR" 2>/dev/null || echo "      (directory not found)"
    exit 1
fi
echo "    ✓ direnv binary exists in $TEST_BIN_DIR"

if [ ! -x "$TEST_BIN_DIR/$EXPECTED_BINARY" ]; then
    echo "    ✗ direnv binary is not executable"
    exit 1
fi
echo "    ✓ direnv binary is executable"

if ! "$TEST_BIN_DIR/$EXPECTED_BINARY" version > /dev/null 2>&1; then
    echo "    ✗ direnv version failed"
    exit 1
fi
DIRENV_VERSION=$("$TEST_BIN_DIR/$EXPECTED_BINARY" version)
echo "    ✓ direnv executes ($DIRENV_VERSION)"

# Step 3: Verify deb-type entry (gh)
echo "  Verifying deb-type entry (gh)..."

if ! dpkg -s "$EXPECTED_PACKAGE" > /dev/null 2>&1; then
    echo "    ✗ Package $EXPECTED_PACKAGE not installed via dpkg"
    exit 1
fi
echo "    ✓ gh package installed via dpkg"

if ! command -v gh &> /dev/null; then
    echo "    ✗ gh binary not found on PATH"
    exit 1
fi
echo "    ✓ gh binary is on PATH"

if ! gh --version > /dev/null 2>&1; then
    echo "    ✗ gh --version failed"
    exit 1
fi
GH_VERSION=$(gh --version | head -1)
echo "    ✓ gh executes ($GH_VERSION)"

# Step 4: Verify install output mentions both success types
echo "  Checking install output for both entries..."
if ! grep -q "direnv/direnv" install_output.txt; then
    echo "    ✗ Install output does not mention direnv/direnv"
    exit 1
fi
echo "    ✓ Output mentions direnv/direnv"

if ! grep -q "cli/cli" install_output.txt; then
    echo "    ✗ Install output does not mention cli/cli"
    exit 1
fi
echo "    ✓ Output mentions cli/cli"

# Step 5: Verify idempotency for both types
echo "  Testing idempotency (second install)..."
dottie install -c "$TEST_DIR/dottie.yml" --profile default > install_output_2.txt 2>&1
EXIT_CODE=$?
cat install_output_2.txt
echo ""

if [ $EXIT_CODE -ne 0 ]; then
    echo "    ✗ Second install failed with exit code $EXIT_CODE"
    exit 1
fi

# Both should be skipped on second run
if ! grep -qi "skipped\|already installed" install_output_2.txt; then
    echo "    ✗ Expected skip messages on second install"
    exit 1
fi
echo "    ✓ Idempotent install succeeded (both entries skipped)"

# Cleanup
echo "  Cleaning up..."
rm -f "$TEST_BIN_DIR/$EXPECTED_BINARY"
sudo dpkg -r "$EXPECTED_PACKAGE" > /dev/null 2>&1 || true

echo "  ✓ All mixed GitHub release types tests passed!"
exit 0
