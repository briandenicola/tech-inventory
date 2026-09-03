<!--
	AppMenuPopover.svelte — F045 §5.3 / Drake D-E / #134 / #144.

	Compact anchored popover replacing the former full-width mobile hamburger
	drawer. Mobile-only (`md:hidden`); desktop primary nav lives in the header
	as horizontal links and the Configuration group is in AppDesktopConfigMenu.

	Density (#144): rows use py-1.5 (vs the former py-2.5) with a 44px min-h
	tap target preserved via min-h-11. Leading icons are rendered from
	navIcons.ts so the role="menu" composite stays accessible (icons are
	aria-hidden; labels carry the name).
-->
<script lang="ts">
	import { tick, untrack } from 'svelte';
	import { t } from '$lib/i18n';
	import {
		adminNavItems,
		getVisibleNavItems,
		isNavItemActive,
		primaryNavItems
	} from '$lib/navigation/appNav';
	import { navIconPaths, settingsIconPath, signOutIconPath } from '$lib/navigation/navIcons';
	import type { CurrentUser } from '$lib/stores/auth';
	import ThemeToggle from '$lib/components/ThemeToggle.svelte';

	interface Props {
		pathname: string;
		currentUser: CurrentUser | null;
		onSignOut: () => void;
		/** Lets the layout raise the header's z-index while the popover is open (Drake D-E stacking fix). */
		onOpenChange?: (open: boolean) => void;
	}

	let { pathname, currentUser, onSignOut, onOpenChange }: Props = $props();

	let isOpen = $state(false);
	let rootElement = $state<HTMLDivElement | null>(null);
	let triggerElement = $state<HTMLButtonElement | null>(null);

	const currentRole = $derived(currentUser?.role ?? null);
	const visiblePrimaryNavItems = $derived(getVisibleNavItems(primaryNavItems, currentRole));
	const visibleAdminNavItems = $derived(getVisibleNavItems(adminNavItems, currentRole));
	const settingsActive = $derived(pathname.startsWith('/settings'));

	async function openMenu() {
		isOpen = true;
		onOpenChange?.(true);
		await tick();
		rootElement?.querySelector<HTMLElement>('[data-menu-item]')?.focus();
	}

	function closeMenu() {
		if (!isOpen) return;
		isOpen = false;
		onOpenChange?.(false);
	}

	function closeAndReturnFocus() {
		closeMenu();
		triggerElement?.focus();
	}

	async function toggleMenu() {
		if (isOpen) {
			closeAndReturnFocus();
			return;
		}
		await openMenu();
	}

	function handleWindowClick(event: MouseEvent) {
		if (!isOpen || !rootElement) return;
		const target = event.target;
		if (target instanceof Node && !rootElement.contains(target)) {
			closeMenu();
		}
	}

	function handleWindowKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape' && isOpen) {
			event.stopPropagation();
			closeAndReturnFocus();
		}
	}

	function menuItems(): HTMLElement[] {
		return Array.from(rootElement?.querySelectorAll<HTMLElement>('[data-menu-item]') ?? []);
	}

	function handleMenuKeydown(event: KeyboardEvent) {
		const items = menuItems();
		if (items.length === 0) return;
		const currentIndex = items.findIndex((el) => el === document.activeElement);

		switch (event.key) {
			case 'ArrowDown':
				event.preventDefault();
				items[(currentIndex + 1) % items.length]?.focus();
				break;
			case 'ArrowUp':
				event.preventDefault();
				items[(currentIndex - 1 + items.length) % items.length]?.focus();
				break;
			case 'Home':
				event.preventDefault();
				items[0]?.focus();
				break;
			case 'End':
				event.preventDefault();
				items[items.length - 1]?.focus();
				break;
		}
	}

	// Close on route change (mirrors the drawer's prior $effect in +layout.svelte).
	// `untrack` is required: closeMenu() reads `isOpen` for its early-return
	// guard, and without untrack that read makes this effect depend on
	// `isOpen` too — so opening the menu (isOpen becomes true) would
	// immediately re-run this effect and self-close the panel it just opened.
	$effect(() => {
		// eslint-disable-next-line @typescript-eslint/no-unused-expressions
		pathname;
		untrack(() => closeMenu());
	});
