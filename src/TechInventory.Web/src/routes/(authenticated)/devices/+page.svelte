<script lang="ts">
import { t } from '$lib/i18n';
import { authStore } from '$lib/stores/auth';

// T13: Placeholder devices list page (full implementation in Round 3)
// This page exists so authenticated users have somewhere to land after login (J1)

const currentUser = $derived($authStore.currentUser);
</script>

<div class="space-y-6">
<div class="border-b border-neutral-200 pb-4 dark:border-neutral-800">
<h1 class="text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
{t('devices.list.title')}
</h1>
{#if currentUser}
<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
{t('auth.welcomeMessage', { name: currentUser.displayName })}
</p>
{/if}
</div>

<div class="rounded-lg border border-neutral-200 bg-white p-12 text-center dark:border-neutral-800 dark:bg-neutral-950">
<svg
class="mx-auto h-16 w-16 text-neutral-400"
fill="none"
viewBox="0 0 24 24"
stroke="currentColor"
>
<path
stroke-linecap="round"
stroke-linejoin="round"
stroke-width="1.5"
d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z"
/>
</svg>
<h2 class="mt-4 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
{t('devices.list.emptyState')}
</h2>
<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
Device list UI coming in Round 3 (T14–T18).
</p>
<p class="mt-1 text-xs text-neutral-500 dark:text-neutral-500">
Phase 2 Round 2 complete: You're authenticated! 🎉
</p>
</div>
</div>
