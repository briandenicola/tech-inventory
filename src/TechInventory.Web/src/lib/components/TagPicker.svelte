<!--
	TagPicker.svelte — F030 multi-select tag picker with type-ahead.

	Multi-select chips + dropdown of remaining tags filtered by the input.
	Keyboard nav follows the WAI-ARIA APG "Editable combobox with list
	autocomplete" pattern:
	  - Input has role="combobox", aria-expanded, aria-controls,
	    aria-autocomplete="list".
	  - Dropdown is a <ul role="listbox"> of <li role="option">.
	  - aria-activedescendant points at the highlighted option for screen
	    readers; visible highlight is driven by the same index.
	  - Down/Up arrow nav, Enter selects, Esc closes, Backspace on empty
	    input removes the trailing chip.

	The component is **uncontrolled w.r.t. the network** — it neither loads
	the tag catalog nor mutates it. The parent passes the full available-tag
	list and the currently-selected IDs; the parent is also responsible for
	calling the relevant POST/DELETE endpoints when `onChange` fires.

	Props:
	  - selectedIds:    currently selected tag ids
	  - availableTags:  full tag catalog (active tags only)
	  - onChange:       fired with the new id list whenever selection changes
	  - disabled?:      blocks input + chip removal
	  - id?:            DOM id for the input (so a parent <label> can target it)
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import type { components } from '$lib/api/generated/types';

	type TagResponse = components['schemas']['TagResponse'];

	interface Props {
		selectedIds: string[];
		availableTags: TagResponse[];
		onChange: (ids: string[]) => void;
		disabled?: boolean;
		id?: string;
	}

	let {
		selectedIds,
		availableTags,
		onChange,
		disabled = false,
		id = 'tag-picker-input'
	}: Props = $props();

	let inputValue = $state('');
	let isOpen = $state(false);
	let highlightIndex = $state(-1);
	let inputElement = $state<HTMLInputElement | undefined>(undefined);

	// Snapshot the id reactively so a parent that re-binds it doesn't desync
	// the listbox/option ids referenced by aria-activedescendant.
	const inputId = $derived(id);
	const listboxId = $derived(`${inputId}-listbox`);

	// Narrow OpenAPI's nullable id/name to non-null so downstream code can
	// safely use string-typed APIs (selectedIds.includes, .toLowerCase()).
	type SafeTag = TagResponse & { id: string; name: string };
	const safeAvailable = $derived(
		availableTags.filter((tag): tag is SafeTag => !!tag.id && !!tag.name)
	);

	const selectedTags = $derived(
		selectedIds
			.map((tagId) => safeAvailable.find((tag) => tag.id === tagId))
			.filter((tag): tag is SafeTag => tag !== undefined)
	);

	// Filter: not already selected, name contains the typed text.
	const filteredOptions = $derived.by(() => {
		const query = inputValue.trim().toLowerCase();
		return safeAvailable
			.filter((tag) => tag.isActive && !selectedIds.includes(tag.id))
			.filter((tag) => (query === '' ? true : tag.name.toLowerCase().includes(query)))
			.slice(0, 50);
	});

	// Clamp highlight when the filtered list shrinks under us.
	$effect(() => {
		if (highlightIndex >= filteredOptions.length) {
			highlightIndex = filteredOptions.length - 1;
		}
	});

	function addTag(tagId: string) {
		if (selectedIds.includes(tagId)) return;
		onChange([...selectedIds, tagId]);
		inputValue = '';
		highlightIndex = -1;
		inputElement?.focus();
	}

	function removeTag(tagId: string) {
		onChange(selectedIds.filter((existing) => existing !== tagId));
		inputElement?.focus();
	}

	function handleInputKeydown(e: KeyboardEvent) {
		if (disabled) return;

		if (e.key === 'ArrowDown') {
			e.preventDefault();
			isOpen = true;
			if (filteredOptions.length > 0) {
				highlightIndex = (highlightIndex + 1) % filteredOptions.length;
			}
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			isOpen = true;
			if (filteredOptions.length > 0) {
				highlightIndex =
					highlightIndex <= 0 ? filteredOptions.length - 1 : highlightIndex - 1;
			}
		} else if (e.key === 'Enter') {
			if (isOpen && highlightIndex >= 0 && highlightIndex < filteredOptions.length) {
				e.preventDefault();
				addTag(filteredOptions[highlightIndex].id);
			}
		} else if (e.key === 'Escape') {
			if (isOpen) {
				e.preventDefault();
				isOpen = false;
				highlightIndex = -1;
			}
		} else if (e.key === 'Backspace' && inputValue === '' && selectedIds.length > 0) {
			e.preventDefault();
			removeTag(selectedIds[selectedIds.length - 1]);
		}
	}

	function handleInputFocus() {
		if (!disabled) isOpen = true;
	}

	function handleInputBlur(e: FocusEvent) {
		// Don't close when focus moves inside the same picker (chip/option click).
		const next = e.relatedTarget as Node | null;
		if (next && wrapperElement?.contains(next)) return;
		isOpen = false;
		highlightIndex = -1;
	}

	function handleOptionMousedown(e: MouseEvent, tagId: string) {
		// Use mousedown (not click) so we add before the blur fires and closes.
		e.preventDefault();
		addTag(tagId);
	}

	let wrapperElement: HTMLDivElement | undefined = $state();

	const activeOptionId = $derived(
		highlightIndex >= 0 && highlightIndex < filteredOptions.length
			? `${listboxId}-option-${filteredOptions[highlightIndex].id}`
			: undefined
	);
