<script lang="ts">
	import ResponsiveAdminList from './ResponsiveAdminList.svelte';
	import ResponsiveListCard from '$lib/components/ResponsiveListCard.svelte';

	type HarnessItem = {
		id: string;
		name: string;
		description: string;
	};

	const items: HarnessItem[] = [
		{ id: 'item-1', name: 'Alpha', description: 'First item description' },
		{ id: 'item-2', name: 'Bravo', description: 'Second item description' }
	];

	</script>

<ResponsiveAdminList
	{items}
	tableLabel="Harness admin table"
	cardsLabel="Harness admin cards"
	keyExtractor={(item) => item.id}
>
	{#snippet tableHead()}
		<th
			scope="col"
			class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
		>
			Name
		</th>
		<th
			scope="col"
			class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
		>
			Description
		</th>
		<th
			scope="col"
			class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
		>
			Actions
		</th>
	{/snippet}

	{#snippet desktopRow(item: HarnessItem)}
		<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900">
			<td class="px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50">{item.name}</td>
			<td class="px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300">{item.description}</td>
			<td class="px-4 py-3 text-right">
				<div class="flex flex-wrap justify-end gap-2">
					<button
						type="button"
						class="inline-flex min-h-11 items-center rounded-full border border-primary-300 px-4 py-2 text-sm font-medium text-primary-700 transition-colors hover:bg-primary-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-primary-800 dark:text-primary-300 dark:hover:bg-primary-950"
					>
						Edit
					</button>
				</div>
			</td>
		</tr>
	{/snippet}

	{#snippet mobileCard(item: HarnessItem)}
		<ResponsiveListCard
			title={item.name}
			titleId={`harness-card-${item.id}`}
			fields={[{ key: 'description', label: 'Description', value: item.description }]}
			actionItems={[{ id: `edit-${item.id}`, label: 'Edit', onSelect: () => undefined, tone: 'primary' }]}
			actionMenuLabel="More actions"
			actionMenuTitle="Actions"
		/>
	{/snippet}
</ResponsiveAdminList>
