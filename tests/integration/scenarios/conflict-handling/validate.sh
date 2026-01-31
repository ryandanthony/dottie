#!/bin/bash
# Validate conflict-handling scenario
# Tests that dottie correctly detects conflicts and handles them with --force

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating conflict-handling scenario in $TEST_DIR"

# Initialize git repo (dottie link requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Setup: Create target files that will conflict
BASHRC_TARGET="$HOME/.test-bashrc"
VIMRC_TARGET="$HOME/.test-vimrc"
NVIM_TARGET="$HOME/.config/test-nvim/init.lua"

# Create conflicting files
mkdir -p "$(dirname "$NVIM_TARGET")"
echo "existing bashrc content" > "$BASHRC_TARGET"
echo "existing vimrc content" > "$VIMRC_TARGET"
echo "existing nvim content" > "$NVIM_TARGET"

# Test 1: Conflict detection without --force should fail
echo "  Test 1: Conflict detection (should fail with exit 1)"
cd "$TEST_DIR"
if dottie link -c "$TEST_DIR/dottie.yml"; then
    echo "ERROR: Expected dottie link to fail due to conflicts"
    exit 1
fi
echo "    PASS: dottie link correctly detected conflicts"

# Verify files were NOT modified (safe behavior)
if [ "$(cat "$BASHRC_TARGET")" != "existing bashrc content" ]; then
    echo "ERROR: Target file was modified without --force"
    exit 1
fi
echo "    PASS: Target files were not modified"

# Test 2: --force should create backups and link
echo "  Test 2: Force linking with backup"
cd "$TEST_DIR"
if ! dottie link -c "$TEST_DIR/dottie.yml" --force; then
    echo "ERROR: dottie link --force failed"
    exit 1
fi
echo "    PASS: dottie link --force succeeded"

# Verify backups were created
if ! ls "$BASHRC_TARGET.backup."* 1> /dev/null 2>&1; then
    echo "ERROR: Backup file was not created for bashrc"
    exit 1
fi
echo "    PASS: Backup files created"

# Verify symlinks were created
if [ ! -L "$BASHRC_TARGET" ]; then
    echo "ERROR: Symlink was not created for bashrc"
    exit 1
fi
echo "    PASS: Symlinks created correctly"

# Test 3: Running link again should skip (already linked)
echo "  Test 3: Re-running link (should skip)"
cd "$TEST_DIR"
if ! dottie link -c "$TEST_DIR/dottie.yml"; then
    echo "ERROR: dottie link should succeed when already linked"
    exit 1
fi
echo "    PASS: Re-running link works (skips already linked)"

# Cleanup
rm -f "$BASHRC_TARGET" "$BASHRC_TARGET.backup."*
rm -f "$VIMRC_TARGET" "$VIMRC_TARGET.backup."*
rm -rf "$HOME/.config/test-nvim"

echo "  All conflict-handling tests passed!"
exit 0
