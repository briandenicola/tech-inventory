<script lang="ts">
	import { t } from '$lib/i18n';
	import type { ReferenceTag } from '$lib/stores/referenceData';

	interface Props {
		options: ReferenceTag[];
		selectedTagIds?: string[];
		disabled?: boolean;
	}

	let { options = [], selectedTagIds = $bindable<string[]>([]), disabled = false }: Props = $props();

	function toggleTag(tagId: string, checked: boolean) {
		selectedTagIds = checked
			? Array.from(new Set([...selectedTagIds, tagId]))
			: selectedTagIds.filter((existingTagId) => existingTagId !== tagId);
	}
</script>

<fieldset class="space-y-3" disabled={disabled}>
	<legend class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
		{t('devices.form.tags')}
	</legend>
	<p id="device-tags-help" class="text-sm text-neutral-600 dark:text-neutral-400">
		{t('devices.form.tagsHint')}
	</p>

	{#if options.length === 0}
		<p class="rounded-lg border border-dashed border-neutral-300 px-3 py-4 text-sm text-neutral-600 dark:border-neutral-700 dark:text-neutral-400">
			{t('devices.form.tagsEmpty')}
		</p>
	{:else}
		<ul class="grid gap-2 sm:grid-cols-2" aria-describedby="device-tags-help">
			{#each options as tag (tag.id)}
				<li>
					<label
						class="flex items-center gap-3 rounded-lg border px-3 py-2 text-sm transition-colors hover:border-primary-400 hover:bg-primary-50/40 dark:hover:bg-primary-950/40 {selectedTagIds.includes(tag.id)
							? 'border-primary-500 bg-primary-50 dark:border-primary-500 dark:bg-primary-950/50'
							: 'border-neutral-300 bg-white dark:border-neutral-700 dark:bg-neutral-950'}"
					>
						<input
							type="checkbox"
							checked={selectedTagIds.includes(tag.id)}
							disabled={disabled}
							onchange={(event) =>
								toggleTag(
									tag.id,
									(event.currentTarget as HTMLInputElement).checked
								)}
							class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 disabled:cursor-not-allowed"
						/>
						<span
							class="h-2.5 w-2.5 rounded-full bg-neutral-400"
							style:background-color={tag.color ?? undefined}
							aria-hidden="true"
						></span>
						<span class="text-neutral-900 dark:text-neutral-100">{tag.name}</span>
					</label>
				</li>
			{/each}
		</ul>
	{/if}
</fieldset>
