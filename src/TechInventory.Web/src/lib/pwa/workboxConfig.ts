/**
 * Workbox `generateSW` config — extracted from `vite.config.ts` (C-15) so
 * the runtime-caching contract can be asserted in a plain Vitest unit test
 * without invoking the Vite plugin pipeline. This is the single source of
 * truth `vite.config.ts` imports; keep the two in sync by editing here.
 *
 * Contract (coverage-migration.md C-15):
 * - `navigateFallback` is `/offline` so the PWA shell still loads when the
 *   API is unreachable.
 * - `navigateFallbackDenylist` excludes API/health/openapi paths — those
 *   must fail normally rather than being served the offline shell.
 * - `POST`/`PUT`/`PATCH`/`DELETE` against `/api/v1/*` are `NetworkOnly` — mutations
 *   are refused offline, never silently queued (C-16).
 * - `GET` against `/api/v1/*` is `NetworkFirst` so a reader online always sees
 *   current data, while the cache still backs "device data viewable offline".
 *
 * It was `StaleWhileRevalidate`, which made the app permanently one fetch behind:
 * SWR answers from cache and refreshes it in the background, so the refetch that
 * follows a create or edit returned the *pre-mutation* body and only warmed the
 * cache with the new one. The user then had to refresh a second time to see their
 * own change. The `maxAgeSeconds` cap did not bound this — SWR re-caches on every
 * request, which resets the entry's age, so an actively-used URL never aged out.
 *
 * `NetworkFirst` keeps the offline guarantee (`coverage-migration.md`:267,
 * manual check M-07) because it falls back to the same cache when the network
 * fails or exceeds `networkTimeoutSeconds`.
 */
import type { VitePWAOptions } from 'vite-plugin-pwa';

export const workboxConfig: Partial<VitePWAOptions['workbox']> = {
	globPatterns: ['client/**/*.{js,css,ico,png,svg,webp,woff,woff2}'],
	navigateFallback: '/offline',
	navigateFallbackDenylist: [/^\/api\//, /^\/health/, /^\/openapi/],
	runtimeCaching: [
		{
			urlPattern: ({ url }: { url: URL }) => url.pathname.startsWith('/api/v1/'),
			handler: 'NetworkOnly',
			method: 'POST'
		},
		{
			urlPattern: ({ url }: { url: URL }) => url.pathname.startsWith('/api/v1/'),
			handler: 'NetworkOnly',
			method: 'PUT'
		},
		{
			urlPattern: ({ url }: { url: URL }) => url.pathname.startsWith('/api/v1/'),
			handler: 'NetworkOnly',
			method: 'PATCH'
		},
		{
			urlPattern: ({ url }: { url: URL }) => url.pathname.startsWith('/api/v1/'),
			handler: 'NetworkOnly',
			method: 'DELETE'
		},
		{
			urlPattern: ({ url }: { url: URL }) => url.pathname.startsWith('/api/v1/'),
			handler: 'NetworkFirst',
			method: 'GET',
			options: {
				cacheName: 'tech-inventory-api',
				// 3s: long enough to ride out a slow mobile round-trip, short enough
				// that a dead network falls back to cached data before the UI feels
				// hung. Only reached when the network stalls — a normal response,
				// fast or slow, is always preferred over the cached copy.
				networkTimeoutSeconds: 3,
				expiration: { maxEntries: 100, maxAgeSeconds: 5 * 60 },
				cacheableResponse: { statuses: [0, 200] }
			}
		}
	]
};
