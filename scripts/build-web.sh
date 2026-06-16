#!/bin/bash
# Build the web console to static files (web/dist/) using a throwaway Node
# container, so the server needs no Node install of its own. ghost-up.sh then
# serves web/dist/ with python's http.server. Re-run after changing web/.
set -e
cd "$(dirname "$0")/../web"
docker run --rm -v "$PWD":/app -w /app node:20 sh -lc "npm ci && npm run build"
echo "Built web/dist/ — ghost-up.sh will serve it on :5173."
