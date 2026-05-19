#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
base_url="${BASE_URL:-http://localhost:3000}"
ready_url="${READY_URL:-http://localhost:8080/health/ready}"

cleanup() {
  cd "$repo_root"
  docker compose down -v
}

trap cleanup EXIT

cd "$repo_root"
# E2E tests rely on the dev-bypass shim (no MSAL bounce off Microsoft).
# Compose forwards this as a build ARG to the web Dockerfile.
export VITE_AUTH_DEV_BYPASS=true
docker compose up -d --build

for attempt in $(seq 1 60); do
  if curl --fail --silent "$ready_url" >/dev/null; then
    break
  fi

  if [[ "$attempt" -eq 60 ]]; then
    echo "API readiness check failed at $ready_url." >&2
    exit 1
  fi

  sleep 2
done

cd "$repo_root/tests/e2e"
npm ci
BASE_URL="$base_url" npx playwright test
