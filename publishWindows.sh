#!/usr/bin/env bash
set -euo pipefail

# -----------------------------
# Config (edit this one value)
# -----------------------------

# This must match your Angular project name under "projects" in angular.json
ANGULAR_PROJECT_NAME="frontend"  # <-- change this if your dist folder name is different

# Runtime ID for Windows 64-bit
RUNTIME_ID="win-x64"

# -----------------------------
# Helpers
# -----------------------------

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FRONTEND_DIR="$ROOT_DIR/frontend"
BACKEND_DIR="$ROOT_DIR/backend"
WWWROOT_DIR="$BACKEND_DIR/wwwroot"
DIST_DIR="$FRONTEND_DIR/dist/$ANGULAR_PROJECT_NAME"
PUBLISH_DIR="$BACKEND_DIR/bin/Release/net8.0/$RUNTIME_ID/publish"

echo "=== PiTunes Build Script ==="
echo "Root:      $ROOT_DIR"
echo "Frontend:  $FRONTEND_DIR"
echo "Backend:   $BACKEND_DIR"
echo "Runtime:   $RUNTIME_ID"
echo ""

# -----------------------------
# Sanity checks
# -----------------------------

command -v dotnet >/dev/null 2>&1 || { echo "ERROR: dotnet SDK not found in PATH."; exit 1; }
command -v npm >/dev/null 2>&1    || { echo "ERROR: npm not found in PATH."; exit 1; }

if [ ! -f "$ROOT_DIR/PiTunes.sln" ]; then
  echo "WARNING: PiTunes.sln not found in $ROOT_DIR. Are you in the repo root?"
fi

if [ ! -f "$FRONTEND_DIR/angular.json" ] && [ ! -f "$ROOT_DIR/angular.json" ]; then
  echo "WARNING: angular.json not found where expected."
fi

# -----------------------------
# Step 1: Build Angular frontend
# -----------------------------

echo "=== Building Angular frontend ==="
cd "$FRONTEND_DIR"

if [ -f "package-lock.json" ]; then
  echo "Running: npm ci"
  npm ci
else
  echo "Running: npm install"
  npm install
fi

echo "Running: npm run build -- --configuration production"
npm run build -- --configuration production

if [ ! -d "$DIST_DIR" ]; then
  echo "ERROR: Angular dist directory not found: $DIST_DIR"
  echo "       Make sure ANGULAR_PROJECT_NAME='$ANGULAR_PROJECT_NAME' matches your angular.json project name."
  exit 1
fi

# -----------------------------
# Step 2: Copy dist -> backend/wwwroot
# -----------------------------

echo "=== Copying Angular build to backend/wwwroot ==="
cd "$ROOT_DIR"

echo "Cleaning existing wwwroot: $WWWROOT_DIR"
rm -rf "$WWWROOT_DIR"
mkdir -p "$WWWROOT_DIR"

echo "Copying from: $DIST_DIR"
cp -R "$DIST_DIR"/. "$WWWROOT_DIR"

# -----------------------------
# Step 3: Publish .NET backend as Windows EXE
# -----------------------------

echo "=== Publishing .NET backend for Windows ($RUNTIME_ID) ==="
cd "$BACKEND_DIR"

dotnet publish ./backend.csproj \
  -c Release \
  -r "$RUNTIME_ID" \
  -p:PublishSingleFile=true \
  -p:SelfContained=true

echo ""
echo "=== Build complete ==="
echo "Published output (Windows EXE) is in:"
echo "  $PUBLISH_DIR"
echo "Main executable should be something like:"
ls -1 "$PUBLISH_DIR" | grep -E '\.exe$' || echo "  (No .exe found? Check publish output.)"
echo ""
echo "Copy that publish folder to your Windows box and run the EXE."
