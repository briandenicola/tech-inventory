#!/usr/bin/env bash
# Commit CSV import to database (DESTRUCTIVE)

set -euo pipefail

CSV_PATH="${1:-}"
CONFIRM="${2:-no}"

if [ "$CONFIRM" != "yes" ]; then
    echo "⚠️  WARNING: This will write devices to the database."
    echo "   Run with CONFIRM=yes to proceed:"
    echo "   task import:commit CONFIRM=yes"
    exit 1
fi

if [ -z "$CSV_PATH" ]; then
    echo "Usage: $0 <csv-path> <confirm>" >&2
    exit 1
fi

if [ ! -f "$CSV_PATH" ]; then
    echo "CSV file not found: $CSV_PATH" >&2
    exit 1
fi

echo "💾 Committing import from: $CSV_PATH"
echo ""

curl -f -X POST http://localhost:8080/api/v1/imports/commit \
    -F "file=@$CSV_PATH" \
    | jq .
