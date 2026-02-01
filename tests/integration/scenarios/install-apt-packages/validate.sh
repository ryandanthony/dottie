#!/bin/bash
# Integration test validation script for APT package installation
# Verifies that packages were successfully installed via apt-get

# Configuration
TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

EXPECTED_PACKAGES=("jq" "tree")

echo "  Validating APT package installation in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

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

# Step 2: Check if packages are installed
echo "  Checking if packages are installed..."
ALL_INSTALLED=true

for package in "${EXPECTED_PACKAGES[@]}"; do
    if dpkg -l 2>/dev/null | grep -q "^ii.*$package"; then
        echo "    ✓ Package $package is installed"
    else
        echo "    ✗ Package $package is NOT installed"
        ALL_INSTALLED=false
    fi
done

if [ "$ALL_INSTALLED" = "true" ]; then
    echo "  ✓ All APT package installation tests passed!"
    exit 0
else
    echo "  ✗ Some packages are missing"
    exit 1
fi