</script>

<div bind:this={wrapperElement} class="relative" data-testid="tag-picker">
	<div
		class="flex flex-wrap items-center gap-1.5 rounded-lg border border-neutral-300 bg-white px-2 py-1.5 transition-colors focus-within:border-primary-500 focus-within:ring-2 focus-within:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-950 {disabled
			? 'cursor-not-allowed opacity-60'
			: ''}"
	>
		{#each selectedTags as tag (tag.id)}
			<span
				class="inline-flex items-center gap-1 rounded-full bg-primary-100 px-2.5 py-0.5 text-xs font-medium text-primary-800 dark:bg-primary-900 dark:text-primary-200"
				data-testid="tag-chip"
			>
				{tag.name}
				{#if !disabled}
					<button
						type="button"
						onclick={() => removeTag(tag.id)}
						class="inline-flex h-4 w-4 items-center justify-center rounded-full text-primary-700 hover:bg-primary-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:text-primary-200 dark:hover:bg-primary-800"
						aria-label={t('devices.tags.removeTag', { name: tag.name })}
					>
						<svg class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M6 18L18 6M6 6l12 12" />
						</svg>
					</button>
				{/if}
			</span>
		{/each}
		<input
			bind:this={inputElement}
			bind:value={inputValue}
			id={inputId}
			type="text"
			role="combobox"
			autocomplete="off"
			aria-autocomplete="list"
			aria-expanded={isOpen}
			aria-controls={listboxId}
			aria-activedescendant={activeOptionId}
			{disabled}
			placeholder={selectedIds.length === 0 ? t('devices.tags.inputPlaceholder') : ''}
			onkeydown={handleInputKeydown}
			onfocus={handleInputFocus}
			onblur={handleInputBlur}
			oninput={() => {
				isOpen = true;
				highlightIndex = -1;
			}}
			class="flex-1 min-w-[8rem] border-0 bg-transparent px-1 py-1 text-sm text-neutral-900 outline-none placeholder:text-neutral-500 disabled:cursor-not-allowed dark:text-neutral-100 dark:placeholder:text-neutral-500"
		/>
	</div>

	{#if isOpen && filteredOptions.length > 0}
		<ul
			id={listboxId}
			role="listbox"
			aria-label={t('devices.tags.listboxLabel')}
			class="absolute left-0 right-0 top-full z-20 mt-1 max-h-60 overflow-y-auto rounded-lg border border-neutral-200 bg-white py-1 shadow-lg dark:border-neutral-700 dark:bg-neutral-900"
		>
			{#each filteredOptions as option, index (option.id)}
				{@const highlighted = index === highlightIndex}
				<li
					id="{listboxId}-option-{option.id}"
					role="option"
					aria-selected={highlighted}
					class="cursor-pointer px-3 py-2 text-sm text-neutral-900 dark:text-neutral-100"
					class:bg-primary-100={highlighted}
					class:dark:bg-primary-900={highlighted}
					onmousedown={(e) => handleOptionMousedown(e, option.id)}
					onmouseenter={() => (highlightIndex = index)}
				>
					{option.name}
				</li>
			{/each}
		</ul>
	{:else if isOpen && inputValue.trim() !== '' && filteredOptions.length === 0}
		<div
			class="absolute left-0 right-0 top-full z-20 mt-1 rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-600 shadow-lg dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400"
			role="status"
		>
			{t('devices.tags.noMatches')}
		</div>
	{/if}
</div>
