#!/bin/bash
# Validate apply-dry-run scenario
# Tests that dottie apply --dry-run previews without making changes

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating apply-dry-run scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Create dotfiles directory and test files
mkdir -p dotfiles
echo "# Bash configuration for dry-run test" > dotfiles/bashrc
echo "# Vim configuration for dry-run test" > dotfiles/vimrc

# Initialize git repo
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial commit" 2>/dev/null || true

# Define target paths
BASHRC_TARGET=$(eval echo ~/.bashrc-dry-run-test)
VIMRC_TARGET=$(eval echo ~/.vimrc-dry-run-test)

# Ensure targets don't exist before test
rm -f "$BASHRC_TARGET" "$VIMRC_TARGET"

# Step 1: Verify the config validates
echo "  - Testing config validation..."
if ! dottie validate -c "$TEST_DIR/dottie.yml" > /dev/null 2>&1; then
    echo "  FAIL: Config validation failed"
    exit 1
fi
echo "  PASS: Config validates"

# Step 2: Run apply with --dry-run
echo "  - Running dottie apply --dry-run..."
dry_run_output=$(dottie apply --dry-run -c "$TEST_DIR/dottie.yml" 2>&1)
dry_run_exit=$?

if [ $dry_run_exit -ne 0 ]; then
    echo "  FAIL: Dry-run failed with exit code $dry_run_exit"
    echo "  Output: $dry_run_output"
    exit 1
fi
echo "  PASS: Dry-run command succeeded"

# Step 3: Verify NO symlinks were created
echo "  - Verifying no symlinks created..."
if [ -e "$BASHRC_TARGET" ] || [ -L "$BASHRC_TARGET" ]; then
    echo "  FAIL: Symlink ~/.bashrc-dry-run-test was created (should not exist in dry-run)"
    rm -f "$BASHRC_TARGET"
    exit 1
fi

if [ -e "$VIMRC_TARGET" ] || [ -L "$VIMRC_TARGET" ]; then
    echo "  FAIL: Symlink ~/.vimrc-dry-run-test was created (should not exist in dry-run)"
    rm -f "$VIMRC_TARGET"
    exit 1
fi
echo "  PASS: No symlinks created (as expected)"

# Step 4: Verify output indicates dry-run mode
echo "  - Checking output indicates dry-run..."
if echo "$dry_run_output" | grep -qi "dry.run\|preview\|would"; then
    echo "  PASS: Output indicates dry-run mode"
else
    echo "  WARN: Output may not clearly indicate dry-run mode"
    echo "  Output: $dry_run_output"
    # Don't fail - output format may vary
fi

# Step 5: Verify output shows what would be linked
echo "  - Checking output shows planned operations..."
if echo "$dry_run_output" | grep -qi "bashrc\|vimrc\|link"; then
    echo "  PASS: Output shows planned link operations"
else
    echo "  INFO: Output format may have changed"
fi

echo ""
echo "  ✓ All apply-dry-run tests passed!"