</script>

<svelte:window onclick={handleWindowClick} onkeydown={handleWindowKeydown} />

<div class="relative md:hidden" bind:this={rootElement}>
	<button
		type="button"
		bind:this={triggerElement}
		class="inline-flex h-11 w-11 items-center justify-center rounded-full text-neutral-700 transition-colors hover:bg-neutral-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:text-neutral-300 dark:hover:bg-neutral-800"
		onclick={() => void toggleMenu()}
		aria-label={isOpen ? t('header.closeMenu') : t('header.menu')}
		aria-expanded={isOpen}
		aria-haspopup="menu"
		aria-controls="app-menu-popover"
	>
		<svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
			{#if isOpen}
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
			{:else}
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
			{/if}
		</svg>
	</button>

	{#if isOpen}
		<div
			id="app-menu-popover"
			style="z-index: var(--z-popover);"
			class="absolute right-0 top-full mt-2 w-64 max-w-[calc(100vw-var(--space-8))] max-h-[85vh] overflow-y-auto overscroll-contain rounded-2xl border border-neutral-200/70 bg-white/95 p-2 shadow-xl backdrop-blur-md origin-top-right dark:border-neutral-800/70 dark:bg-neutral-950/95"
		>
			<!--
				`role="menu"` only wraps genuine command/navigation items (menuitem
				family). ThemeToggle is a real widget (radio-style buttons with its
				own aria-pressed semantics) and is not a valid menu-owned child per
				aria-required-children, so it lives outside this boundary, below,
				as a trailing sibling within the same popover panel.
			-->
			<div role="menu" tabindex="-1" aria-label={t('navigation.primary')} onkeydown={handleMenuKeydown}>
			{#each visiblePrimaryNavItems as item (item.href)}
				{@const active = isNavItemActive(pathname, item)}
				{@const iconPath = navIconPaths[item.href]}
				<a
					data-menu-item
					href={item.href}
					role="menuitem"
					tabindex="-1"
					onclick={closeMenu}
					class="flex min-h-11 items-center gap-2 rounded-xl px-3 py-1.5 text-sm font-medium transition-colors"
					class:bg-primary-50={active}
					class:text-primary-700={active}
					class:dark:bg-primary-900={active}
					class:dark:text-primary-200={active}
					class:text-neutral-700={!active}
					class:hover:bg-neutral-100={!active}
					class:dark:text-neutral-300={!active}
					class:dark:hover:bg-neutral-800={!active}
				>
					{#if iconPath}
						<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d={iconPath} />
						</svg>
					{/if}
					{t(item.labelKey)}
				</a>
			{/each}

			{#if visibleAdminNavItems.length > 0}
				<hr class="my-1.5 border-t border-neutral-200 dark:border-neutral-800" />
				<div class="px-3 py-1 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
					{t('navigation.configuration')}
				</div>
				{#each visibleAdminNavItems as item (item.href)}
					{@const active = isNavItemActive(pathname, item)}
					{@const iconPath = navIconPaths[item.href]}
					<a
						data-menu-item
						href={item.href}
						role="menuitem"
						tabindex="-1"
						onclick={closeMenu}
						class="flex min-h-11 items-center gap-2 rounded-xl px-3 py-1.5 text-sm font-medium transition-colors"
						class:bg-primary-50={active}
						class:text-primary-700={active}
						class:dark:bg-primary-900={active}
						class:dark:text-primary-200={active}
						class:text-neutral-700={!active}
						class:hover:bg-neutral-100={!active}
						class:dark:text-neutral-300={!active}
						class:dark:hover:bg-neutral-800={!active}
					>
						{#if iconPath}
							<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d={iconPath} />
							</svg>
						{/if}
						{t(item.labelKey)}
					</a>
				{/each}
			{/if}

			<hr class="my-1.5 border-t border-neutral-200 dark:border-neutral-800" />
			<a
				data-menu-item
				href="/settings"
				role="menuitem"
				tabindex="-1"
				onclick={closeMenu}
				class="flex min-h-11 items-center gap-2 rounded-xl px-3 py-1.5 text-sm font-medium transition-colors"
				class:bg-primary-50={settingsActive}
				class:text-primary-700={settingsActive}
				class:dark:bg-primary-900={settingsActive}
				class:dark:text-primary-200={settingsActive}
				class:text-neutral-700={!settingsActive}
				class:hover:bg-neutral-100={!settingsActive}
				class:dark:text-neutral-300={!settingsActive}
				class:dark:hover:bg-neutral-800={!settingsActive}
			>
				<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d={settingsIconPath} />
				</svg>
				{t('navigation.settings')}
			</a>

			{#if currentUser}
				<hr class="my-1.5 border-t border-neutral-200 dark:border-neutral-800" />
				<div class="flex items-center gap-2 rounded-full bg-neutral-100 px-3 py-1.5 dark:bg-neutral-800">
					<span class="text-sm font-medium text-neutral-900 dark:text-neutral-100">
						{currentUser.displayName}
					</span>
					<span
						class="rounded-full px-2 py-0.5 text-xs font-medium"
						class:bg-primary-100={currentUser.role === 'Admin'}
						class:text-primary-700={currentUser.role === 'Admin'}
						class:dark:bg-primary-900={currentUser.role === 'Admin'}
						class:dark:text-primary-300={currentUser.role === 'Admin'}
						class:bg-success-100={currentUser.role === 'Member'}
						class:text-success-700={currentUser.role === 'Member'}
						class:dark:bg-success-900={currentUser.role === 'Member'}
						class:dark:text-success-300={currentUser.role === 'Member'}
						class:bg-neutral-200={currentUser.role === 'Viewer'}
						class:text-neutral-700={currentUser.role === 'Viewer'}
						class:dark:bg-neutral-700={currentUser.role === 'Viewer'}
						class:dark:text-neutral-300={currentUser.role === 'Viewer'}
					>
						{currentUser.role}
					</span>
				</div>
				<button
					data-menu-item
					type="button"
					role="menuitem"
					tabindex="-1"
					onclick={() => {
						closeMenu();
						onSignOut();
					}}
					class="flex min-h-11 w-full items-center gap-2 rounded-xl px-3 py-1.5 text-left text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d={signOutIconPath} />
					</svg>
					{t('auth.signOut.button')}
				</button>
			{/if}
			</div>

			<hr class="my-2 border-t border-neutral-200 dark:border-neutral-800" />
			<div
				role="group"
				aria-labelledby="app-menu-theme-heading"
				class="rounded-2xl border border-neutral-200 bg-neutral-50 p-3 dark:border-neutral-800 dark:bg-neutral-900/70"
			>
				<p
					id="app-menu-theme-heading"
					class="mb-2 text-xs font-semibold uppercase tracking-wider text-neutral-600 dark:text-neutral-400"
				>
					{t('settings.theme.heading')}
				</p>
				<ThemeToggle />
			</div>
		</div>
	{/if}
</div>

<style>
	#app-menu-popover {
		/* Entrance only, gated. Colour transitions on items are not motion. */
		transform-origin: top right;
	}

	@media (prefers-reduced-motion: no-preference) {
		#app-menu-popover {
			animation: menu-pop var(--duration-fast) var(--ease-out);
		}
	}
	@keyframes menu-pop {
		from {
			transform: scale(0.96);
			opacity: 0;
		}
	}
</style>
