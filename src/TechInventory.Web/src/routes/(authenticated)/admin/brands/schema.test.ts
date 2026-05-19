/**
 * Brands Schema Validation Tests — T33 Reference entity tests (partial)
 * 
 * Tests Zod validation for brand admin form (name, website, notes).
 * Full page-level tests deferred to E2E per D-124 (Zod 4.x import-time errors).
 * 
 * Constitution §3.4: Tests cover business logic; axe-core checked in E2E
 */

import { describe, it, expect } from 'vitest';
import { brandSchema } from '$lib/schemas/brand';

describe('brandSchema validation', () => {
	describe('name field', () => {
		it('accepts valid brand name', () => {
			const result = brandSchema.safeParse({ name: 'Apple', website: '', notes: '' });
			expect(result.success).toBe(true);
		});

		it('rejects empty name', () => {
			const result = brandSchema.safeParse({ name: '', website: '', notes: '' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects whitespace-only name', () => {
			const result = brandSchema.safeParse({ name: '   ', website: '', notes: '' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects name longer than 100 characters', () => {
			const result = brandSchema.safeParse({
				name: 'A'.repeat(101),
				website: '',
				notes: ''
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('100');
			}
		});

		it('accepts name exactly 100 characters', () => {
			const result = brandSchema.safeParse({
				name: 'A'.repeat(100),
				website: '',
				notes: ''
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from name', () => {
			const result = brandSchema.safeParse({ name: '  Apple  ', website: '', notes: '' });
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.name).toBe('Apple');
			}
		});
	});

	describe('website field', () => {
		it('accepts valid URL', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: 'https://www.apple.com',
				notes: ''
			});
			expect(result.success).toBe(true);
		});

		it('accepts empty website (optional)', () => {
			const result = brandSchema.safeParse({ name: 'Apple', website: '', notes: '' });
			expect(result.success).toBe(true);
		});

		it('rejects invalid URL format', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: 'not-a-url',
				notes: ''
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('valid URL');
			}
		});

		it('accepts URL with http protocol', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: 'http://example.com',
				notes: ''
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from website', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: '  https://www.apple.com  ',
				notes: ''
			});
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.website).toBe('https://www.apple.com');
			}
		});
	});

	describe('notes field', () => {
		it('accepts valid notes', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: '',
				notes: 'Premium consumer electronics brand'
			});
			expect(result.success).toBe(true);
		});

		it('accepts empty notes (optional)', () => {
			const result = brandSchema.safeParse({ name: 'Apple', website: '', notes: '' });
			expect(result.success).toBe(true);
		});

		it('rejects notes longer than 500 characters', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: '',
				notes: 'A'.repeat(501)
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('500');
			}
		});

		it('accepts notes exactly 500 characters', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: '',
				notes: 'A'.repeat(500)
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from notes', () => {
			const result = brandSchema.safeParse({
				name: 'Apple',
				website: '',
				notes: '  Some notes  '
			});
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.notes).toBe('Some notes');
			}
		});
	});
});
