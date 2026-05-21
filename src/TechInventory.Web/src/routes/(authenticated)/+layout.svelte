<script lang="ts">
	import { page } from '$app/stores';
	import { authStore } from '$lib/stores/auth';
	import { msalInstance } from '$lib/auth/msal';
	import { ensureMsalInitialized } from '$lib/auth/msal';
	import { clearAuth } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import { goto, invalidateAll } from '$app/navigation';
	import PullToRefresh from '$lib/components/PullToRefresh.svelte';
	import ThemeToggle from '$lib/components/ThemeToggle.svelte';
	import ToastContainer from '$lib/components/ToastContainer.svelte';
	import { fetchReferenceData, referenceDataStore } from '$lib/stores/referenceData';
	import { activePullToRefresh } from '$lib/stores/pullToRefresh';
	import {
		adminNavItems,
		getVisibleNavItems,
		isNavItemActive,
		primaryNavItems
	} from '$lib/navigation/appNav';

	let { children } = $props();

	// T13: App shell — header + nav + footer
	// Per Spec §4.4, §6.1, J13
	// Role-aware nav: Admin link visible only to Admin role
	// D-137: Apple-elegant aesthetic — generous padding, larger labels,
	// real Admin dropdown (replaces previous route-toggle button), 44px+
	// touch targets, pill-style active state.

	let mobileMenuOpen = $state(false);
	let userMenuOpen = $state(false);
	let userMenuTrigger = $state<HTMLButtonElement | undefined>(undefined);

	// Derive auth state from store
	const currentUser = $derived($authStore.currentUser);
	const currentRole = $derived(currentUser?.role ?? null);
	const settingsActive = $derived($page.url.pathname.startsWith('/settings'));
	const visiblePrimaryNavItems = $derived(getVisibleNavItems(primaryNavItems, currentRole));
	const visibleAdminNavItems = $derived(getVisibleNavItems(adminNavItems, currentRole));
	const pageRefreshHandler = $derived.by(() => {
		const registration = $activePullToRefresh;
		return registration?.routePath === $page.url.pathname ? registration.onRefresh : invalidateAll;
	});

	// T09 + J3: Sign out — branch on auth method.
	// Local (F025) sessions live in sessionStorage and have no Entra session
	// to terminate; calling msalInstance.logoutRedirect would kick the user
	// out of their unrelated Entra account. Entra sessions still go through
	// MSAL so the IdP session + cookies are cleared.
	async function handleSignOut() {
		const authMethod = $authStore.authMethod;

		if (authMethod === 'local') {
			clearAuth();
			await goto('/auth/login');
			return;
		}

		try {
			clearAuth();
			await ensureMsalInitialized();
			await msalInstance.logoutRedirect({
				postLogoutRedirectUri: window.location.origin
			});
		} catch (error) {
			console.error('[auth] Sign-out failed:', error);
			// Fallback: navigate to login even if MSAL logout fails
			goto('/auth/login');
		}
	}

	// Close menus when route changes
	$effect(() => {
		// eslint-disable-next-line @typescript-eslint/no-unused-expressions
		$page.url.pathname; // Trigger effect on route change
		mobileMenuOpen = false;
		userMenuOpen = false;
	});

	// Hydrate reference data (brands, categories, owners, locations, networks)
	// once per authenticated session so any page (edit, new, table lookups)
	// can render its dropdowns and name lookups without a hard refresh dance.
	let refDataHydrated = false;
	$effect(() => {
		if (!currentUser || refDataHydrated) return;
		const snapshot = $referenceDataStore;
		if (
			snapshot.brands.length === 0 &&
			snapshot.categories.length === 0 &&
			snapshot.owners.length === 0 &&
			snapshot.locations.length === 0 &&
			snapshot.networks.length === 0 &&
			snapshot.tags.length === 0
		) {
			refDataHydrated = true;
			void fetchReferenceData();
		} else {
			refDataHydrated = true;
		}
	});

	// User dropdown: close on outside-click + Escape
	function handleUserMenuKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && userMenuOpen) {
			userMenuOpen = false;
			userMenuTrigger?.focus();
		}
	}

	function handleDocumentClick(e: MouseEvent) {
		if (!userMenuOpen) return;
		const target = e.target as Node | null;
		if (target && userMenuTrigger && !userMenuTrigger.parentElement?.contains(target)) {
			userMenuOpen = false;
		}
	}
</script>

<svelte:window onkeydown={handleUserMenuKeydown} onclick={handleDocumentClick} />

