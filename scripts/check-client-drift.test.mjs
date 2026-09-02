import test from 'node:test';
import assert from 'node:assert/strict';
import { compareArtifacts, checkClientDrift } from './check-client-drift.mjs';

test('compareArtifacts: identical content is not drifted', () => {
  const result = compareArtifacts('export type Foo = {};\n', 'export type Foo = {};\n');
  assert.equal(result.drifted, false);
  assert.equal(result.message, null);
});

test('compareArtifacts: CRLF vs LF alone is not drifted (checkout-normalization noise, not real content drift)', () => {
  const result = compareArtifacts('export type Foo = {};\r\nexport type Bar = {};\r\n', 'export type Foo = {};\nexport type Bar = {};\n');
  assert.equal(result.drifted, false);
});

test('compareArtifacts: a real content difference is drifted, with the first differing line reported', () => {
  const result = compareArtifacts(
    'export type Foo = {};\nexport type Bar = {};\n',
    'export type Foo = {};\nexport type Baz = {};\n',
  );
  assert.equal(result.drifted, true);
  assert.match(result.message, /line 2/);
  assert.match(result.message, /generate:client/);
});

function makeSequentialReader(values) {
  let call = 0;
  return () => {
    const value = values[Math.min(call, values.length - 1)];
    call += 1;
    return Buffer.from(value, 'utf8');
  };
}

test('checkClientDrift: clean case — regeneration matches the snapshot, exits 0, and restores the original bytes', () => {
  const writes = [];
  const outcome = checkClientDrift({
    repoRoot: 'C:/repo',
    readArtifact: makeSequentialReader(['type A = {};\n', 'type A = {};\n']),
    writeArtifact: (absolutePath, buffer) => writes.push({ absolutePath, content: buffer.toString('utf8') }),
    runGenerate: () => ({ status: 0 }),
  });

  assert.equal(outcome.failed, false);
  assert.equal(outcome.exitCode, 0);
  assert.equal(writes.length, 1, 'must restore exactly once');
  assert.equal(writes[0].content, 'type A = {};\n');
});

test('checkClientDrift: stale case — regeneration differs from the snapshot, exits 1, and restores the ORIGINAL (stale) bytes, not the regenerated ones', () => {
  const writes = [];
  const outcome = checkClientDrift({
    repoRoot: 'C:/repo',
    readArtifact: makeSequentialReader(['type A = {};\n', 'type A = {}; type B = {};\n']),
    writeArtifact: (absolutePath, buffer) => writes.push({ absolutePath, content: buffer.toString('utf8') }),
    runGenerate: () => ({ status: 0 }),
  });

  assert.equal(outcome.failed, true);
  assert.equal(outcome.exitCode, 1);
  assert.match(outcome.messages[0], /FAILED/);
  assert.equal(writes.length, 1, 'must restore exactly once even on detected drift');
  assert.equal(writes[0].content, 'type A = {};\n', 'restored content must be the pre-regeneration snapshot');
});

test('checkClientDrift: passes when the current client is already synchronized, regardless of an uncommitted-but-consistent spec change', () => {
  // Simulates T102's scenario: openapi.yaml has an uncommitted change and the
  // generated client already reflects it. Both reads return the same
  // (uncommitted) content because regeneration from the working-tree spec is
  // a no-op given an already-synchronized client.
  const writes = [];
  const outcome = checkClientDrift({
    repoRoot: 'C:/repo',
    readArtifact: makeSequentialReader(['type A = { code: 403 };\n', 'type A = { code: 403 };\n']),
    writeArtifact: (absolutePath, buffer) => writes.push(buffer.toString('utf8')),
    runGenerate: () => ({ status: 0 }),
  });

  assert.equal(outcome.failed, false);
  assert.equal(outcome.exitCode, 0);
});

test('checkClientDrift: generate-command failure (nonzero exit) fails closed and still restores the original bytes without reading a possibly-broken regeneration', () => {
  const writes = [];
  let regeneratedRead = false;
  const outcome = checkClientDrift({
    repoRoot: 'C:/repo',
    readArtifact: makeSequentialReader(['type A = {};\n']),
    writeArtifact: (absolutePath, buffer) => writes.push(buffer.toString('utf8')),
    runGenerate: () => {
      regeneratedRead = true;
      return { status: 1 };
    },
  });

  assert.equal(regeneratedRead, true);
  assert.equal(outcome.failed, true);
  assert.equal(outcome.exitCode, 1);
  assert.match(outcome.messages[0], /generate:client' failed/);
  assert.equal(writes.length, 1, 'must restore even when the generate command itself fails');
  assert.equal(writes[0], 'type A = {};\n');
});

test('checkClientDrift: a spawn error (e.g. pnpm not on PATH) fails closed and still restores', () => {
  const writes = [];
  const outcome = checkClientDrift({
    repoRoot: 'C:/repo',
    readArtifact: makeSequentialReader(['type A = {};\n']),
    writeArtifact: (absolutePath, buffer) => writes.push(buffer.toString('utf8')),
    runGenerate: () => ({ error: new Error('ENOENT: pnpm not found') }),
  });

  assert.equal(outcome.failed, true);
  assert.equal(outcome.exitCode, 1);
  assert.match(outcome.messages[0], /ENOENT/);
  assert.equal(writes.length, 1);
});

test('checkClientDrift: restores even if comparing/regenerating throws unexpectedly', () => {
  const writes = [];
  assert.throws(() => {
    checkClientDrift({
      repoRoot: 'C:/repo',
      readArtifact: makeSequentialReader(['type A = {};\n']),
      writeArtifact: (absolutePath, buffer) => writes.push(buffer.toString('utf8')),
      runGenerate: () => {
        throw new Error('unexpected failure mid-generation');
      },
    });
  }, /unexpected failure mid-generation/);

  assert.equal(writes.length, 1, 'restore must still happen even when runGenerate throws');
  assert.equal(writes[0], 'type A = {};\n');
});
