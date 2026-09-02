#!/usr/bin/env node
//
// Stale-reference guard for the retired Playwright harness
// (specs/004-agentic-development-foundation, T101 · AC-005).
//
// Playwright has zero future automated role in this repository — no
// merge-blocking, scheduled, release, or optional suite (`brief.md` §2.1).
// This guard fails closed if a Playwright reference returns to an active
// manifest, script, workflow, config file, doc/instruction, or executable
// test tree.
//
// Exemptions (deliberate, not oversights — see `coverage-migration.md` §5.3/§5.4/§5.5/§5.6):
//   - An explicit, named allowlist of historical/work-package spec files —
//     NOT a `specs/**` prefix. `coverage-migration.md` §5.5 previously
//     exempted the entire `specs/` tree as "historical evidence only"; Ripley's
//     T101 review (`validation.md` §7 B2) found that blanket exemption hid ten
//     live, unchecked `specs/_backlog/**` acceptance-criteria promises to write
//     new Playwright tests — backlog entries are Constitution §0 authority
//     source #6, a forward-looking definition of done, not a historical log.
//     The exemption below covers only the closed, evidence-of-what-happened
//     files for completed phases/work-packages named in §5.5; `specs/_backlog/**`
//     and any other `specs/**` path (present or future) is deliberately **not**
//     exempt and will fail this guard on a Playwright reference like anything
//     else in the repository
//   - `.squad/decisions.md`, `.squad/decisions/**`, `.squad/session-log.md` — decision/session ledgers
//   - `.squad/agents/*/history.md`       — per-agent session history
//   - `.squad/log/**`, `.squad/orchestration-log/**` — dated, narrative session
//     logs predating this work package; already gitignored for *future*
//     writes (`.gitignore`), so their continued tracking is historical, not
//     living instruction. Not individually enumerated in `coverage-migration.md`
//     §5.5's file list — extended here on the same historical-record principle
//     already applied to `.squad/session-log.md` and `.squad/agents/*/history.md`,
//     and called out explicitly in the T101 report for independent review
//   - `SESSION-NOTES.md`, `.copilot-state.md` — session handoff logs
//   - `docs/testing/manual-pwa-validation.md` — explicitly names retirement
//     as the reason the manual checklist exists; deleting the mention would
//     make the checklist's own rationale unreadable, and the checklist itself
//     must never be deleted (`plan.md` §2.6, `brief.md` §2.1)
//   - `.env.e2e`, `docker-compose.e2e.yml` — resolved by T104: neither file
//     had a real non-browser role (all HTTP integration tests use in-process
//     `WebApplicationFactory<Program>`, no Docker), so both were deleted
//     rather than renamed (`coverage-migration.md` §5.3). No exemption is
//     needed for files that no longer exist.
//   - `.specify/memory/constitution.md`, `docs/prd.md` — normative documents;
//     amending their Playwright-mandating clauses requires an ADR and is
//     "surfaced, not performed" by T101 (`coverage-migration.md` §5.4,
//     `plan.md` §6). Recorded as an explicit, human-visible exception in
//     `validation.md`, not a silent gap
//   - this script and its test file — the guard must say the word to define
//     the pattern it checks for
//   - lines that only reference this guard's own script filename
//     (`check-stale-playwright-references[.test].mjs`) wherever it is
//     invoked or documented (Taskfile.yml, verify.ps1/.sh,
//     .github/workflows/README.md) — the filename is a required
//     self-reference, not a stale mention of the retired tool
//   - `pnpm-lock.yaml` lines naming `@vitest/browser-playwright` — an
//     *optional peer dependency of vitest itself*, never installed, causes
//     no browser download (`coverage-migration.md` §5.6)
//
// Independent of the keyword scan, this guard also fails if `tests/e2e/`
// exists as a tracked path or any `playwright.config.*` file exists anywhere
// — belt-and-suspenders against a reintroduced harness that avoids the
// literal word "playwright" in its own source.

import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const PLAYWRIGHT_PATTERN = /playwright/i;
const LOCKFILE_ALLOWED_LINE = /@vitest\/browser-playwright/;
// The guard's own script name is a required, unavoidable self-reference
// wherever it is invoked or documented (Taskfile.yml, verify.ps1/.sh,
// .github/workflows/README.md, this file, its test). Matching the filename
// itself is not a stale mention of the retired tool — it is the name of the
// thing that guards against the tool returning.
const GUARD_SELF_NAME_LINE = /check-stale-playwright-references(\.test)?\.mjs/;

const EXEMPT_PATH_PREFIXES = ['.squad/decisions/', '.squad/log/', '.squad/orchestration-log/'];

// Narrow, explicit allowlist of historical/work-package spec files —
// deliberately not a `specs/` prefix (see the header comment and
// `coverage-migration.md` §5.5). Any other path under `specs/` (most
// importantly all of `specs/_backlog/**`) is NOT exempt.
const EXEMPT_SPEC_PATHS = [
  'specs/004-agentic-development-foundation/brief.md',
  'specs/004-agentic-development-foundation/plan.md',
  'specs/004-agentic-development-foundation/tasks.md',
  'specs/004-agentic-development-foundation/validation.md',
  'specs/004-agentic-development-foundation/evidence.md',
  'specs/004-agentic-development-foundation/coverage-migration.md',
  'specs/001-core-api/plan.md',
  'specs/001-core-api/tasks.md',
  'specs/002-frontend-mvp/plan.md',
  'specs/002-frontend-mvp/spec.md',
  'specs/002-frontend-mvp/tasks.md',
  'specs/003-pwa-polish/tasks.md',
];

