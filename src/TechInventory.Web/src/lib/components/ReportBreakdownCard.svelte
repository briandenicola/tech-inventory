<script lang="ts">
	interface BreakdownItem {
		label: string;
		count: number;
	}

	interface Props {
		title: string;
		items: BreakdownItem[];
		emptyText: string;
		maxListHeightClass?: string;
	}

	let { title, items, emptyText, maxListHeightClass }: Props = $props();
	const maxCount = $derived(items.reduce((largest, item) => Math.max(largest, item.count), 0));
</script>

<div class="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
	<h2 class="text-sm font-medium text-neutral-500 dark:text-neutral-400">{title}</h2>

	{#if items.length === 0}
		<p class="mt-4 text-sm text-neutral-600 dark:text-neutral-300">{emptyText}</p>
	{:else}
		<ul class={`mt-4 space-y-3 ${maxListHeightClass ? `${maxListHeightClass} overflow-y-auto pr-2` : ''}`}>
			{#each items as item (item.label)}
				<li class="space-y-1">
					<div class="flex items-center justify-between gap-3 text-sm">
						<span class="font-medium text-neutral-900 dark:text-neutral-50">{item.label}</span>
						<span class="text-neutral-500 dark:text-neutral-400">{item.count}</span>
					</div>
					<div class="h-2 overflow-hidden rounded-full bg-neutral-100 dark:bg-neutral-800">
						<div
							class="h-full rounded-full bg-primary-500 dark:bg-primary-400"
							style={`width: ${maxCount === 0 ? 0 : (item.count / maxCount) * 100}%`}
						></div>
					</div>
				</li>
			{/each}
		</ul>
	{/if}
</div>
