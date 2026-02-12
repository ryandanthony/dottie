#!/bin/bash
# Test script placed at scripts/ubuntu/setup.sh
# The dottie config references scripts/${ID}/setup.sh which resolves to this path

OUTPUT_FILE="${HOME}/.local/bin/variable-script-test.txt"
mkdir -p "$(dirname "$OUTPUT_FILE")"
echo "Variable script execution successful - ID resolved correctly" > "$OUTPUT_FILE"
echo "Variable script setup completed"
