import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AccountInfo, AuthenticationResult } from '@azure/msal-browser';

const {
	ensureMsalInitialized,
	getAllAccounts,
	getActiveMsalAccount,
	setActiveAccount,
	acquireTokenSilent,
	acquireTokenRedirect,
	handleRedirectPromise
} = vi.hoisted(() => ({
	ensureMsalInitialized: vi.fn(),
	getAllAccounts: vi.fn(),
	getActiveMsalAccount: vi.fn(),
	setActiveAccount: vi.fn(),
	acquireTokenSilent: vi.fn(),
	acquireTokenRedirect: vi.fn(),
	handleRedirectPromise: vi.fn()
}));

vi.mock('./msal', () => ({
	apiTokenRequest: { scopes: ['api://tech-inventory/access_as_user'] },
	ensureMsalInitialized,
	msalInstance: {
		getAllAccounts,
		getActiveAccount: getActiveMsalAccount,
		setActiveAccount,
		acquireTokenSilent,
		acquireTokenRedirect,
		handleRedirectPromise
	}
}));

import {
	acquireApiToken,
	getActiveAccount,
	handleRedirectPromise as handleAuthRedirectPromise,
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
		acquireTokenSilent.mockResolvedValue(authResult);
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

	it('treats interaction_required as a non-fatal silent sign-in miss', async () => {
		getAllAccounts.mockReturnValue([account]);
		acquireTokenSilent.mockRejectedValue({ errorCode: 'interaction_required' });

		await expect(tryAcquireApiTokenSilent()).resolves.toBeNull();
		expect(acquireTokenRedirect).not.toHaveBeenCalled();
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
		handleRedirectPromise.mockResolvedValue(authResult);

		await expect(handleAuthRedirectPromise()).resolves.toEqual(authResult);
		expect(ensureMsalInitialized).toHaveBeenCalled();
	});
});
