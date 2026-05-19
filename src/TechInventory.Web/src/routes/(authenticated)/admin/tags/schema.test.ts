/**
 * Tags Schema Validation Tests — T33 Reference entity tests (partial)
 * 
 * Tests Zod validation for tag admin form (name, color hex).
 * Full page-level tests deferred to E2E per D-124 (Zod 4.x import-time errors).
 * 
 * Constitution §3.4: Tests cover business logic; axe-core checked in E2E
 */

import { describe, it, expect } from 'vitest';
import { tagSchema, TAG_PRESET_COLORS } from '$lib/schemas/tag';

describe('tagSchema validation', () => {
	describe('name field', () => {
		it('accepts valid tag name', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '#EF4444' });
			expect(result.success).toBe(true);
		});

		it('rejects empty name', () => {
			const result = tagSchema.safeParse({ name: '', color: '#EF4444' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects whitespace-only name', () => {
			const result = tagSchema.safeParse({ name: '   ', color: '#EF4444' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects name longer than 50 characters', () => {
			const result = tagSchema.safeParse({
				name: 'A'.repeat(51),
				color: '#EF4444'
			});
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('50');
			}
		});

		it('accepts name exactly 50 characters', () => {
			const result = tagSchema.safeParse({
				name: 'A'.repeat(50),
				color: '#EF4444'
			});
			expect(result.success).toBe(true);
		});

		it('trims whitespace from name', () => {
			const result = tagSchema.safeParse({ name: '  Critical  ', color: '#EF4444' });
			expect(result.success).toBe(true);
			if (result.success) {
				expect(result.data.name).toBe('Critical');
			}
		});
	});

	describe('color field (hex)', () => {
		it('accepts valid 6-digit hex color', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '#EF4444' });
			expect(result.success).toBe(true);
		});

		it('accepts lowercase hex color', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '#ef4444' });
			expect(result.success).toBe(true);
		});

		it('accepts mixed-case hex color', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '#Ef4444' });
			expect(result.success).toBe(true);
		});

		it('rejects hex color without # prefix', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: 'EF4444' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('valid color');
			}
		});

		it('rejects 3-digit hex color (shorthand)', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '#F44' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('valid color');
			}
		});

		it('rejects 8-digit hex color (with alpha)', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '#EF4444FF' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('valid color');
			}
		});

		it('rejects invalid characters in hex', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '#GGGGGG' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('valid color');
			}
		});

		it('rejects empty color', () => {
			const result = tagSchema.safeParse({ name: 'Critical', color: '' });
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('valid color');
			}
		});

		it('accepts all preset colors', () => {
			TAG_PRESET_COLORS.forEach((color) => {
				const result = tagSchema.safeParse({ name: 'Critical', color });
				expect(result.success).toBe(true);
			});
		});
	});

	describe('TAG_PRESET_COLORS constant', () => {
		it('exports 8 preset colors', () => {
			expect(TAG_PRESET_COLORS).toHaveLength(8);
		});

		it('all preset colors are valid hex format', () => {
			TAG_PRESET_COLORS.forEach((color) => {
				expect(color).toMatch(/^#[0-9A-Fa-f]{6}$/);
			});
		});

		it('preset colors are unique', () => {
			const uniqueColors = new Set(TAG_PRESET_COLORS);
			expect(uniqueColors.size).toBe(TAG_PRESET_COLORS.length);
		});
	});
});
