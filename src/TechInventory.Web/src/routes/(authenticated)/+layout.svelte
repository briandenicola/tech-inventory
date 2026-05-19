<script lang="ts">
	import { page } from '$app/stores';
	import { authStore } from '$lib/stores/auth';
	import { msalInstance } from '$lib/auth/msal';
	import { ensureMsalInitialized } from '$lib/auth/msal';
	import { clearAuth } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import { goto } from '$app/navigation';
	import ToastContainer from '$lib/components/ToastContainer.svelte';

	let { children } = $props();

	// T13: App shell — header + nav + footer
	// Per Spec §4.4, §6.1, J13
	// Role-aware nav: Admin link visible only to Admin role
	// D-137: Apple-elegant aesthetic — generous padding, larger labels,
	// real Admin dropdown (replaces previous route-toggle button), 44px+
	// touch targets, pill-style active state.

	let mobileMenuOpen = $state(false);
	let adminMenuOpen = $state(false);
	let adminMenuTrigger = $state<HTMLButtonElement | undefined>(undefined);

	const adminLinks = [
		{ href: '/admin/brands', key: 'navigation.adminBrands' },
		{ href: '/admin/categories', key: 'navigation.adminCategories' },
		{ href: '/admin/locations', key: 'navigation.adminLocations' },
		{ href: '/admin/networks', key: 'navigation.adminNetworks' },
		{ href: '/admin/owners', key: 'navigation.adminOwners' },
		{ href: '/admin/tags', key: 'navigation.adminTags' },
		{ href: '/admin/import', key: 'navigation.imports' },
		{ href: '/admin/export', key: 'navigation.exports' }
	] as const;

	// Derive auth state from store
	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');
	const isMemberOrAdmin = $derived(currentUser?.role === 'Admin' || currentUser?.role === 'Member');
	const isAdminPath = $derived($page.url.pathname.startsWith('/admin'));

	// T09 + J3: Sign out — call MSAL logoutRedirect + clear auth store
	async function handleSignOut() {
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
		adminMenuOpen = false;
	});

	// Admin dropdown: close on outside-click + Escape
	function handleAdminMenuKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && adminMenuOpen) {
			adminMenuOpen = false;
			adminMenuTrigger?.focus();
		}
	}

	function handleDocumentClick(e: MouseEvent) {
		if (!adminMenuOpen) return;
		const target = e.target as Node | null;
		if (target && adminMenuTrigger && !adminMenuTrigger.parentElement?.contains(target)) {
			adminMenuOpen = false;
		}
	}
</script>

<svelte:window onkeydown={handleAdminMenuKeydown} onclick={handleDocumentClick} />

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

			<!-- Desktop Nav (hidden on mobile) -->
			<nav class="hidden items-center gap-1 md:flex" aria-label="Main navigation">
				{@render desktopNavLink('/devices', t('navigation.devices'))}

				{#if isMemberOrAdmin}
					{@render desktopNavLink('/import', t('navigation.imports'))}
				{/if}

				{@render desktopNavLink('/export', t('navigation.exports'))}

				{#if isAdmin}
					<!-- Admin Dropdown (Desktop) -->
					<div class="relative">
						<button
							type="button"
							bind:this={adminMenuTrigger}
							onclick={() => (adminMenuOpen = !adminMenuOpen)}
							aria-haspopup="menu"
							aria-expanded={adminMenuOpen}
							class="inline-flex min-h-11 items-center gap-1.5 rounded-full px-4 py-2 text-base font-medium transition-colors duration-150"
							class:bg-neutral-100={isAdminPath}
							class:text-primary-700={isAdminPath}
							class:dark:bg-neutral-800={isAdminPath}
							class:dark:text-primary-300={isAdminPath}
							class:text-neutral-700={!isAdminPath}
							class:hover:bg-neutral-100={!isAdminPath}
							class:hover:text-neutral-900={!isAdminPath}
							class:dark:text-neutral-300={!isAdminPath}
							class:dark:hover:bg-neutral-800={!isAdminPath}
							class:dark:hover:text-neutral-50={!isAdminPath}
						>
							<span>{t('navigation.admin')}</span>
							<svg
								class="h-4 w-4 transition-transform duration-150"
								class:rotate-180={adminMenuOpen}
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
								aria-hidden="true"
							>
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
							</svg>
						</button>

						{#if adminMenuOpen}
							<div
								class="absolute right-0 mt-2 w-56 origin-top-right rounded-2xl border border-neutral-200/70 bg-white/95 p-2 shadow-xl backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/95"
								role="menu"
								aria-label={t('navigation.admin')}
							>
								{#each adminLinks as link (link.href)}
									{@const active = $page.url.pathname.startsWith(link.href)}
									<a
										href={link.href}
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
										{t(link.key)}
									</a>
								{/each}
							</div>
						{/if}
					</div>
				{/if}
			</nav>

			<!-- Right: User Display Name + Role Badge + Sign Out (desktop) -->
			<div class="hidden items-center gap-3 md:flex">
				{#if currentUser}
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
						type="button"
						onclick={handleSignOut}
						class="inline-flex min-h-11 items-center rounded-full px-4 py-2 text-base font-medium text-neutral-700 transition-colors duration-150 hover:bg-neutral-100 hover:text-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800 dark:hover:text-neutral-50"
					>
						{t('auth.signOut.button')}
					</button>
				{/if}
			</div>

			<!-- Mobile: Hamburger Menu Button -->
			<button
				type="button"
				class="inline-flex h-11 w-11 items-center justify-center rounded-full text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800 md:hidden"
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
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					{:else}
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
					{/if}
				</svg>
			</button>
		</div>

		<!-- Mobile Menu (expanded) -->
		{#if mobileMenuOpen}
			<nav
				class="border-t border-neutral-200/70 bg-white/95 px-4 py-4 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/95 md:hidden"
				aria-label="Mobile navigation"
			>
				<div class="flex flex-col gap-1.5">
					{@render mobileNavLink('/devices', t('navigation.devices'))}

					{#if isMemberOrAdmin}
						{@render mobileNavLink('/import', t('navigation.imports'))}
					{/if}

					{@render mobileNavLink('/export', t('navigation.exports'))}

					{#if isAdmin}
						<!-- Admin Section (Mobile) -->
						<div class="mt-2 space-y-1 border-t border-neutral-200 pt-3 dark:border-neutral-800">
							<div class="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
								{t('navigation.admin')}
							</div>
							{#each adminLinks as link (link.href)}
								{@render mobileNavLink(link.href, t(link.key))}
							{/each}
						</div>
					{/if}

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
								class="flex min-h-11 w-full items-center rounded-xl px-3 py-2.5 text-left text-base font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
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
		{@render children()}
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

{#snippet desktopNavLink(href: string, label: string)}
	{@const active = $page.url.pathname.startsWith(href)}
	<a
		{href}
		class="inline-flex min-h-11 items-center rounded-full px-4 py-2 text-base font-medium transition-colors duration-150"
		class:bg-neutral-100={active}
		class:text-primary-700={active}
		class:dark:bg-neutral-800={active}
		class:dark:text-primary-300={active}
		class:text-neutral-700={!active}
		class:hover:bg-neutral-100={!active}
		class:hover:text-neutral-900={!active}
		class:dark:text-neutral-300={!active}
		class:dark:hover:bg-neutral-800={!active}
		class:dark:hover:text-neutral-50={!active}
	>
		{label}
	</a>
{/snippet}

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
