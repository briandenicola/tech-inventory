import { sveltekit } from '@sveltejs/kit/vite';
import { SvelteKitPWA } from '@vite-pwa/sveltekit';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig, type Plugin } from 'vitest/config';

// vite-plugin-pwa declares a peer on vite@5; the repo runs vite@6. The Plugin
// type instances are structurally compatible but nominally distinct across
// versions, so we widen the SvelteKitPWA result to the local Plugin type.
const pwa = SvelteKitPWA({
	srcDir: './src',
	strategies: 'generateSW',
	registerType: 'prompt',
	injectRegister: false,
	scope: '/',
	base: '/',
	manifest: false,
	workbox: {
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
	},
	devOptions: {
		enabled: false,
		type: 'module',
		navigateFallback: '/offline'
	}
}) as unknown as Plugin[];

export default defineConfig({
	plugins: [
		// @ts-expect-error -- vite-plugin-pwa peers on vite@5, repo runs vite@6; types are structurally compatible
		tailwindcss(),
		// @ts-expect-error -- vite version peer mismatch (see above)
		sveltekit(),
		...pwa
	],
	resolve: {
		conditions: ['browser']
	},
	test: {
		environment: 'jsdom',
		globals: true,
		setupFiles: ['./vitest.setup.ts'],
		include: ['src/**/*.{test,spec}.{js,ts}'],
		coverage: {
			reporter: ['text', 'json', 'html'],
			exclude: ['node_modules/', 'build/', '.svelte-kit/']
		}
	}
});
