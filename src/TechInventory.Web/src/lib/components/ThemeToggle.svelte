<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import {
		initializeTheme,
		setThemePreference,
		themeStore,
		type ThemePreference
	} from '$lib/stores/theme.svelte';

	const currentPreference = $derived(themeStore.preference);

	onMount(() => {
		initializeTheme();
	});

	function selectTheme(preference: ThemePreference) {
		setThemePreference(preference);
	}
</script>

<div
	class="inline-flex w-full max-w-full items-center rounded-full bg-neutral-100 p-1 dark:bg-neutral-800"
	role="group"
	aria-label={t('settings.theme.toggleLabel')}
>
	<button
		type="button"
		onclick={() => selectTheme('light')}
		aria-pressed={currentPreference === 'light'}
		aria-label={t('settings.theme.light')}
		class="inline-flex min-w-0 flex-1 min-h-11 items-center justify-center gap-1.5 rounded-full px-3 text-sm font-medium transition-colors {currentPreference === 'light'
			? 'bg-white text-neutral-900 shadow-sm dark:bg-neutral-700 dark:text-neutral-50'
			: 'text-neutral-600 dark:text-neutral-400'}"
	>
		<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 3v2.25M12 18.75V21M4.97 4.97l1.59 1.59M17.44 17.44l1.59 1.59M3 12h2.25M18.75 12H21M4.97 19.03l1.59-1.59M17.44 6.56l1.59-1.59M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z" />
		</svg>
		<span class="truncate">{t('settings.theme.light')}</span>
	</button>
	<button
		type="button"
		onclick={() => selectTheme('dark')}
		aria-pressed={currentPreference === 'dark'}
		aria-label={t('settings.theme.dark')}
		class="inline-flex min-w-0 flex-1 min-h-11 items-center justify-center gap-1.5 rounded-full px-3 text-sm font-medium transition-colors {currentPreference === 'dark'
			? 'bg-white text-neutral-900 shadow-sm dark:bg-neutral-700 dark:text-neutral-50'
			: 'text-neutral-600 dark:text-neutral-400'}"
	>
		<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M21 12.79A9 9 0 1111.21 3a7 7 0 009.79 9.79z" />
		</svg>
		<span class="truncate">{t('settings.theme.dark')}</span>
	</button>
	<button
		type="button"
		onclick={() => selectTheme('system')}
		aria-pressed={currentPreference === 'system'}
		aria-label={t('settings.theme.system')}
		class="inline-flex min-w-0 flex-1 min-h-11 items-center justify-center gap-1.5 rounded-full px-3 text-sm font-medium transition-colors {currentPreference === 'system'
			? 'bg-white text-neutral-900 shadow-sm dark:bg-neutral-700 dark:text-neutral-50'
			: 'text-neutral-600 dark:text-neutral-400'}"
	>
		<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M4.5 7.5h15m-15 9h15M7.5 4.5v15m9-15v15" />
		</svg>
		<span class="truncate">{t('settings.theme.system')}</span>
	</button>
</div>
