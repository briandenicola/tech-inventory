#!/usr/bin/env node
// Idempotency check for `task tools:gitleaks` (specs/004-agentic-development-
// foundation, T105 revision · B-4). Exits 0 only if the pinned gitleaks
// binary is already present at the exact pinned version, so `task restore`
// (and everything that depends on it, directly or transitively — every
// `check:*`/`test:*`/`verify:*` task lists `restore` as a dep) can depend on
// `tools:gitleaks` on every invocation without re-downloading the binary each
// time. `status:` entries in Taskfile.yml must be plain command strings, not
// per-platform objects (an object there fails Task's own schema with
// "cannot unmarshal !!map into string") — this script is the single,
// cross-platform check so no platform-specific branching lives in the
// Taskfile, mirroring `install-gitleaks.ps1`/`.sh`'s own platform detection.
//
// Usage: node scripts/check-gitleaks-installed.mjs <pinned-version>

import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

export function resolveGitleaksBinaryPath(repoRoot, platform = process.platform) {
  return path.join(repoRoot, '.tools', 'gitleaks', platform === 'win32' ? 'gitleaks.exe' : 'gitleaks');
}

export function isPinnedVersionInstalled(binaryPath, pinnedVersion, { exists = fs.existsSync, runVersion } = {}) {
  if (!pinnedVersion || !exists(binaryPath)) {
    return false;
  }

  const run = runVersion ?? ((bin) => spawnSync(bin, ['version'], { encoding: 'utf8' }));
  const result = run(binaryPath);

  if (!result || result.error || result.status !== 0) {
    return false;
  }

  return (result.stdout ?? '').trim() === pinnedVersion;
}

const isMainModule = path.resolve(fileURLToPath(import.meta.url)) === path.resolve(process.argv[1] ?? '');

if (isMainModule) {
  const repoRoot = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
  const pinnedVersion = process.argv[2];

  if (!pinnedVersion) {
    console.error('Usage: node scripts/check-gitleaks-installed.mjs <pinned-version>');
    process.exit(1);
  }

  const binaryPath = resolveGitleaksBinaryPath(repoRoot);
  process.exit(isPinnedVersionInstalled(binaryPath, pinnedVersion) ? 0 : 1);
}
