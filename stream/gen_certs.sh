#!/usr/bin/env bash
# Generate a self-signed certificate for MediaMTX's WHEP endpoint (LAN use).
# Includes the machine's hostname and IPs as subject alternative names so
# browsers accept it for whichever address operators use.
set -euo pipefail
cd "$(dirname "$0")"

mkdir -p certs

ips=$(hostname -I 2>/dev/null | tr ' ' '\n' | grep -v '^$' | sed 's/^/IP:/' | paste -sd, -)
san="DNS:localhost,DNS:$(hostname),IP:127.0.0.1${ips:+,$ips}"

openssl req -x509 -newkey rsa:2048 -nodes -days 825 \
  -keyout certs/server.key -out certs/server.crt \
  -subj "/CN=ghost-stream" -addext "subjectAltName=${san}"

echo "wrote certs/server.crt (SAN: ${san})"
