/**
 * Proxy test for CSS containing-block behavior.
 *
 * Per CSS Transforms Level 1 §3 and CSS Will Change Level 1 §3:
 * - `transform` set to ANY value (including `translateY(0px)`) establishes a containing block.
 * - `will-change: transform` also establishes a containing block.
 *
 * Both break `position: fixed` descendants (modals, FABs) by resolving them
 * against the wrapper instead of the viewport.
 *
 * JSDOM cannot model the containing-block algorithm, so we test the DOM
 * attributes that cause the bug as a reliable proxy.
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, waitFor } from '@testing-library/svelte';
import { tick } from 'svelte';
import PullToRefresh from './PullToRefresh.svelte';

function setPointerMode(matches: boolean): void {
	Object.defineProperty(window, 'matchMedia', {
		configurable: true,
		writable: true,
		value: vi.fn().mockImplementation(() => ({
			matches,
			media: '(pointer: coarse)',
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			addListener: vi.fn(),
			removeListener: vi.fn()
		}))
	});
}

function setScrollY(value: number): void {
	Object.defineProperty(window, 'scrollY', {
		configurable: true,
		writable: true,
		value
	});
}

function dispatchTouch(
	target: Element,
	type: 'touchstart' | 'touchmove' | 'touchend',
	clientY: number
): void {
	const event = new window.Event(type, { bubbles: true, cancelable: true });
	const touchPoint = { clientY };

	Object.defineProperty(event, 'touches', {
		configurable: true,
		value: type === 'touchend' ? [] : [touchPoint]
	});
	Object.defineProperty(event, 'changedTouches', {
		configurable: true,
		value: [touchPoint]
	});

	target.dispatchEvent(event);
}

afterEach(() => {
	vi.restoreAllMocks();
	setScrollY(0);
});

describe('PullToRefresh — containing-block safety', () => {
	it('at rest: content wrapper has no inline transform and no will-change-transform class', () => {
		setPointerMode(true);
		setScrollY(0);

		const { getByTestId } = render(PullToRefresh, {
			props: { onRefresh: vi.fn() }
		});

		const contentWrapper = getByTestId('pull-to-refresh-content');

		// No inline transform style (prevents containing block creation)
		expect(contentWrapper.style.transform).toBe('');

		// No will-change-transform class (also prevents containing block creation)
		expect(contentWrapper.classList.contains('will-change-transform')).toBe(false);
	});

	it('during pull: content wrapper has transform and will-change-transform', async () => {
		setPointerMode(true);
		setScrollY(0);

		const { getByTestId, container } = render(PullToRefresh, {
			props: { onRefresh: vi.fn() }
		});

		const outerWrapper = container.firstElementChild as HTMLElement;

		// Simulate pull gesture
		dispatchTouch(outerWrapper, 'touchstart', 0);
		dispatchTouch(outerWrapper, 'touchmove', 80);
		await tick();

		const contentWrapper = getByTestId('pull-to-refresh-content');

		// During active pull, transform must be set for visual feedback
		await waitFor(() => {
			expect(contentWrapper.style.transform).not.toBe('');
		});
		expect(contentWrapper.style.transform).toContain('translateY');

		// will-change-transform should be present for GPU optimization
		expect(contentWrapper.classList.contains('will-change-transform')).toBe(true);
	});
});
