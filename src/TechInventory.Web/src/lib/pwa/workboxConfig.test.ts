/**
 * Workbox config contract — C-15. Asserts the properties `vite.config.ts`
 * relies on for PWA offline behaviour: `navigateFallback`, the API/health/
 * openapi denylist, `NetworkOnly` for every mutating HTTP method against
 * `/api/v1/*` (C-16 — mutations are refused offline, never queued), and
 * `StaleWhileRevalidate` GET caching (cache name, entry/age limits, and
 * cacheable statuses). Real service-worker registration/interception
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

	it('refuses POST/PUT/DELETE against /api/v1/* offline (NetworkOnly, never queued)', () => {
		for (const method of ['POST', 'PUT', 'DELETE']) {
			const entry = findEntry(method, 'NetworkOnly');
			const matches = typeof entry.urlPattern === 'function' && entry.urlPattern({ url: new URL('https://x/api/v1/devices') } as never);
			expect(matches).toBe(true);
		}
	});

	it('serves cached GET /api/v1/* data via StaleWhileRevalidate with bounded cache options', () => {
		const entry = findEntry('GET', 'StaleWhileRevalidate');
		const matches = typeof entry.urlPattern === 'function' && entry.urlPattern({ url: new URL('https://x/api/v1/devices') } as never);
		expect(matches).toBe(true);

		expect(entry.options?.cacheName).toBe('tech-inventory-api');
		expect(entry.options?.expiration).toEqual({ maxEntries: 100, maxAgeSeconds: 5 * 60 });
		expect(entry.options?.cacheableResponse).toEqual({ statuses: [0, 200] });
	});

	it('does not match non-API GET paths for the runtime-caching url patterns', () => {
		const entry = findEntry('GET', 'StaleWhileRevalidate');
		const matches = typeof entry.urlPattern === 'function' && entry.urlPattern({ url: new URL('https://x/devices') } as never);
		expect(matches).toBe(false);
	});
});
