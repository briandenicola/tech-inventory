#!/usr/bin/env bash
set -euo pipefail

echo "======================================"
echo "Tech Inventory — Verification Pipeline"
echo "======================================"
echo ""

# Backend verification
echo "🔍 [1/7] Checking .NET code formatting..."
dotnet format --verify-no-changes

echo ""
echo "🔨 [2/7] Building .NET solution..."
dotnet build -c Release

echo ""
echo "🧪 [3/7] Running .NET tests..."
dotnet test -c Release --no-build

echo ""
echo "🔒 [4/7] Scanning for vulnerable packages..."
dotnet list package --vulnerable --include-transitive

# Frontend verification
echo ""
echo "📦 [5/7] Installing frontend dependencies..."
cd src/TechInventory.Web
pnpm install --frozen-lockfile

echo ""
echo "🔍 [6/7] Checking frontend (tsc + svelte-check)..."
pnpm run check

echo ""
echo "✨ [7/7] Linting frontend..."
pnpm run lint

# Frontend tests are run separately via `task test` (requires running stack for E2E)

echo ""
echo "======================================"
echo "✅ Verification complete!"
echo "======================================"
