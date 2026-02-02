#!/bin/bash
# Validate multi-run-idempotency scenario
# Tests that running dottie commands multiple times is safe and produces consistent results

TEST_DIR="$1"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating multi-run-idempotency scenario in $TEST_DIR"

# Navigate to test directory
cd "$TEST_DIR"

# Create test files
echo "content1" > testfile1.txt
echo "content2" > testfile2.txt

# Initialize git repo
git init -q
git config user.email "test@test.com"
git config user.name "Test User"
git add .
git commit -q -m "initial" 2>/dev/null || true

# Step 1: First run
echo "  - Running dottie link (first time)..."
output1=$(dottie link -c "$TEST_DIR/dottie.yml" 2>&1)
exit_code1=$?

if [ $exit_code1 -ne 0 ]; then
    echo "  FAIL: First run failed with exit code $exit_code1"
    echo "  Output: $output1"
    exit 1
fi
echo "  PASS: First run succeeded"

# Step 2: Verify symlinks were created
expanded_target1=$(eval echo ~/.dottie-test-multi-1)
expanded_target2=$(eval echo ~/.dottie-test-multi-2)

if [ ! -L "$expanded_target1" ] || [ ! -L "$expanded_target2" ]; then
    echo "  FAIL: Symlinks not created on first run"
    ls -la ~ | grep dottie-test || echo "  No symlinks found"
    exit 1
fi
echo "  PASS: Symlinks created on first run"

# Step 3: Record stat info
stat1_before=$(stat "$expanded_target1" 2>/dev/null || echo "")
stat2_before=$(stat "$expanded_target2" 2>/dev/null || echo "")

# Sleep a bit to ensure timestamp would differ if files were modified
sleep 1

# Step 4: Second run
echo "  - Running dottie link (second time)..."
output2=$(dottie link -c "$TEST_DIR/dottie.yml" 2>&1)
exit_code2=$?

if [ $exit_code2 -ne 0 ]; then
    echo "  FAIL: Second run failed with exit code $exit_code2"
    echo "  Output: $output2"
    exit 1
fi
echo "  PASS: Second run succeeded"

# Step 5: Verify symlinks still exist and point to same targets
if [ ! -L "$expanded_target1" ] || [ ! -L "$expanded_target2" ]; then
    echo "  FAIL: Symlinks disappeared after second run"
    exit 1
fi
echo "  PASS: Symlinks still exist after second run"

# Step 6: Verify symlink targets unchanged
target1_after=$(readlink -f "$expanded_target1")
target2_after=$(readlink -f "$expanded_target2")

if [ "$target1_after" != "$(readlink -f "$TEST_DIR/testfile1.txt")" ] || \
   [ "$target2_after" != "$(readlink -f "$TEST_DIR/testfile2.txt")" ]; then
    echo "  FAIL: Symlink targets changed"
    exit 1
fi
echo "  PASS: Symlink targets unchanged"

# Step 7: Third run to ensure stability
echo "  - Running dottie link (third time)..."
output3=$(dottie link -c "$TEST_DIR/dottie.yml" 2>&1)
exit_code3=$?

if [ $exit_code3 -ne 0 ]; then
    echo "  FAIL: Third run failed"
    exit 1
fi
echo "  PASS: Third run succeeded"

# Step 8: Verify no error messages in outputs
for output in "$output1" "$output2" "$output3"; do
    if echo "$output" | grep -qi "error"; then
        echo "  WARN: Error message found in output: $output"
    fi
done

# Cleanup
rm -f "$expanded_target1" "$expanded_target2"

echo ""
echo "  ✓ All multi-run-idempotency tests passed!"
exit 0
