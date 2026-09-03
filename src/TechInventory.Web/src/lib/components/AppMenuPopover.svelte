<!--
	AppMenuPopover.svelte — F045 §5.3 / Drake D-E / #134 / #144.

	Compact anchored popover replacing the former full-width mobile hamburger
	drawer. Mobile-only (`md:hidden`); desktop primary nav lives in the header
	as horizontal links and the Configuration group is in AppDesktopConfigMenu.

	PIXEL VALUES IN THIS FILE ARE RENDERED px, NOT NOMINAL TAILWIND px.
	tokens.css sets `html { font-size: 17px }` (D-137), so every rem-based
	utility renders 6.25% larger than its 16px-root name suggests: `h-11` is
	2.75rem = 46.75px, not 44px; `h-9` is 2.25rem = 38.25px, not 36px. Earlier
	revisions of this comment quoted the nominal 16px figures and were wrong.
	Multiply by 17 to check any number below.

	Density (R3 — supersedes the R2 correction below / rejected #166):
	The R2 pass kept every row at an exact `h-11` (46.75px) block. Rendered at
	phone scale that still left a wide blank band above and below the 14.88px
	label in each row, and the panel read as mostly empty space. R3 keeps the
	"one shared block, colour-only state difference" contract that R2
	established and shrinks the block itself: rows are an exact `h-9`
	(38.25px), the role="menu" container is `gap-0` (rows sit flush; the
	hover/active tint is what separates them), the panel is `p-1.5`, section
	dividers are `my-1`, and the Configuration caption, identity chip, and
	theme card lose their surplus padding to match.

	What that buys, on the 13-row Admin menu (5 primary + 6 config + Settings
	+ Sign Out): row pitch 48.88px → 38.25px (-22%), and the blank band
	between one label and the next 27.63px → 17px (-38%), since the 2.13px
	inter-row gap is gone and the remaining 17px is the tint pill's own
	padding rather than a gutter. Whole panel ≈820px → ≈652px, ~168px shorter,
	excluding the unchanged theme toggle.

	Still exactly one block size: `h-9` with no `py-*`, so active and inactive
	rows occupy identical geometry and the active tint cannot inflate a row
	(the #166 regression). Leading icons come from navIcons.ts. `rounded-lg`
	radius on all rows. Focus-visible ring is inset so it is not clipped by
	the panel's overflow-y-auto.

	Touch target: 38.25px tall x the full row width (w-64 = 272px, less the
	panel's 12.75px padding and 2px borders = 257.25px) clears WCAG 2.5.8
	Target Size (Minimum, AA — 24x24 CSS px) with room to spare. It is below
	the 46.75px that `h-11` was giving against 2.5.5 Target Size (Enhanced,
	AAA), which this menu now intentionally trades away for legible density
	per direct product direction; the AAA floor still holds for standalone
	controls (bottom nav, FABs, row ellipsis buttons, and the hamburger
	trigger itself, all `h-11` = 46.75px).
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

	// Single source of truth for row geometry (R3).
	// EXACT 38.25px block via `h-9` (2.25rem at the 17px root — see the file
	// header; it is NOT the 36px its Tailwind name implies) — no `min-h-*`,
	// no `py-*`.
	// Every menuitem row — primary, admin, Settings, Sign Out — applies this
	// exact string. Active/inactive state may only ever add colour via `class:`
	// bindings alongside it; it must never gain its own padding, margin,
	// height, min-height, gap, radius, border, or transform. A fixed height
	// with zero internal vertical padding is what keeps active and inactive
	// rows geometrically identical (#166 regression) — R3 only shrinks that
	// shared block from 46.75px to 38.25px to remove the blank band around
	// the label.
	const menuRowClass =
		'flex h-9 items-center gap-2 rounded-lg px-3 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-inset';

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
			class="absolute right-0 top-full mt-2 w-64 max-w-[calc(100vw-var(--space-8))] max-h-[85vh] overflow-y-auto overscroll-contain rounded-2xl border border-neutral-200/70 bg-white/95 p-1.5 shadow-xl backdrop-blur-md origin-top-right dark:border-neutral-800/70 dark:bg-neutral-950/95"
		>
			<!--
				`role="menu"` only wraps genuine command/navigation items (menuitem
				family). ThemeToggle is a real widget (radio-style buttons with its
				own aria-pressed semantics) and is not a valid menu-owned child per
				aria-required-children, so it lives outside this boundary, below,
				as a trailing sibling within the same popover panel.
			-->
			<div
				role="menu"
				tabindex="-1"
				aria-label={t('navigation.primary')}
				onkeydown={handleMenuKeydown}
				class="flex flex-col gap-0"
			>
			{#each visiblePrimaryNavItems as item (item.href)}
				{@const active = isNavItemActive(pathname, item)}
				{@const iconPath = navIconPaths[item.href]}
				<a
					data-menu-item
					href={item.href}
					role="menuitem"
					tabindex="-1"
					onclick={closeMenu}
					class={menuRowClass}
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
				<hr class="my-1 border-t border-neutral-200 dark:border-neutral-800" />
				<div class="px-3 pb-0.5 pt-1 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
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
						class={menuRowClass}
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

			<hr class="my-1 border-t border-neutral-200 dark:border-neutral-800" />
			<a
				data-menu-item
				href="/settings"
				role="menuitem"
				tabindex="-1"
				onclick={closeMenu}
				class={menuRowClass}
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
				<hr class="my-1 border-t border-neutral-200 dark:border-neutral-800" />
				<div class="my-0.5 flex items-center gap-2 rounded-full bg-neutral-100 px-3 py-1 dark:bg-neutral-800">
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
					class="{menuRowClass} w-full text-left text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					<svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d={signOutIconPath} />
					</svg>
					{t('auth.signOut.button')}
				</button>
			{/if}
			</div>

			<hr class="my-1.5 border-t border-neutral-200 dark:border-neutral-800" />
			<div
				role="group"
				aria-labelledby="app-menu-theme-heading"
				class="rounded-xl border border-neutral-200 bg-neutral-50 p-2 dark:border-neutral-800 dark:bg-neutral-900/70"
			>
				<p
					id="app-menu-theme-heading"
					class="mb-1.5 text-xs font-semibold uppercase tracking-wider text-neutral-600 dark:text-neutral-400"
				>
					{t('settings.theme.heading')}
				</p>
				<!--
				#147: iconOnly=true hides visible text labels in the compact popover,
				showing only Sun/Moon/System icons. Accessible names (aria-label) are
				preserved on each button. Desktop Settings page renders ThemeToggle
				without this prop so labels remain visible there.
			-->
			<ThemeToggle iconOnly />
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
