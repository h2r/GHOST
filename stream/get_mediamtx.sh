#!/usr/bin/env bash
# Download the MediaMTX server binary into bin/ (not committed).
# Usage: ./get_mediamtx.sh [linux|windows]
set -euo pipefail
cd "$(dirname "$0")"

os="${1:-linux}"
case "$os" in
  linux) pattern="linux_amd64.tar.gz" ;;
  windows) pattern="windows_amd64.zip" ;;
  *) echo "usage: $0 [linux|windows]" >&2; exit 1 ;;
esac

url=$(curl -fsSL https://api.github.com/repos/bluenviron/mediamtx/releases/latest |
  grep -o "\"browser_download_url\": *\"[^\"]*${pattern}\"" | cut -d'"' -f4 | head -1)
[ -n "$url" ] || { echo "could not resolve mediamtx release for ${pattern}" >&2; exit 1; }

mkdir -p bin
echo "downloading ${url}"
if [ "$os" = linux ]; then
  curl -fsSL "$url" | tar -xz -C bin mediamtx
else
  curl -fsSL -o bin/mediamtx.zip "$url"
  unzip -o -d bin bin/mediamtx.zip mediamtx.exe >/dev/null
  rm bin/mediamtx.zip
fi
echo "mediamtx ready in bin/"
