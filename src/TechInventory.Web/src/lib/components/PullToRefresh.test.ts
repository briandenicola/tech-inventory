import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, waitFor } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
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

describe('PullToRefresh', () => {
	it('does not trigger refresh on non-coarse pointers', async () => {
		setPointerMode(false);
		setScrollY(0);
		const onRefresh = vi.fn();
		const { container } = render(PullToRefresh, {
			props: {
				onRefresh
			}
		});
		const wrapper = container.firstElementChild as HTMLElement;

		dispatchTouch(wrapper, 'touchstart', 0);
		dispatchTouch(wrapper, 'touchmove', 180);
		dispatchTouch(wrapper, 'touchend', 180);

		await waitFor(() => {
			expect(onRefresh).not.toHaveBeenCalled();
		});
	});

	it('triggers refresh after pulling past the threshold at scroll top', async () => {
		setPointerMode(true);
		setScrollY(0);
		let resolveRefresh: (() => void) | undefined;
		const onRefresh = vi.fn(
			() =>
				new Promise<void>((resolve) => {
					resolveRefresh = resolve;
				})
		);
		const { container } = render(PullToRefresh, {
			props: {
				onRefresh
			}
		});
		const wrapper = container.firstElementChild as HTMLElement;

		dispatchTouch(wrapper, 'touchstart', 0);
		dispatchTouch(wrapper, 'touchmove', 140);
		dispatchTouch(wrapper, 'touchend', 140);

		await waitFor(() => {
			expect(onRefresh).toHaveBeenCalledOnce();
		});
		expect(container.querySelector('[data-refreshing="true"]')).toBeTruthy();

		resolveRefresh?.();

		await waitFor(() => {
			expect(container.querySelector('[data-refreshing="true"]')).toBeFalsy();
		});
	});

	it('does not trigger refresh when the page is scrolled down', async () => {
		setPointerMode(true);
		setScrollY(24);
		const onRefresh = vi.fn();
		const { container } = render(PullToRefresh, {
			props: {
				onRefresh
			}
		});
		const wrapper = container.firstElementChild as HTMLElement;

		dispatchTouch(wrapper, 'touchstart', 0);
		dispatchTouch(wrapper, 'touchmove', 180);
		dispatchTouch(wrapper, 'touchend', 180);

		await waitFor(() => {
			expect(onRefresh).not.toHaveBeenCalled();
		});
	});

	it('has no accessibility violations', async () => {
		setPointerMode(true);
		setScrollY(0);
		const { container } = render(PullToRefresh, {
			props: {
				onRefresh: vi.fn()
			}
		});

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});
