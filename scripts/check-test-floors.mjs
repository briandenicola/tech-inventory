#!/usr/bin/env node
//
// Fail-closed collected-test floors (specs/004-agentic-development-foundation,
// T104 · AC-008 item 7).
//
// Runs one named test suite to completion (streaming its normal console
// output, same as running it directly) and then asserts the suite actually
// collected at least a floor number of tests. This exists to catch the
// failure mode where a test run reports success (exit 0) while having
// silently discovered zero or a small fraction of its tests — a broken
// filter, a misconfigured project reference, a bad glob — not to duplicate
// the pass/fail judgment the runner already makes (a real test failure still
// fails the process the normal way).
//
// Floors are deliberately set below the measured baseline so intentional
// test growth or a handful of consolidations never trips this guard; they
// exist to catch collection collapsing toward zero, not to pin an exact
// count. Each floor's baseline was independently measured on 2026-09-02 by
// running the suite directly on a clean Release build:
//   unit        278 collected  -> floor 250
//   integration 296 collected  -> floor 265 (292 passed + 4 intentionally
//                                  skipped; skips still count as collected)
//   frontend    649 tests / 83 files -> floor 580 tests / 74 files
//
// Usage: node scripts/check-test-floors.mjs <unit|integration|frontend>

import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { execFileSync } from 'node:child_process';

const SUITES = {
  unit: {
    kind: 'dotnet',
    project: 'tests/TechInventory.UnitTests/TechInventory.UnitTests.csproj',
    floor: 250,
    baseline: 278,
    measuredOn: '2026-09-02',
  },
  integration: {
    kind: 'dotnet',
    project: 'tests/TechInventory.IntegrationTests/TechInventory.IntegrationTests.csproj',
    floor: 265,
    baseline: 296,
    measuredOn: '2026-09-02',
  },
  frontend: {
    kind: 'vitest',
    floor: 580,
    fileFloor: 74,
    baseline: 649,
    fileBaseline: 83,
    measuredOn: '2026-09-02',
  },
};

function getRepoRoot() {
  return execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
}

function runDotnetSuite(name, suite, repoRoot) {
  const resultsDir = path.join(repoRoot, 'TestResults', 'floor-check');
  fs.mkdirSync(resultsDir, { recursive: true });
  const trxName = `${name}.trx`;
  const trxPath = path.join(resultsDir, trxName);
  if (fs.existsSync(trxPath)) {
    fs.rmSync(trxPath);
  }

  const args = [
    'test',
    suite.project,
    '-c',
    'Release',
    '--logger',
    `trx;LogFileName=${trxName}`,
    '--results-directory',
    resultsDir,
    '--collect',
    'XPlat Code Coverage',
  ];

  const result = spawnSync('dotnet', args, { cwd: repoRoot, stdio: 'inherit' });
  const runFailed = result.status !== 0;

  if (!fs.existsSync(trxPath)) {
    console.error(`\ncheck-test-floors: no TRX result file was produced at ${trxPath}.`);
    console.error('Treating this as zero collected tests — cannot verify the floor.');
    return { collected: 0, runFailed: true };
  }

  const trxContent = fs.readFileSync(trxPath, 'utf8');
  const match = trxContent.match(/<Counters\b[^>]*\btotal="(\d+)"/);
  const collected = match ? Number(match[1]) : 0;

  return { collected, runFailed };
}

function runFrontendSuite(repoRoot) {
  const webDir = path.join(repoRoot, 'src', 'TechInventory.Web');
  const resultsDir = path.join(repoRoot, 'TestResults', 'floor-check');
  fs.mkdirSync(resultsDir, { recursive: true });
  const jsonPath = path.join(resultsDir, 'frontend.json');
  if (fs.existsSync(jsonPath)) {
    fs.rmSync(jsonPath);
  }

  const args = [
    'exec',
    'vitest',
    '--run',
    '--reporter=default',
    '--reporter=json',
    `--outputFile.json=${jsonPath}`,
  ];

  const result = spawnSync('pnpm', args, { cwd: webDir, stdio: 'inherit', shell: process.platform === 'win32' });
  const runFailed = result.status !== 0;

  if (!fs.existsSync(jsonPath)) {
    console.error(`\ncheck-test-floors: no JSON result file was produced at ${jsonPath}.`);
    console.error('Treating this as zero collected tests — cannot verify the floor.');
    return { collected: 0, files: 0, runFailed: true };
  }

  const summary = JSON.parse(fs.readFileSync(jsonPath, 'utf8'));
  const collected = Number(summary.numTotalTests ?? 0);
  const files = Array.isArray(summary.testResults) ? summary.testResults.length : 0;

  return { collected, files, runFailed };
}

function main() {
  const suiteName = process.argv[2];
  const suite = SUITES[suiteName];

  if (!suite) {
    console.error(`Usage: node scripts/check-test-floors.mjs <${Object.keys(SUITES).join('|')}>`);
    process.exit(2);
  }

  const repoRoot = getRepoRoot();
  const outcome = suite.kind === 'dotnet' ? runDotnetSuite(suiteName, suite, repoRoot) : runFrontendSuite(repoRoot);

  console.log('');
  console.log(`check-test-floors: ${suiteName} collected ${outcome.collected} test(s)` + (typeof outcome.files === 'number' ? ` across ${outcome.files} file(s)` : '') + `.`);

  let floorFailed = false;
  if (outcome.collected < suite.floor) {
    console.error(
      `check-test-floors: ${suiteName} collected ${outcome.collected} test(s), below the floor of ${suite.floor} ` +
        `(measured baseline ${suite.baseline} on ${suite.measuredOn}). Test collection may be broken.`,
    );
    floorFailed = true;
  }
  if (typeof suite.fileFloor === 'number' && outcome.files < suite.fileFloor) {
    console.error(
      `check-test-floors: ${suiteName} collected ${outcome.files} test file(s), below the floor of ${suite.fileFloor} ` +
        `(measured baseline ${suite.fileBaseline} on ${suite.measuredOn}). Test collection may be broken.`,
    );
    floorFailed = true;
  }

  if (outcome.runFailed || floorFailed) {
    process.exit(1);
  }

  console.log(`check-test-floors: ${suiteName} passed the collected-test floor.`);
  process.exit(0);
}

main();
