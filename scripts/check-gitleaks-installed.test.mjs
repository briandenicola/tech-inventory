import test from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { resolveGitleaksBinaryPath, isPinnedVersionInstalled } from './check-gitleaks-installed.mjs';

// Repository-native regression suite for the tools:gitleaks status check
// (specs/004-agentic-development-foundation, T105 revision · B-4). Reached
// from the authoritative Task graph via `task restore`'s own cmds, so a
// regression in this checker cannot rot unnoticed — the same pattern used by
// check-client-drift.test.mjs and check-vulnerable.test.mjs.
//
// `path.join` always uses the *running* Node process's native separator
// regardless of the `platform` argument passed to resolveGitleaksBinaryPath
// (that argument only selects the binary filename) — expected values below
// are built with the same `path.join` so the assertions test the binary-name
// selection, not host path-separator conventions.

test('resolveGitleaksBinaryPath appends gitleaks.exe on win32', () => {
  assert.equal(
    resolveGitleaksBinaryPath('C:\\repo', 'win32'),
    path.join('C:\\repo', '.tools', 'gitleaks', 'gitleaks.exe'),
  );
});

test('resolveGitleaksBinaryPath appends the extensionless binary name on linux', () => {
  assert.equal(
    resolveGitleaksBinaryPath('/repo', 'linux'),
    path.join('/repo', '.tools', 'gitleaks', 'gitleaks'),
  );
});

test('resolveGitleaksBinaryPath appends the extensionless binary name on darwin', () => {
  assert.equal(
    resolveGitleaksBinaryPath('/repo', 'darwin'),
    path.join('/repo', '.tools', 'gitleaks', 'gitleaks'),
  );
});

test('isPinnedVersionInstalled is false when no pinned version is given', () => {
  assert.equal(isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '', {}), false);
});

test('isPinnedVersionInstalled is false when the binary path does not exist', () => {
  const result = isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '8.30.1', {
    exists: () => false,
    runVersion: () => {
      throw new Error('must not be called when the binary is missing');
    },
  });
  assert.equal(result, false);
});

test('isPinnedVersionInstalled is false when the binary fails to run (ENOENT-style spawn error)', () => {
  const result = isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '8.30.1', {
    exists: () => true,
    runVersion: () => ({ error: new Error('spawn ENOENT'), status: null, stdout: '' }),
  });
  assert.equal(result, false);
});

test('isPinnedVersionInstalled is false when the binary exits non-zero', () => {
  const result = isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '8.30.1', {
    exists: () => true,
    runVersion: () => ({ error: null, status: 1, stdout: '' }),
  });
  assert.equal(result, false);
});

test('isPinnedVersionInstalled is false when the installed version does not match the pin', () => {
  const result = isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '8.30.1', {
    exists: () => true,
    runVersion: () => ({ error: null, status: 0, stdout: '8.29.0\n' }),
  });
  assert.equal(result, false);
});

test('isPinnedVersionInstalled is true when the installed version exactly matches the pin, trimming trailing newline', () => {
  const result = isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '8.30.1', {
    exists: () => true,
    runVersion: () => ({ error: null, status: 0, stdout: '8.30.1\n' }),
  });
  assert.equal(result, true);
});

test('isPinnedVersionInstalled rejects a version string that merely contains the pin as a substring', () => {
  // Regression guard: a naive `.includes()` comparison would wrongly accept
  // "18.30.1" or "8.30.10" as a match for the pin "8.30.1". Equality only.
  const resultLonger = isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '8.30.1', {
    exists: () => true,
    runVersion: () => ({ error: null, status: 0, stdout: '8.30.10\n' }),
  });
  const resultPrefixed = isPinnedVersionInstalled('/repo/.tools/gitleaks/gitleaks', '8.30.1', {
    exists: () => true,
    runVersion: () => ({ error: null, status: 0, stdout: '18.30.1\n' }),
  });
  assert.equal(resultLonger, false);
  assert.equal(resultPrefixed, false);
});
