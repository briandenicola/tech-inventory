import test from 'node:test';
import assert from 'node:assert/strict';
import { findStaleReferences } from './check-stale-playwright-references.mjs';

test('passes on a clean, Playwright-free file set', () => {
  const violations = findStaleReferences([
    { path: 'Taskfile.yml', content: 'tasks:\n  test:unit:\n    cmds:\n      - dotnet test\n' },
    { path: 'README.md', content: '# Tech Inventory\n\nRun `task test`.\n' },
  ]);

  assert.deepEqual(violations, []);
});

test('flags a reintroduced Playwright invocation in a script', () => {
  const violations = findStaleReferences([
    { path: 'scripts/verify.sh', content: 'echo "run"\nnpx playwright test\n' },
  ]);

  assert.equal(violations.length, 1);
  assert.equal(violations[0].file, 'scripts/verify.sh');
  assert.equal(violations[0].line, 2);
});

test('flags a reintroduced Playwright dependency in a manifest', () => {
  const violations = findStaleReferences([
    { path: 'tests/e2e/package.json', content: '{"devDependencies":{"@playwright/test":"^1.60.0"}}' },
  ]);

  // tests/e2e/** is also a hard-fail path, independent of the keyword scan.
  assert.equal(violations.length, 1);
  assert.match(violations[0].text, /retired tests\/e2e/);
});

test('flags Playwright reappearing in an active doc/instruction file', () => {
  const violations = findStaleReferences([
    {
      path: 'docs/testing.md',
      content: '## Playwright E2E Tests\n\nRun `npx playwright test`.\n',
    },
  ]);

  assert.equal(violations.length, 2);
});

test('allows the optional @vitest/browser-playwright peer-dependency line in a lockfile', () => {
  const violations = findStaleReferences([
    {
      path: 'src/TechInventory.Web/pnpm-lock.yaml',
      content: "peerDependenciesMeta:\n  '@vitest/browser-playwright':\n    optional: true\n",
    },
  ]);

  assert.deepEqual(violations, []);
});

test('still flags a non-peer-dependency Playwright line inside a lockfile', () => {
  const violations = findStaleReferences([
    {
      path: 'src/TechInventory.Web/pnpm-lock.yaml',
      content: "  /@playwright/test@1.60.0:\n    resolution: {integrity: sha512-x}\n",
    },
  ]);

  assert.equal(violations.length, 1);
});

test('exempts a bare reference to the guard\'s own script filename (self-invocation, not a stale mention)', () => {
  const violations = findStaleReferences([
    {
      path: 'Taskfile.yml',
      content: 'check:stale-refs:\n  cmds:\n    - node ./scripts/check-stale-playwright-references.mjs\n    - node --test ./scripts/check-stale-playwright-references.test.mjs\n',
    },
    {
      path: '.github/workflows/README.md',
      content: '- **Stale-reference guard**: `node ./scripts/check-stale-playwright-references.mjs`\n',
    },
  ]);

  assert.deepEqual(violations, []);
});

test('still flags an actual Playwright mention on the same line as the guard filename', () => {
  const violations = findStaleReferences([
    {
      path: 'docs/testing.md',
      content: 'Run check-stale-playwright-references.mjs to make sure Playwright never comes back.\n',
    },
  ]);

  assert.equal(violations.length, 1);
});

test('exempts .squad/decisions*, .squad/agents/*/history.md, SESSION-NOTES.md, and .copilot-state.md', () => {
  const violations = findStaleReferences([
    { path: '.squad/decisions.md', content: 'D-005: Playwright browser matrix.' },
    { path: '.squad/decisions/merged/apone-t26-t33.md', content: 'Playwright fixture note.' },
    { path: '.squad/agents/apone/history.md', content: 'Wrote Playwright specs this round.' },
    { path: 'SESSION-NOTES.md', content: 'Ran the Playwright suite before it was retired.' },
    { path: '.copilot-state.md', content: 'Next: retire Playwright.' },
  ]);

  assert.deepEqual(violations, []);
});

test('exempts the named historical/work-package spec files (not a specs/ prefix)', () => {
  const violations = findStaleReferences([
    { path: 'specs/004-agentic-development-foundation/coverage-migration.md', content: 'Playwright is retired.' },
    { path: 'specs/004-agentic-development-foundation/validation.md', content: 'Playwright RBAC test covers it.' },
    { path: 'specs/001-core-api/plan.md', content: 'Playwright E2E scaffolded.' },
    { path: 'specs/002-frontend-mvp/spec.md', content: 'Playwright journey stubbed.' },
    { path: 'specs/003-pwa-polish/tasks.md', content: 'Playwright a11y pass.' },
  ]);

  assert.deepEqual(violations, []);
});

test('does NOT exempt specs/_backlog/** — regression guard for the B2 blanket-specs/ defect (validation.md §7 B2)', () => {
  const violations = findStaleReferences([
    {
      path: 'specs/_backlog/F031-merge-reference-data.md',
      content: '- [ ] Playwright journey: create two brands, assign devices to each, merge,\n',
    },
  ]);

  assert.equal(violations.length, 1);
  assert.equal(violations[0].file, 'specs/_backlog/F031-merge-reference-data.md');
});

