#!/bin/bash
# Integration test validation script for GitHub release installation
# Verifies that the binary was successfully downloaded and installed

# Configuration
TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

# Use the default bin directory (~/bin) - same as dottie installer
TEST_BIN_DIR="${HOME}/bin"
EXPECTED_BINARY="jq"

echo "  Validating GitHub release installation in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 0.5: Test GitHub API connectivity and show response
echo "  Testing GitHub API connectivity..."
API_URL="https://api.github.com/repos/jqlang/jq/releases/latest"
RESPONSE_FILE="/tmp/github-api-response.json"
HTTP_CODE=$(curl -s -o "$RESPONSE_FILE" -w "%{http_code}" "$API_URL" 2>&1)

if [ "$HTTP_CODE" != "200" ]; then
    echo "    ✗ GitHub API returned HTTP $HTTP_CODE"
    head -c 200 "$RESPONSE_FILE"
    echo ""
    rm -f "$RESPONSE_FILE"
    exit 1
fi

echo "    ✓ GitHub API reachable (HTTP $HTTP_CODE)"
echo "    Response preview (first 50 lines):"
head -50 "$RESPONSE_FILE" | sed 's/^/      /'
echo ""

# Check if jq asset exists in the response
echo "    Assets in response:"
grep '"name":' "$RESPONSE_FILE" | sed 's/^/      /'
echo ""

if grep -q 'jq.*linux64' "$RESPONSE_FILE"; then
    echo "    ✓ Found jq-*-linux64 asset in response"
else
    echo "    ✗ No jq-*-linux64 asset found in response"
    echo "    Available assets:"
    grep '"name":' "$RESPONSE_FILE" | head -5 | sed 's/^/      /'
fi
echo ""

rm -f "$RESPONSE_FILE"

# Step 1: Run dottie install (capture output for debugging)
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
VERSION=$("$TEST_BIN_DIR/$EXPECTED_BINARY" --version)
echo "    ✓ Binary executes successfully (version: $VERSION)"

# Cleanup - only remove the binary, not the bin directory
rm -f "$TEST_BIN_DIR/$EXPECTED_BINARY"

echo "  ✓ All GitHub release installation tests passed!"
exit 0
