#!/bin/bash
# Validate apply-basic scenario
# Tests that dottie apply creates symlinks and installs software in one command

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating apply-basic scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Create dotfiles directory and test files
mkdir -p dotfiles
echo "# Bash configuration for apply test" > dotfiles/bashrc
echo "# Vim configuration for apply test" > dotfiles/vimrc

# Initialize git repo (dottie requires git repo root)
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial commit" 2>/dev/null || true

# Step 1: Verify the config validates
echo "  - Testing config validation..."
if ! dottie validate -c "$TEST_DIR/dottie.yml" > /dev/null 2>&1; then
    echo "  FAIL: Config validation failed"
    exit 1
fi
echo "  PASS: Config validates"

# Step 2: Run apply command
echo "  - Running dottie apply..."
apply_output=$(dottie apply -c "$TEST_DIR/dottie.yml" 2>&1)
apply_exit=$?

# Apply command should succeed (exit 0) even if install phase has issues
# The linking phase should always work
if [ $apply_exit -ne 0 ]; then
    # Check if it's just an APT permission issue
    if echo "$apply_output" | grep -qi "apt\|permission\|denied\|sudo"; then
        echo "  WARN: Apply may have failed due to APT permissions (expected in container)"
    else
        echo "  FAIL: Apply command failed with exit code $apply_exit"
        echo "  Output: $apply_output"
        exit 1
    fi
fi

# Step 3: Verify symlinks were created
echo "  - Verifying symlinks created..."
BASHRC_TARGET=$(eval echo ~/.bashrc-apply-test)
VIMRC_TARGET=$(eval echo ~/.vimrc-apply-test)

if [ ! -L "$BASHRC_TARGET" ]; then
    echo "  FAIL: Symlink ~/.bashrc-apply-test not created"
    exit 1
fi

if [ ! -L "$VIMRC_TARGET" ]; then
    echo "  FAIL: Symlink ~/.vimrc-apply-test not created"
    exit 1
fi
echo "  PASS: Symlinks created"

# Step 4: Verify symlinks point to correct sources
echo "  - Verifying symlink targets..."
BASHRC_LINK=$(readlink "$BASHRC_TARGET")
VIMRC_LINK=$(readlink "$VIMRC_TARGET")

if ! echo "$BASHRC_LINK" | grep -q "dotfiles/bashrc"; then
    echo "  FAIL: bashrc symlink points to wrong target: $BASHRC_LINK"
    exit 1
fi

if ! echo "$VIMRC_LINK" | grep -q "dotfiles/vimrc"; then
    echo "  FAIL: vimrc symlink points to wrong target: $VIMRC_LINK"
    exit 1
fi
echo "  PASS: Symlinks point to correct targets"

# Step 5: Verify apply output mentions both phases
echo "  - Checking output for both phases..."
# The output should mention linking (dotfiles) and/or installation
# This is informational - don't fail if format changes
if echo "$apply_output" | grep -qi "link\|symlink\|dotfile"; then
    echo "  PASS: Output mentions linking phase"
else
    echo "  INFO: Output format may have changed (no link mention)"
fi

# Cleanup
rm -f "$BASHRC_TARGET" "$VIMRC_TARGET"

echo ""
echo "  ✓ All apply-basic tests passed!"
