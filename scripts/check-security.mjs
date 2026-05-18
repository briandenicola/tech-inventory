#!/usr/bin/env node

import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { execFileSync, spawnSync } from 'node:child_process';

const initialCwd = process.cwd();
const tokenStoragePattern = "localStorage\\s*\\.(set|get|remove)Item\\s*\\(\\s*['\"`][^'\"`]*?(token|jwt|access|refresh|id_token|msal)";
const cliArgs = process.argv.slice(2);
const args = new Set(cliArgs);
const diffRangeIndex = cliArgs.indexOf('--diff-range');
const diffRange = diffRangeIndex === -1 ? null : cliArgs[diffRangeIndex + 1];
const mode = args.has('--staged') ? 'staged' : diffRange ? 'diff' : args.has('--repo') ? 'repo' : null;
const scanLabel = mode === 'diff' ? `diff ${diffRange}` : mode;

if (!mode) {
  console.error('Usage: node scripts/check-security.mjs [--staged|--repo|--diff-range <git-range>]');
  process.exit(1);
}

if (diffRangeIndex !== -1 && !diffRange) {
  console.error('Missing value for --diff-range.');
  process.exit(1);
}

const repoRoot = runGit(['rev-parse', '--show-toplevel']).trim();
process.chdir(repoRoot);

const files = mode === 'staged'
  ? listStagedFiles()
  : mode === 'diff'
    ? listDiffFiles(diffRange)
    : listTrackedFiles();
if (files.length === 0) {
  console.log(`Security scan (${scanLabel}): no files to inspect.`);
  process.exit(0);
}

const gitleaksBinary = resolveGitleaksBinary(repoRoot);
const gitleaksAvailabilityError = verifyGitleaksBinary(gitleaksBinary, repoRoot);
if (gitleaksAvailabilityError !== null) {
  console.error(gitleaksAvailabilityError);
  process.exit(1);
}

const tokenViolations = [];
const secretViolations = [];

for (const file of files) {
  const content = readContent(mode, file);
  if (content === null) {
    continue;
  }

  tokenViolations.push(...scanTokenStorage(file, content));

  const secretViolation = scanSecrets(file, content, gitleaksBinary, repoRoot);
  if (secretViolation !== null) {
    secretViolations.push(secretViolation);
  }
}

if (tokenViolations.length === 0 && secretViolations.length === 0) {
  console.log(`Security scan (${scanLabel}) passed for ${files.length} file(s).`);
  process.exit(0);
}

console.error(`Security scan (${scanLabel}) failed.`);

if (tokenViolations.length > 0) {
  console.error('');
  console.error('Blocked auth token persistence in localStorage:');
  for (const violation of tokenViolations) {
    console.error(`- ${violation.file}:${violation.line}`);
    console.error(`  ${violation.text}`);
  }
}

if (secretViolations.length > 0) {
  console.error('');
  console.error('Possible secrets detected by gitleaks:');
  for (const violation of secretViolations) {
    console.error(`- ${violation.file}`);
    if (violation.output.length > 0) {
      console.error(indent(violation.output.trim(), '  '));
    }
  }
}

process.exit(1);

function listStagedFiles() {
  return splitNullSeparated(runGit(['diff', '--cached', '--name-only', '--diff-filter=ACMR', '-z']));
}

function listTrackedFiles() {
  return splitNullSeparated(runGit(['ls-files', '-z']));
}

function listDiffFiles(range) {
  return splitNullSeparated(runGit(['diff', '--name-only', '--diff-filter=ACMR', '-z', range]));
}

function splitNullSeparated(output) {
  return output.split('\u0000').filter(Boolean);
}

function readContent(modeName, file) {
  let buffer;

  if (modeName === 'staged') {
    buffer = runGitBuffer(['show', `:${file}`]);
  } else {
    try {
      buffer = fs.readFileSync(path.join(repoRoot, file));
    } catch (error) {
      if (error?.code === 'ENOENT') {
        return null;
      }

      throw error;
    }
  }

  if (buffer.includes(0)) {
    return null;
  }

  return buffer.toString('utf8');
}

function scanTokenStorage(file, content) {
  const matches = [];
  const regex = new RegExp(tokenStoragePattern, 'gi');

  for (const match of content.matchAll(regex)) {
    const index = match.index ?? 0;
    const { line, text } = findLine(content, index);
    matches.push({ file, line, text });
  }

  return matches;
}

function scanSecrets(file, content, binary, cwd) {
  const result = spawnSync(binary, ['stdin', '--config', '.gitleaks.toml', '--no-banner', '--redact=100', '--log-level', 'error'], {
    cwd,
    encoding: 'utf8',
    input: content,
    maxBuffer: 10 * 1024 * 1024,
  });

  if (result.error) {
    return {
      file,
      output: result.error.message,
    };
  }

  if (result.status === 0) {
    return null;
  }

  return {
    file,
    output: `${result.stdout}${result.stderr}`.trim() || 'gitleaks detected a possible secret in this file.',
  };
}

function verifyGitleaksBinary(binary, cwd) {
  const result = spawnSync(binary, ['version'], {
    cwd,
    encoding: 'utf8',
  });

  if (result.error?.code === 'ENOENT') {
    return 'gitleaks is not installed. Run task hooks:install to download the pinned binary and wire the hook.';
  }

  if (result.error) {
    return result.error.message;
  }

  return null;
}

function findLine(content, index) {
  const lineStart = content.lastIndexOf('\n', index - 1) + 1;
  const lineEnd = content.indexOf('\n', index);
  const lineNumber = content.slice(0, index).split('\n').length;
  const lineText = content.slice(lineStart, lineEnd === -1 ? content.length : lineEnd).trim();

  return {
    line: lineNumber,
    text: lineText,
  };
}

function indent(text, prefix) {
  return text
    .split(/\r?\n/)
    .map((line) => `${prefix}${line}`)
    .join('\n');
}

function resolveGitleaksBinary(root) {
  if (process.env.GITLEAKS_BIN) {
    return process.env.GITLEAKS_BIN;
  }

  const bundledBinary = path.join(root, '.tools', 'gitleaks', process.platform === 'win32' ? 'gitleaks.exe' : 'gitleaks');
  if (fs.existsSync(bundledBinary)) {
    return bundledBinary;
  }

  return 'gitleaks';
}

function runGit(args) {
  return execFileSync('git', args, {
    cwd: initialCwd,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
}

function runGitBuffer(args) {
  return execFileSync('git', args, {
    cwd: repoRoot,
    encoding: 'buffer',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
}
