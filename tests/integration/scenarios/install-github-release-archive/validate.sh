#!/bin/bash
# Integration test validation script for GitHub release archive extraction
# Verifies that a binary from a compressed tar.gz asset is extracted and installed

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

EXPECTED_BINARY="fzf"
EXPECTED_VERSION="0.57.0"
TEST_BIN_DIR="${HOME}/bin"

echo "  Validating GitHub release archive extraction in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 0: Clean up any prior state
echo "  Cleaning prior state..."
rm -f "$TEST_BIN_DIR/$EXPECTED_BINARY"
echo "    ✓ Clean state"

# Step 1: Run dottie install
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

# Step 2: Verify bin directory exists
echo "  Checking if bin directory exists..."
if [ ! -d "$TEST_BIN_DIR" ]; then
    echo "    ✗ Bin directory does not exist at $TEST_BIN_DIR"
    exit 1
fi
echo "    ✓ Bin directory exists"

# Step 3: Verify binary exists
echo "  Checking if $EXPECTED_BINARY binary exists..."
if [ ! -f "$TEST_BIN_DIR/$EXPECTED_BINARY" ]; then
    echo "    ✗ Binary does not exist at $TEST_BIN_DIR/$EXPECTED_BINARY"
    echo "    Contents of $TEST_BIN_DIR:"
    ls -la "$TEST_BIN_DIR" || echo "    (directory is empty or not accessible)"
    exit 1
fi
echo "    ✓ Binary exists"

# Step 4: Verify binary is executable
echo "  Checking if binary is executable..."
if [ ! -x "$TEST_BIN_DIR/$EXPECTED_BINARY" ]; then
    echo "    ✗ Binary is not executable"
    exit 1
fi
echo "    ✓ Binary is executable"

# Step 5: Verify binary works by running --version
echo "  Testing binary execution..."
if ! "$TEST_BIN_DIR/$EXPECTED_BINARY" --version > /dev/null 2>&1; then
    echo "    ✗ Binary failed to execute"
    exit 1
fi
VERSION=$("$TEST_BIN_DIR/$EXPECTED_BINARY" --version | head -1)
echo "    ✓ Binary executes successfully (version: $VERSION)"

# Step 6: Verify the version contains the expected version number
echo "  Checking version number..."
if ! echo "$VERSION" | grep -q "$EXPECTED_VERSION"; then
    echo "    ✗ Version mismatch: expected to contain $EXPECTED_VERSION, got: $VERSION"
    exit 1
fi
echo "    ✓ Version matches ($EXPECTED_VERSION)"

# Step 7: Verify idempotency
echo "  Testing idempotency (second install)..."
dottie install -c "$TEST_DIR/dottie.yml" --profile default > install_output_2.txt 2>&1
EXIT_CODE=$?
cat install_output_2.txt
echo ""

if [ $EXIT_CODE -ne 0 ]; then
    echo "    ✗ Second install failed with exit code $EXIT_CODE"
    exit 1
fi

if grep -qi "skipped\|already installed" install_output_2.txt; then
    echo "    ✓ Idempotent install succeeded (binary skipped)"
else
    echo "    ✗ Expected skip message on second install"
    exit 1
fi

# Cleanup
rm -f "$TEST_BIN_DIR/$EXPECTED_BINARY"

echo "  ✓ All GitHub release archive extraction tests passed!"
exit 0
