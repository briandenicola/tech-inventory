<script lang="ts">
	/**
	 * Breadcrumbs — C-07 extraction.
	 *
	 * Shared breadcrumb trail markup that was previously duplicated verbatim
	 * across devices/new, devices/[id], and devices/[id]/edit. The final item
	 * (the one with no `href`) renders as the current page (`aria-current`),
	 * matching the pre-extraction markup exactly.
	 */
	export interface BreadcrumbItem {
		label: string;
		href?: string;
	}

	let {
		items,
		class: navClass = 'mb-4 flex text-sm text-neutral-600 dark:text-neutral-400'
	}: { items: BreadcrumbItem[]; class?: string } = $props();
</script>

<nav class={navClass} aria-label="Breadcrumb">
	<ol class="flex items-center space-x-2">
		{#each items as item, index (item.label + index)}
			{#if item.href}
				<li>
					<a href={item.href} class="hover:text-primary-600 dark:hover:text-primary-400">
						{item.label}
					</a>
				</li>
			{:else}
				<li aria-current="page" class="font-medium text-neutral-900 dark:text-neutral-100">
					{item.label}
				</li>
			{/if}
			{#if index < items.length - 1}
				<li>
					<svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
						<path
							fill-rule="evenodd"
							d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
							clip-rule="evenodd"
						/>
					</svg>
				</li>
			{/if}
		{/each}
	</ol>
</nav>
