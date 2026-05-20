<script lang="ts">
	import { tick } from 'svelte';

	type ActionItemTone = 'default' | 'primary' | 'warning' | 'danger';

	export interface ActionOverflowMenuItem {
		id: string;
		label: string;
		href?: string;
		onSelect?: () => void;
		tone?: ActionItemTone;
	}

	interface Props {
		items: ActionOverflowMenuItem[];
		buttonLabel: string;
		menuTitle: string;
	}

	let { items, buttonLabel, menuTitle }: Props = $props();

	let isOpen = $state(false);
	let rootElement = $state<HTMLDivElement | null>(null);

	const hasItems = $derived(items.length > 0);

	function getDesktopClass(tone: ActionItemTone = 'default'): string {
		switch (tone) {
			case 'primary':
				return 'flex min-h-11 w-full items-center rounded-xl px-3 py-2 text-left text-sm font-medium text-primary-700 transition-colors hover:bg-primary-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-primary-300 dark:hover:bg-neutral-800';
			case 'warning':
				return 'flex min-h-11 w-full items-center rounded-xl px-3 py-2 text-left text-sm font-medium text-warning-700 transition-colors hover:bg-warning-50 focus:outline-none focus:ring-2 focus:ring-warning-500 dark:text-warning-300 dark:hover:bg-neutral-800';
			case 'danger':
				return 'flex min-h-11 w-full items-center rounded-xl px-3 py-2 text-left text-sm font-medium text-danger-700 transition-colors hover:bg-danger-50 focus:outline-none focus:ring-2 focus:ring-danger-500 dark:text-danger-300 dark:hover:bg-neutral-800';
			default:
				return 'flex min-h-11 w-full items-center rounded-xl px-3 py-2 text-left text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-200 dark:hover:bg-neutral-800';
		}
	}

	function getMobileClass(tone: ActionItemTone = 'default'): string {
		switch (tone) {
			case 'primary':
				return 'flex min-h-11 w-full items-center rounded-2xl border border-primary-200 px-4 py-3 text-left text-sm font-medium text-primary-700 transition-colors hover:bg-primary-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-primary-900 dark:text-primary-300 dark:hover:bg-neutral-800';
			case 'warning':
				return 'flex min-h-11 w-full items-center rounded-2xl border border-warning-200 px-4 py-3 text-left text-sm font-medium text-warning-700 transition-colors hover:bg-warning-50 focus:outline-none focus:ring-2 focus:ring-warning-500 dark:border-warning-900 dark:text-warning-300 dark:hover:bg-neutral-800';
			case 'danger':
				return 'flex min-h-11 w-full items-center rounded-2xl border border-danger-200 px-4 py-3 text-left text-sm font-medium text-danger-700 transition-colors hover:bg-danger-50 focus:outline-none focus:ring-2 focus:ring-danger-500 dark:border-danger-900 dark:text-danger-300 dark:hover:bg-neutral-800';
			default:
				return 'flex min-h-11 w-full items-center rounded-2xl border border-neutral-200 px-4 py-3 text-left text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-800 dark:text-neutral-200 dark:hover:bg-neutral-800';
		}
	}

	async function openMenu() {
		isOpen = true;
		await tick();
		rootElement?.querySelector<HTMLElement>('[data-overflow-item]')?.focus();
	}

	function closeMenu() {
		isOpen = false;
	}

	async function toggleMenu() {
		if (isOpen) {
			closeMenu();
			return;
		}

		await openMenu();
	}

	function handleWindowClick(event: MouseEvent) {
		if (!isOpen || !rootElement) {
			return;
		}

		const target = event.target;
		if (target instanceof Node && !rootElement.contains(target)) {
			closeMenu();
		}
	}

	function handleWindowKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape' && isOpen) {
			event.stopPropagation();
			closeMenu();
		}
	}

	function handleSelect(item: ActionOverflowMenuItem) {
		closeMenu();
		item.onSelect?.();
	}
</script>

<svelte:window onclick={handleWindowClick} onkeydown={handleWindowKeydown} />

{#if hasItems}
	<div class="relative shrink-0" bind:this={rootElement}>
		<button
			type="button"
			onclick={() => void toggleMenu()}
			class="inline-flex h-11 w-11 items-center justify-center rounded-full border border-neutral-300 bg-white text-neutral-700 transition-colors hover:bg-neutral-100 hover:text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
			aria-expanded={isOpen}
			aria-haspopup="true"
			aria-label={buttonLabel}
		>
			<svg class="h-5 w-5" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
				<path d="M10 6a1.5 1.5 0 110-3 1.5 1.5 0 010 3zm0 5.5A1.5 1.5 0 1010 8.5a1.5 1.5 0 000 3zm0 5.5a1.5 1.5 0 100-3 1.5 1.5 0 000 3z" />
			</svg>
		</button>

		{#if isOpen}
			<button
				type="button"
				class="fixed inset-0 bg-black/40 md:hidden"
				style="z-index: var(--z-modal-backdrop);"
				onclick={closeMenu}
				aria-label={buttonLabel}
			></button>

			<div
				class="absolute right-0 top-full z-10 mt-2 hidden w-64 overflow-hidden rounded-2xl border border-neutral-200 bg-white p-2 shadow-xl dark:border-neutral-800 dark:bg-neutral-950 md:block"
				style="z-index: var(--z-dropdown);"
			>
				<div class="space-y-1">
					{#each items as item (item.id)}
						{#if item.href}
							<a
								data-overflow-item
								href={item.href}
								onclick={closeMenu}
								class={getDesktopClass(item.tone)}
							>
								{item.label}
							</a>
						{:else}
							<button
								data-overflow-item
								type="button"
								onclick={() => handleSelect(item)}
								class={getDesktopClass(item.tone)}
							>
								{item.label}
							</button>
						{/if}
					{/each}
				</div>
			</div>

			<div
				class="fixed inset-x-0 bottom-0 rounded-t-3xl border border-neutral-200 bg-white px-5 pb-6 pt-4 shadow-2xl dark:border-neutral-800 dark:bg-neutral-950 md:hidden"
				style="z-index: var(--z-modal);"
			>
				<div class="mx-auto mb-4 h-1.5 w-12 rounded-full bg-neutral-300 dark:bg-neutral-700"></div>
				<h2 class="text-base font-semibold text-neutral-900 dark:text-neutral-50">{menuTitle}</h2>
				<div class="mt-4 space-y-2">
					{#each items as item (item.id)}
						{#if item.href}
							<a
								data-overflow-item
								href={item.href}
								onclick={closeMenu}
								class={getMobileClass(item.tone)}
							>
								{item.label}
							</a>
						{:else}
							<button
								data-overflow-item
								type="button"
								onclick={() => handleSelect(item)}
								class={getMobileClass(item.tone)}
							>
								{item.label}
							</button>
						{/if}
					{/each}
				</div>
			</div>
		{/if}
	</div>
{/if}
