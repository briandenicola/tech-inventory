#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-8.30.1}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INSTALL_DIR="$ROOT_DIR/.tools/gitleaks"
EXTRACT_DIR="$INSTALL_DIR/_extract"

case "$(uname -s)" in
  Linux) PLATFORM="linux" ;;
  Darwin) PLATFORM="darwin" ;;
  *)
    echo "Unsupported platform for gitleaks install." >&2
    exit 1
    ;;
esac

case "$(uname -m)" in
  x86_64|amd64) ARCH="x64" ;;
  arm64|aarch64) ARCH="arm64" ;;
  *)
    echo "Unsupported CPU architecture for gitleaks install." >&2
    exit 1
    ;;
esac

ASSET_NAME="gitleaks_${VERSION}_${PLATFORM}_${ARCH}.tar.gz"
DOWNLOAD_URL="https://github.com/gitleaks/gitleaks/releases/download/v${VERSION}/${ASSET_NAME}"
ARCHIVE_PATH="$INSTALL_DIR/$ASSET_NAME"
BINARY_PATH="$INSTALL_DIR/gitleaks"

mkdir -p "$INSTALL_DIR"
rm -rf "$EXTRACT_DIR"
mkdir -p "$EXTRACT_DIR"

curl -fsSL "$DOWNLOAD_URL" -o "$ARCHIVE_PATH"
tar -xzf "$ARCHIVE_PATH" -C "$EXTRACT_DIR"

if [[ ! -f "$EXTRACT_DIR/gitleaks" ]]; then
  echo "Unable to locate gitleaks in $ASSET_NAME" >&2
  exit 1
fi

cp "$EXTRACT_DIR/gitleaks" "$BINARY_PATH"
chmod +x "$BINARY_PATH"
rm -f "$ARCHIVE_PATH"
rm -rf "$EXTRACT_DIR"

"$BINARY_PATH" version
