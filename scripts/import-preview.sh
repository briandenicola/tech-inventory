#!/usr/bin/env bash
# Preview CSV import via multipart/form-data POST to /api/v1/imports/preview

set -euo pipefail

CSV_PATH="${1:-}"

if [ -z "$CSV_PATH" ]; then
    echo "Usage: $0 <csv-path>" >&2
    exit 1
fi

if [ ! -f "$CSV_PATH" ]; then
    echo "CSV file not found: $CSV_PATH" >&2
    exit 1
fi

echo "📄 Previewing import from: $CSV_PATH"
echo ""

curl -f -X POST http://localhost:8080/api/v1/imports/preview \
    -F "file=@$CSV_PATH" \
    | jq .
