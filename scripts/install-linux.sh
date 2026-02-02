#!/bin/bash
# Install dottie CLI from the latest GitHub release into ~/bin

set -e

REPO="ryandanthony/dottie"
BIN_DIR="${HOME}/bin"

# Create ~/bin if it doesn't exist
mkdir -p "$BIN_DIR"

# Get the latest release tag
LATEST_RELEASE=$(curl -s https://api.github.com/repos/$REPO/releases/latest | grep -o '"tag_name": "[^"]*' | cut -d'"' -f4)

if [ -z "$LATEST_RELEASE" ]; then
    echo "Error: Could not fetch latest release"
    exit 1
fi

echo "Installing dottie $LATEST_RELEASE..."

# Download the linux-x64 binary
DOWNLOAD_URL="https://github.com/$REPO/releases/download/$LATEST_RELEASE/dottie"
curl -sL "$DOWNLOAD_URL" -o "$BIN_DIR/dottie"
chmod +x "$BIN_DIR/dottie"

echo "✓ dottie $LATEST_RELEASE installed to $BIN_DIR/dottie"

# Check if ~/bin is in PATH
if [[ ":$PATH:" == *":$HOME/bin:"* ]]; then
    echo "✓ $BIN_DIR is already in your PATH"
else
    echo "⚠ $BIN_DIR is not in your PATH"
    echo "  Add this line to your shell profile (~/.bashrc, ~/.zshrc, etc.):"
    echo "  export PATH=\"\$HOME/bin:\$PATH\""
fi
