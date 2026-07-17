<!--
	/offline — PWA offline shell (T52)

	Served by the service worker's navigateFallback when the network is
	unreachable. Must be pre-renderable (no API calls, no auth state).

	No LoadingSkeleton — static shell with no data fetch of any kind.
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	function tryAgain() {
		if (typeof window !== 'undefined') {
			window.location.reload();
		}
	}
</script>

<svelte:head>
	<title>{t('offline.title')} · Tech Inventory</title>
</svelte:head>

<div
	class="flex min-h-screen flex-col items-center justify-center bg-neutral-50 px-6 text-center dark:bg-neutral-950"
>
	<svg
		class="h-20 w-20 text-neutral-400 dark:text-neutral-600"
		fill="none"
		viewBox="0 0 24 24"
		stroke="currentColor"
		aria-hidden="true"
	>
		<path
			stroke-linecap="round"
			stroke-linejoin="round"
			stroke-width="1.5"
			d="M18.364 5.636a9 9 0 010 12.728m0 0l-2.829-2.829m2.829 2.829L21 21M15.536 8.464a5 5 0 010 7.072m0 0l-2.829-2.829m-4.243 2.829a4.978 4.978 0 01-1.414-2.83m-1.414 5.658a9 9 0 01-2.167-9.238m7.824 2.167a1 1 0 111.414 1.414m-1.414-1.414L3 3m8.293 8.293l1.414 1.414"
		/>
	</svg>

	<h1 class="mt-6 text-2xl font-semibold text-neutral-900 dark:text-neutral-50">
		{t('offline.title')}
	</h1>
	<p class="mt-2 max-w-md text-base text-neutral-600 dark:text-neutral-400">
		{t('offline.description')}
	</p>

	<button
		type="button"
		onclick={tryAgain}
		class="mt-8 inline-flex items-center gap-2 rounded-xl bg-primary-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
	>
		<svg
			class="h-5 w-5"
			fill="none"
			viewBox="0 0 24 24"
			stroke="currentColor"
			aria-hidden="true"
		>
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="2"
				d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
			/>
		</svg>
		{t('offline.retry')}
	</button>
</div>
