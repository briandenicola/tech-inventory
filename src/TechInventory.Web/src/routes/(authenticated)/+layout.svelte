<script lang="ts">
	import { page } from '$app/stores';
	import { authStore } from '$lib/stores/auth';
	import { msalInstance } from '$lib/auth/msal';
	import { clearAuth } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import { goto } from '$app/navigation';
	import ToastContainer from '$lib/components/ToastContainer.svelte';

	let { children } = $props();

	// T13: App shell — header + nav + footer
	// Per Spec §4.4, §6.1, J13
	// Role-aware nav: Admin link visible only to Admin role

	let mobileMenuOpen = $state(false);

	// Derive auth state from store
	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');
	const isMemberOrAdmin = $derived(currentUser?.role === 'Admin' || currentUser?.role === 'Member');

	// T09 + J3: Sign out — call MSAL logoutRedirect + clear auth store
	async function handleSignOut() {
		try {
			clearAuth();
			await msalInstance.logoutRedirect({
				postLogoutRedirectUri: window.location.origin
			});
		} catch (error) {
			console.error('[auth] Sign-out failed:', error);
			// Fallback: navigate to login even if MSAL logout fails
			goto('/auth/login');
		}
	}

	// Close mobile menu when route changes
	$effect(() => {
		// eslint-disable-next-line @typescript-eslint/no-unused-expressions
		$page.url.pathname; // Trigger effect on route change
		mobileMenuOpen = false;
	});
</script>

<div class="flex min-h-screen flex-col bg-neutral-50 dark:bg-neutral-900">
	<!-- Header -->
	<header class="sticky top-0 z-50 border-b border-neutral-200 bg-white dark:border-neutral-800 dark:bg-neutral-950">
		<div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
			<!-- Left: Logo + App Name -->
			<div class="flex items-center gap-3">
				<img src="/icons/icon.svg" alt={t('app.title')} class="h-8 w-8" />
				<span class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
					{t('app.title')}
				</span>
			</div>

			<!-- Desktop Nav (hidden on mobile) -->
			<nav class="hidden gap-6 md:flex" aria-label="Main navigation">
				<a
					href="/devices"
					class="text-sm font-medium text-neutral-700 hover:text-primary-600 dark:text-neutral-300 dark:hover:text-primary-400"
					class:text-primary-600={$page.url.pathname.startsWith('/devices')}
					class:dark:text-primary-400={$page.url.pathname.startsWith('/devices')}
				>
					{t('navigation.devices')}
				</a>

				{#if isMemberOrAdmin}
					<a
						href="/import"
						class="text-sm font-medium text-neutral-700 hover:text-primary-600 dark:text-neutral-300 dark:hover:text-primary-400"
						class:text-primary-600={$page.url.pathname.startsWith('/import')}
						class:dark:text-primary-400={$page.url.pathname.startsWith('/import')}
					>
						{t('navigation.imports')}
					</a>
				{/if}

				<a
					href="/export"
					class="text-sm font-medium text-neutral-700 hover:text-primary-600 dark:text-neutral-300 dark:hover:text-primary-400"
					class:text-primary-600={$page.url.pathname.startsWith('/export')}
					class:dark:text-primary-400={$page.url.pathname.startsWith('/export')}
				>
					{t('navigation.exports')}
				</a>

				{#if isAdmin}
					<!-- Admin Dropdown (Desktop) -->
					<div class="relative">
						<button
							type="button"
							class="text-sm font-medium text-neutral-700 hover:text-primary-600 dark:text-neutral-300 dark:hover:text-primary-400"
							class:text-primary-600={$page.url.pathname.startsWith('/admin')}
							class:dark:text-primary-400={$page.url.pathname.startsWith('/admin')}
							onclick={() => {
								if ($page.url.pathname.startsWith('/admin')) {
									goto('/devices');
								} else {
									goto('/admin/brands');
								}
							}}
						>
							{t('navigation.admin')}
						</button>
					</div>
				{/if}
			</nav>

			<!-- Right: User Display Name + Role Badge + Sign Out (desktop) -->
			<div class="hidden items-center gap-4 md:flex">
				{#if currentUser}
					<div class="flex items-center gap-2">
						<span class="text-sm font-medium text-neutral-700 dark:text-neutral-300">
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
						class="text-sm font-medium text-neutral-700 hover:text-primary-600 dark:text-neutral-300 dark:hover:text-primary-400"
					>
						{t('auth.signOut.button')}
					</button>
				{/if}
			</div>

			<!-- Mobile: Hamburger Menu Button -->
			<button
				type="button"
				class="md:hidden"
				onclick={() => (mobileMenuOpen = !mobileMenuOpen)}
				aria-label={mobileMenuOpen ? t('header.closeMenu') : t('header.menu')}
				aria-expanded={mobileMenuOpen}
			>
				<svg
					class="h-6 w-6 text-neutral-700 dark:text-neutral-300"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
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
			<nav class="border-t border-neutral-200 bg-white px-4 py-3 dark:border-neutral-800 dark:bg-neutral-950 md:hidden" aria-label="Mobile navigation">
				<div class="flex flex-col gap-3">
					<a
						href="/devices"
						class="block rounded-lg px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('navigation.devices')}
					</a>

					{#if isMemberOrAdmin}
						<a
							href="/import"
							class="block rounded-lg px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
						>
							{t('navigation.imports')}
						</a>
					{/if}

					<a
						href="/export"
						class="block rounded-lg px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('navigation.exports')}
					</a>

					{#if isAdmin}
						<!-- Admin Section (Mobile) -->
						<div class="space-y-1">
							<div class="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
								{t('navigation.admin')}
							</div>
							<a
								href="/admin/brands"
								class="block rounded-lg px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
							>
								{t('navigation.adminBrands')}
							</a>
							<a
								href="/admin/locations"
								class="block rounded-lg px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
							>
								{t('navigation.adminLocations')}
							</a>
							<a
								href="/admin/networks"
								class="block rounded-lg px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
							>
								{t('navigation.adminNetworks')}
							</a>
							<a
								href="/admin/tags"
								class="block rounded-lg px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
							>
								{t('navigation.adminTags')}
							</a>
						</div>
					{/if}

					<!-- Mobile: User Info + Sign Out -->
					{#if currentUser}
						<div class="mt-3 border-t border-neutral-200 pt-3 dark:border-neutral-800">
							<div class="mb-2 flex items-center gap-2 px-3">
								<span class="text-sm font-medium text-neutral-700 dark:text-neutral-300">
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
								class="block w-full rounded-lg px-3 py-2 text-left text-sm font-medium text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
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
	<main class="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6 lg:px-8" id="main-content">
		{@render children()}
	</main>

	<!-- Footer -->
	<footer class="border-t border-neutral-200 bg-white px-4 py-6 dark:border-neutral-800 dark:bg-neutral-950">
		<div class="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 text-sm text-neutral-600 dark:text-neutral-400 sm:flex-row">
			<div class="flex items-center gap-4">
				<span>{t('footer.version', { version: '0.1.0' })}</span>
			</div>
			<a
				href="https://github.com/briandenicola/tech-inventory"
				target="_blank"
				rel="noopener noreferrer"
				class="hover:text-primary-600 dark:hover:text-primary-400"
			>
				{t('footer.github')}
			</a>
		</div>
	</footer>

	<!-- Toast notifications (fixed top-right, z-50) -->
	<ToastContainer />
</div>
