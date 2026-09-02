<script lang="ts">
	import { displayMode } from '$lib/stores/displayMode.svelte';

	interface Props {
		visible?: boolean;
		label: string;
		onClick: () => void;
	}

	let { visible = false, label, onClick }: Props = $props();

	// F045 §5.7: AppBottomNav floats a pill+bubble across the same bottom-right
	// corner in standalone-PWA mode (bottom: safe-area + space-4, height
	// 3.5rem). Clear it instead of stacking on top of the Settings bubble.
	const bottomOffset = $derived(
		displayMode.isPwa
			? 'calc(env(safe-area-inset-bottom, 0px) + 5.5rem)'
			: 'calc(env(safe-area-inset-bottom, 0px) + var(--space-6))'
	);
</script>

{#if visible}
	<button
		type="button"
		onclick={onClick}
		class="fixed inline-flex h-14 w-14 items-center justify-center rounded-full bg-primary-600 text-white shadow-lg transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
		style="right: calc(env(safe-area-inset-right, 0px) + var(--space-6)); bottom: {bottomOffset}; z-index: var(--z-fixed);"
		aria-label={label}
		title={label}
	>
		<svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="2"
				d="M5 15l7-7 7 7"
			/>
		</svg>
	</button>
{/if}
