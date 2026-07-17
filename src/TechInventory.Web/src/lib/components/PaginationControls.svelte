<!--
	PaginationControls.svelte — Pagination controls for devices list
	
	Per T15: Prev / Next buttons (disabled at boundaries), page indicator,
	page size selector. URL-backed via $page.url.searchParams + goto().
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		currentPage: number;
		pageSize: number;
		totalCount: number;
		onPageChange: (page: number, pageSize: number) => void;
	}

	let { currentPage, pageSize, totalCount, onPageChange }: Props = $props();

	// Calculate total pages
	const totalPages = $derived(Math.ceil(totalCount / pageSize));

	// Calculate showing range
	const start = $derived((currentPage - 1) * pageSize + 1);
	const end = $derived(Math.min(currentPage * pageSize, totalCount));

	// Handle page change
	function handlePageChange(newPage: number) {
		if (newPage < 1 || newPage > totalPages) return;
		onPageChange(newPage, pageSize);
	}

	// Handle page size change
	function handlePageSizeChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		const newPageSize = parseInt(target.value, 10);
		onPageChange(1, newPageSize); // Reset to page 1 when changing size
	}
</script>

<div class="flex items-center justify-between border-t border-neutral-200 px-4 py-3 dark:border-neutral-800 sm:px-6">
	<!-- Mobile: Simple prev/next -->
	<div class="flex flex-1 justify-between sm:hidden">
		<button
			type="button"
			onclick={() => handlePageChange(currentPage - 1)}
			disabled={currentPage === 1}
			class="relative inline-flex items-center rounded-md border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
			aria-label={t('devices.pagination.previous')}
		>
			{t('common.actions.previous')}
		</button>
		<button
			type="button"
			onclick={() => handlePageChange(currentPage + 1)}
			disabled={currentPage === totalPages}
			class="relative ml-3 inline-flex items-center rounded-md border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
			aria-label={t('devices.pagination.next')}
		>
			{t('common.actions.next')}
		</button>
	</div>

	<!-- Desktop: Full controls -->
	<div class="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
		<!-- Showing info + page size selector -->
		<div class="flex items-center gap-4">
			<p class="text-sm text-neutral-700 dark:text-neutral-300">
				{t('devices.pagination.showing', { start, end, total: totalCount })}
			</p>

			<!-- Page size selector -->
			<label for="page-size" class="sr-only">{t('devices.pagination.pageSize')}</label>
			<select
				id="page-size"
				value={pageSize}
				onchange={handlePageSizeChange}
				class="rounded-md border-neutral-300 text-sm focus-visible:border-primary-500 focus-visible:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300"
			>
				<option value="25">25</option>
				<option value="50">50</option>
				<option value="100">100</option>
			</select>
		</div>

		<!-- Page navigation -->
		<nav class="inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
			<!-- Previous button -->
			<button
				type="button"
				onclick={() => handlePageChange(currentPage - 1)}
				disabled={currentPage === 1}
				class="relative inline-flex items-center rounded-l-md border border-neutral-300 bg-white px-2 py-2 text-sm font-medium text-neutral-500 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:hover:bg-neutral-800"
				aria-label={t('devices.pagination.previous')}
			>
				<svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
					<path
						fill-rule="evenodd"
						d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z"
						clip-rule="evenodd"
					/>
				</svg>
			</button>

			<!-- Page indicator -->
			<span
				class="relative inline-flex items-center border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300"
			>
				{t('devices.pagination.page')} {currentPage} {t('devices.pagination.of')} {totalPages}
			</span>

			<!-- Next button -->
			<button
				type="button"
				onclick={() => handlePageChange(currentPage + 1)}
				disabled={currentPage === totalPages}
				class="relative inline-flex items-center rounded-r-md border border-neutral-300 bg-white px-2 py-2 text-sm font-medium text-neutral-500 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400 dark:hover:bg-neutral-800"
				aria-label={t('devices.pagination.next')}
			>
				<svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
					<path
						fill-rule="evenodd"
						d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
						clip-rule="evenodd"
					/>
				</svg>
			</button>
		</nav>
	</div>
</div>
