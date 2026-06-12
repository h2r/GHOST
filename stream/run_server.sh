#!/usr/bin/env bash
# Run MediaMTX with the GHOST config. Get the binary with get_mediamtx.sh
# and certs with gen_certs.sh first.
set -euo pipefail
cd "$(dirname "$0")"
exec bin/mediamtx mediamtx.yml
