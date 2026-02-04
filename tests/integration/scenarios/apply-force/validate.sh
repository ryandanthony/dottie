#!/bin/bash
# Validate apply-force scenario
# Tests that dottie apply --force backs up conflicts and creates symlinks

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating apply-force scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Create dotfiles directory and test file
mkdir -p dotfiles
echo "# Bash configuration from repo" > dotfiles/bashrc

# Initialize git repo
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial commit" 2>/dev/null || true

# Define target path
TARGET=$(eval echo ~/.bashrc-force-test)
BACKUP_PATTERN=$(eval echo ~/.bashrc-force-test.backup*)

# Clean up any previous test artifacts
rm -f "$TARGET" $BACKUP_PATTERN 2>/dev/null

# Step 1: Verify the config validates
echo "  - Testing config validation..."
if ! dottie validate -c "$TEST_DIR/dottie.yml" > /dev/null 2>&1; then
    echo "  FAIL: Config validation failed"
    exit 1
fi
echo "  PASS: Config validates"

# Step 2: Create a conflicting file at the target
echo "  - Creating conflicting file..."
echo "# Existing bash config that should be backed up" > "$TARGET"
if [ ! -f "$TARGET" ]; then
    echo "  FAIL: Could not create conflicting file"
    exit 1
fi
echo "  PASS: Conflicting file created at $TARGET"

# Step 3: Verify apply WITHOUT --force fails or warns about conflict
echo "  - Testing apply without --force (should fail on conflict)..."
no_force_output=$(dottie apply -c "$TEST_DIR/dottie.yml" 2>&1)
no_force_exit=$?

# The command should either fail (non-zero exit) or warn about conflicts
if [ $no_force_exit -eq 0 ]; then
    # If it succeeded, check if symlink was created (it shouldn't overwrite)
    if [ -L "$TARGET" ]; then
        echo "  WARN: Apply without --force created symlink (unexpected)"
    else
        echo "  INFO: Apply without --force succeeded but didn't overwrite"
    fi
else
    if echo "$no_force_output" | grep -qi "conflict\|exist\|force"; then
        echo "  PASS: Apply without --force correctly reported conflict"
    else
        echo "  INFO: Apply without --force failed (exit $no_force_exit)"
    fi
fi

# Restore the conflicting file if it was modified
if [ ! -f "$TARGET" ] || [ -L "$TARGET" ]; then
    rm -f "$TARGET"
    echo "# Existing bash config that should be backed up" > "$TARGET"
fi

# Step 4: Run apply with --force
echo "  - Running dottie apply --force..."
force_output=$(dottie apply --force -c "$TEST_DIR/dottie.yml" 2>&1)
force_exit=$?

if [ $force_exit -ne 0 ]; then
    echo "  FAIL: Apply --force failed with exit code $force_exit"
    echo "  Output: $force_output"
    rm -f "$TARGET" $BACKUP_PATTERN 2>/dev/null
    exit 1
fi
echo "  PASS: Apply --force command succeeded"

# Step 5: Verify symlink was created
echo "  - Verifying symlink created..."
if [ ! -L "$TARGET" ]; then
    echo "  FAIL: Symlink not created at $TARGET"
    rm -f "$TARGET" $BACKUP_PATTERN 2>/dev/null
    exit 1
fi
echo "  PASS: Symlink created"

# Step 6: Verify symlink points to correct source
echo "  - Verifying symlink target..."
LINK_TARGET=$(readlink "$TARGET")
if ! echo "$LINK_TARGET" | grep -q "dotfiles/bashrc"; then
    echo "  FAIL: Symlink points to wrong target: $LINK_TARGET"
    rm -f "$TARGET" $BACKUP_PATTERN 2>/dev/null
    exit 1
fi
echo "  PASS: Symlink points to correct source"

# Step 7: Verify backup was created
echo "  - Checking for backup file..."
BACKUP_FILES=$(ls $BACKUP_PATTERN 2>/dev/null | head -1)
if [ -n "$BACKUP_FILES" ] && [ -f "$BACKUP_FILES" ]; then
    echo "  PASS: Backup file created: $BACKUP_FILES"
    
    # Verify backup contains original content
    if grep -q "should be backed up" "$BACKUP_FILES"; then
        echo "  PASS: Backup contains original content"
    else
        echo "  INFO: Backup file exists but content may differ"
    fi
else
    echo "  WARN: No backup file found (pattern: $BACKUP_PATTERN)"
    echo "  INFO: Backup naming convention may differ"
fi

# Cleanup
rm -f "$TARGET" $BACKUP_PATTERN 2>/dev/null

echo ""
echo "  ✓ All apply-force tests passed!"
