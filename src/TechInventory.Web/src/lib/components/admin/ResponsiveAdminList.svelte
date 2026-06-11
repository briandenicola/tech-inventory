<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';

	interface Props {
		items: T[];
		tableLabel: string;
		cardsLabel: string;
		keyExtractor: (item: T) => string;
		tableHead: Snippet;
		desktopRow: Snippet<[T]>;
		mobileCard: Snippet<[T]>;
	}

	let { items, tableLabel, cardsLabel, keyExtractor, tableHead, desktopRow, mobileCard }: Props =
		$props();
</script>

<div class="md:hidden">
	<div class="grid gap-3" role="list" aria-label={cardsLabel}>
		{#each items as item (keyExtractor(item))}
			<div role="listitem">
				{@render mobileCard(item)}
			</div>
		{/each}
	</div>
</div>

<div
	class="hidden overflow-hidden rounded-lg border border-neutral-200 bg-white shadow dark:border-neutral-800 dark:bg-neutral-950 md:block"
>
	<div class="overflow-x-auto">
		<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
			<caption class="sr-only">{tableLabel}</caption>
			<thead class="bg-neutral-50 dark:bg-neutral-900">
				<tr>
					{@render tableHead()}
				</tr>
			</thead>
			<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
				{#each items as item (keyExtractor(item))}
					{@render desktopRow(item)}
				{/each}
			</tbody>
		</table>
	</div>
</div>
