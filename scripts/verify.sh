#!/usr/bin/env bash
set -e
echo "🔍 Running verify pipeline..."
dotnet format --verify-no-changes
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet list package --vulnerable --include-transitive
echo "✅ Verify complete"
