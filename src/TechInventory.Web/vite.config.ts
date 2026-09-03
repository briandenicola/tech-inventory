import { sveltekit } from '@sveltejs/kit/vite';
import { SvelteKitPWA } from '@vite-pwa/sveltekit';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig, type Plugin } from 'vitest/config';
import { workboxConfig } from './src/lib/pwa/workboxConfig';

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
	workbox: workboxConfig,
	devOptions: {
		enabled: false,
		type: 'module',
		navigateFallback: '/offline'
	}
}) as unknown as Plugin[];

export default defineConfig({
	plugins: [
		tailwindcss(),
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
