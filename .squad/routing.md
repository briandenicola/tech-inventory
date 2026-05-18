# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, ADRs, scope, cross-cutting code review | Ripley | "Should we use X?", new spec/plan, layer boundary disputes, reviewer-rejection routing |
| Backend / API / Domain / EF Core / MediatR | Hicks | "Add a Devices endpoint", "write the import handler", domain model changes, migrations |
| Frontend / SvelteKit / PWA / TS client / Tailwind | Vasquez | "Build the device list view", "wire MSAL on the client", design tokens, i18n, offline cache |
| Docker / Compose / Taskfile / GHA / observability / backups | Hudson | "Set up `task up`", healthchecks, prod compose, CI workflow, backup drill |
| Entra ID / OIDC / authz policies / audit / threat model / secret handling | Bishop | "Design the role policies", "review token storage", threat model updates, vuln triage |
| Tests at any level — unit, integration, contract, Playwright E2E, axe-core, Lighthouse | Apone | "Write Playwright for journey #3", "find why this test is flaky", coverage gaps, perf budgets |
| Icons, favicons, PWA icon sets, logos, brand palette, SVG illustrations, raster pipelines | Drake | "Design the app icon", "make a maskable variant", "extend the color palette", "build the README hero" |
| Code review (domain-specific) | Owning agent | Each owner reviews PRs in their domain; Ripley arbitrates cross-cutting |
| Scope & priorities, what to build next | Ripley | Trade-offs, sequencing, feature decomposition |
| Session logging, decisions merging, history archive, git commit | Scribe | Automatic — never needs routing |
| Work-queue scanning, GitHub triage loop, PR/CI watch | Ralph | "Ralph, go", "what's on the board?", merge-when-green |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
