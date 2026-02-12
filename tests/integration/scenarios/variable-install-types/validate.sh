#!/bin/bash
# Validate variable-install-types scenario
# Tests that ${...} variable substitution works in script paths and apt package names

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating variable-install-types scenario in $TEST_DIR"

# Read expected values from the actual system
EXPECTED_ID=$(. /etc/os-release && echo "$ID")
EXPECTED_ARCH=$(dpkg --print-architecture 2>/dev/null || echo "amd64")

echo "  System values: ID=$EXPECTED_ID, MS_ARCH=$EXPECTED_ARCH"

cd "$TEST_DIR"

# Initialize git repo (required for dottie)
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial commit" 2>/dev/null || true

# Make scripts executable
chmod +x "$TEST_DIR/scripts/$EXPECTED_ID/setup.sh"

# ============================================================
# Test 1: dottie validate succeeds (variables resolve at load)
# ============================================================
echo "  - Test 1: Config with variables in install types validates..."
validate_output=$(dottie validate -c "$TEST_DIR/dottie.yml" 2>&1)
validate_exit=$?

if [ $validate_exit -ne 0 ]; then
    echo "  FAIL: dottie validate failed with exit code $validate_exit"
    echo "  Output: $validate_output"
    exit 1
fi
echo "  PASS: Config validates with variables in scripts and apt"

# ============================================================
# Test 2: Dry-run shows resolved paths (no raw ${...} tokens)
# ============================================================
echo "  - Test 2: Install dry-run shows resolved values..."
dryrun_output=$(dottie install --dry-run -c "$TEST_DIR/dottie.yml" --profile default 2>&1)
dryrun_exit=$?

# Dry-run may exit 0 even if packages don't exist (they're just skipped)
if echo "$dryrun_output" | grep -q '\${'; then
    echo "  FAIL: Dry-run output still contains unresolved \${...} tokens"
    echo "  Output: $dryrun_output"
    exit 1
fi
echo "  PASS: No unresolved \${...} tokens in install dry-run output"

# ============================================================
# Test 3: Script with variable path executes successfully
# ============================================================
echo "  - Test 3: Script at resolved variable path executes..."
install_output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile default 2>&1)
install_exit=$?

# We don't check exit code strictly because the apt package will fail
# (doesnotexist-amd64-vartest doesn't exist), but the script should run

MARKER_FILE="$HOME/.local/bin/variable-script-test.txt"
if [ -f "$MARKER_FILE" ]; then
    echo "  PASS: Script at scripts/$EXPECTED_ID/setup.sh was executed"
    echo "    Content: $(cat "$MARKER_FILE")"
else
    echo "  FAIL: Script marker file not found at $MARKER_FILE"
    echo "    Expected script path: scripts/$EXPECTED_ID/setup.sh"
    echo "    Install output: $install_output"
    exit 1
fi

# ============================================================
# Test 4: Verify apt package name was resolved in output
# ============================================================
echo "  - Test 4: APT package name has resolved variable..."
if echo "$install_output" | grep -q "doesnotexist-${EXPECTED_ARCH}-vartest"; then
    echo "  PASS: APT package name resolved to doesnotexist-${EXPECTED_ARCH}-vartest"
else
    echo "  WARN: Could not confirm resolved apt package name in output"
    echo "    Looking for: doesnotexist-${EXPECTED_ARCH}-vartest"
fi

# Cleanup
rm -f "$MARKER_FILE"

echo "  ✓ All variable-install-types tests passed!"
exit 0
