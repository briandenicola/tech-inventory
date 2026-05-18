import { BrowserCacheLocation } from '@azure/msal-browser';

// TODO: Phase 2 (specs/002-auth-entra)
// MSAL.js configuration and auth helpers
// Bishop owns the auth design; this locks the cache policy now.
export const msalCacheLocation = BrowserCacheLocation.SessionStorage;

export const msalConfig = {
	cache: {
		cacheLocation: msalCacheLocation
	}
};
