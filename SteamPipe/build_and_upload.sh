#!/bin/bash
# GenWorld Steam Build & Upload Script
# Usage: ./build_and_upload.sh
# Requires: steamcmd installed, Unity build completed to Build/Windows/

set -e

STEAM_CMD="steamcmd"
BUILDER_DIR="$(dirname "$0")"
VDF_PATH="$BUILDER_DIR/app_build.vdf"

echo "=== GenWorld Steam Build Upload ==="
echo "VDF: $VDF_PATH"
echo ""

# Check build exists
if [ ! -d "../Build/Windows" ]; then
    echo "ERROR: Build directory not found. Run Unity build first."
    exit 1
fi

# Upload to Steam
$STEAM_CMD +login "$STEAM_USER" "$STEAM_PASS" +run_app_build "$VDF_PATH" +quit

echo "=== Upload Complete ==="
