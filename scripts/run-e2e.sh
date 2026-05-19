#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
base_url="${BASE_URL:-http://localhost:3000}"
ready_url="${READY_URL:-http://localhost:8080/health/ready}"

# Combined compose invocation: prod compose + e2e override + stub env file.
# The env file satisfies docker-compose.yml's `${VAR:?}` required-var guards
# (which abort interpolation before any service starts); the override file
# swaps GHCR images for local builds, forces Development env (so the F025
# LocalAdminSeedHostedService is willing to run without SeedAllowInProd), and
# seeds the local admin Playwright fixtures sign in as.
compose=(docker compose --env-file "$repo_root/.env.e2e"
  -f "$repo_root/docker-compose.yml"
  -f "$repo_root/docker-compose.e2e.yml")

cleanup() {
  cd "$repo_root"
  "${compose[@]}" down -v
}

trap cleanup EXIT

cd "$repo_root"
"${compose[@]}" up -d --build

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
