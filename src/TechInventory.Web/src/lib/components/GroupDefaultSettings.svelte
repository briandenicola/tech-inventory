<!--
	GroupDefaultSettings.svelte — starting state for grouped device lists.

	Seeds the collapse state of DevicePwaList's group headers. The installed app is
	the only presentation with collapsible groups today, so the copy talks about
	grouping generally rather than promising an effect on the desktop table.
	Persists to localStorage via userPrefs; read on the devices page at mount.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import { addToast } from '$lib/stores/toast';
	import {
		GROUP_DEFAULT_STATES,
		DEFAULT_GROUP_STATE,
		getGroupDefaultState,
		setGroupDefaultState,
		type GroupDefaultState
	} from '$lib/stores/userPrefs';

	const currentUser = $derived($authStore.currentUser);

	let selected = $state<GroupDefaultState>(DEFAULT_GROUP_STATE);

	$effect(() => {
		if (!currentUser?.id) return;
		selected = getGroupDefaultState(currentUser.id);
	});

	function choose(state: GroupDefaultState) {
		if (state === selected) return;
		selected = state;
		setGroupDefaultState(currentUser?.id, state);
		addToast({ type: 'success', message: t('settings.groupDefault.toast.saved') });
	}
</script>

<section
	class="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
	aria-labelledby="group-default-heading"
>
	<h2
		id="group-default-heading"
		class="text-lg font-semibold text-neutral-900 dark:text-neutral-50"
	>
		{t('settings.groupDefault.heading')}
	</h2>
	<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
		{t('settings.groupDefault.subheading')}
	</p>

	<fieldset class="mt-4">
		<legend class="text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('settings.groupDefault.legend')}
		</legend>
		<div class="mt-2 space-y-2">
			{#each GROUP_DEFAULT_STATES as state (state)}
				<label
					class="flex cursor-pointer items-start gap-3 rounded-lg border border-neutral-200 p-3 hover:bg-neutral-50 dark:border-neutral-800 dark:hover:bg-neutral-900"
					class:border-primary-500={selected === state}
					class:dark:border-primary-500={selected === state}
				>
					<input
						type="radio"
						name="group-default-state"
						value={state}
						checked={selected === state}
						onchange={() => choose(state)}
						class="mt-0.5 h-4 w-4 cursor-pointer border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
					/>
					<span class="text-sm">
						<span class="block font-medium text-neutral-900 dark:text-neutral-100">
							{t(`settings.groupDefault.${state}`)}
						</span>
						<span class="block text-neutral-600 dark:text-neutral-400">
							{t(`settings.groupDefault.${state}Help`)}
						</span>
					</span>
				</label>
			{/each}
		</div>
	</fieldset>
</section>
