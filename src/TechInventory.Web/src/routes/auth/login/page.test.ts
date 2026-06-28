import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/svelte';
import type { AccountInfo } from '@azure/msal-browser';
import type { Writable } from 'svelte/store';
import type { AuthState } from '$lib/stores/auth';

const { goto, ensureMsalInitialized, getAllAccounts, loginRedirect, localSignIn } = vi.hoisted(
	() => ({
		goto: vi.fn(),
		ensureMsalInitialized: vi.fn(),
		getAllAccounts: vi.fn(),
		loginRedirect: vi.fn(),
		localSignIn: vi.fn()
	})
);

vi.mock('$app/navigation', () => ({ goto }));

vi.mock('$lib/auth/msal', () => ({
	loginRequest: {
		scopes: ['api://tech-inventory/access_as_user', 'openid', 'profile', 'offline_access']
	},
	ensureMsalInitialized,
	msalInstance: {
		getAllAccounts,
		loginRedirect
	}
}));

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	const initialAuthState: AuthState = {
		currentUser: null,
		isAuthenticated: false,
		isLoading: false,
		error: null,
		authMethod: null,
		mustChangePassword: false
	};

	return {
		authStore: writable(initialAuthState),
		localSignIn
	};
});

import { authStore } from '$lib/stores/auth';
import Page from './+page.svelte';

const unauthenticatedState: AuthState = {
	currentUser: null,
	isAuthenticated: false,
	isLoading: false,
	error: null,
	authMethod: null,
	mustChangePassword: false
};

const account: AccountInfo = {
	homeAccountId: 'home-account',
	localAccountId: 'local-account',
	username: 'family@example.com',
	environment: 'login.microsoftonline.com',
	tenantId: 'tenant-id',
	name: 'Family User'
};

function setStandaloneDisplayMode(matches: boolean): void {
	Object.defineProperty(window, 'matchMedia', {
		configurable: true,
		value: vi.fn().mockImplementation((query: string) => ({
			matches: query === '(display-mode: standalone)' ? matches : false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn()
		}))
	});
}

describe('login page', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		ensureMsalInitialized.mockResolvedValue(undefined);
		getAllAccounts.mockReturnValue([]);
		loginRedirect.mockResolvedValue(undefined);
		sessionStorage.clear();
		setStandaloneDisplayMode(false);
		(authStore as Writable<AuthState>).set(unauthenticatedState);
	});

	it('auto-starts Entra redirect when MSAL has a cached account', async () => {
		getAllAccounts.mockReturnValue([account]);

		render(Page);

		await waitFor(() => expect(loginRedirect).toHaveBeenCalledTimes(1));
		expect(ensureMsalInitialized).toHaveBeenCalled();
		expect(loginRedirect).toHaveBeenCalledWith({
			scopes: ['api://tech-inventory/access_as_user', 'openid', 'profile', 'offline_access']
		});
		expect(sessionStorage.getItem('ti_auto_interactive_signin_suppressed')).toBeNull();
	});

	it('auto-starts Entra redirect in standalone PWA mode even when no cached account exists', async () => {
		setStandaloneDisplayMode(true);

		render(Page);

		await waitFor(() => expect(loginRedirect).toHaveBeenCalledTimes(1));
		expect(loginRedirect).toHaveBeenCalledWith({
			scopes: ['api://tech-inventory/access_as_user', 'openid', 'profile', 'offline_access']
		});
		expect(sessionStorage.getItem('ti_auto_interactive_signin_suppressed')).toBeNull();
	});

	it('clears stale auto-redirect suppression before an auto-started PWA sign-in', async () => {
		setStandaloneDisplayMode(true);
		sessionStorage.setItem('ti_auto_interactive_signin_suppressed', 'true');

		render(Page);

		await waitFor(() => expect(loginRedirect).toHaveBeenCalledTimes(1));
		expect(sessionStorage.getItem('ti_auto_interactive_signin_suppressed')).toBeNull();
	});

	it('keeps the Entra button visible in browser mode when no cached account exists', async () => {
		render(Page);

		expect(screen.getByRole('button', { name: 'Sign In' })).toBeInTheDocument();
		expect(screen.getByRole('button', { name: /Use a local account instead/i })).toBeInTheDocument();
		await Promise.resolve();
		expect(loginRedirect).not.toHaveBeenCalled();
	});

	it('honors redirect suppression and keeps local fallback reachable', async () => {
		setStandaloneDisplayMode(true);
		sessionStorage.setItem('ti_silent_sso_suppressed', 'true');
		sessionStorage.setItem('ti_auto_interactive_signin_suppressed', 'true');

		render(Page);
		await fireEvent.click(screen.getByRole('button', { name: /Use a local account instead/i }));

		expect(loginRedirect).not.toHaveBeenCalled();
		expect(screen.getByRole('heading', { name: /Local account sign-in/i })).toBeInTheDocument();
		expect(screen.getByLabelText(/Username/i)).toBeInTheDocument();
	});

	it('lets a manual Entra sign-in clear the auto-redirect guard', async () => {
		sessionStorage.setItem('ti_silent_sso_suppressed', 'true');
		sessionStorage.setItem('ti_auto_interactive_signin_suppressed', 'true');

		render(Page);
		await fireEvent.click(screen.getByRole('button', { name: 'Sign In' }));

		await waitFor(() => expect(loginRedirect).toHaveBeenCalledTimes(1));
		expect(sessionStorage.getItem('ti_silent_sso_suppressed')).toBeNull();
		expect(sessionStorage.getItem('ti_auto_interactive_signin_suppressed')).toBeNull();
	});
});
