<!--
	AppBottomNav.svelte — F045 §5.2 / Drake D-A through D-D / #143.

	Installed-PWA bottom navigation: an OPAQUE, FULL-WIDTH bar anchored to
	the bottom edge. Contains Home / (Add) / Reports / Settings as equal-width
	items. Replaces the former floating pill + separate Settings bubble.

	Rendered by the authenticated layout as a sibling of `<main>` — never
	inside `PullToRefresh`, which animates `transform` and would become an
	unwanted containing block for this `position: fixed` element (the same
	class of bug covered by `PullToRefresh.containing-block.test.ts`).

	Opacity decision (#143): the translucent pill allowed device rows to show
	through the bar. The opaque surface occludes content completely and removes
	the need for backdrop-filter, which also fails gracefully on older
	Android WebViews without a separate @supports block.

	Safe-area: `padding-bottom: env(safe-area-inset-bottom, 0px)` extends the
	bar background behind the iOS home indicator without pushing the icon row
	downward. The layout <main> adds matching bottom padding so the last row
	scrolls fully clear of the bar.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { memberRoles, isNavItemActive, isNavItemVisible } from '$lib/navigation/appNav';
	import type { AppNavItem } from '$lib/navigation/appNav';
	import type { CurrentUser } from '$lib/stores/auth';

	interface Props {
		pathname: string;
		currentUser: CurrentUser | null;
		onAdd: () => void;
	}

	let { pathname, currentUser, onAdd }: Props = $props();

	const homeItem: AppNavItem = { href: '/devices', labelKey: 'navigation.home', activePaths: ['/devices'] };
	const reportsItem: AppNavItem = {
		href: '/reports',
		labelKey: 'navigation.reports',
		activePaths: ['/reports']
	};
	const settingsItem: AppNavItem = {
		href: '/settings',
		labelKey: 'navigation.settings',
		activePaths: ['/settings']
	};
	// F045 §5.2: Add is role-gated the same as the rest of app chrome —
	// Viewers never get a create affordance, so the pill collapses to two
	// items for them (flex: 1 1 0 handles the two-item case with no extra CSS).
	const addVisibleItem: AppNavItem = { href: '', labelKey: '', activePaths: [], roles: memberRoles };

	const role = $derived(currentUser?.role ?? null);
	const canAdd = $derived(isNavItemVisible(addVisibleItem, role));
	const homeActive = $derived(isNavItemActive(pathname, homeItem));
	const reportsActive = $derived(isNavItemActive(pathname, reportsItem));
	const settingsActive = $derived(isNavItemActive(pathname, settingsItem));
</script>

<nav class="app-bottom-nav" aria-label={t('navigation.primary')}>
	<!-- Home -->
	<a
		href={homeItem.href}
		class="app-bottom-nav__item"
		aria-current={homeActive ? 'page' : undefined}
		data-active={homeActive}
	>
		<svg
			class="h-6 w-6"
			fill="none"
			viewBox="0 0 24 24"
			stroke="currentColor"
			stroke-width="1.8"
			stroke-linecap="round"
			stroke-linejoin="round"
			aria-hidden="true"
		>
			<path
				d="M3 12l9-9 9 9M5 10v10a1 1 0 001 1h3a1 1 0 001-1v-5a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 001 1h3a1 1 0 001-1V10"
			/>
		</svg>
		<span class="app-bottom-nav__label">{t('navigation.home')}</span>
	</a>

	<!-- Add (role-gated: Member+) -->
	{#if canAdd}
		<button
			type="button"
			class="app-bottom-nav__item"
			onclick={onAdd}
			aria-label={t('common.actions.add')}
		>
			<svg
				class="h-6 w-6"
				fill="none"
				viewBox="0 0 24 24"
				stroke="currentColor"
				stroke-width="1.8"
				stroke-linecap="round"
				stroke-linejoin="round"
				aria-hidden="true"
			>
				<path d="M12 4v16m8-8H4" />
			</svg>
			<span class="app-bottom-nav__label">{t('common.actions.add')}</span>
		</button>
	{/if}

	<!-- Reports -->
	<a
		href={reportsItem.href}
		class="app-bottom-nav__item"
		aria-current={reportsActive ? 'page' : undefined}
		data-active={reportsActive}
	>
		<svg
			class="h-6 w-6"
			fill="none"
			viewBox="0 0 24 24"
			stroke="currentColor"
			stroke-width="1.8"
			stroke-linecap="round"
			stroke-linejoin="round"
			aria-hidden="true"
		>
			<path
				d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
			/>
		</svg>
		<span class="app-bottom-nav__label">{t('navigation.reports')}</span>
	</a>

	<!-- Settings (merged into bar — no longer a separate bubble) -->
	<a
		href={settingsItem.href}
		class="app-bottom-nav__item"
		aria-current={settingsActive ? 'page' : undefined}
		data-active={settingsActive}
		aria-label={t('navigation.settings')}
	>
		<svg
			class="h-6 w-6"
			fill="none"
			viewBox="0 0 24 24"
			stroke="currentColor"
			stroke-width="1.5"
			stroke-linecap="round"
			stroke-linejoin="round"
			aria-hidden="true"
		>
			<path
				d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
			/>
			<path d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
		</svg>
		<span class="app-bottom-nav__label">{t('navigation.settings')}</span>
	</a>
</nav>

<style>
	/*
	 * #143: Opaque full-width bottom bar.
	 *
	 * Design rationale:
	 *   - position: fixed; left/right: 0 → full viewport width, no side gaps.
	 *   - background: var(--color-bg) → fully opaque; no translucency, no
	 *     backdrop-filter; content is completely occluded behind the bar.
	 *   - padding-bottom: env(safe-area-inset-bottom) → extends bar background
	 *     behind the iOS home indicator; items stay in the un-padded area.
	 *   - border-top instead of shadow → reads clearly in both themes.
	 *
	 * Foreground colours reuse the same token vocabulary as the former pill so
	 * active/inactive contrast budget is unchanged.
	 */
	.app-bottom-nav {
		--nav-hairline: var(--color-border);
		--nav-fg-inactive: var(--app-color-neutral-600);
		--nav-fg-active: var(--app-color-primary-700);
		--nav-chip: color-mix(in srgb, var(--app-color-primary-500) 12%, transparent);
		--nav-press: rgb(0 0 0 / 0.05);

		position: fixed;
		left: 0;
		right: 0;
		bottom: 0;
		z-index: var(--z-fixed);
		display: flex;
		align-items: stretch;
		background: var(--color-bg);
		border-top: 1px solid var(--nav-hairline);
		/* Extends bar surface behind the iOS home indicator */
		padding-bottom: env(safe-area-inset-bottom, 0px);
	}

	:root[data-theme='dark'] .app-bottom-nav {
		--nav-fg-inactive: var(--app-color-neutral-300);
		--nav-fg-active: var(--app-color-primary-200);
		--nav-chip: color-mix(in srgb, var(--app-color-primary-500) 22%, transparent);
		--nav-press: rgb(255 255 255 / 0.08);
	}

	@media (prefers-color-scheme: dark) {
		:root:not([data-theme='light']) .app-bottom-nav {
			--nav-fg-inactive: var(--app-color-neutral-300);
			--nav-fg-active: var(--app-color-primary-200);
			--nav-chip: color-mix(in srgb, var(--app-color-primary-500) 22%, transparent);
			--nav-press: rgb(255 255 255 / 0.08);
		}
	}

	@media (prefers-reduced-motion: no-preference) {
		.app-bottom-nav {
			animation: nav-rise var(--duration-base) var(--ease-out);
		}
	}
	@keyframes nav-rise {
		from {
			transform: translateY(8px);
			opacity: 0;
		}
	}

	.app-bottom-nav__item {
		flex: 1 1 0;
		min-width: 0;
		/* 3rem = 48px — clears the 44px minimum tap target (constitution §6.5.6) */
		min-height: 3rem;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 2px;
		padding: var(--space-1) var(--space-2);
		color: var(--nav-fg-inactive);
		transition:
			background-color var(--duration-fast) var(--ease-out),
			color var(--duration-fast) var(--ease-out);
	}

	.app-bottom-nav__item[aria-current='page'],
	.app-bottom-nav__item[data-active='true'] {
		background: var(--nav-chip);
		color: var(--nav-fg-active);
	}

	.app-bottom-nav__item:active {
		background: var(--nav-press);
	}

	.app-bottom-nav__item:focus-visible {
		outline: 2px solid var(--color-border-focus);
		outline-offset: 2px;
	}

	.app-bottom-nav__label {
		font-size: 0.625rem; /* 10.6px @ 17px root */
		line-height: 12px;
		font-weight: var(--font-medium);
		letter-spacing: 0.005em;
		max-width: 100%;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.app-bottom-nav__item[aria-current='page'] .app-bottom-nav__label {
		font-weight: var(--font-semibold);
	}
</style>
