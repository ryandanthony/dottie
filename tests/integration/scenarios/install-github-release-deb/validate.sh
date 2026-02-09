#!/bin/bash
# Integration test validation script for GitHub release deb package installation
# Verifies that the .deb package was successfully downloaded and installed via dpkg

# Configuration
TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

EXPECTED_PACKAGE="gh"
EXPECTED_VERSION="2.86.0"

echo "  Validating GitHub release deb installation in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 0: Verify prerequisites
echo "  Checking prerequisites..."
if ! command -v dpkg &> /dev/null; then
    echo "    ✗ dpkg not available"
    exit 1
fi
echo "    ✓ dpkg available"

if ! command -v sudo &> /dev/null; then
    echo "    ✗ sudo not available"
    exit 1
fi
echo "    ✓ sudo available"

# Step 0.5: Test GitHub API connectivity
echo "  Testing GitHub API connectivity..."
API_URL="https://api.github.com/repos/cli/cli/releases/tags/v${EXPECTED_VERSION}"
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

# Check if deb asset exists in the response
if grep -q "gh_${EXPECTED_VERSION}_linux_amd64.deb" "$RESPONSE_FILE"; then
    echo "    ✓ Found deb asset in release"
else
    echo "    ✗ Deb asset not found in release"
    echo "    Available assets:"
    grep '"name":' "$RESPONSE_FILE" | head -10 | sed 's/^/      /'
    rm -f "$RESPONSE_FILE"
    exit 1
fi
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

# Step 2: Verify package is installed via dpkg
echo "  Checking if $EXPECTED_PACKAGE is installed via dpkg..."
if ! dpkg -s "$EXPECTED_PACKAGE" > /dev/null 2>&1; then
    echo "    ✗ Package $EXPECTED_PACKAGE is not installed"
    echo "    dpkg -s output:"
    dpkg -s "$EXPECTED_PACKAGE" 2>&1 | sed 's/^/      /'
    exit 1
fi
echo "    ✓ Package is installed"

# Step 3: Verify installed version
echo "  Checking installed version..."
INSTALLED_VERSION=$(dpkg -s "$EXPECTED_PACKAGE" 2>/dev/null | grep '^Version:' | awk '{print $2}')
if [ -z "$INSTALLED_VERSION" ]; then
    echo "    ✗ Could not determine installed version"
    exit 1
fi
echo "    ✓ Installed version: $INSTALLED_VERSION"

# Step 4: Verify binary is on PATH and executable
echo "  Checking if gh binary is available..."
if ! command -v gh &> /dev/null; then
    echo "    ✗ gh binary not found on PATH"
    exit 1
fi
echo "    ✓ gh binary is on PATH"

# Step 5: Verify binary works by running --version
echo "  Testing binary execution..."
if ! gh --version > /dev/null 2>&1; then
    echo "    ✗ gh --version failed"
    exit 1
fi
VERSION=$(gh --version | head -1)
echo "    ✓ Binary executes successfully ($VERSION)"

# Step 6: Verify idempotency - run install again, should succeed
echo "  Testing idempotency (second install)..."
dottie install -c "$TEST_DIR/dottie.yml" --profile default > install_output_2.txt 2>&1
EXIT_CODE=$?
cat install_output_2.txt
echo ""

if [ $EXIT_CODE -ne 0 ]; then
    echo "    ✗ Second install failed with exit code $EXIT_CODE"
    exit 1
fi
echo "    ✓ Idempotent install succeeded"

# Cleanup - remove the installed package
echo "  Cleaning up..."
sudo dpkg -r "$EXPECTED_PACKAGE" > /dev/null 2>&1 || true

echo "  ✓ All GitHub release deb installation tests passed!"
exit 0
