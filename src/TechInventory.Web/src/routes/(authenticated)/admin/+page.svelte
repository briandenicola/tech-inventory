<script lang="ts">
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import AuditLogModal from '$lib/components/AuditLogModal.svelte';
	import { adminNavItems, getVisibleNavItems } from '$lib/navigation/appNav';

	/**
	 * Admin Landing Page — navigational hub for every admin reference-data route.
	 *
	 * Decision: D-092 (Admin landing page as nav hub vs redirect — chose simple card navigation)
	 */

	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');
	const adminSections = $derived(getVisibleNavItems(adminNavItems, currentUser?.role));
	let showAuditLogModal = $state(false);

	// Redirect non-admins
	$effect(() => {
		if (!isAdmin && currentUser !== null) {
			goto('/devices');
		}
	});

</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
		<div>
			<h1 class="text-3xl font-bold text-neutral-900 dark:text-neutral-50">
				{t('admin.hub.title')}
			</h1>
			<p class="mt-2 text-neutral-600 dark:text-neutral-400">
				{t('admin.hub.description')}
			</p>
		</div>
		<button
			type="button"
			onclick={() => (showAuditLogModal = true)}
			class="inline-flex min-h-11 items-center justify-center rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-200 dark:hover:bg-neutral-900"
		>
			{t('admin.audit.actions.open')}
		</button>
	</div>

	<div class="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
		{#each adminSections as section (section.href)}
			<a
				href={section.href}
				class="group flex min-h-44 rounded-lg border border-neutral-200 bg-white p-6 shadow-sm transition-colors hover:border-primary-400 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-800 dark:bg-neutral-950 dark:hover:border-primary-600"
			>
				<div class="flex items-start gap-3">
					<span class="text-3xl" aria-hidden="true">{section.icon}</span>
					<div>
						<h2 class="text-lg font-semibold text-neutral-900 group-hover:text-primary-600 dark:text-neutral-50 dark:group-hover:text-primary-400">
							{t(section.labelKey)}
						</h2>
						{#if section.descriptionKey}
							<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
								{t(section.descriptionKey)}
							</p>
						{/if}
					</div>
				</div>
			</a>
		{/each}
	</div>

	{#if showAuditLogModal}
		<AuditLogModal isOpen={showAuditLogModal} onClose={() => (showAuditLogModal = false)} />
	{/if}
</div>
