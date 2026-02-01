#!/bin/bash
# Integration test validation script for GitHub release installation
# Verifies that the binary was successfully downloaded and installed

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEST_BIN_DIR="${HOME}/.dottie-test/bin"
EXPECTED_BINARY="jq"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "================================================"
echo "GitHub Release Installation Validation Test"
echo "================================================"
echo ""

# Test 1: Verify bin directory exists
echo "Test 1: Checking if bin directory exists..."
if [ -d "$TEST_BIN_DIR" ]; then
    echo -e "${GREEN}✓ Bin directory exists at $TEST_BIN_DIR${NC}"
else
    echo -e "${RED}✗ Bin directory does not exist at $TEST_BIN_DIR${NC}"
    exit 1
fi
echo ""

# Test 2: Verify binary exists
echo "Test 2: Checking if $EXPECTED_BINARY binary exists..."
if [ -f "$TEST_BIN_DIR/$EXPECTED_BINARY" ]; then
    echo -e "${GREEN}✓ Binary exists at $TEST_BIN_DIR/$EXPECTED_BINARY${NC}"
else
    echo -e "${RED}✗ Binary does not exist at $TEST_BIN_DIR/$EXPECTED_BINARY${NC}"
    echo "   Contents of $TEST_BIN_DIR:"
    ls -la "$TEST_BIN_DIR" || echo "   (directory is empty or not accessible)"
    exit 1
fi
echo ""

# Test 3: Verify binary is executable
echo "Test 3: Checking if binary is executable..."
if [ -x "$TEST_BIN_DIR/$EXPECTED_BINARY" ]; then
    echo -e "${GREEN}✓ Binary is executable${NC}"
else
    echo -e "${RED}✗ Binary is not executable${NC}"
    ls -la "$TEST_BIN_DIR/$EXPECTED_BINARY"
    exit 1
fi
echo ""

# Test 4: Verify binary works by running --version
echo "Test 4: Testing binary execution..."
if "$TEST_BIN_DIR/$EXPECTED_BINARY" --version > /dev/null 2>&1; then
    VERSION=$("$TEST_BIN_DIR/$EXPECTED_BINARY" --version)
    echo -e "${GREEN}✓ Binary executes successfully${NC}"
    echo "   Version: $VERSION"
else
    echo -e "${RED}✗ Binary failed to execute${NC}"
    exit 1
fi
echo ""

echo "================================================"
echo -e "${GREEN}All tests passed!${NC}"
echo "================================================"
exit 0
