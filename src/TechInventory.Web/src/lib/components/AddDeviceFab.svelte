<script lang="ts">
	interface Props {
		visible?: boolean;
		label: string;
		onClick: () => void;
		raised?: boolean;
	}

	let { visible = false, label, onClick, raised = false }: Props = $props();

	const fabOffset = $derived(
		raised
			? 'calc(env(safe-area-inset-bottom, 0px) + var(--space-24))'
			: 'calc(env(safe-area-inset-bottom, 0px) + var(--space-6))'
	);
</script>

{#if visible}
	<button
		type="button"
		onclick={onClick}
		class="fixed inline-flex h-14 w-14 items-center justify-center rounded-full bg-primary-600 text-white shadow-xl transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
		style={`right: calc(env(safe-area-inset-right, 0px) + var(--space-6)); bottom: ${fabOffset}; z-index: var(--z-fixed);`}
		aria-label={label}
		title={label}
	>
		<svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4" />
		</svg>
	</button>
{/if}
