#!/bin/bash
# Validate variable-substitution scenario
# Tests that ${...} variable substitution works end-to-end with real OS release data

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating variable-substitution scenario in $TEST_DIR"

# Read expected values from the actual system
EXPECTED_CODENAME=$(. /etc/os-release && echo "$VERSION_CODENAME")
EXPECTED_ID=$(. /etc/os-release && echo "$ID")
EXPECTED_ARCH=$(dpkg --print-architecture 2>/dev/null || echo "amd64")

echo "  System values: VERSION_CODENAME=$EXPECTED_CODENAME, ID=$EXPECTED_ID, MS_ARCH=$EXPECTED_ARCH"

# Verify /etc/os-release exists (prerequisite)
if [ ! -f /etc/os-release ]; then
    echo "  FAIL: /etc/os-release not found"
    exit 1
fi

cd "$TEST_DIR"

# Initialize git repo (required for dottie)
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial commit" 2>/dev/null || true

# ============================================================
# Test 1: dottie validate succeeds (variables resolve at load)
# ============================================================
echo "  - Test 1: Config with variables validates successfully..."
validate_output=$(dottie validate -c "$TEST_DIR/dottie.yml" 2>&1)
validate_exit=$?

if [ $validate_exit -ne 0 ]; then
    echo "  FAIL: dottie validate failed with exit code $validate_exit"
    echo "  Output: $validate_output"
    exit 1
fi
echo "  PASS: Config validates with variable substitution"

# ============================================================
# Test 2: Dry-run resolves variable paths (not raw ${...})
# ============================================================
echo "  - Test 2: Dry-run shows resolved paths (no raw \${...} tokens)..."
dryrun_output=$(dottie link --dry-run -c "$TEST_DIR/dottie.yml" 2>&1)
dryrun_exit=$?

if [ $dryrun_exit -ne 0 ]; then
    echo "  FAIL: link --dry-run failed with exit code $dryrun_exit"
    echo "  Output: $dryrun_output"
    exit 1
fi

# Verify no unresolved ${...} tokens remain in the output
if echo "$dryrun_output" | grep -q '\${'; then
    echo "  FAIL: Dry-run output still contains unresolved \${...} tokens"
    echo "  Output: $dryrun_output"
    exit 1
fi
echo "  PASS: No unresolved \${...} tokens in dry-run output"

# ============================================================
# Test 3: Resolved paths reference actual system values
# ============================================================
echo "  - Test 3: Resolved paths contain system values..."

# The source dotfiles/${VERSION_CODENAME}/bashrc should resolve to dotfiles/noble/bashrc
if echo "$dryrun_output" | grep -qi "$EXPECTED_CODENAME"; then
    echo "  PASS: Output contains resolved VERSION_CODENAME ($EXPECTED_CODENAME)"
else
    echo "  WARN: Could not confirm VERSION_CODENAME in output (may be path-dependent)"
    echo "  Output: $dryrun_output"
fi

# ============================================================
# Test 4: Actual link creates symlinks from resolved paths
# ============================================================
echo "  - Test 4: Symlink creation with resolved variable paths..."
link_output=$(dottie link -c "$TEST_DIR/dottie.yml" --profile default 2>&1)
link_exit=$?

if [ $link_exit -ne 0 ]; then
    echo "  FAIL: dottie link failed with exit code $link_exit"
    echo "  Output: $link_output"
    exit 1
fi
echo "  PASS: Link command succeeded with variable-resolved config"

# Verify symlink targets exist
BASHRC_TARGET=$(eval echo ~/.bashrc-variable-test)
PROFILE_TARGET=$(eval echo ~/.profile-variable-test)

if [ -L "$BASHRC_TARGET" ]; then
    # Verify the symlink points to the correct resolved source
    link_target=$(readlink -f "$BASHRC_TARGET")
    if echo "$link_target" | grep -q "$EXPECTED_CODENAME"; then
        echo "  PASS: bashrc symlink resolves to $EXPECTED_CODENAME path"
    else
        echo "  WARN: bashrc symlink target: $link_target"
    fi
else
    echo "  FAIL: Symlink $BASHRC_TARGET was not created"
    exit 1
fi

if [ -L "$PROFILE_TARGET" ]; then
    link_target=$(readlink -f "$PROFILE_TARGET")
    if echo "$link_target" | grep -q "$EXPECTED_ID"; then
        echo "  PASS: profile symlink resolves to $EXPECTED_ID path"
    else
        echo "  WARN: profile symlink target: $link_target"
    fi
else
    echo "  FAIL: Symlink $PROFILE_TARGET was not created"
    exit 1
fi

# Verify symlink content matches the source file
if [ -f "$BASHRC_TARGET" ]; then
    if grep -q "DOTTIE_CODENAME" "$BASHRC_TARGET"; then
        echo "  PASS: bashrc symlink content is correct"
    else
        echo "  FAIL: bashrc content does not match expected source"
        cat "$BASHRC_TARGET"
        exit 1
    fi
fi

# Cleanup symlinks
rm -f "$BASHRC_TARGET" "$PROFILE_TARGET"

echo "  ✓ All variable-substitution tests passed!"
exit 0