<div class="flex min-h-screen flex-col bg-neutral-50 dark:bg-neutral-900">
	<!-- Header -->
	<header class="sticky top-0 z-50 border-b border-neutral-200/70 bg-white/85 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/85">
		<div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
			<!-- Left: Logo + App Name -->
			<a href="/devices" class="flex items-center gap-3 rounded-full px-1 py-1 outline-none focus-visible:ring-2 focus-visible:ring-primary-500">
				<img src="/icons/icon.svg" alt={t('app.title')} class="h-9 w-9" />
				<span class="text-lg font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
					{t('app.title')}
				</span>
			</a>

			<!-- regression-watch: Desktop primary nav links removed per design rule (hamburger-only).
				 Regression history: dd52e98 removed, 1de8da8 re-introduced. Do NOT restore. -->

			<!-- Right: Unified user menu (display name + role + dropdown) -->
			<div class="relative hidden md:block">
				{#if currentUser}
					<button
						type="button"
						bind:this={userMenuTrigger}
						onclick={() => (userMenuOpen = !userMenuOpen)}
						aria-haspopup="menu"
						aria-expanded={userMenuOpen}
						aria-label={t('header.userMenu')}
						class="inline-flex min-h-11 items-center gap-2 rounded-full bg-neutral-100 px-3 py-1.5 text-neutral-900 transition-colors duration-150 hover:bg-neutral-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:hover:bg-neutral-700"
					>
						<span class="text-sm font-medium">
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
						<svg
							class="h-4 w-4 transition-transform duration-150"
							class:rotate-180={userMenuOpen}
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
							aria-hidden="true"
						>
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
						</svg>
					</button>

					{#if userMenuOpen}
						<div
							class="absolute right-0 mt-2 w-56 origin-top-right rounded-2xl border border-neutral-200/70 bg-white/95 p-2 shadow-xl backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/95"
							role="menu"
							aria-label={t('header.userMenu')}
						>
							{#if visibleAdminNavItems.length > 0}
								<div class="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
									{t('navigation.admin')}
								</div>
								{#each visibleAdminNavItems as item (item.href)}
									{@const active = isNavItemActive($page.url.pathname, item)}
									<a
										href={item.href}
										role="menuitem"
										class="flex min-h-11 items-center rounded-xl px-3 py-2.5 text-base font-medium transition-colors duration-150"
										class:bg-primary-50={active}
										class:text-primary-700={active}
										class:dark:bg-primary-900={active}
										class:dark:text-primary-200={active}
										class:text-neutral-700={!active}
										class:hover:bg-neutral-100={!active}
										class:dark:text-neutral-300={!active}
										class:dark:hover:bg-neutral-800={!active}
									>
										{t(item.labelKey)}
									</a>
								{/each}
								<hr class="my-2 border-t border-neutral-200 dark:border-neutral-800" />
							{/if}
							<a
								href="/settings"
								role="menuitem"
								class="flex min-h-11 items-center rounded-xl px-3 py-2.5 text-base font-medium transition-colors duration-150"
								class:bg-primary-50={settingsActive}
								class:text-primary-700={settingsActive}
								class:dark:bg-primary-900={settingsActive}
								class:dark:text-primary-200={settingsActive}
								class:text-neutral-700={!settingsActive}
								class:hover:bg-neutral-100={!settingsActive}
								class:dark:text-neutral-300={!settingsActive}
								class:dark:hover:bg-neutral-800={!settingsActive}
							>
								{t('navigation.settings')}
							</a>
							<button
								type="button"
								role="menuitem"
								onclick={handleSignOut}
								class="flex min-h-11 w-full items-center rounded-xl px-3 py-2.5 text-left text-base font-medium text-neutral-700 transition-colors duration-150 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
							>
								{t('auth.signOut.button')}
							</button>
						</div>
					{/if}
				{/if}
			</div>

			<!-- Hamburger Menu Button (all screen sizes — primary nav is hamburger-only) -->
			<button
				type="button"
				class="inline-flex h-11 w-11 items-center justify-center rounded-full text-neutral-700 transition-colors hover:bg-neutral-100 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-300 dark:hover:bg-neutral-800"
				onclick={() => (mobileMenuOpen = !mobileMenuOpen)}
				aria-label={mobileMenuOpen ? t('header.closeMenu') : t('header.menu')}
				aria-expanded={mobileMenuOpen}
			>
				<svg
					class="h-6 w-6"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					{#if mobileMenuOpen}
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M6 18L18 6M6 6l12 12"
						/>
					{:else}
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M4 6h16M4 12h16M4 18h16"
						/>
					{/if}
				</svg>
			</button>
		</div>

		{#if visibleAdminNavItems.length > 0}
			<nav
				class="hidden border-t border-neutral-200 bg-neutral-50 md:block dark:border-neutral-800 dark:bg-neutral-900/70"
				aria-label="Admin navigation"
			>
				<div class="mx-auto flex max-w-7xl flex-wrap gap-2 px-4 py-3 sm:px-6 lg:px-8">
					{#each visibleAdminNavItems as item (item.href)}
						<a
							href={item.href}
							class="inline-flex min-h-11 items-center rounded-lg px-3 text-sm font-medium text-neutral-700 transition-colors hover:bg-white hover:text-primary-600 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-300 dark:hover:bg-neutral-800 dark:hover:text-primary-400"
							class:bg-white={isNavItemActive($page.url.pathname, item)}
							class:text-primary-600={isNavItemActive($page.url.pathname, item)}
							class:shadow-sm={isNavItemActive($page.url.pathname, item)}
							class:dark:bg-neutral-800={isNavItemActive($page.url.pathname, item)}
							class:dark:text-primary-400={isNavItemActive($page.url.pathname, item)}
						>
							{t(item.labelKey)}
						</a>
					{/each}
				</div>
			</nav>
		{/if}

		<!-- Hamburger Menu (expanded — visible on all screen sizes) -->
		{#if mobileMenuOpen}
			<nav
				class="border-t border-neutral-200/70 bg-white/95 px-4 py-4 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/95"
				aria-label="Primary navigation"
			>
				<div class="flex flex-col gap-1.5">
					{#each visiblePrimaryNavItems as item (item.href)}
						{@render mobileNavLink(item.href, t(item.labelKey))}
					{/each}

					{#if visibleAdminNavItems.length > 0}
						<div class="mt-2 space-y-1 border-t border-neutral-200 pt-3 dark:border-neutral-800">
							<div class="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
								{t('navigation.admin')}
							</div>
							{#each visibleAdminNavItems as item (item.href)}
								{@render mobileNavLink(item.href, t(item.labelKey))}
							{/each}
						</div>
					{/if}

					<div class="mt-2 space-y-1 border-t border-neutral-200 pt-3 dark:border-neutral-800">
						{@render mobileNavLink('/settings', t('navigation.settings'))}
					</div>

					<div class="rounded-2xl border border-neutral-200 bg-neutral-50 p-3 dark:border-neutral-800 dark:bg-neutral-900/70">
						<p class="mb-2 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
							{t('settings.theme.heading')}
						</p>
						<ThemeToggle />
					</div>

					<!-- Mobile: User Info + Sign Out -->
					{#if currentUser}
						<div class="mt-3 border-t border-neutral-200 pt-3 dark:border-neutral-800">
							<div class="mb-2 flex items-center gap-2 rounded-full bg-neutral-100 px-3 py-2 dark:bg-neutral-800">
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
								type="button"
								onclick={handleSignOut}
								class="flex min-h-11 w-full items-center rounded-xl px-3 py-2.5 text-left text-base font-medium text-neutral-700 transition-colors hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
							>
								{t('auth.signOut.button')}
							</button>
						</div>
					{/if}
				</div>
			</nav>
		{/if}
	</header>

	<!-- Main Content Area -->
	<main class="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6 lg:px-8" id="main-content">
		<PullToRefresh onRefresh={pageRefreshHandler}>
			{@render children()}
		</PullToRefresh>
	</main>

	<!-- Footer -->
	<footer class="border-t border-neutral-200/70 bg-white px-4 py-6 dark:border-neutral-800/70 dark:bg-neutral-950">
		<div class="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 text-sm text-neutral-600 dark:text-neutral-400 sm:flex-row">
			<div class="flex items-center gap-4">
				<span>{t('footer.version', { version: '0.1.0' })}</span>
			</div>
			<a
				href="https://github.com/briandenicola/tech-inventory"
				target="_blank"
				rel="noopener noreferrer"
				class="rounded-full px-2 py-1 hover:text-primary-600 dark:hover:text-primary-400"
			>
				{t('footer.github')}
			</a>
		</div>
	</footer>

	<!-- Toast notifications (fixed top-right, z-50) -->
	<ToastContainer />
</div>

{#snippet mobileNavLink(href: string, label: string)}
	{@const active = $page.url.pathname.startsWith(href)}
	<a
		{href}
		class="flex min-h-11 items-center rounded-xl px-3 py-2.5 text-base font-medium"
		class:bg-neutral-100={active}
		class:text-primary-700={active}
		class:dark:bg-neutral-800={active}
		class:dark:text-primary-300={active}
		class:text-neutral-700={!active}
		class:hover:bg-neutral-100={!active}
		class:dark:text-neutral-300={!active}
		class:dark:hover:bg-neutral-800={!active}
	>
		{label}
	</a>
{/snippet}
