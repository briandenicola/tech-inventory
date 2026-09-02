<!--
	AppBottomNav.svelte — F045 §5.2 / Drake D-A through D-D.

	Installed-PWA bottom navigation: a pill (Home / Add / Reports) plus a
	separate circular Settings bubble. Rendered by the authenticated layout as
	a sibling of `<main>` — never inside `PullToRefresh`, which animates
	`transform` and would become an unwanted containing block for this
	`position: fixed` element (the same class of bug covered by
	`PullToRefresh.containing-block.test.ts`).

	Visual recipe, contrast budget, and geometry are Drake's spec
	(`.squad/decisions/inbox/drake-pwa-visual-rules.md`, D-A…D-D) — this file
	implements it verbatim, including the two-selector dark-theme gating
	pattern already used in `tokens.css`.
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

<nav class="app-nav" aria-label={t('navigation.primary')}>
	<div class="app-nav__pill" role="group">
		<a
			href={homeItem.href}
			class="app-nav__item"
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
			<span class="app-nav__label">{t('navigation.home')}</span>
		</a>

		{#if canAdd}
			<button
				type="button"
				class="app-nav__item"
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
				<span class="app-nav__label">{t('common.actions.add')}</span>
			</button>
		{/if}

		<a
			href={reportsItem.href}
			class="app-nav__item"
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
			<span class="app-nav__label">{t('navigation.reports')}</span>
		</a>
	</div>

	<a
		href={settingsItem.href}
		class="app-nav__bubble"
		aria-current={settingsActive ? 'page' : undefined}
		aria-label={t('navigation.settings')}
		title={t('navigation.settings')}
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
	</a>
</nav>

<style>
	.app-nav {
		/* Surface. Alpha is a contrast decision, not a taste decision — see D-C. */
		--nav-surface: rgb(255 255 255 / 0.92);
		--nav-hairline: rgb(0 0 0 / 0.06);
		--nav-blur: blur(20px) saturate(180%);

		/* Foreground */
		--nav-fg-inactive: var(--app-color-neutral-700); /* #515154 */
		--nav-fg-active: var(--app-color-primary-700); /* #005bb5 */
		--nav-chip: color-mix(in srgb, var(--app-color-primary-500) 12%, transparent);
		--nav-press: rgb(0 0 0 / 0.05);

		position: fixed;
		left: calc(env(safe-area-inset-left, 0px) + var(--space-4));
		right: calc(env(safe-area-inset-right, 0px) + var(--space-4));
		bottom: calc(env(safe-area-inset-bottom, 0px) + var(--space-4));
		z-index: var(--z-fixed);
		display: flex;
		align-items: center;
		gap: var(--space-3); /* 12px between pill and bubble */
		pointer-events: none; /* children re-enable — see note below */
	}
	.app-nav > * {
		pointer-events: auto;
	}

	:root[data-theme='dark'] .app-nav {
		--nav-surface: rgb(29 29 31 / 0.88); /* neutral-900 @ 88% */
		--nav-hairline: rgb(255 255 255 / 0.1);
		--nav-fg-inactive: var(--app-color-neutral-200); /* #d2d2d7 */
		--nav-fg-active: var(--app-color-primary-200); /* #a3cdff */
		--nav-chip: color-mix(in srgb, var(--app-color-primary-500) 22%, transparent);
		--nav-press: rgb(255 255 255 / 0.08);
	}

	@media (prefers-color-scheme: dark) {
		:root:not([data-theme='light']) .app-nav {
			--nav-surface: rgb(29 29 31 / 0.88);
			--nav-hairline: rgb(255 255 255 / 0.1);
			--nav-fg-inactive: var(--app-color-neutral-200);
			--nav-fg-active: var(--app-color-primary-200);
			--nav-chip: color-mix(in srgb, var(--app-color-primary-500) 22%, transparent);
			--nav-press: rgb(255 255 255 / 0.08);
		}
	}

	/* Fallback — mandatory. backdrop-filter is unsupported/disabled on some
	   Android WebViews and under some privacy settings; without it the pill
	   becomes a plain translucent panel and the contrast math above collapses. */
	@supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
		.app-nav {
			--nav-surface: var(--color-bg);
		}
	}

	@media (prefers-reduced-motion: no-preference) {
		.app-nav {
			animation: nav-rise var(--duration-base) var(--ease-out);
		}
	}
	@keyframes nav-rise {
		from {
			transform: translateY(8px);
			opacity: 0;
		}
	}

	.app-nav__pill {
		flex: 1 1 auto;
		display: flex;
		align-items: stretch;
		min-height: 3.5rem; /* 59.5px @ 17px root */
		padding: var(--space-1); /* 4px */
		border-radius: var(--radius-full);
		background: var(--nav-surface);
		border: 1px solid var(--nav-hairline);
		box-shadow: var(--shadow-lg);
		backdrop-filter: var(--nav-blur);
		-webkit-backdrop-filter: var(--nav-blur);
	}

	.app-nav__item {
		flex: 1 1 0; /* equal thirds; two items stay centered */
		min-width: 0;
		min-height: 3rem; /* 51px — clears the 44px minimum */
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 2px;
		padding: var(--space-1) var(--space-2);
		border-radius: var(--radius-lg); /* 16px chip inside a full-round pill */
		color: var(--nav-fg-inactive);
		transition:
			background-color var(--duration-fast) var(--ease-out),
			color var(--duration-fast) var(--ease-out);
	}
	.app-nav__item[aria-current='page'],
	.app-nav__item[data-active='true'] {
		background: var(--nav-chip);
		color: var(--nav-fg-active);
	}
	.app-nav__item:active {
		background: var(--nav-press);
	}

	.app-nav__label {
		font-size: 0.625rem; /* 10.6px @ 17px root */
		line-height: 12px;
		font-weight: var(--font-medium);
		letter-spacing: 0.005em; /* neutralises tokens.css's -0.011em body tracking, harmful at this size */
		max-width: 100%;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.app-nav__item[aria-current='page'] .app-nav__label {
		font-weight: var(--font-semibold);
	}

	.app-nav__item:focus-visible,
	.app-nav__bubble:focus-visible {
		outline: 2px solid var(--color-border-focus);
		outline-offset: 2px;
	}

	.app-nav__bubble {
		flex: 0 0 auto;
		height: 3.5rem;
		width: 3.5rem; /* h-14 w-14, matches pill height */
		display: inline-flex;
		align-items: center;
		justify-content: center;
		border-radius: var(--radius-full);
		background: var(--nav-surface);
		border: 1px solid var(--nav-hairline);
		box-shadow: var(--shadow-lg);
		backdrop-filter: var(--nav-blur);
		-webkit-backdrop-filter: var(--nav-blur);
		color: var(--nav-fg-inactive);
	}
	.app-nav__bubble[aria-current='page'] {
		color: var(--nav-fg-active);
		background: var(--nav-chip);
	}
</style>
