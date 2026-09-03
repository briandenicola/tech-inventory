#!/usr/bin/env node
//
// Fail-closed OpenAPI generated-client drift check (specs/004-agentic-
// development-foundation, T104 revision · B-1).
//
// The Taskfile version of this check used to run:
//   pnpm --dir src/TechInventory.Web run generate:client
//   git diff --exit-code -- src/TechInventory.Web/src/lib/api/generated/types.ts
//
// `generate:client` reads the *working-tree* `openapi.yaml`, but `git diff`
// then compared the result against the *index/HEAD* copy of `types.ts` — two
// different points in time. Any uncommitted-but-internally-consistent change
// to either file reported as "drift" even when the generated client was
// perfectly in sync with the working-tree spec (Apone's T104 review, B-1).
//
// This script compares like with like: it snapshots the current working-tree
// artifact, regenerates it in place with the exact same `generate:client`
// command a developer runs, diffs the regenerated content against the
// snapshot it just took, and — on both a clean pass and a detected drift —
// restores the original bytes before exiting. It never touches git and never
// depends on anything being committed.
//
// Usage: node scripts/check-client-drift.mjs

import { spawnSync, execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';

const ARTIFACT_RELATIVE_PATH = 'src/TechInventory.Web/src/lib/api/generated/types.ts';
const WEB_DIR_RELATIVE_PATH = 'src/TechInventory.Web';

function normalizeLineEndings(text) {
  return text.replace(/\r\n/g, '\n');
}

// Pure comparison — no filesystem, no process spawning — so it can be unit
// tested directly. Line endings are normalized before comparing: this
// check's job is to detect genuine content drift, not to fail on a CRLF/LF
// difference introduced by git's own checkout normalization (core.autocrlf),
// which is exactly the kind of serializer noise `check:openapi-drift`
// already ignores for its own artifact.
export function compareArtifacts(committedText, regeneratedText) {
  const committed = normalizeLineEndings(committedText);
  const regenerated = normalizeLineEndings(regeneratedText);

  if (committed === regenerated) {
    return { drifted: false, message: null };
  }

  const committedLines = committed.split('\n');
  const regeneratedLines = regenerated.split('\n');
  const maxLines = Math.max(committedLines.length, regeneratedLines.length);
  let firstDifferenceLine = maxLines;
  for (let i = 0; i < maxLines; i += 1) {
    if (committedLines[i] !== regeneratedLines[i]) {
      firstDifferenceLine = i + 1;
      break;
    }
  }

  return {
    drifted: true,
    message:
      `generated client differs from a fresh regeneration starting at line ${firstDifferenceLine} ` +
      `(${committedLines.length} -> ${regeneratedLines.length} lines). Run ` +
      `'pnpm --dir ${WEB_DIR_RELATIVE_PATH} run generate:client' and commit the result.`,
  };
}

// Snapshots the artifact, regenerates it in place via the real `generate:client`
// command, compares the result against the snapshot, and ALWAYS restores the
// original bytes in a `finally` — on a clean pass, on detected drift, and on a
// failure of the generate command itself — so the working tree is never left
// modified by this check and the outcome never depends on anything being
// committed. Filesystem and process-spawn calls are injectable so the whole
// flow (including restoration) can be unit tested without pnpm/openapi-typescript.
export function checkClientDrift({
  repoRoot,
  readArtifact = (absolutePath) => fs.readFileSync(absolutePath),
  writeArtifact = (absolutePath, buffer) => fs.writeFileSync(absolutePath, buffer),
  runGenerate = () =>
    spawnSync('pnpm', ['--dir', WEB_DIR_RELATIVE_PATH, 'run', 'generate:client'], {
      cwd: repoRoot,
      stdio: 'inherit',
      shell: process.platform === 'win32',
    }),
}) {
  const artifactAbsolutePath = path.join(repoRoot, ARTIFACT_RELATIVE_PATH);
  const snapshot = readArtifact(artifactAbsolutePath);

  try {
    const generateResult = runGenerate();

    if (!generateResult || generateResult.error || generateResult.status !== 0) {
      const detail = generateResult?.error?.message ?? `exit code ${generateResult?.status ?? 'unknown'}`;
      return {
        failed: true,
        exitCode: 1,
        messages: [
          `check:client-drift: 'pnpm run generate:client' failed (${detail}) — cannot verify the client is current.`,
        ],
      };
    }

    const regenerated = readArtifact(artifactAbsolutePath);
    const { drifted, message } = compareArtifacts(snapshot.toString('utf8'), regenerated.toString('utf8'));

    if (drifted) {
      return { failed: true, exitCode: 1, messages: [`check:client-drift: FAILED — ${message}`] };
    }

    return {
      failed: false,
      exitCode: 0,
      messages: ['check:client-drift: passed — generated client matches the working-tree openapi.yaml.'],
    };
  } finally {
    writeArtifact(artifactAbsolutePath, snapshot);
  }
}

function getRepoRoot() {
  return execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
}

function main() {
  const repoRoot = getRepoRoot();
  const outcome = checkClientDrift({ repoRoot });

  for (const message of outcome.messages) {
    (outcome.failed ? console.error : console.log)(message);
  }

  process.exit(outcome.exitCode);
}

const isMain = process.argv[1] && fileURLToPath(import.meta.url) === path.resolve(process.argv[1]);
if (isMain) {
  main();
}
