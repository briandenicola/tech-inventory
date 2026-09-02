/**
 * displayMode.svelte.ts tests — F045 D-175.
 *
 * Verifies the three presentation modes, the SSR/pre-mount `desktop` default
 * (no hydration-mismatch flash), reactive updates when the underlying
 * `matchMedia` queries change, and that `destroy()` actually detaches its
 * listeners. `standalone`/`wide` are always injected via options so these
 * tests never depend on the real `isStandalonePwa()` detection, except the
 * one test that deliberately exercises the default (un-injected) path.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createDisplayMode } from './displayMode.svelte';

interface MockMediaQueryList {
	matches: boolean;
	media: string;
	addEventListener: (type: 'change', listener: (event: MediaQueryListEvent) => void) => void;
	removeEventListener: (type: 'change', listener: (event: MediaQueryListEvent) => void) => void;
}

function installMatchMediaMock(initialMatches: Record<string, boolean>) {
	const registries = new Map<string, Set<(event: MediaQueryListEvent) => void>>();
	const lists = new Map<string, MockMediaQueryList>();

	function getOrCreate(query: string): MockMediaQueryList {
		let list = lists.get(query);
		if (!list) {
			list = {
				matches: initialMatches[query] ?? false,
				media: query,
				addEventListener: vi.fn((_type, listener) => {
					let set = registries.get(query);
					if (!set) {
						set = new Set();
						registries.set(query, set);
					}
					set.add(listener);
				}),
				removeEventListener: vi.fn((_type, listener) => {
					registries.get(query)?.delete(listener);
				})
			};
			lists.set(query, list);
		}
		return list;
	}

	Object.defineProperty(window, 'matchMedia', {
		writable: true,
		configurable: true,
		value: vi.fn((query: string) => getOrCreate(query))
	});

	return {
		fireChange(query: string, matches: boolean) {
			const list = lists.get(query);
			if (list) list.matches = matches;
			const event = { matches } as MediaQueryListEvent;
			for (const listener of registries.get(query) ?? []) {
				listener(event);
			}
		},
		getList(query: string) {
			return lists.get(query);
		}
	};
}

const STANDALONE_QUERY = '(display-mode: standalone)';
const WIDE_QUERY = '(min-width: 768px)';

describe('createDisplayMode', () => {
	beforeEach(() => {
		installMatchMediaMock({});
	});

	it('resolves to "pwa" when injected standalone=true, regardless of width', () => {
		const handle = createDisplayMode({ standalone: true, wide: false });
		expect(handle.mode).toBe('pwa');
		expect(handle.isPwa).toBe(true);
		expect(handle.isMounted).toBe(true);
		handle.destroy();
	});

	it('resolves to "desktop" when standalone=false and wide=true', () => {
		const handle = createDisplayMode({ standalone: false, wide: true });
		expect(handle.mode).toBe('desktop');
		expect(handle.isPwa).toBe(false);
		handle.destroy();
	});

	it('resolves to "mobile" when standalone=false and wide=false', () => {
		const handle = createDisplayMode({ standalone: false, wide: false });
		expect(handle.mode).toBe('mobile');
		expect(handle.isPwa).toBe(false);
		handle.destroy();
	});

	it('defaults to "desktop" with isMounted=false when matchMedia is unavailable (SSR/pre-mount)', () => {
		const original = window.matchMedia;
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		delete (window as any).matchMedia;

		const handle = createDisplayMode();
		expect(handle.mode).toBe('desktop');
		expect(handle.isMounted).toBe(false);
		expect(handle.isPwa).toBe(false);

		window.matchMedia = original;
	});

	it('reacts to the standalone media query changing after mount', () => {
		const mock = installMatchMediaMock({ [STANDALONE_QUERY]: false, [WIDE_QUERY]: true });
		const handle = createDisplayMode();
		expect(handle.mode).toBe('desktop');

		mock.fireChange(STANDALONE_QUERY, true);
		expect(handle.mode).toBe('pwa');
		expect(handle.isPwa).toBe(true);

		handle.destroy();
	});

	it('reacts to the wide media query changing after mount', () => {
		const mock = installMatchMediaMock({ [STANDALONE_QUERY]: false, [WIDE_QUERY]: true });
		const handle = createDisplayMode();
		expect(handle.mode).toBe('desktop');

		mock.fireChange(WIDE_QUERY, false);
		expect(handle.mode).toBe('mobile');

		handle.destroy();
	});

	it('destroy() detaches both media query listeners', () => {
		const mock = installMatchMediaMock({});
		const handle = createDisplayMode();
		const modeBeforeDestroy = handle.mode;

		const standaloneList = mock.getList(STANDALONE_QUERY);
		const wideList = mock.getList(WIDE_QUERY);
		expect(standaloneList?.addEventListener).toHaveBeenCalledTimes(1);
		expect(wideList?.addEventListener).toHaveBeenCalledTimes(1);

		handle.destroy();

		expect(standaloneList?.removeEventListener).toHaveBeenCalledTimes(1);
		expect(wideList?.removeEventListener).toHaveBeenCalledTimes(1);

		// A change fired after destroy must not update state anymore.
		mock.fireChange(STANDALONE_QUERY, true);
		expect(handle.mode).toBe(modeBeforeDestroy);
	});

	it('falls back to the real isStandalonePwa() detection when no options are injected', () => {
		installMatchMediaMock({ [STANDALONE_QUERY]: true, [WIDE_QUERY]: true });
		const handle = createDisplayMode();
		expect(handle.mode).toBe('pwa');
		expect(handle.isPwa).toBe(true);
		handle.destroy();
	});
});
