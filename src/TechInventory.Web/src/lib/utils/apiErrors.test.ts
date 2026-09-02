import { describe, it, expect } from 'vitest';
import { ApiError } from '$lib/api/client';
import { getApiErrorMessage, mapApiFieldErrors } from './apiErrors';

const FALLBACK = 'Failed to save device';

describe('getApiErrorMessage', () => {
	it('prefers the ProblemDetails detail when the server sent one', () => {
		const err = new ApiError(409, 'Conflict', 'A device with that serial already exists.');
		expect(getApiErrorMessage(err, FALLBACK)).toBe('A device with that serial already exists.');
	});

	// Regression: `detail` is a constructor parameter property, so the own
	// property exists on every ApiError even when undefined. The old
	// `'detail' in err ? err.detail : fallback` check therefore always chose
	// `undefined` and rendered a blank red toast.
	it('falls through to the Error message when detail is absent', () => {
		const err = new ApiError(400, 'Bad Request');
		// Asserting the exact trap the lint rule exists to prevent: the own
		// property is always present, so the check tells you nothing.
		// eslint-disable-next-line no-restricted-syntax
		expect('detail' in err).toBe(true);
		expect(err.detail).toBeUndefined();
		expect(getApiErrorMessage(err, FALLBACK)).toBe('Bad Request');
	});

	it('falls through when detail is present but blank', () => {
		const err = new ApiError(400, 'Bad Request', '   ');
		expect(getApiErrorMessage(err, FALLBACK)).toBe('Bad Request');
	});

	it('surfaces a field-level validation message when there is no detail', () => {
		const err = new ApiError(400, '', undefined, '/api/v1/devices', {
			Name: ['Name is required.']
		});
		expect(getApiErrorMessage(err, FALLBACK)).toBe('Name is required.');
	});

	it('uses the fallback when nothing usable is present', () => {
		const err = new ApiError(500, '');
		expect(getApiErrorMessage(err, FALLBACK)).toBe(FALLBACK);
	});

	it('handles a plain Error, such as an offline fetch rejection', () => {
		expect(getApiErrorMessage(new Error('Failed to fetch'), FALLBACK)).toBe('Failed to fetch');
	});

	it('handles non-Error throws without returning undefined', () => {
		expect(getApiErrorMessage(undefined, FALLBACK)).toBe(FALLBACK);
		expect(getApiErrorMessage(null, FALLBACK)).toBe(FALLBACK);
		expect(getApiErrorMessage({}, FALLBACK)).toBe(FALLBACK);
		expect(getApiErrorMessage('boom', FALLBACK)).toBe('boom');
	});

	it('never returns a blank string', () => {
		const cases: unknown[] = [
			new ApiError(400, ''),
			new ApiError(400, '', ''),
			new Error(''),
			{ detail: '', message: '' }
		];
		for (const err of cases) {
			expect(getApiErrorMessage(err, FALLBACK).trim().length).toBeGreaterThan(0);
		}
	});
});

describe('mapApiFieldErrors', () => {
	it('camelCases PascalCase keys and takes the first message', () => {
		expect(mapApiFieldErrors({ OwnerId: ['Owner is required.', 'Ignored.'] })).toEqual({
			ownerId: 'Owner is required.'
		});
	});

	it('returns an empty map for undefined', () => {
		expect(mapApiFieldErrors(undefined)).toEqual({});
	});
});
