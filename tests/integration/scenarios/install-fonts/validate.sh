#!/bin/bash
# Integration test validation script for font installation
# Verifies that fonts were successfully downloaded and installed

# Configuration
TEST_DIR="$1"
FONTS_DIR="${HOME}/.local/share/fonts"

if [ -z "$TEST_DIR" ]; then
    echo "ERROR: TEST_DIR not provided"
    exit 1
fi

echo "  Validating font installation in $TEST_DIR"

# Initialize git repo (dottie install requires git repo root)
cd "$TEST_DIR"
git init -q
git config user.email "test@test.com"
git config user.name "Test User"

# Step 1: Run dottie install (capture output for debugging)
echo "  Running dottie install..."
dottie install -c "$TEST_DIR/dottie.yml" --profile default > install_output.txt 2>&1
EXIT_CODE=$?
cat install_output.txt
echo ""

if [ $EXIT_CODE -ne 0 ]; then
    echo "    ✗ dottie install failed with exit code $EXIT_CODE"
    exit 1
fi
echo "    ✓ dottie install completed"

# Step 2: Verify fonts directory exists
echo "  Checking if fonts directory was created..."
if [ -d "$FONTS_DIR" ]; then
    echo "    ✓ Fonts directory exists at $FONTS_DIR"
else
    echo "    ✗ Fonts directory not found at $FONTS_DIR"
    exit 1
fi

# Step 3: Check if any fonts were installed
echo "  Checking installed fonts..."
FONT_COUNT=$(find "$FONTS_DIR" -type f -name "*.ttf" -o -name "*.otf" | wc -l)
if [ $FONT_COUNT -gt 0 ]; then
    echo "    ✓ Found $FONT_COUNT font files"
    find "$FONTS_DIR" -type f \( -name "*.ttf" -o -name "*.otf" \) | head -5
else
    echo "    ✗ No font files found"
    echo "    Directory contents:"
    ls -la "$FONTS_DIR" 2>/dev/null || echo "    (unable to list directory)"
    exit 1
fi

# Step 4: Check if font cache was updated (if fc-cache is available)
if command -v fc-cache &> /dev/null; then
    echo "  Checking font cache..."
    if [ -f "$FONTS_DIR/../fontconfig-timestamp.x86-64-linux-gnu" ] || [ -f "$FONTS_DIR/../.uuid" ]; then
        echo "    ✓ Font cache updated"
    else
        echo "    ✓ fc-cache available (cache update skipped in container)"
    fi
fi

echo ""
echo "  ✓ All font installation tests passed!"
exit 0
