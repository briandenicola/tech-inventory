<script lang="ts">
	import { t } from '$lib/i18n';
	import {
		adminNavItems,
		getVisibleNavItems,
		isNavItemActive,
		primaryNavItems,
		type AppNavRole
	} from './appNav';

	let {
		role = null,
		pathname = '/devices'
	}: {
		role?: AppNavRole | null;
		pathname?: string;
	} = $props();

	const visiblePrimaryNavItems = $derived(getVisibleNavItems(primaryNavItems, role));
	const visibleAdminNavItems = $derived(getVisibleNavItems(adminNavItems, role));
</script>

<nav aria-label="Harness primary navigation" class="space-y-4">
	<ul class="flex flex-col gap-2">
		{#each visiblePrimaryNavItems as item (item.href)}
			<li>
				<a href={item.href} aria-current={isNavItemActive(pathname, item) ? 'page' : undefined}>
					{t(item.labelKey)}
				</a>
			</li>
		{/each}
	</ul>

	{#if visibleAdminNavItems.length > 0}
		<section aria-label="Harness admin section">
			<h2>{t('navigation.admin')}</h2>
			<ul class="flex flex-col gap-2">
				{#each visibleAdminNavItems as item (item.href)}
					<li>
						<a href={item.href} aria-current={isNavItemActive(pathname, item) ? 'page' : undefined}>
							{t(item.labelKey)}
						</a>
					</li>
				{/each}
			</ul>
		</section>
	{/if}
</nav>
