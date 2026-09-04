/**
 * Workbox config contract — C-15. Asserts the properties `vite.config.ts`
 * relies on for PWA offline behaviour: `navigateFallback`, the API/health/
 * openapi denylist, `NetworkOnly` for every mutating HTTP method against
 * `/api/v1/*` (C-16 — mutations are refused offline, never queued), and
 * `NetworkFirst` GET caching (cache name, entry/age limits, cacheable
 * statuses, and the network timeout that keeps offline reads working). Real service-worker registration/interception
 * behaviour is out of scope here (jsdom has no Workbox runtime) — see
 * coverage-migration.md M-06/M-07/M-08 for the residual manual-check gap.
 */
import { describe, expect, it } from 'vitest';
import { workboxConfig } from './workboxConfig';

type RuntimeCachingEntry = NonNullable<typeof workboxConfig.runtimeCaching>[number];

function findEntry(method: string, handler: string): RuntimeCachingEntry {
	const entry = workboxConfig.runtimeCaching?.find(
		(candidate) => candidate.method === method && candidate.handler === handler
	);
	if (!entry) {
		throw new Error(`No runtimeCaching entry found for ${method}/${handler}`);
	}
	return entry;
}

describe('workboxConfig (C-15)', () => {
	it('falls back to /offline for navigation requests', () => {
		expect(workboxConfig.navigateFallback).toBe('/offline');
	});

	it('excludes API, health, and openapi paths from the navigate fallback', () => {
		const denylist = workboxConfig.navigateFallbackDenylist ?? [];
		expect(denylist.some((pattern) => pattern.test('/api/v1/devices'))).toBe(true);
		expect(denylist.some((pattern) => pattern.test('/health'))).toBe(true);
		expect(denylist.some((pattern) => pattern.test('/openapi'))).toBe(true);
		// A real page route must NOT match the denylist, or it would never
		// receive the offline fallback.
		expect(denylist.some((pattern) => pattern.test('/devices'))).toBe(false);
	});

	it('refuses every API mutation method offline (NetworkOnly, never queued)', () => {
		for (const method of ['POST', 'PUT', 'PATCH', 'DELETE']) {
			const entry = findEntry(method, 'NetworkOnly');
			const matches =
				typeof entry.urlPattern === 'function' &&
				entry.urlPattern({ url: new URL('https://x/api/v1/devices') } as never);
			expect(matches).toBe(true);
		}
	});

	it('serves GET /api/v1/* NetworkFirst with bounded cache options', () => {
		const entry = findEntry('GET', 'NetworkFirst');
		const matches =
			typeof entry.urlPattern === 'function' &&
			entry.urlPattern({ url: new URL('https://x/api/v1/devices') } as never);
		expect(matches).toBe(true);

		expect(entry.options?.cacheName).toBe('tech-inventory-api');
		expect(entry.options?.expiration).toEqual({ maxEntries: 100, maxAgeSeconds: 5 * 60 });
		expect(entry.options?.cacheableResponse).toEqual({ statuses: [0, 200] });
	});

	// The bug this replaced: StaleWhileRevalidate answers from cache and refreshes
	// in the background, so the refetch after a create or edit returned the
	// pre-mutation body and merely warmed the cache with the new one — the user had
	// to refresh twice to see their own change. Nothing bounded it either: SWR
	// re-caches on every request, resetting the entry's age, so maxAgeSeconds never
	// expired an actively-used URL.
	//
	// TAMPER-TESTED: setting the GET handler back to 'StaleWhileRevalidate' fails
	//   this test plus the three others that look the GET entry up by handler —
	//   four in all, and nothing outside this file.
	it('never serves API reads from cache first', () => {
		const getEntries =
			workboxConfig.runtimeCaching?.filter((entry) => entry.method === 'GET') ?? [];

		expect(getEntries.length).toBeGreaterThan(0);
		for (const entry of getEntries) {
			expect(entry.handler).not.toBe('StaleWhileRevalidate');
			expect(entry.handler).not.toBe('CacheFirst');
			expect(entry.handler).not.toBe('CacheOnly');
		}
	});

	// Without a timeout NetworkFirst waits on the network indefinitely, which on a
	// dead mobile connection reads as a hung app rather than an offline one.
	it('falls back to cache when the network stalls, so offline reads still work', () => {
		const entry = findEntry('GET', 'NetworkFirst');

		expect(entry.options?.networkTimeoutSeconds).toBe(3);
	});

	it('does not match non-API GET paths for the runtime-caching url patterns', () => {
		const entry = findEntry('GET', 'NetworkFirst');
		const matches =
			typeof entry.urlPattern === 'function' &&
			entry.urlPattern({ url: new URL('https://x/devices') } as never);
		expect(matches).toBe(false);
	});
});