const EXEMPT_EXACT_PATHS = new Set([
  ...EXEMPT_SPEC_PATHS,
  '.squad/decisions.md',
  '.squad/session-log.md',
  'SESSION-NOTES.md',
  '.copilot-state.md',
  'docs/testing/manual-pwa-validation.md',
  '.specify/memory/constitution.md',
  'docs/prd.md',
]);

const selfPath = toRepoRelative(fileURLToPath(import.meta.url));
const selfTestPath = selfPath.replace(/\.mjs$/, '.test.mjs');
EXEMPT_EXACT_PATHS.add(selfPath);
EXEMPT_EXACT_PATHS.add(selfTestPath);

const AGENT_HISTORY_PATTERN = /^\.squad\/agents\/[^/]+\/history\.md$/;

/**
 * Pure classification function — exported so the automated test can drive it
 * with synthetic fixtures instead of mutating the real repository.
 *
 * @param {{ path: string, content: string }[]} files
 * @returns {{ file: string, line: number, text: string }[]} violations
 */
export function findStaleReferences(files) {
  const violations = [];

  for (const { path: filePath, content } of files) {
    const normalizedPath = filePath.replace(/\\/g, '/');

    if (isExempt(normalizedPath)) {
      continue;
    }

    if (isReintroducedHarnessPath(normalizedPath)) {
      violations.push({
        file: normalizedPath,
        line: 1,
        text: 'path lies under the retired tests/e2e/ tree or is a Playwright config file',
      });
      continue;
    }

    if (typeof content !== 'string') {
      continue;
    }

    const isLockfile = normalizedPath.endsWith('pnpm-lock.yaml') || normalizedPath.endsWith('package-lock.json');
    const lines = content.split(/\r?\n/);

    for (let index = 0; index < lines.length; index++) {
      const line = lines[index];
      // Strip the guard's own filename before testing — a bare
      // self-reference must not mask a genuine Playwright mention that
      // happens to share the line.
      const lineWithoutSelfName = line.replace(new RegExp(GUARD_SELF_NAME_LINE, 'g'), '');

      if (!PLAYWRIGHT_PATTERN.test(lineWithoutSelfName)) {
        continue;
      }

      if (isLockfile && LOCKFILE_ALLOWED_LINE.test(line)) {
        continue;
      }

      violations.push({
        file: normalizedPath,
        line: index + 1,
        text: line.trim(),
      });
    }
  }

  return violations;
}

function isExempt(normalizedPath) {
  if (EXEMPT_EXACT_PATHS.has(normalizedPath)) {
    return true;
  }

  if (AGENT_HISTORY_PATTERN.test(normalizedPath)) {
    return true;
  }

  return EXEMPT_PATH_PREFIXES.some((prefix) => normalizedPath.startsWith(prefix));
}

function isReintroducedHarnessPath(normalizedPath) {
  if (normalizedPath === 'tests/e2e' || normalizedPath.startsWith('tests/e2e/')) {
    return true;
  }

  return /(^|\/)playwright\.config\.[cm]?[jt]s$/.test(normalizedPath);
}

function toRepoRelative(absolutePath) {
  const repoRoot = getRepoRoot();
  return path.relative(repoRoot, absolutePath).split(path.sep).join('/');
}

function getRepoRoot() {
  return execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
}

async function main() {
  const repoRoot = getRepoRoot();
  process.chdir(repoRoot);

  const trackedFiles = execFileSync('git', ['ls-files', '-z'], { encoding: 'utf8' })
    .split('\u0000')
    .filter(Boolean);

  const files = [];
  for (const filePath of trackedFiles) {
    const absolutePath = path.join(repoRoot, filePath);
    let buffer;
    try {
      buffer = fs.readFileSync(absolutePath);
    } catch (error) {
      if (error?.code === 'ENOENT') {
        continue; // deleted-but-still-listed in a race; not our concern
      }
      throw error;
    }

    if (buffer.includes(0)) {
      files.push({ path: filePath, content: null }); // binary; path check still applies
      continue;
    }

    files.push({ path: filePath, content: buffer.toString('utf8') });
  }

  const violations = findStaleReferences(files);

  if (violations.length === 0) {
    console.log(`Stale-reference guard passed: 0 active Playwright references across ${files.length} tracked file(s).`);
    process.exit(0);
  }

  console.error(`Stale-reference guard failed: ${violations.length} active Playwright reference(s) found.`);
  console.error('');
  for (const violation of violations) {
    console.error(`- ${violation.file}:${violation.line}`);
    console.error(`  ${violation.text}`);
  }
  console.error('');
  console.error('Playwright is retired (specs/004-agentic-development-foundation/brief.md §2.1).');
  console.error('If this reference is genuinely historical evidence, it belongs under an explicit');
  console.error('exemption (a named specs/004-.../001-.../002-.../003-... file, .squad/decisions*,');
  console.error('.squad/agents/*/history.md, SESSION-NOTES.md, .copilot-state.md) — not in an active');
  console.error('manifest, script, workflow, config, doc, test tree, or specs/_backlog/** entry.');
  process.exit(1);
}

const isMain = process.argv[1] && fileURLToPath(import.meta.url) === path.resolve(process.argv[1]);
if (isMain) {
  await main();
}
