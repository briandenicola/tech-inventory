<script lang="ts">
	import ActionOverflowMenu from '$lib/components/ActionOverflowMenu.svelte';

	interface ActionOverflowMenuItem {
		id: string;
		label: string;
		href?: string;
		onSelect?: () => void;
		tone?: 'default' | 'primary' | 'warning' | 'danger';
	}

	export interface ResponsiveListCardField {
		key: string;
		label: string;
		value?: string | null;
		href?: string;
		monospace?: boolean;
		valueClass?: string;
		chipText?: string | null;
		chipClass?: string;
		chipStyle?: string;
	}

	export interface ResponsiveListCardBadge {
		text: string;
		className?: string;
	}

	interface Props {
		title: string;
		titleId: string;
		titlePrefix?: string | null;
		subtitle?: string | null;
		selected?: boolean;
		checked?: boolean;
		selectLabel?: string | null;
		onToggleSelect?: (() => void) | undefined;
		badge?: ResponsiveListCardBadge | null;
		fields?: ResponsiveListCardField[];
		actionItems?: ActionOverflowMenuItem[];
		actionMenuLabel?: string;
		actionMenuTitle?: string;
	}

	let {
		title,
		titleId,
		titlePrefix = null,
		subtitle = null,
		selected = false,
		checked = false,
		selectLabel = null,
		onToggleSelect,
		badge = null,
		fields = [],
		actionItems = [],
		actionMenuLabel = '',
		actionMenuTitle = ''
	}: Props = $props();

	const visibleFields = $derived(
		fields.filter((field) => {
			if (field.chipText && field.chipText.trim().length > 0) {
				return true;
			}

			return (field.value ?? '').trim().length > 0;
		})
	);
</script>

<article
	class="rounded-2xl border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950 {selected
		? 'border-primary-400 bg-primary-50/70 ring-1 ring-primary-300 dark:border-primary-700 dark:bg-primary-950/20 dark:ring-primary-800'
		: ''}"
	aria-labelledby={titleId}
>
	<header class="flex items-start gap-3">
		{#if selectLabel}
			<label class="mt-0.5 flex h-11 w-11 shrink-0 items-center justify-center rounded-full border border-neutral-200 bg-neutral-50 dark:border-neutral-700 dark:bg-neutral-900">
				<input
					type="checkbox"
					class="h-5 w-5 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
					checked={checked}
					onchange={() => onToggleSelect?.()}
					aria-label={selectLabel}
				/>
			</label>
		{/if}

		<div class="min-w-0 flex-1">
			<div class="flex items-start gap-2">
				{#if titlePrefix}
					<span class="text-lg leading-none" aria-hidden="true">{titlePrefix}</span>
				{/if}
				<h3 id={titleId} class="min-w-0 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
					{title}
				</h3>
			</div>
			{#if subtitle}
				<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">{subtitle}</p>
			{/if}
		</div>

		<div class="ml-auto flex items-start gap-2">
			{#if badge}
				<span class={badge.className}>{badge.text}</span>
			{/if}
			{#if actionItems.length > 0}
				<ActionOverflowMenu
					items={actionItems}
					buttonLabel={actionMenuLabel}
					menuTitle={actionMenuTitle}
				/>
			{/if}
		</div>
	</header>

	{#if visibleFields.length > 0}
		<dl class="mt-3 space-y-2">
			{#each visibleFields as field (field.key)}
				<div class="rounded-xl bg-neutral-50/70 px-3 py-2 dark:bg-neutral-900/70">
					<dt class="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
						{field.label}
					</dt>
					<dd class="mt-1 text-sm text-neutral-800 dark:text-neutral-200 {field.monospace ? 'font-mono' : ''} {field.valueClass ?? ''}">
						{#if field.chipText}
							<span class={field.chipClass} style={field.chipStyle}>{field.chipText}</span>
						{:else if field.href && field.value}
							<a
								href={field.href}
								target="_blank"
								rel="noopener noreferrer"
								class="break-all text-primary-600 hover:underline dark:text-primary-400"
							>
								{field.value}
							</a>
						{:else}
							{field.value}
						{/if}
					</dd>
				</div>
			{/each}
		</dl>
	{/if}
</article>
