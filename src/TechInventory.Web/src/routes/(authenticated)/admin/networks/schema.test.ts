/**
 * Networks Schema Validation Tests — T33 Reference entity tests (partial)
 * 
 * Tests Zod validation for network admin form (name, description).
 * Full page-level tests deferred to E2E per D-124 (Zod 4.x import-time errors).
 * 
 * Constitution §3.4: Tests cover business logic; axe-core checked in E2E
 */

import { describe, it, expect } from 'vitest';
import { networkSchema } from '$lib/schemas/network';

describe('networkSchema validation', () => {
	describe('name field', () => {
		it('accepts valid network name', () => {
			const result = networkSchema.safeParse({ name: 'Home WiFi', description: '' });
			expect(result.success).toBe(true);
		});

		it('rejects empty name', () => {
			const result = networkSchema.safeParse({ name: '', description: '' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects whitespace-only name', () => {
			const result = networkSchema.safeParse({ name: '   ', description: '' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects name longer than 100 characters', () => {
			const result = networkSchema.safeParse({
				name: 'A'.repeat(101),
				description: ''
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('100');
			}
		});

		it('accepts name exactly 100 characters', () => {
			const result = networkSchema.safeParse({
				name: 'A'.repeat(100),
				description: ''
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from name', () => {
			const result = networkSchema.safeParse({ name: '  Home WiFi  ', description: '' });
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.name).toBe('Home WiFi');
			}
		});
	});

	describe('description field', () => {
		it('accepts valid description', () => {
			const result = networkSchema.safeParse({
				name: 'Home WiFi',
				description: 'Primary household wireless network'
			});
			expect(result.success).toBe(true);
		});

		it('accepts empty description (optional)', () => {
			const result = networkSchema.safeParse({ name: 'Home WiFi', description: '' });
			expect(result.success).toBe(true);
		});

		it('rejects description longer than 500 characters', () => {
			const result = networkSchema.safeParse({
				name: 'Home WiFi',
				description: 'A'.repeat(501)
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('500');
			}
		});

		it('accepts description exactly 500 characters', () => {
			const result = networkSchema.safeParse({
				name: 'Home WiFi',
				description: 'A'.repeat(500)
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from description', () => {
			const result = networkSchema.safeParse({
				name: 'Home WiFi',
				description: '  Some description  '
			});
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.description).toBe('Some description');
			}
		});
	});
});
