#!/usr/bin/env bash
set -euo pipefail

# Tech Inventory verification pipeline (Linux/macOS)
#
# Thin compatibility wrapper — Task (https://taskfile.dev) is the one
# authoritative verification interface (specs/004-agentic-development-
# foundation, T104 · AC-008). This script does not duplicate any pipeline
# logic; it only confirms `task` is on PATH and then delegates to
# `task verify`, which is exactly what CI invokes. Run a narrower stage
# directly if you don't need the full pipeline:
#   task verify:fast      — format, backend build, frontend type-check, lint, unit + frontend tests
#   task verify:contracts — stale-reference guard, OpenAPI/client drift, EF migration drift, integration tests
#   task verify:full      — verify:fast + verify:contracts + frontend production build + vulnerability scan

if ! command -v task >/dev/null 2>&1; then
  echo "Task (https://taskfile.dev) is not installed or not on PATH. Install it with 'brew install go-task/tap/go-task' and re-run." >&2
  exit 1
fi

task verify
