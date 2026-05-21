<script lang="ts">
	interface Props {
		visible?: boolean;
		label: string;
		href?: string;
		onClick?: () => void;
		raised?: boolean;
	}

	let { visible = false, label, href, onClick, raised = false }: Props = $props();

	const fabClass =
		'fixed left-0 inline-flex h-14 w-14 items-center justify-center rounded-full bg-primary-600 text-white shadow-xl transition-all hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 md:hidden dark:bg-primary-500 dark:hover:bg-primary-600';
	const fabStyle = $derived(
		`left: calc(env(safe-area-inset-left, 0px) + var(--space-6)); bottom: calc(env(safe-area-inset-bottom, 0px) + ${raised ? 'var(--space-20, 5rem)' : 'var(--space-6)'}); z-index: var(--z-fixed);`
	);
</script>

{#if visible}
	{#if href}
		<a
			{href}
			class={fabClass}
			style={fabStyle}
			aria-label={label}
			title={label}
		>
			<svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4" />
			</svg>
		</a>
	{:else if onClick}
		<button
			type="button"
			onclick={onClick}
			class={fabClass}
			style={fabStyle}
			aria-label={label}
			title={label}
		>
			<svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4" />
			</svg>
		</button>
	{/if}
{/if}
