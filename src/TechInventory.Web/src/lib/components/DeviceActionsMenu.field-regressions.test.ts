/**
 * DeviceActionsMenu — field regression guards (#165).
 *
 * Issue: The action menu dropdown can be clipped or trapped below stacking
 * contexts created by sibling/group elements. Two source contracts guard
 * against this:
 *
 *   1. Tokenized z-index: the dropdown div must carry
 *      `style="z-index: var(--z-dropdown);"` so it renders above all
 *      sibling content including the DevicePwaRow's relative-positioned
 *      stacking context and group headers.
 *
 *   2. Root container overflow: the DeviceActionsMenu wrapper div must NOT
 *      carry `overflow-hidden` (which would clip absolutely-positioned
 *      children regardless of z-index).
 *
 *   3. compact prop: `compact=true` renders a ghost icon trigger (no border,
 *      no background circle) for inline row/list contexts; `compact=false`
 *      (default) retains the bordered circle used in DeviceDetailModal.
 *
 * TAMPER-TESTED: All guards are currently met by the implementation.
 * Removing `style="z-index: var(--z-dropdown);"` from the dropdown div, or
 * adding `overflow-hidden` to the root container, causes the respective test
 * to fail.  Restoring the source makes them pass again.
 *
 * jsdom LIMITATION: jsdom cannot verify that a CSS variable resolves to a
 * value higher than sibling stacking contexts, or that `overflow-hidden` on
 * the DevicePwaList container actually clips the menu.  These tests assert
 * the structural contracts that prevent the clipping bug; the real-device
 * verification lives in the manual-pwa-validation.md checklist (M-22 to
 * M-24).
 */
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import DeviceActionsMenu from './DeviceActionsMenu.svelte';

describe('DeviceActionsMenu — #165 tokenized z-index + overflow contract', () => {
	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The open dropdown must carry `z-index: var(--z-dropdown)` as an inline
	// style so the design-system token controls its layer. A Tailwind z-class
	// alone (e.g. z-10) cannot reference the token and so would not satisfy
	// the tokenized-layer contract.
	it('open dropdown carries z-index: var(--z-dropdown) as an inline style', async () => {
		const user = userEvent.setup();
		render(DeviceActionsMenu, { props: { editHref: '/devices/d1/edit' } });

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		const menu = screen.getByRole('menu');
		const style = menu.getAttribute('style') ?? '';
		expect(
			style,
			'dropdown [role="menu"] must carry z-index: var(--z-dropdown) as an inline style; ' +
				'a Tailwind z-class alone does not satisfy the tokenized-layer contract'
		).toMatch(/z-index\s*:\s*var\(--z-dropdown\)/);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The outermost wrapper div (the component root) must be `relative` and
	// must NOT carry `overflow-hidden`. overflow:hidden clips absolutely-
	// positioned children regardless of their z-index, which is the root cause
	// of the #165 menu-clipping regression.
	it('component root wrapper does not carry overflow-hidden', () => {
		const { container } = render(DeviceActionsMenu, {
			props: { editHref: '/devices/d1/edit' }
		});

		// The root element is the div[class="relative"] that wraps trigger + menu.
		const root = container.firstElementChild as HTMLElement | null;
		expect(root, 'DeviceActionsMenu must render a root element').toBeTruthy();
		expect(
			root!.className,
			'root wrapper must not carry overflow-hidden; that clips the absolute dropdown'
		).not.toMatch(/\boverflow-hidden\b/);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The trigger button must not itself be wrapped in or carry overflow-hidden.
	it('trigger button is not inside an overflow-hidden ancestor within the component', async () => {
		const { container } = render(DeviceActionsMenu, {
			props: { editHref: '/devices/d1/edit' }
		});
		const trigger = screen.getByRole('button', { name: /more actions/i });
		// Walk up to the component root — none of the ancestors within the
		// component shadow should carry overflow-hidden.
		let node: Element | null = trigger.parentElement;
		while (node && node !== container) {
			expect(
				node.className,
				`ancestor <${node.tagName.toLowerCase()}> must not carry overflow-hidden`
			).not.toMatch(/\boverflow-hidden\b/);
			node = node.parentElement;
		}
	});
});

describe('DeviceActionsMenu — #165 compact trigger visual contract', () => {
	// ── TAMPER-TESTED sentinel ───────────────────────────────────────────────
	// compact=true renders a ghost icon button (no border, no background
	// circle) for inline DevicePwaRow contexts. The trigger must NOT carry
	// the bordered-circle classes (rounded-full, border) that indicate a
	// standalone detail-view affordance.
	//
	// The distinction matters for #165: DevicePwaRow passes compact so the
	// trigger is visually lightweight inside the card, while DeviceDetailModal
	// uses the default (compact=false) bordered circle.
	it('compact=true trigger lacks rounded-full and border classes (ghost icon, no bordered bubble)', () => {
		render(DeviceActionsMenu, { props: { compact: true, editHref: '/devices/d1/edit' } });
		const trigger = screen.getByRole('button', { name: /more actions/i });
		expect(
			trigger.className,
			'compact trigger must not carry rounded-full — that is the detail-view bordered-circle style'
		).not.toMatch(/\brounded-full\b/);
		expect(
			trigger.className,
			'compact trigger must not carry a border class — ghost triggers have no visible border ring'
		).not.toMatch(/\bborder\b/);
	});

	// ── TAMPER-TESTED sentinel ───────────────────────────────────────────────
	// compact=false (the default) retains the bordered circle used in
	// DeviceDetailModal and the device detail page. The trigger must carry
	// both rounded-full and border so it renders as a visible circle.
	it('compact=false (default) trigger carries rounded-full and border classes (bordered circle preserved)', () => {
		render(DeviceActionsMenu, { props: { editHref: '/devices/d1/edit' } });
		const trigger = screen.getByRole('button', { name: /more actions/i });
		expect(
			trigger.className,
			'default trigger must carry rounded-full — the bordered-circle affordance for detail views'
		).toMatch(/\brounded-full\b/);
		expect(
			trigger.className,
			'default trigger must carry border — the visible ring distinguishing it from ghost triggers'
		).toMatch(/\bborder\b/);
	});
});
