<script lang="ts">
	interface Props {
		visible?: boolean;
		label: string;
		onClick: () => void;
	}

	let { visible = false, label, onClick }: Props = $props();
</script>

{#if visible}
	<button
		type="button"
		onclick={onClick}
		class="fixed bottom-6 right-6 inline-flex h-14 w-14 items-center justify-center rounded-full bg-primary-600 text-white shadow-lg transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
		style="z-index: var(--z-fixed);"
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
