import test from 'node:test';
import assert from 'node:assert/strict';
import {
  collectVulnerabilities,
  parseVulnerabilityReport,
  evaluateScanResult,
  severityAtOrAboveThreshold,
  THRESHOLD_SEVERITY,
} from './check-vulnerable.mjs';

// Fixtures below mirror the real `dotnet list package --vulnerable
// --include-transitive --format json` shape (verified against installed
// .NET SDK 10.0.204: clean run against this repo, and a throwaway project
// pinned to Newtonsoft.Json 12.0.1 for the vulnerable shape).

const CLEAN_REPORT = JSON.stringify({
  version: 1,
  parameters: '--vulnerable --include-transitive',
  sources: ['https://api.nuget.org/v3/index.json'],
  projects: [
    { path: 'C:/repo/src/TechInventory.Api/TechInventory.Api.csproj' },
    { path: 'C:/repo/src/TechInventory.Domain/TechInventory.Domain.csproj' },
  ],
});

const VULNERABLE_TOP_LEVEL_REPORT = JSON.stringify({
  version: 1,
  parameters: '--vulnerable --include-transitive',
  sources: ['https://api.nuget.org/v3/index.json'],
  projects: [
    {
      path: 'C:/repo/vulnprobe/vulnprobe.csproj',
      frameworks: [
        {
          framework: 'net10.0',
          topLevelPackages: [
            {
              id: 'Newtonsoft.Json',
              requestedVersion: '12.0.1',
              resolvedVersion: '12.0.1',
              vulnerabilities: [
                { severity: 'High', advisoryurl: 'https://github.com/advisories/GHSA-5crp-9r3c-p9vr' },
              ],
            },
          ],
        },
      ],
    },
  ],
});

const VULNERABLE_TRANSITIVE_REPORT = JSON.stringify({
  version: 1,
  parameters: '--vulnerable --include-transitive',
  sources: ['https://api.nuget.org/v3/index.json'],
  projects: [
    {
      path: 'C:/repo/src/TechInventory.Api/TechInventory.Api.csproj',
      frameworks: [
        {
          framework: 'net10.0',
          transitivePackages: [
            {
              id: 'System.Text.Json',
              resolvedVersion: '4.7.0',
              vulnerabilities: [
                { severity: 'Moderate', advisoryurl: 'https://github.com/advisories/GHSA-example' },
              ],
            },
          ],
        },
      ],
    },
  ],
});

const LOW_SEVERITY_ONLY_REPORT = JSON.stringify({
  version: 1,
  parameters: '--vulnerable --include-transitive',
  sources: ['https://api.nuget.org/v3/index.json'],
  projects: [
    {
      path: 'C:/repo/src/TechInventory.Api/TechInventory.Api.csproj',
      frameworks: [
        {
          framework: 'net10.0',
          topLevelPackages: [
            {
              id: 'Some.LowRisk.Package',
              requestedVersion: '1.0.0',
              resolvedVersion: '1.0.0',
              vulnerabilities: [{ severity: 'Low', advisoryurl: 'https://github.com/advisories/GHSA-low' }],
            },
          ],
        },
      ],
    },
  ],
});

test('severityAtOrAboveThreshold: ranks Low below the Moderate+ policy threshold, and Moderate/High/Critical at/above it', () => {
  assert.equal(severityAtOrAboveThreshold('Low', THRESHOLD_SEVERITY), false);
  assert.equal(severityAtOrAboveThreshold('Moderate', THRESHOLD_SEVERITY), true);
  assert.equal(severityAtOrAboveThreshold('High', THRESHOLD_SEVERITY), true);
  assert.equal(severityAtOrAboveThreshold('Critical', THRESHOLD_SEVERITY), true);
  assert.equal(severityAtOrAboveThreshold('CRITICAL', THRESHOLD_SEVERITY), true, 'case-insensitive');
});

test('severityAtOrAboveThreshold: an unranked/unknown severity string fails closed (treated as reportable)', () => {
  assert.equal(severityAtOrAboveThreshold('Unknown', THRESHOLD_SEVERITY), true);
  assert.equal(severityAtOrAboveThreshold(undefined, THRESHOLD_SEVERITY), true);
});

