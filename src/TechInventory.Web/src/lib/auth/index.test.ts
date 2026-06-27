import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AccountInfo, AuthenticationResult } from '@azure/msal-browser';

const {
	ensureMsalInitialized,
	getAllAccounts,
	getActiveMsalAccount,
	setActiveAccount,
	acquireTokenSilent,
	ssoSilent,
	acquireTokenRedirect,
	handleRedirectPromise
} = vi.hoisted(() => ({
	ensureMsalInitialized: vi.fn(),
	getAllAccounts: vi.fn(),
	getActiveMsalAccount: vi.fn(),
	setActiveAccount: vi.fn(),
	acquireTokenSilent: vi.fn(),
	ssoSilent: vi.fn(),
	acquireTokenRedirect: vi.fn(),
	handleRedirectPromise: vi.fn()
}));

vi.mock('./msal', () => ({
	apiTokenRequest: { scopes: ['api://tech-inventory/access_as_user'] },
	loginRequest: {
		scopes: ['api://tech-inventory/access_as_user', 'openid', 'profile', 'offline_access']
	},
	ensureMsalInitialized,
	msalInstance: {
		getAllAccounts,
		getActiveAccount: getActiveMsalAccount,
		setActiveAccount,
		acquireTokenSilent,
		ssoSilent,
		acquireTokenRedirect,
		handleRedirectPromise
	}
}));

import {
	acquireApiToken,
	clearAutoInteractiveSignInSuppression,
	getActiveAccount,
	handleRedirectPromise as handleAuthRedirectPromise,
	shouldAutoStartInteractiveSignIn,
	suppressAutoInteractiveSignIn,
	tryAcquireApiTokenSilent
} from './index';

const account: AccountInfo = {
	homeAccountId: 'home-account',
	localAccountId: 'local-account',
	username: 'family@example.com',
	environment: 'login.microsoftonline.com',
	tenantId: 'tenant-id',
	name: 'Family User'
};

const authResult: AuthenticationResult = {
	uniqueId: 'unique-id',
	tenantId: 'tenant-id',
	scopes: ['api://tech-inventory/access_as_user'],
	account,
	idToken: 'id-token',
	idTokenClaims: {},
	accessToken: 'access-token',
	fromCache: false,
	expiresOn: new Date(Date.now() + 60_000),
	extExpiresOn: new Date(Date.now() + 60_000),
	state: '',
	tokenType: 'Bearer',
	correlationId: 'correlation-id',
	authority: 'https://login.microsoftonline.com/tenant-id',
	familyId: '',
	cloudGraphHostName: '',
	msGraphHost: ''
};

describe('auth helpers', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		ensureMsalInitialized.mockResolvedValue(undefined);
		getAllAccounts.mockReturnValue([]);
		getActiveMsalAccount.mockReturnValue(null);
		setActiveAccount.mockReturnValue(undefined);
		sessionStorage.clear();
		acquireTokenSilent.mockResolvedValue(authResult);
		ssoSilent.mockResolvedValue(authResult);
		acquireTokenRedirect.mockResolvedValue(undefined);
		handleRedirectPromise.mockResolvedValue(null);
	});

	it('promotes the first cached account to the active account', () => {
		getAllAccounts.mockReturnValue([account]);

		expect(getActiveAccount()).toEqual(account);
		expect(setActiveAccount).toHaveBeenCalledWith(account);
	});

	it('returns a silent token result when the cached Entra session is still valid', async () => {
		getAllAccounts.mockReturnValue([account]);

		await expect(tryAcquireApiTokenSilent()).resolves.toEqual(authResult);
		expect(acquireTokenSilent).toHaveBeenCalledWith({
			scopes: ['api://tech-inventory/access_as_user'],
			account
		});
	});

	it('falls back to ssoSilent when this tab has no cached MSAL account yet', async () => {
		await expect(tryAcquireApiTokenSilent()).resolves.toEqual(authResult);
		expect(ssoSilent).toHaveBeenCalledWith({
			scopes: ['api://tech-inventory/access_as_user', 'openid', 'profile', 'offline_access']
		});
		expect(setActiveAccount).toHaveBeenCalledWith(account);
	});

	it('treats interaction_required as a non-fatal silent sign-in miss', async () => {
		getAllAccounts.mockReturnValue([account]);
		acquireTokenSilent.mockRejectedValue({ errorCode: 'interaction_required' });

		await expect(tryAcquireApiTokenSilent()).resolves.toBeNull();
		expect(acquireTokenRedirect).not.toHaveBeenCalled();
	});

	it('skips silent SSO after an explicit sign-out suppression flag', async () => {
		sessionStorage.setItem('ti_silent_sso_suppressed', 'true');

		await expect(tryAcquireApiTokenSilent()).resolves.toBeNull();
		expect(acquireTokenSilent).not.toHaveBeenCalled();
		expect(ssoSilent).not.toHaveBeenCalled();
	});

	it('auto-starts interactive sign-in when a cached account exists and suppression is clear', () => {
		getAllAccounts.mockReturnValue([account]);

		expect(shouldAutoStartInteractiveSignIn()).toBe(true);
	});

	it('does not auto-start interactive sign-in when no cached accounts exist', () => {
		getAllAccounts.mockReturnValue([]);

		expect(shouldAutoStartInteractiveSignIn()).toBe(false);
	});

	it('auto-starts interactive sign-in in standalone PWA mode without a cached account', () => {
		getAllAccounts.mockReturnValue([]);

		expect(shouldAutoStartInteractiveSignIn({ standalonePwa: true })).toBe(true);
	});

	it('does not auto-start interactive sign-in in browser mode without a cached account', () => {
		getAllAccounts.mockReturnValue([]);

		expect(shouldAutoStartInteractiveSignIn({ standalonePwa: false })).toBe(false);
	});

	it('does not auto-start interactive sign-in when suppression is active', () => {
		getAllAccounts.mockReturnValue([account]);
		sessionStorage.setItem('ti_silent_sso_suppressed', 'true');

		expect(shouldAutoStartInteractiveSignIn({ standalonePwa: true })).toBe(false);
		sessionStorage.removeItem('ti_silent_sso_suppressed');
	});

	it('does not auto-start interactive sign-in after the one-shot redirect guard is armed', () => {
		getAllAccounts.mockReturnValue([account]);
		suppressAutoInteractiveSignIn();

		expect(shouldAutoStartInteractiveSignIn()).toBe(false);

		clearAutoInteractiveSignInSuppression();
		expect(shouldAutoStartInteractiveSignIn()).toBe(true);
	});

	it('falls back to interactive redirect for API calls after a silent miss', async () => {
		getAllAccounts.mockReturnValue([account]);
		acquireTokenSilent.mockRejectedValue({ errorCode: 'interaction_required' });

		await expect(acquireApiToken()).resolves.toBeNull();
		expect(acquireTokenRedirect).toHaveBeenCalledWith({
			scopes: ['api://tech-inventory/access_as_user'],
			account
		});
	});

	it('passes through MSAL redirect results after initialization', async () => {
		sessionStorage.setItem('ti_auto_interactive_signin_suppressed', 'true');
		handleRedirectPromise.mockResolvedValue(authResult);

		await expect(handleAuthRedirectPromise()).resolves.toEqual(authResult);
		expect(ensureMsalInitialized).toHaveBeenCalled();
		expect(setActiveAccount).toHaveBeenCalledWith(account);
		expect(sessionStorage.getItem('ti_auto_interactive_signin_suppressed')).toBeNull();
	});
});