test('does NOT exempt an arbitrary specs/ path outside the named historical allowlist', () => {
  const violations = findStaleReferences([
    { path: 'specs/001-core-api/spec.md', content: 'Playwright E2E covers this journey.' },
    { path: 'specs/005-some-future-package/plan.md', content: 'Add a Playwright suite for this.' },
  ]);

  assert.equal(violations.length, 2);
});

test('exempts the one deliberate mention in the manual PWA validation checklist', () => {
  const violations = findStaleReferences([
    {
      path: 'docs/testing/manual-pwa-validation.md',
      content: 'Playwright is retired (`brief.md` §2.1) and there is no automated browser layer.',
    },
  ]);

  assert.deepEqual(violations, []);
});

test('hard-fails if playwright.config.* exists anywhere, even with no matching content', () => {
  const violations = findStaleReferences([
    { path: 'playwright.config.ts', content: 'export default {};' },
  ]);

  assert.equal(violations.length, 1);
  assert.match(violations[0].text, /Playwright config file/);
});

test('exempts .squad/log/** and .squad/orchestration-log/** as dated historical session logs', () => {
  const violations = findStaleReferences([
    { path: '.squad/log/2026-05-18T13-32-00Z-phase0-scaffold.md', content: 'Playwright E2E (6-project matrix)' },
    { path: '.squad/orchestration-log/2026-05-18T13-18-00Z-apone.md', content: 'Playwright config: 6 projects' },
    { path: '.squad/session-log.md', content: 'Desktop Playwright suite (no edits required)' },
  ]);

  assert.deepEqual(violations, []);
});

test('does NOT exempt .env.e2e or docker-compose.e2e.yml — T104 deleted both files rather than revising them', () => {
  const violations = findStaleReferences([
    { path: '.env.e2e', content: '# Playwright fixtures sign in as this seeded admin.' },
    { path: 'docker-compose.e2e.yml', content: '#   can sign in via POST /api/v1/auth/local/login. Playwright fixtures' },
  ]);

  assert.equal(violations.length, 2);
});

test('exempts constitution.md and prd.md — amendment requires an ADR, surfaced not performed', () => {
  const violations = findStaleReferences([
    { path: '.specify/memory/constitution.md', content: '- **Playwright** is the required E2E framework — no substitutes' },
    { path: 'docs/prd.md', content: '### 7.5.3 Playwright (Mandatory E2E)' },
  ]);

  assert.deepEqual(violations, []);
});

test('does not exempt unrelated docs or agent charters — regression guard for AC-005', () => {
  const violations = findStaleReferences([
    { path: '.squad/agents/apone/charter.md', content: 'Playwright is the only E2E framework.' },
    { path: '.github/copilot-instructions.md', content: 'npx playwright install --with-deps' },
  ]);

  assert.equal(violations.length, 2);
});

test('exempts the retirement ADR by its exact path — historical evidence, not living instruction', () => {
  const violations = findStaleReferences([
    {
      path: 'docs/adr/0002-retire-browser-e2e-framework.md',
      content:
        '# 2. Retire the browser end-to-end test framework (Playwright)\n\n' +
        'Supersedes: the Playwright mandates in `.specify/memory/constitution.md`\n\n' +
        '**Playwright is retired from Tech Inventory, entirely.**\n',
    },
  ]);

  assert.deepEqual(violations, []);
});

test('does NOT exempt an unlisted/new ADR naming Playwright — regression guard against a docs/adr/** blanket exemption', () => {
  const violations = findStaleReferences([
    {
      path: 'docs/adr/0003-add-visual-regression-suite.md',
      content: '# 3. Add a visual regression suite\n\nWe will adopt Playwright for visual snapshot testing.\n',
    },
  ]);

  assert.equal(violations.length, 1);
  assert.equal(violations[0].file, 'docs/adr/0003-add-visual-regression-suite.md');
  assert.equal(violations[0].line, 3);
});

test('exempts t105-tamper-evidence.md by its exact path — historical tamper-matrix evidence, not living instruction (T105 revision, B-1)', () => {
  const violations = findStaleReferences([
    {
      path: 'specs/004-agentic-development-foundation/t105-tamper-evidence.md',
      content:
        '### 2.1 Keyword tamper\n\n' +
        '- Break: appended `<!-- tamper-test: playwright keyword probe -->` to README.md\n' +
        '- Command: `npx playwright test`\n' +
        '- Observed: exit 1 — Stale-reference guard failed: 1 active Playwright reference(s) found.\n',
    },
  ]);

  assert.deepEqual(violations, []);
});

test('does NOT exempt an unlisted/different foundation evidence file naming an active future Playwright promise — regression guard against a specs/004-agentic-development-foundation/** blanket exemption (T105 revision, B-1)', () => {
  const violations = findStaleReferences([
    {
      path: 'specs/004-agentic-development-foundation/t105-evidence-revision-probe.md',
      content: 'Follow-up: we will add a new Playwright suite next sprint to cover this gap.\n',
    },
  ]);

  assert.equal(violations.length, 1);
  assert.equal(violations[0].file, 'specs/004-agentic-development-foundation/t105-evidence-revision-probe.md');
  assert.equal(violations[0].line, 1);
});
