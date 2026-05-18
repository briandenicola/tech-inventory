<script lang="ts">
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { t } from '$lib/i18n';

	/**
	 * Admin Landing Page — navigational hub to 4 admin sub-pages (Brands, Locations, Networks, Tags)
	 * 
	 * Decision: D-092 (Admin landing page as nav hub vs redirect — chose simple card navigation)
	 */

	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');

	// Redirect non-admins
	$effect(() => {
		if (!isAdmin && currentUser !== null) {
			goto('/devices');
		}
	});

	const adminSections = [
		{
			title: t('navigation.adminBrands'),
			href: '/admin/brands',
			description: 'Manage device brands and manufacturers',
			icon: '🏷️'
		},
		{
			title: t('navigation.adminLocations'),
			href: '/admin/locations',
			description: 'Manage storage and deployment locations',
			icon: '📍'
		},
		{
			title: t('navigation.adminNetworks'),
			href: '/admin/networks',
			description: 'Manage network segments and VLANs',
			icon: '🌐'
		},
		{
			title: t('navigation.adminTags'),
			href: '/admin/tags',
			description: 'Manage categorization tags',
			icon: '🏳️'
		}
	];
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-8">
		<h1 class="text-3xl font-bold text-neutral-900 dark:text-neutral-50">
			Admin
		</h1>
		<p class="mt-2 text-neutral-600 dark:text-neutral-400">
			Manage reference data and system configuration
		</p>
	</div>

	<div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-2">
		{#each adminSections as section (section.href)}
			<a
				href={section.href}
				class="group block rounded-lg border border-neutral-200 bg-white p-6 shadow-sm hover:border-primary-400 hover:shadow-md dark:border-neutral-800 dark:bg-neutral-950 dark:hover:border-primary-600"
			>
				<div class="flex items-center gap-3">
					<span class="text-3xl" aria-hidden="true">{section.icon}</span>
					<div>
						<h2 class="text-lg font-semibold text-neutral-900 group-hover:text-primary-600 dark:text-neutral-50 dark:group-hover:text-primary-400">
							{section.title}
						</h2>
						<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
							{section.description}
						</p>
					</div>
				</div>
			</a>
		{/each}
	</div>
</div>
