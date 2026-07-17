<!--
	ToastContainer.svelte — ARIA live region for toast notifications
	
	T19-T22: Render toasts in top-right corner (fixed position).
	Auto-dismiss + manual dismiss. ARIA live="polite" for screen readers.
	
	Mount once in (authenticated)/+layout.svelte.
-->
<script lang="ts">
	import { toasts, dismissToast } from '$lib/stores/toast';
	import { fly } from 'svelte/transition';
	import { motionDuration } from '$lib/utils/motion';

	const activeToasts = $derived($toasts);
</script>

<!-- Toast container (top-right, fixed, z-modal) -->
<div
	class="pointer-events-none fixed right-0 top-0 z-50 flex flex-col items-end gap-3 p-4"
	aria-live="polite"
	aria-atomic="false"
>
	{#each activeToasts as toast (toast.id)}
		<div
			transition:fly={{ y: -20, duration: motionDuration(300) }}
			class="pointer-events-auto flex min-w-[320px] items-start gap-3 rounded-lg px-4 py-3 shadow-lg {toast.type ===
			'success'
				? 'bg-success-50 text-success-900 dark:bg-success-900 dark:text-success-50'
				: toast.type === 'error'
					? 'bg-danger-50 text-danger-900 dark:bg-danger-900 dark:text-danger-50'
					: 'bg-info-50 text-info-900 dark:bg-info-900 dark:text-info-50'}"
			role="status"
		>
			<!-- Icon -->
			<svg
				class="h-5 w-5 flex-shrink-0 {toast.type === 'success'
					? 'text-success-600 dark:text-success-400'
					: toast.type === 'error'
						? 'text-danger-600 dark:text-danger-400'
						: 'text-info-600 dark:text-info-400'}"
				fill="none"
				viewBox="0 0 24 24"
				stroke="currentColor"
				aria-hidden="true"
			>
				{#if toast.type === 'success'}
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
					/>
				{:else if toast.type === 'error'}
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
					/>
				{:else}
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
					/>
				{/if}
			</svg>

			<!-- Message -->
			<p class="flex-1 text-sm font-medium">{toast.message}</p>

			<!-- Dismiss button -->
			<button
				type="button"
				onclick={() => dismissToast(toast.id)}
				class="flex-shrink-0 rounded p-1 transition-colors hover:bg-black/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 {toast.type ===
				'success'
					? 'focus-visible:ring-success-600 dark:focus-visible:ring-success-400'
					: toast.type === 'error'
						? 'focus-visible:ring-danger-600 dark:focus-visible:ring-danger-400'
						: 'focus-visible:ring-info-600 dark:focus-visible:ring-info-400'}"
				aria-label="Dismiss notification"
			>
				<svg
					class="h-4 w-4"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
				</svg>
			</button>
		</div>
	{/each}
</div>
