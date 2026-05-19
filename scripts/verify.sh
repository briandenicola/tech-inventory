#!/usr/bin/env bash
set -euo pipefail

echo "======================================"
echo "Tech Inventory — Verification Pipeline"
echo "======================================"
echo ""

# Backend verification
echo "🔍 [1/9] Checking .NET code formatting..."
dotnet format --verify-no-changes

echo ""
echo "🔨 [2/9] Building .NET solution..."
dotnet build -c Release

echo ""
echo "🧪 [3/9] Running backend unit tests..."
dotnet test tests/TechInventory.UnitTests/TechInventory.UnitTests.csproj -c Release --no-build

echo ""
echo "🧪 [4/9] Running backend integration tests..."
dotnet test tests/TechInventory.IntegrationTests/TechInventory.IntegrationTests.csproj -c Release --no-build

echo ""
echo "🔒 [5/9] Scanning for vulnerable packages..."
dotnet list package --vulnerable --include-transitive

# Frontend verification
echo ""
echo "📦 [6/9] Installing frontend dependencies..."
cd src/TechInventory.Web
pnpm install --frozen-lockfile

echo ""
echo "🔍 [7/9] Checking frontend (tsc + svelte-check)..."
pnpm run generate:client
pnpm run check

echo ""
echo "✨ [8/9] Linting frontend..."
pnpm run lint

cd ../..

echo ""
echo "🎭 [9/9] Running Playwright against the hermetic compose stack..."
./scripts/run-e2e.sh

echo ""
echo "======================================"
echo "✅ Verification complete!"
echo "======================================"
