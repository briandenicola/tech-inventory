import { describe, it, expect } from 'vitest';
import {
	parseAuditPayload,
	computeDiff,
	deepEqual,
	formatValueForDisplay
} from './jsonDiff';

describe('parseAuditPayload', () => {
	it('returns null for null / undefined / empty string', () => {
		expect(parseAuditPayload(null)).toBeNull();
		expect(parseAuditPayload(undefined)).toBeNull();
		expect(parseAuditPayload('')).toBeNull();
	});

	it('parses valid JSON', () => {
		expect(parseAuditPayload('{"name":"iPhone"}')).toEqual({ name: 'iPhone' });
		expect(parseAuditPayload('[1,2,3]')).toEqual([1, 2, 3]);
	});

	it('returns raw string for invalid JSON', () => {
		expect(parseAuditPayload('not json')).toBe('not json');
	});
});

describe('deepEqual', () => {
	it('handles primitives', () => {
		expect(deepEqual(1, 1)).toBe(true);
		expect(deepEqual(1, 2)).toBe(false);
		expect(deepEqual('a', 'a')).toBe(true);
		expect(deepEqual(null, null)).toBe(true);
		expect(deepEqual(null, undefined)).toBe(false);
	});

	it('handles arrays', () => {
		expect(deepEqual([1, 2, 3], [1, 2, 3])).toBe(true);
		expect(deepEqual([1, 2], [1, 2, 3])).toBe(false);
		expect(deepEqual([1, 2], [2, 1])).toBe(false);
	});

	it('handles nested objects', () => {
		expect(deepEqual({ a: { b: 1 } }, { a: { b: 1 } })).toBe(true);
		expect(deepEqual({ a: { b: 1 } }, { a: { b: 2 } })).toBe(false);
		expect(deepEqual({ a: 1, b: 2 }, { b: 2, a: 1 })).toBe(true);
	});
});

describe('computeDiff', () => {
	it('returns empty diff for two nulls', () => {
		expect(computeDiff(null, null)).toEqual([]);
	});

	it('flags added field', () => {
		const diff = computeDiff({ name: 'iPhone' }, { name: 'iPhone', model: '15' });
		expect(diff).toContainEqual({
			path: 'model',
			kind: 'added',
			before: undefined,
			after: '15'
		});
	});

	it('flags removed field', () => {
		const diff = computeDiff({ name: 'iPhone', model: '15' }, { name: 'iPhone' });
		expect(diff).toContainEqual({
			path: 'model',
			kind: 'removed',
			before: '15',
			after: undefined
		});
	});

	it('flags changed field', () => {
		const diff = computeDiff({ name: 'iPhone' }, { name: 'iPhone 15' });
		expect(diff).toContainEqual({
			path: 'name',
			kind: 'changed',
			before: 'iPhone',
			after: 'iPhone 15'
		});
	});

	it('flags unchanged field', () => {
		const diff = computeDiff({ name: 'iPhone' }, { name: 'iPhone' });
		expect(diff).toContainEqual({
			path: 'name',
			kind: 'unchanged',
			before: 'iPhone',
			after: 'iPhone'
		});
	});

	it('sorts keys deterministically', () => {
		const diff = computeDiff({ z: 1, a: 1 }, { z: 1, a: 2 });
		expect(diff.map((e) => e.path)).toEqual(['a', 'z']);
	});

	it('falls back to whole-value diff for non-object before (creation)', () => {
		const diff = computeDiff(null, { name: 'iPhone' });
		expect(diff).toEqual([
			{ path: '', kind: 'added', before: undefined, after: { name: 'iPhone' } }
		]);
	});

	it('falls back to whole-value diff for non-object after (deletion)', () => {
		const diff = computeDiff({ name: 'iPhone' }, null);
		expect(diff).toEqual([
			{ path: '', kind: 'removed', before: { name: 'iPhone' }, after: undefined }
		]);
	});

	it('falls back to whole-value diff for primitive payloads', () => {
		const diff = computeDiff('old', 'new');
		expect(diff).toEqual([{ path: '', kind: 'changed', before: 'old', after: 'new' }]);
	});
});

describe('formatValueForDisplay', () => {
	it('renders null/undefined as em-dash', () => {
		expect(formatValueForDisplay(null)).toBe('—');
		expect(formatValueForDisplay(undefined)).toBe('—');
	});

	it('renders strings unquoted', () => {
		expect(formatValueForDisplay('iPhone')).toBe('iPhone');
	});

	it('renders numbers and booleans as String()', () => {
		expect(formatValueForDisplay(42)).toBe('42');
		expect(formatValueForDisplay(true)).toBe('true');
	});

	it('renders objects/arrays as JSON', () => {
		expect(formatValueForDisplay({ a: 1 })).toBe('{"a":1}');
		expect(formatValueForDisplay([1, 2])).toBe('[1,2]');
	});
});
