<script lang="ts">
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import AddDeviceFab from '$lib/components/AddDeviceFab.svelte';

	interface Props {
		selectedCount?: number;
		detailOpen?: boolean;
	}

	let { selectedCount = 0, detailOpen = false }: Props = $props();

	const currentUser = $derived($authStore.currentUser);
	const canCreateDevice = $derived(
		currentUser?.role === 'Admin' || currentUser?.role === 'Member'
	);
</script>

{#if canCreateDevice}
	<a
		href="/devices/new"
		class="hidden md:inline-flex min-h-11 items-center gap-2 rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
	>
		<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
		</svg>
		{t('devices.list.addButton')}
	</a>
{/if}

<AddDeviceFab
	visible={canCreateDevice && selectedCount === 0 && !detailOpen}
	label={t('devices.list.addFab')}
	href="/devices/new"
/>
