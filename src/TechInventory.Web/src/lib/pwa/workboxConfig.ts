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
 * - `GET` against `/api/v1/*` is `StaleWhileRevalidate` so cached device
 *   data remains viewable offline.
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
			handler: 'StaleWhileRevalidate',
			method: 'GET',
			options: {
				cacheName: 'tech-inventory-api',
				expiration: { maxEntries: 100, maxAgeSeconds: 5 * 60 },
				cacheableResponse: { statuses: [0, 200] }
			}
		}
	]
};
