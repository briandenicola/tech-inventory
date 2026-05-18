<!--
	ErrorState.svelte — Error state for devices list
	
	Per T15: Error message + retry button. Uses error from useDevices().
	User-safe messages (never leak server internals).
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		error: string;
		onRetry: () => void;
	}

	let { error, onRetry }: Props = $props();
</script>

<div
	class="flex min-h-[400px] flex-col items-center justify-center rounded-lg border border-danger-200 bg-danger-50 p-12 text-center dark:border-danger-900 dark:bg-danger-950"
	role="alert"
	aria-live="assertive"
>
	<!-- Icon -->
	<svg
		class="h-16 w-16 text-danger-600 dark:text-danger-500"
		fill="none"
		viewBox="0 0 24 24"
		stroke="currentColor"
		aria-hidden="true"
	>
		<path
			stroke-linecap="round"
			stroke-linejoin="round"
			stroke-width="1.5"
			d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
		/>
	</svg>

	<!-- Message -->
	<h2 class="mt-4 text-lg font-semibold text-danger-900 dark:text-danger-100">
		{t('common.states.error')}
	</h2>
	<p class="mt-2 text-sm text-danger-700 dark:text-danger-300">
		{error}
	</p>

	<!-- Retry button -->
	<button
		type="button"
		onclick={onRetry}
		class="mt-6 inline-flex items-center gap-2 rounded-lg bg-danger-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-danger-700 focus:outline-none focus:ring-2 focus:ring-danger-500 focus:ring-offset-2 dark:bg-danger-500 dark:hover:bg-danger-600"
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
		{t('common.actions.retry')}
	</button>
</div>
