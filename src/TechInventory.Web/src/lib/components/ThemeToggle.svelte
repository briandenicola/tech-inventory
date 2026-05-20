<!--
	ThemeToggle.svelte — 3-way segmented control for theme preference (F029).
	
	Choices: Light / Dark / System (follows prefers-color-scheme).
	Persists to userPrefs store (localStorage today; server-sync when F022b ships).
	Syncs immediately to document.documentElement.dataset.theme via $effect.
	
	Matches the devices view-mode toggle's segmented-control visual pattern
	(rounded-full pill container, active option has white bg + shadow).
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import {
		getThemePreference,
		setThemePreference,
		type ThemePreference
	} from '$lib/stores/userPrefs';

	const currentUser = $derived($authStore.currentUser);

	// Read persisted preference on mount; default to 'system' if absent.
	let theme = $state<ThemePreference>('system');
	$effect(() => {
		if (!currentUser?.id) return;
		const persisted = getThemePreference(currentUser.id);
		theme = persisted ?? 'system';
	});

	// Apply theme to <html data-theme> immediately when changed.
	$effect(() => {
		const html = document.documentElement;
		if (theme === 'light') {
			html.dataset.theme = 'light';
		} else if (theme === 'dark') {
			html.dataset.theme = 'dark';
		} else {
			// 'system' → remove explicit data-theme so prefers-color-scheme takes over
			delete html.dataset.theme;
		}
	});

	function selectTheme(newTheme: ThemePreference) {
		theme = newTheme;
		if (currentUser?.id) {
			setThemePreference(currentUser.id, newTheme);
		}
	}
</script>

<div
	class="inline-flex items-center rounded-full bg-neutral-100 p-1 dark:bg-neutral-800"
	role="group"
	aria-label={t('settings.theme.toggleLabel')}
>
	<button
		type="button"
		onclick={() => selectTheme('light')}
		aria-pressed={theme === 'light'}
		class="min-h-11 px-4 rounded-full text-sm font-medium transition-colors {theme === 'light'
			? 'bg-white shadow-sm text-neutral-900 dark:bg-neutral-700 dark:text-neutral-50'
			: 'text-neutral-600 dark:text-neutral-400'}"
	>
		{t('settings.theme.light')}
	</button>
	<button
		type="button"
		onclick={() => selectTheme('dark')}
		aria-pressed={theme === 'dark'}
		class="min-h-11 px-4 rounded-full text-sm font-medium transition-colors {theme === 'dark'
			? 'bg-white shadow-sm text-neutral-900 dark:bg-neutral-700 dark:text-neutral-50'
			: 'text-neutral-600 dark:text-neutral-400'}"
	>
		{t('settings.theme.dark')}
	</button>
	<button
		type="button"
		onclick={() => selectTheme('system')}
		aria-pressed={theme === 'system'}
		class="min-h-11 px-4 rounded-full text-sm font-medium transition-colors {theme === 'system'
			? 'bg-white shadow-sm text-neutral-900 dark:bg-neutral-700 dark:text-neutral-50'
			: 'text-neutral-600 dark:text-neutral-400'}"
	>
		{t('settings.theme.system')}
	</button>
</div>
