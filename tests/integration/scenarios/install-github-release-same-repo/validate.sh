#!/bin/bash
# Validate install-github-release-same-repo scenario
# Tests that two GitHub release entries from the same repo (ahmetb/kubectx)
# with different binaries (kubectx, kubens) both survive profile inheritance
# and are installed correctly.

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

TEST_BIN_DIR="${HOME}/bin"

echo "  Validating install-github-release-same-repo scenario in $TEST_DIR"

cd "$TEST_DIR"

# Initialize git repo (required for dottie)
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial commit" 2>/dev/null || true

# ============================================================
# Test 1: Configuration validates successfully
# ============================================================
echo "  - Test 1: Config with duplicate repos validates..."
validate_output=$(dottie validate -c "$TEST_DIR/dottie.yml" 2>&1)
validate_exit=$?

if [ $validate_exit -ne 0 ]; then
    echo "  FAIL: dottie validate failed with exit code $validate_exit"
    echo "  Output: $validate_output"
    exit 1
fi
echo "  PASS: Configuration validates"

# ============================================================
# Test 2: Dry-run shows BOTH binaries from same repo
# ============================================================
echo "  - Test 2: Dry-run on inherited profile shows both binaries..."
dryrun_output=$(dottie install --dry-run -c "$TEST_DIR/dottie.yml" --profile work 2>&1)

has_kubectx=false
has_kubens=false

if echo "$dryrun_output" | grep -qi "kubectx"; then
    has_kubectx=true
fi
if echo "$dryrun_output" | grep -qi "kubens"; then
    has_kubens=true
fi

if [ "$has_kubectx" = true ] && [ "$has_kubens" = true ]; then
    echo "  PASS: Both kubectx and kubens appear in dry-run output"
else
    echo "  FAIL: Expected both kubectx and kubens in dry-run output"
    echo "    kubectx present: $has_kubectx"
    echo "    kubens present: $has_kubens"
    echo "  Dry-run output:"
    echo "$dryrun_output" | sed 's/^/    /'
    exit 1
fi

# ============================================================
# Test 3: Actual install downloads both binaries
# ============================================================
echo "  - Test 3: Installing both binaries from same repo..."
install_output=$(dottie install -c "$TEST_DIR/dottie.yml" --profile work 2>&1)
install_exit=$?

echo "  Install output:"
echo "$install_output" | sed 's/^/    /'
echo ""

# Check kubectx binary
if [ -f "$TEST_BIN_DIR/kubectx" ]; then
    echo "  PASS: kubectx binary exists at $TEST_BIN_DIR/kubectx"
else
    echo "  FAIL: kubectx binary not found at $TEST_BIN_DIR/kubectx"
    echo "  Contents of $TEST_BIN_DIR:"
    ls -la "$TEST_BIN_DIR" 2>/dev/null || echo "    (directory not found)"
    exit 1
fi

# Check kubens binary
if [ -f "$TEST_BIN_DIR/kubens" ]; then
    echo "  PASS: kubens binary exists at $TEST_BIN_DIR/kubens"
else
    echo "  FAIL: kubens binary not found at $TEST_BIN_DIR/kubens"
    echo "  Contents of $TEST_BIN_DIR:"
    ls -la "$TEST_BIN_DIR" 2>/dev/null || echo "    (directory not found)"
    exit 1
fi

# ============================================================
# Test 4: Both binaries are executable
# ============================================================
echo "  - Test 4: Verifying binaries are executable..."

if [ -x "$TEST_BIN_DIR/kubectx" ]; then
    echo "  PASS: kubectx is executable"
else
    echo "  FAIL: kubectx is not executable"
    exit 1
fi

if [ -x "$TEST_BIN_DIR/kubens" ]; then
    echo "  PASS: kubens is executable"
else
    echo "  FAIL: kubens is not executable"
    exit 1
fi

# Cleanup
rm -f "$TEST_BIN_DIR/kubectx" "$TEST_BIN_DIR/kubens"

echo "  ✓ All install-github-release-same-repo tests passed!"
exit 0
