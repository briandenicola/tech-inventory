#!/usr/bin/env node
//
// Fail-closed .NET vulnerability scan (specs/004-agentic-development-
// foundation, T104 revision · B-2).
//
// `dotnet list package --vulnerable --include-transitive` (the command
// T104's Taskfile used verbatim) prints a human-readable table of advisories
// but exits 0 regardless of what it finds — there is nothing downstream that
// parses its output, so this stage of the pipeline could never fail no
// matter how severe the finding (Apone's T104 review, B-2; proven with a
// throwaway project pinned to Newtonsoft.Json 12.0.1).
//
// This script instead runs the same command with `--format json` — a
// stable, versioned machine-readable output the installed .NET SDK
// (10.0.204 here) supports directly, no extra tooling required — and parses
// the documented `projects[].frameworks[].{topLevelPackages,
// transitivePackages}[].vulnerabilities[]` shape. It fails closed:
//
//   - any advisory at or above the policy severity threshold -> exit 1
//   - the `dotnet` command failing to run, or exiting non-zero -> exit 1
//     (a failed scan is not a clean scan)
//   - output that isn't valid JSON -> exit 1 (never silently treated as "no
//     vulnerabilities found")
//
// Policy threshold: constitution.md §5.8 — "`dotnet list package
// --vulnerable` clean (no Moderate+)". Low-severity-only findings are still
// printed (so nothing is hidden) but do not fail the gate; Moderate, High,
// and Critical do. No project-specific threshold exists, so this is the
// exact rule already on the books, not an invented exception.
//
// Usage: node scripts/check-vulnerable.mjs [extra dotnet-list-package args]

import { spawnSync } from 'node:child_process';
import path from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';

const SEVERITY_RANK = { low: 0, moderate: 1, high: 2, critical: 3 };
export const THRESHOLD_SEVERITY = 'moderate';

// True if `severity` is unranked (fails closed) or ranks at/above `threshold`.
export function severityAtOrAboveThreshold(severity, threshold = THRESHOLD_SEVERITY) {
  const rank = SEVERITY_RANK[String(severity).toLowerCase()];
  const thresholdRank = SEVERITY_RANK[String(threshold).toLowerCase()];
  if (thresholdRank === undefined) {
    throw new Error(`check:vulnerable: unknown threshold severity '${threshold}'`);
  }
  if (rank === undefined) {
    return true; // an advisory we can't rank is not one we can safely ignore
  }
  return rank >= thresholdRank;
}

// Walks the documented `dotnet list package --vulnerable --format json`
// shape and flattens every reported vulnerability, from both top-level and
// transitive packages, across every project and target framework.
export function collectVulnerabilities(report) {
  const findings = [];
  const projects = Array.isArray(report?.projects) ? report.projects : [];

  for (const project of projects) {
    const frameworks = Array.isArray(project?.frameworks) ? project.frameworks : [];
    for (const framework of frameworks) {
      const groups = [
        ['topLevelPackages', framework?.topLevelPackages],
        ['transitivePackages', framework?.transitivePackages],
      ];
      for (const [kind, packages] of groups) {
        for (const pkg of Array.isArray(packages) ? packages : []) {
          for (const vuln of Array.isArray(pkg?.vulnerabilities) ? pkg.vulnerabilities : []) {
            findings.push({
              project: project?.path ?? '<unknown project>',
              framework: framework?.framework ?? '<unknown framework>',
              kind,
              id: pkg?.id ?? '<unknown package>',
              resolvedVersion: pkg?.resolvedVersion ?? '<unknown version>',
              severity: vuln?.severity ?? '<unknown severity>',
              advisoryUrl: vuln?.advisoryurl ?? vuln?.advisoryUrl ?? '<no advisory url>',
            });
          }
        }
      }
    }
  }

  return findings;
}

// Parses the raw stdout of `dotnet list package --vulnerable --format json`.
// Never throws for malformed input — a parse failure is reported as a
// failed, unparseable scan (`ok: false`), not swallowed into a clean result.
export function parseVulnerabilityReport(stdout, { threshold = THRESHOLD_SEVERITY } = {}) {
  let report;
  try {
    report = JSON.parse(stdout);
  } catch (error) {
    return {
      ok: false,
      failed: true,
      findings: [],
      reportable: [],
      error:
        "check:vulnerable: could not parse 'dotnet list package --vulnerable --format json' output as JSON " +
        `(${error.message}). Treating this as a failed scan, not a clean one.`,
    };
  }

  const findings = collectVulnerabilities(report);
  const reportable = findings.filter((finding) => severityAtOrAboveThreshold(finding.severity, threshold));

  return { ok: true, failed: reportable.length > 0, findings, reportable, error: null };
}

function formatFinding(finding) {
  return (
    `  ${String(finding.severity).toUpperCase()}  ${finding.id} ${finding.resolvedVersion} ` +
    `(${finding.kind}, ${finding.framework}) — ${finding.advisoryUrl}\n    ${finding.project}`
  );
}

// Evaluates an already-completed `dotnet list package` invocation (a
// spawnSync-shaped result: { status, stdout, stderr, error }) into a
// pass/fail outcome. Kept separate from the actual process spawn so it can
// be unit tested against clean/vulnerable/malformed/tool-failure fixtures
// without needing dotnet or network access.
export function evaluateScanResult(result, { threshold = THRESHOLD_SEVERITY } = {}) {
  if (result?.error) {
    return {
      failed: true,
      exitCode: 1,
      messages: [`check:vulnerable: failed to launch 'dotnet': ${result.error.message}`],
    };
  }

  if (typeof result?.status !== 'number' || result.status !== 0) {
    const messages = [
      `check:vulnerable: 'dotnet list package --vulnerable --format json' exited ` +
        `${result?.status ?? 'with no status'} — treating as a failed scan, not a clean one.`,
    ];
    if (result?.stderr) messages.push(result.stderr);
    return { failed: true, exitCode: 1, messages };
  }

  const outcome = parseVulnerabilityReport(result.stdout ?? '', { threshold });

  if (!outcome.ok) {
    return { failed: true, exitCode: 1, messages: [outcome.error] };
  }

  const messages = [];
  if (outcome.findings.length > 0) {
    messages.push(`check:vulnerable: ${outcome.findings.length} advisory(ies) reported (all severities):`);
    messages.push(...outcome.findings.map(formatFinding));
  }

  if (outcome.failed) {
    messages.push(
      `check:vulnerable: FAILED — ${outcome.reportable.length} advisory(ies) at or above the policy ` +
        `threshold (${threshold}+, constitution.md §5.8).`,
    );
    return { failed: true, exitCode: 1, messages };
  }

  messages.push(`check:vulnerable: passed — no advisories at or above the policy threshold (${threshold}+).`);
  return { failed: false, exitCode: 0, messages };
}

function runDotnetListPackage(extraArgs) {
  return spawnSync(
    'dotnet',
    ['list', ...extraArgs, 'package', '--vulnerable', '--include-transitive', '--format', 'json'],
    { encoding: 'utf8' },
  );
}

function main() {
  const extraArgs = process.argv.slice(2);
  const result = runDotnetListPackage(extraArgs);
  const outcome = evaluateScanResult(result);

  for (const message of outcome.messages) {
    (outcome.failed ? console.error : console.log)(message);
  }

  process.exit(outcome.exitCode);
}

const isMain = process.argv[1] && fileURLToPath(import.meta.url) === path.resolve(process.argv[1]);
if (isMain) {
  main();
}
