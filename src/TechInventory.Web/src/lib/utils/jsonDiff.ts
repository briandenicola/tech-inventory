/**
 * jsonDiff.ts — Minimal in-house JSON diff for the F021 audit log drawer.
 *
 * Computes a flat, dot-pathed list of {added | removed | changed | unchanged}
 * entries between two parsed JSON payloads. Designed for AuditEvent BEFORE /
 * AFTER snapshots, which are small (one entity's worth of fields), so we
 * intentionally avoid pulling in a heavier dependency like `jsondiffpatch`
 * or `deep-object-diff` (constitution §2 — keep deps tight).
 *
 * Limitations (explicit by design):
 * - Arrays are compared as whole values via deepEqual — no per-index move
 *   detection. Audit payloads for our domain don't carry meaningful arrays
 *   today; revisit if that changes.
 * - Non-object payloads fall back to a single whole-value entry.
 */
export type DiffKind = 'added' | 'removed' | 'changed' | 'unchanged';

export interface DiffEntry {
	path: string;
	kind: DiffKind;
	before: unknown;
	after: unknown;
}

/**
 * Parse a raw audit payload string into a JSON value.
 * Returns `null` for null/empty input and gracefully treats non-JSON strings
 * as their raw string value so legacy/text audit rows still render something.
 */
export function parseAuditPayload(raw: string | null | undefined): unknown {
	if (raw === null || raw === undefined || raw === '') return null;
	try {
		return JSON.parse(raw);
	} catch {
		return raw;
	}
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
	return (
		value !== null &&
		typeof value === 'object' &&
		!Array.isArray(value) &&
		Object.getPrototypeOf(value) === Object.prototype
	);
}

export function deepEqual(a: unknown, b: unknown): boolean {
	if (a === b) return true;
	if (a === null || b === null) return false;
	if (typeof a !== typeof b) return false;
	if (Array.isArray(a) && Array.isArray(b)) {
		if (a.length !== b.length) return false;
		for (let i = 0; i < a.length; i++) {
			if (!deepEqual(a[i], b[i])) return false;
		}
		return true;
	}
	if (isPlainObject(a) && isPlainObject(b)) {
		const aKeys = Object.keys(a);
		const bKeys = Object.keys(b);
		if (aKeys.length !== bKeys.length) return false;
		for (const k of aKeys) {
			if (!Object.prototype.hasOwnProperty.call(b, k)) return false;
			if (!deepEqual(a[k], b[k])) return false;
		}
		return true;
	}
	return false;
}

/**
 * Compute a flat diff between two parsed payloads.
 *
 * - If either side is not a plain object, returns a single whole-value entry
 *   keyed at path "" so the drawer can still show something sensible.
 * - Otherwise walks the union of keys (sorted) and emits one entry per key.
 */
export function computeDiff(before: unknown, after: unknown): DiffEntry[] {
	if (!isPlainObject(before) || !isPlainObject(after)) {
		if (before === null && after === null) return [];
		if (deepEqual(before, after)) {
			return [{ path: '', kind: 'unchanged', before, after }];
		}
		if (before === null || before === undefined) {
			return [{ path: '', kind: 'added', before: undefined, after }];
		}
		if (after === null || after === undefined) {
			return [{ path: '', kind: 'removed', before, after: undefined }];
		}
		return [{ path: '', kind: 'changed', before, after }];
	}

	const keys = Array.from(new Set([...Object.keys(before), ...Object.keys(after)])).sort();
	const entries: DiffEntry[] = [];
	for (const key of keys) {
		const inBefore = Object.prototype.hasOwnProperty.call(before, key);
		const inAfter = Object.prototype.hasOwnProperty.call(after, key);
		const b = before[key];
		const a = after[key];
		if (inBefore && !inAfter) {
			entries.push({ path: key, kind: 'removed', before: b, after: undefined });
		} else if (!inBefore && inAfter) {
			entries.push({ path: key, kind: 'added', before: undefined, after: a });
		} else if (deepEqual(b, a)) {
			entries.push({ path: key, kind: 'unchanged', before: b, after: a });
		} else {
			entries.push({ path: key, kind: 'changed', before: b, after: a });
		}
	}
	return entries;
}

/**
 * Render a JSON value for compact display in the diff table.
 * Strings unquoted, null/undefined as em-dash, objects/arrays JSON-stringified.
 */
export function formatValueForDisplay(value: unknown): string {
	if (value === null || value === undefined) return '—';
	if (typeof value === 'string') return value;
	if (typeof value === 'number' || typeof value === 'boolean') return String(value);
	try {
		return JSON.stringify(value);
	} catch {
		return String(value);
	}
}
