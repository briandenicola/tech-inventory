/**
 * headerStacking.test.ts — F045 B1 regression guard.
 *
 * Locks in the closed-state header z-index at `--z-fixed` (30), not
 * `--z-sticky` (20). Dropping to 20 ties with the `sticky top-[73px] z-20`
 * sub-header present on `devices` and six `admin/*` pages; because those
 * sub-headers are later in DOM order, an equal z-index hands them the
 * stacking tie and the desktop user-menu dropdown paints beneath them.
 */
import { describe, it, expect } from 'vitest';
import { headerZIndexToken } from './headerStacking';

describe('headerZIndexToken (F045 B1)', () => {
	it('closed state resolves to --z-fixed (30), not --z-sticky (20)', () => {
		expect(headerZIndexToken(false)).toBe('var(--z-fixed)');
		expect(headerZIndexToken(false)).not.toBe('var(--z-sticky)');
	});

	it('open state raises to --z-popover (60) so the popover panel clears the sticky sub-header and bottom nav', () => {
		expect(headerZIndexToken(true)).toBe('var(--z-popover)');
	});
});
