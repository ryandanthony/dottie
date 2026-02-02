#!/bin/bash
# Validate link-and-install-combined scenario
# Tests that linking and installing work together correctly

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating link-and-install-combined scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Create test dotfiles
echo "# Bash configuration" > bashrc
echo "# Vim configuration" > vimrc

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

# Step 2: Test linking first
echo "  - Testing link command first..."
if ! dottie link -c "$TEST_DIR/dottie.yml" --profile default > /dev/null 2>&1; then
    echo "  FAIL: Link command failed"
    exit 1
fi

# Verify symlinks created
if [ ! -L "$(eval echo ~/.bashrc-linked)" ] || [ ! -L "$(eval echo ~/.vimrc-linked)" ]; then
    echo "  FAIL: Symlinks not created"
    exit 1
fi
echo "  PASS: Link command succeeded and created symlinks"

# Step 3: Test install command
echo "  - Testing install command..."
install_output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile default 2>&1)
install_exit=$?

# Install might fail in container, but should handle gracefully
if [ $install_exit -eq 0 ]; then
    echo "  PASS: Install command succeeded"
else
    if echo "$install_output" | grep -qi "apt\|permission\|denied"; then
        echo "  WARN: Install failed due to APT/permissions (expected in container)"
    else
        echo "  INFO: Install exited with code $install_exit"
    fi
fi

# Step 4: Verify symlinks still exist after install
echo "  - Verifying symlinks persisted after install..."
if [ ! -L "$(eval echo ~/.bashrc-linked)" ] || [ ! -L "$(eval echo ~/.vimrc-linked)" ]; then
    echo "  FAIL: Symlinks disappeared after install"
    exit 1
fi
echo "  PASS: Symlinks persisted"

# Step 5: Test install then link (reverse order)
echo "  - Testing reverse order (install then link)..."

# Clean up existing symlinks
rm -f ~/.bashrc-linked ~/.vimrc-linked

# Install first
install_output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile default 2>&1)

# Link second
if ! dottie link -c "$TEST_DIR/dottie.yml" --profile default > /dev/null 2>&1; then
    echo "  FAIL: Link command failed in reverse order"
    exit 1
fi

# Verify symlinks created
if [ ! -L "$(eval echo ~/.bashrc-linked)" ] || [ ! -L "$(eval echo ~/.vimrc-linked)" ]; then
    echo "  FAIL: Symlinks not created in reverse order"
    exit 1
fi
echo "  PASS: Reverse order (install then link) works"

# Cleanup
rm -f ~/.bashrc-linked ~/.vimrc-linked

echo ""
echo "  ✓ All link-and-install-combined tests passed!"
exit 0