test('collectVulnerabilities: flattens both topLevelPackages and transitivePackages across projects/frameworks', () => {
  const findings = collectVulnerabilities(JSON.parse(VULNERABLE_TOP_LEVEL_REPORT));
  assert.equal(findings.length, 1);
  assert.equal(findings[0].kind, 'topLevelPackages');
  assert.equal(findings[0].id, 'Newtonsoft.Json');
  assert.equal(findings[0].severity, 'High');

  const transitiveFindings = collectVulnerabilities(JSON.parse(VULNERABLE_TRANSITIVE_REPORT));
  assert.equal(transitiveFindings.length, 1);
  assert.equal(transitiveFindings[0].kind, 'transitivePackages');
  assert.equal(transitiveFindings[0].id, 'System.Text.Json');
});

test('parseVulnerabilityReport: clean case — no projects report any vulnerabilities', () => {
  const outcome = parseVulnerabilityReport(CLEAN_REPORT);
  assert.equal(outcome.ok, true);
  assert.equal(outcome.failed, false);
  assert.deepEqual(outcome.findings, []);
});

test('parseVulnerabilityReport: vulnerable case (top-level, High) — must be flagged as failed', () => {
  const outcome = parseVulnerabilityReport(VULNERABLE_TOP_LEVEL_REPORT);
  assert.equal(outcome.ok, true);
  assert.equal(outcome.failed, true);
  assert.equal(outcome.reportable.length, 1);
  assert.equal(outcome.reportable[0].severity, 'High');
});

test('parseVulnerabilityReport: vulnerable case (transitive, Moderate) — Moderate is at the policy floor and must fail', () => {
  const outcome = parseVulnerabilityReport(VULNERABLE_TRANSITIVE_REPORT);
  assert.equal(outcome.failed, true);
  assert.equal(outcome.reportable.length, 1);
});

test('parseVulnerabilityReport: Low-severity-only findings are reported but do not fail the gate', () => {
  const outcome = parseVulnerabilityReport(LOW_SEVERITY_ONLY_REPORT);
  assert.equal(outcome.ok, true);
  assert.equal(outcome.findings.length, 1, 'still visible, not hidden');
  assert.equal(outcome.failed, false, 'Low alone must not fail the Moderate+ policy gate');
});

test('parseVulnerabilityReport: malformed JSON fails closed with a clear error, never silently treated as clean', () => {
  const outcome = parseVulnerabilityReport('{ this is not valid JSON');
  assert.equal(outcome.ok, false);
  assert.equal(outcome.failed, true);
  assert.match(outcome.error, /could not parse/);
});

test('evaluateScanResult: clean run exits 0', () => {
  const outcome = evaluateScanResult({ status: 0, stdout: CLEAN_REPORT, stderr: '' });
  assert.equal(outcome.failed, false);
  assert.equal(outcome.exitCode, 0);
});

test('evaluateScanResult: vulnerable run MUST return a non-zero exit code', () => {
  const outcome = evaluateScanResult({ status: 0, stdout: VULNERABLE_TOP_LEVEL_REPORT, stderr: '' });
  assert.equal(outcome.failed, true);
  assert.equal(outcome.exitCode, 1);
  assert.ok(outcome.messages.some((m) => /FAILED/.test(m)));
});

test('evaluateScanResult: tool-failure case (dotnet exits non-zero) fails closed rather than being read as clean', () => {
  const outcome = evaluateScanResult({ status: 1, stdout: '', stderr: 'error NU1301: Unable to load the service index' });
  assert.equal(outcome.failed, true);
  assert.equal(outcome.exitCode, 1);
  assert.ok(outcome.messages.some((m) => /exited 1/.test(m)));
});

test('evaluateScanResult: tool-failure case (dotnet could not be launched at all) fails closed', () => {
  const outcome = evaluateScanResult({ error: new Error('ENOENT: dotnet not found') });
  assert.equal(outcome.failed, true);
  assert.equal(outcome.exitCode, 1);
  assert.ok(outcome.messages.some((m) => /failed to launch/.test(m)));
});

test('evaluateScanResult: malformed JSON output from an otherwise-successful dotnet run fails closed', () => {
  const outcome = evaluateScanResult({ status: 0, stdout: 'not json at all', stderr: '' });
  assert.equal(outcome.failed, true);
  assert.equal(outcome.exitCode, 1);
  assert.ok(outcome.messages.some((m) => /could not parse/.test(m)));
});
