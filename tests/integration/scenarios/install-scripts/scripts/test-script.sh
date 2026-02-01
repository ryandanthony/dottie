#!/bin/bash
# Test installation script
# Creates a marker file to demonstrate successful execution

OUTPUT_FILE="${HOME}/.local/bin/script-test.txt"
mkdir -p "$(dirname "$OUTPUT_FILE")"
echo "Script execution successful at $(date)" > "$OUTPUT_FILE"
echo "Setup script completed"
