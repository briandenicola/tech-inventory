<!--
	PwaUpdatePrompt.svelte — Toast-style banner shown when a new service
	worker version has been installed and is waiting to take control (T52).

	Click "Reload" to activate the new SW and reload the app; click "Dismiss"
	to defer the update until the next reload.
-->
<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';

	let needsRefresh = $state(false);
	let updateSW: ((reloadPage?: boolean) => Promise<void>) | null = null;

	onMount(async () => {
		if (typeof window === 'undefined') return;
		try {
			// @ts-expect-error -- virtual module injected by @vite-pwa/sveltekit
			const mod = await import('virtual:pwa-register');
			updateSW = mod.registerSW({
				immediate: true,
				onNeedRefresh() {
					needsRefresh = true;
				},
				onRegisterError(err: unknown) {
					console.warn('[pwa] SW registration error:', err);
				}
			});
		} catch (err) {
			// Module is only present in production build; safe to ignore in dev
			if (import.meta.env.PROD) {
				console.warn('[pwa] Failed to load SW registration module:', err);
			}
		}
	});

	async function reload() {
		needsRefresh = false;
		if (updateSW) await updateSW(true);
	}

	function dismiss() {
		needsRefresh = false;
	}
</script>

{#if needsRefresh}
	<div
		role="status"
		aria-live="polite"
		class="fixed bottom-6 left-1/2 z-[60] flex w-[min(90vw,28rem)] -translate-x-1/2 items-center gap-3 rounded-2xl border border-primary-200 bg-white/95 p-4 shadow-lg backdrop-blur-md dark:border-primary-800 dark:bg-neutral-900/95"
	>
		<svg
			class="h-6 w-6 flex-shrink-0 text-primary-600 dark:text-primary-400"
			fill="none"
			viewBox="0 0 24 24"
			stroke="currentColor"
			aria-hidden="true"
		>
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="1.8"
				d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
			/>
		</svg>
		<div class="flex-1 text-sm text-neutral-800 dark:text-neutral-200">
			{t('pwa.update.message')}
		</div>
		<button
			type="button"
			onclick={reload}
			class="rounded-lg bg-primary-600 px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
		>
			{t('pwa.update.reload')}
		</button>
		<button
			type="button"
			onclick={dismiss}
			class="rounded-lg px-2 py-1.5 text-sm text-neutral-600 hover:bg-neutral-100 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-300 dark:hover:bg-neutral-800"
			aria-label={t('pwa.update.dismiss')}
		>
			{t('pwa.update.dismiss')}
		</button>
	</div>
{/if}
