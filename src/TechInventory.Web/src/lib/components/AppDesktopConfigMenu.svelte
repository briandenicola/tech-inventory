<!--
	AppDesktopConfigMenu.svelte — #134 / #144.

	Desktop-only compact popover for Admin-gated Configuration destinations
	(Brands, Categories, Locations, Networks, Owners, Tags). The trigger is a
	gear-icon button placed in the header right cluster; it is invisible on
	mobile (`md:hidden` on the wrapper) because AppMenuPopover already carries
	these items in the mobile/PWA path.

	Keyboard model lifted from AppMenuPopover (Drake D-E): roving focus via
	data-menu-item, Home/End/Arrow, Escape closes + returns focus, outside
	click closes. No body-scroll lock; no full-screen backdrop.
-->
<script lang="ts">
	import { tick, untrack } from 'svelte';
	import { t } from '$lib/i18n';
	import { adminNavItems, getVisibleNavItems, isNavItemActive } from '$lib/navigation/appNav';
	import { navIconPaths, settingsIconPath } from '$lib/navigation/navIcons';
	import type { CurrentUser } from '$lib/stores/auth';

	interface Props {
		pathname: string;
		currentUser: CurrentUser | null;
	}

	let { pathname, currentUser }: Props = $props();

	let isOpen = $state(false);
	let rootElement = $state<HTMLDivElement | null>(null);
	let triggerElement = $state<HTMLButtonElement | null>(null);

	const currentRole = $derived(currentUser?.role ?? null);
	const visibleAdminNavItems = $derived(getVisibleNavItems(adminNavItems, currentRole));

	/**
	 * Whether the current route is one this menu links to. The other nav items get an
	 * active tint from aria-current; this trigger is a button, not a link, so it has to
	 * work that out itself — without it, navigating to Brands would leave the whole nav
	 * row looking as though nothing were selected.
	 */
	const anyAdminRouteActive = $derived(
		visibleAdminNavItems.some((item) => isNavItemActive(pathname, item))
	);

	async function openMenu() {
		isOpen = true;
		await tick();
		rootElement?.querySelector<HTMLElement>('[data-menu-item]')?.focus();
	}

	function closeMenu() {
		if (!isOpen) return;
		isOpen = false;
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

	$effect(() => {
		// eslint-disable-next-line @typescript-eslint/no-unused-expressions
		pathname;
		untrack(() => closeMenu());
	});
</script>

<svelte:window onclick={handleWindowClick} onkeydown={handleWindowKeydown} />

{#if visibleAdminNavItems.length > 0}
	<div class="relative" bind:this={rootElement}>
		<!--
			Styled to match the primary nav links it now sits beside (same padding,
			radius, size, hover and active colours) rather than the round icon button it
			was in the right-hand cluster — the point of moving it into the row was that
			it should not read as a different kind of control.

			Labelled "Config", not "Settings": this menu opens Brands/Categories/
			Locations/Networks/Owners/Tags, while /settings is a separate page reached
			from the user dropdown. Two controls both called Settings would be a trap.
			The visible label replaces the old aria-label rather than sitting alongside
			it, so the accessible name has exactly one source.
		-->
		<button
			type="button"
			bind:this={triggerElement}
			class="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium transition-colors duration-150"
			class:bg-primary-50={isOpen || anyAdminRouteActive}
			class:text-primary-700={isOpen || anyAdminRouteActive}
			class:dark:bg-primary-900={isOpen || anyAdminRouteActive}
			class:dark:text-primary-200={isOpen || anyAdminRouteActive}
			class:text-neutral-600={!isOpen && !anyAdminRouteActive}
			class:hover:bg-neutral-100={!isOpen && !anyAdminRouteActive}
			class:hover:text-neutral-900={!isOpen && !anyAdminRouteActive}
			class:dark:text-neutral-400={!isOpen && !anyAdminRouteActive}
			class:dark:hover:bg-neutral-800={!isOpen && !anyAdminRouteActive}
			class:dark:hover:text-neutral-100={!isOpen && !anyAdminRouteActive}
			onclick={() => void toggleMenu()}
			aria-expanded={isOpen}
			aria-haspopup="menu"
			aria-controls="desktop-config-menu"
		>
			<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d={settingsIconPath} />
			</svg>
			{t('navigation.config')}
		</button>

		{#if isOpen}
			<div
				id="desktop-config-menu"
				style="z-index: var(--z-popover);"
				class="absolute right-0 top-full mt-2 w-52 rounded-2xl border border-neutral-200/70 bg-white/95 p-2 shadow-xl backdrop-blur-md origin-top-right dark:border-neutral-800/70 dark:bg-neutral-950/95"
			>
				<div role="menu" tabindex="-1" aria-label={t('navigation.configuration')} onkeydown={handleMenuKeydown}>
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
							aria-current={active ? 'page' : undefined}
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
				</div>
			</div>
		{/if}
	</div>
{/if}

<style>
	@media (prefers-reduced-motion: no-preference) {
		#desktop-config-menu {
			animation: config-pop var(--duration-fast) var(--ease-out);
		}
	}
	@keyframes config-pop {
		from {
			transform: scale(0.96);
			opacity: 0;
		}
	}
</style>
