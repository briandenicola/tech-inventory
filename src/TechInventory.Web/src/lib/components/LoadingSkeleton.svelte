<!--
	LoadingSkeleton.svelte — Skeleton loader for devices table
	
	Per T15: Replace loading spinners with skeleton loaders.
	Shows 5-10 shimmer rows in table shape while data loads.
	Uses design tokens for skeleton color.
-->
<script lang="ts">
	interface Props {
		rows?: number;
	}

	let { rows = 7 }: Props = $props();
</script>

<div class="space-y-4">
	<!-- Table header skeleton -->
	<div class="flex gap-4">
		<div class="h-10 w-1/4 animate-pulse rounded bg-neutral-200 dark:bg-neutral-800"></div>
		<div class="h-10 w-1/6 animate-pulse rounded bg-neutral-200 dark:bg-neutral-800"></div>
		<div class="h-10 w-1/6 animate-pulse rounded bg-neutral-200 dark:bg-neutral-800"></div>
		<div class="h-10 w-1/6 animate-pulse rounded bg-neutral-200 dark:bg-neutral-800"></div>
		<div class="h-10 w-1/6 animate-pulse rounded bg-neutral-200 dark:bg-neutral-800"></div>
		<div class="h-10 w-1/6 animate-pulse rounded bg-neutral-200 dark:bg-neutral-800"></div>
	</div>

	<!-- Table rows skeleton -->
	<!-- eslint-disable-next-line @typescript-eslint/no-unused-vars -->
	{#each Array.from({ length: rows }) as _, i (i)}
		<div class="flex gap-4">
			<div class="h-12 w-1/4 animate-pulse rounded bg-neutral-100 dark:bg-neutral-900"></div>
			<div class="h-12 w-1/6 animate-pulse rounded bg-neutral-100 dark:bg-neutral-900"></div>
			<div class="h-12 w-1/6 animate-pulse rounded bg-neutral-100 dark:bg-neutral-900"></div>
			<div class="h-12 w-1/6 animate-pulse rounded bg-neutral-100 dark:bg-neutral-900"></div>
			<div class="h-12 w-1/6 animate-pulse rounded bg-neutral-100 dark:bg-neutral-900"></div>
			<div class="h-12 w-1/6 animate-pulse rounded bg-neutral-100 dark:bg-neutral-900"></div>
		</div>
	{/each}
</div>
