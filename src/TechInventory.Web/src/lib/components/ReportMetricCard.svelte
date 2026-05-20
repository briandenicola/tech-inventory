<script lang="ts">
	interface Props {
		title: string;
		value: string;
		subtitle?: string;
	}

	let { title, value, subtitle }: Props = $props();
</script>

<div class="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
	<p class="text-sm font-medium text-neutral-500 dark:text-neutral-400">{title}</p>
	<p class="mt-3 text-3xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">{value}</p>
	{#if subtitle}
		<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-300">{subtitle}</p>
	{/if}
</div>
