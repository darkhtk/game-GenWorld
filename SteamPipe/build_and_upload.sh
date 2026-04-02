#!/bin/bash
# GenWorld Steam Build & Upload Script
# Usage: ./build_and_upload.sh
# Requires: steamcmd installed and STEAM_USER/STEAM_PASS env vars set

set -e

STEAM_CMD="steamcmd"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
VDF_PATH="$SCRIPT_DIR/app_build.vdf"
BUILD_DIR="$SCRIPT_DIR/../SteamBuild/output"

echo "=== GenWorld Steam Build Upload ==="
echo "VDF: $VDF_PATH"
echo "Build: $BUILD_DIR"

# Check build exists
if [ ! -d "$BUILD_DIR" ]; then
    echo "ERROR: Build directory not found at $BUILD_DIR"
    echo "Run Unity 'Build > Steam Windows' first."
    exit 1
fi

# Show version
if [ -f "$BUILD_DIR/version.txt" ]; then
    echo "Version: $(cat "$BUILD_DIR/version.txt")"
fi

# Ensure steam_appid.txt is not in build
rm -f "$BUILD_DIR/steam_appid.txt"

# Check credentials
if [ -z "$STEAM_USER" ]; then
    echo "ERROR: STEAM_USER env var not set"
    exit 1
fi

# Upload to Steam
echo ""
echo "Uploading to Steam..."
$STEAM_CMD +login "$STEAM_USER" "$STEAM_PASS" +run_app_build "$VDF_PATH" +quit

echo "=== Upload Complete ==="
