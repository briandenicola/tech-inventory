#!/usr/bin/env bash
#
# Fails when the committed OpenAPI document no longer describes what the API
# project actually exposes. Modelled on the "Verify OpenAPI snapshot" gate in
# briandenicola/aurearia, adapted for the format difference described in
# scripts/compare-openapi.py.
#
# Usage: ./scripts/check-openapi-drift.sh
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
committed="$repo_root/openapi.yaml"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT
generated="$tmp_dir/openapi.generated.yaml"

echo "Regenerating the OpenAPI document from the API project..."
dotnet run --project "$repo_root/src/TechInventory.Api/TechInventory.Api.csproj" \
	-c Release --no-launch-profile -- export-openapi "$generated"

if [[ ! -s "$generated" ]]; then
	echo "export-openapi produced no output at $generated" >&2
	exit 1
fi

python3 "$repo_root/scripts/compare-openapi.py" "$committed" "$generated"
