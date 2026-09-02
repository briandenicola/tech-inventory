/**
 * Locations Schema Validation Tests — T33 Reference entity tests (partial)
 * 
 * Tests Zod validation for location admin form (name, type enum, notes).
 *
 * C-22 (was: "Full page-level tests deferred to E2E per D-124, Zod 4.x
 * import-time errors"): re-verified against the currently installed Zod
 * version — the page module now imports cleanly in Vitest (see the
 * "page module" smoke test below), so D-124 no longer applies. Coverage
 * stays schema-focused here (full page interaction is out of scope for this
 * file) but the import-time regression is now guarded against directly.
 * 
 * Constitution §3.4: Tests cover business logic; axe-core checked elsewhere
 */

import { describe, it, expect } from 'vitest';
import { locationSchema } from '$lib/schemas/location';

describe('locationSchema validation', () => {
	describe('name field', () => {
		it('accepts valid location name', () => {
			const result = locationSchema.safeParse({ name: 'Living Room', type: 'Home', notes: '' });
			expect(result.success).toBe(true);
		});

		it('rejects empty name', () => {
			const result = locationSchema.safeParse({ name: '', type: 'Home', notes: '' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects whitespace-only name', () => {
			const result = locationSchema.safeParse({ name: '   ', type: 'Home', notes: '' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects name longer than 100 characters', () => {
			const result = locationSchema.safeParse({
				name: 'A'.repeat(101),
				type: 'Home',
				notes: ''
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('100');
			}
		});

		it('accepts name exactly 100 characters', () => {
			const result = locationSchema.safeParse({
				name: 'A'.repeat(100),
				type: 'Home',
				notes: ''
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from name', () => {
			const result = locationSchema.safeParse({
				name: '  Living Room  ',
				type: 'Home',
				notes: ''
			});
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.name).toBe('Living Room');
			}
		});
	});

	describe('type field (enum)', () => {
		it('accepts Home type', () => {
			const result = locationSchema.safeParse({ name: 'Living Room', type: 'Home', notes: '' });
			expect(result.success).toBe(true);
		});

		it('accepts Storage type', () => {
			const result = locationSchema.safeParse({ name: 'Garage', type: 'Storage', notes: '' });
			expect(result.success).toBe(true);
		});

		it('accepts External type', () => {
			const result = locationSchema.safeParse({
				name: 'Office',
				type: 'External',
				notes: ''
			});
			expect(result.success).toBe(true);
		});

		it('rejects invalid type', () => {
			const result = locationSchema.safeParse({
				name: 'Living Room',
				type: 'InvalidType',
				notes: ''
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('Invalid option');
			}
		});

		it('rejects empty type', () => {
			const result = locationSchema.safeParse({
				name: 'Living Room',
				type: '',
				notes: ''
			});
			expect(result.success).toBe(false);
		});
	});

	describe('notes field', () => {
		it('accepts valid notes', () => {
			const result = locationSchema.safeParse({
				name: 'Living Room',
				type: 'Home',
				notes: 'Main living area with entertainment center'
			});
			expect(result.success).toBe(true);
		});

		it('accepts empty notes (optional)', () => {
			const result = locationSchema.safeParse({ name: 'Living Room', type: 'Home', notes: '' });
			expect(result.success).toBe(true);
		});

		it('rejects notes longer than 500 characters', () => {
			const result = locationSchema.safeParse({
				name: 'Living Room',
				type: 'Home',
				notes: 'A'.repeat(501)
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('500');
			}
		});

		it('accepts notes exactly 500 characters', () => {
			const result = locationSchema.safeParse({
				name: 'Living Room',
				type: 'Home',
				notes: 'A'.repeat(500)
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from notes', () => {
			const result = locationSchema.safeParse({
				name: 'Living Room',
				type: 'Home',
				notes: '  Some notes  '
			});
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.notes).toBe('Some notes');
			}
		});
	});
});


describe('page module (C-22 regression guard)', () => {
	it('imports the admin page module without throwing (guards against D-124 recurring)', async () => {
		await expect(import('./+page.svelte')).resolves.toBeTruthy();
	}, 20000);
});
