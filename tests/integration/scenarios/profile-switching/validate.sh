#!/bin/bash
# Validate profile-switching scenario
# Tests that switching between profiles works correctly and maintains state

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating profile-switching scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Create test files
echo "default bash config" > bashrc-default
echo "default profile config" > profile-default
echo "work bash config" > bashrc-work
echo "work vim config" > vimrc-work
echo "minimal bash config" > bashrc-minimal

# Initialize git repo
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial" 2>/dev/null || true

echo "  - Testing default profile..."
if ! dottie link -c "$TEST_DIR/dottie.yml" --profile default > /dev/null 2>&1; then
    echo "  FAIL: Default profile link failed"
    exit 1
fi

# Verify default profile symlinks
if [ ! -L "$(eval echo ~/.bashrc-default)" ] || [ ! -L "$(eval echo ~/.profile-default)" ]; then
    echo "  FAIL: Default profile symlinks not created"
    exit 1
fi
echo "  PASS: Default profile applied correctly"

echo "  - Testing work profile..."
if ! dottie link -c "$TEST_DIR/dottie.yml" --profile work > /dev/null 2>&1; then
    echo "  FAIL: Work profile link failed"
    exit 1
fi

# Verify work profile symlinks
if [ ! -L "$(eval echo ~/.bashrc-work)" ] || [ ! -L "$(eval echo ~/.vimrc-work)" ]; then
    echo "  FAIL: Work profile symlinks not created"
    exit 1
fi
echo "  PASS: Work profile applied correctly"

echo "  - Testing minimal profile..."
if ! dottie link -c "$TEST_DIR/dottie.yml" --profile minimal > /dev/null 2>&1; then
    echo "  FAIL: Minimal profile link failed"
    exit 1
fi

# Verify minimal profile symlinks
if [ ! -L "$(eval echo ~/.bashrc-minimal)" ]; then
    echo "  FAIL: Minimal profile symlinks not created"
    exit 1
fi
echo "  PASS: Minimal profile applied correctly"

# Verify all profiles' symlinks still exist
echo "  - Verifying all profile symlinks still exist..."
if [ ! -L "$(eval echo ~/.bashrc-default)" ] || \
   [ ! -L "$(eval echo ~/.profile-default)" ] || \
   [ ! -L "$(eval echo ~/.bashrc-work)" ] || \
   [ ! -L "$(eval echo ~/.vimrc-work)" ] || \
   [ ! -L "$(eval echo ~/.bashrc-minimal)" ]; then
    echo "  FAIL: Some symlinks disappeared"
    exit 1
fi
echo "  PASS: All symlinks persisted correctly"

# Cleanup
rm -f ~/.bashrc-default ~/.profile-default ~/.bashrc-work ~/.vimrc-work ~/.bashrc-minimal

echo ""
echo "  ✓ All profile-switching tests passed!"
exit 0
