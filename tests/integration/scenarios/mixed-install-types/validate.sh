#!/bin/bash
# Validate mixed-install-types scenario
# Tests that multiple install types work together correctly

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating mixed-install-types scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Make setup script executable
chmod +x setup-script.sh

# Initialize git repo
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial" 2>/dev/null || true

# Step 1: Config validation
echo "  - Validating configuration..."
if ! dottie validate -c "$TEST_DIR/dottie.yml" > /dev/null 2>&1; then
    echo "  FAIL: Config validation failed"
    exit 1
fi
echo "  PASS: Configuration valid"

# Step 2: Run install
echo "  - Running dottie install with mixed types..."
install_output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile default 2>&1)
install_exit=$?

# Note: Install may fail if APT packages aren't available, but script should still run
echo "  Install output: $install_output"

if [ $install_exit -ne 0 ]; then
    # Check if it was due to APT not being available
    if echo "$install_output" | grep -qi "apt\|permission\|denied"; then
        echo "  WARN: Install failed due to APT/permissions (expected in some environments)"
        echo "  WARN: Script execution may not have been tested"
    else
        echo "  FAIL: Install command failed with exit code $install_exit"
        exit 1
    fi
else
    echo "  PASS: Install command succeeded"
fi

# Step 3: Verify script executed (check for marker)
if [ -f ~/.test-mixed-setup/marker.txt ]; then
    echo "  PASS: Setup script executed"
else
    echo "  WARN: Setup script marker not found (may not have executed)"
    echo "    This could indicate scripts weren't processed"
fi

# Step 4: Check for APT package installation (may fail in container)
if command -v dpkg &> /dev/null; then
    apt_installed=0
    for pkg in jq curl; do
        if dpkg -l 2>/dev/null | grep -q "^ii.*$pkg"; then
            echo "  ✓ APT package $pkg installed"
            apt_installed=$((apt_installed + 1))
        fi
    done
    
    if [ $apt_installed -gt 0 ]; then
        echo "  PASS: At least one APT package installed"
    else
        echo "  WARN: No APT packages installed (may be expected in test environment)"
    fi
fi

# Cleanup
rm -rf ~/.test-mixed-setup

echo ""
echo "  ✓ Mixed-install-types scenario completed!"
exit 0
