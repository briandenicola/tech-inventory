/**
 * API Client Bootstrap
 * 
 * Wires MSAL token acquisition into the API client's auth header injection.
 * Side-effect import in +layout.svelte configures the client at app startup.
 *
 * F025 — when a local-account session exists in sessionStorage, the local JWT
 * wins over MSAL. This lets break-glass admins use the API without an Entra
 * sign-in.
 */

import { setApiConfig } from './client';
import { acquireApiToken } from '$lib/auth';
import { getLocalToken } from '$lib/auth/local-session';

setApiConfig({
	getAuthToken: async () => {
		const localToken = getLocalToken();
		if (localToken) {
			return localToken;
		}
		return acquireApiToken();
	}
});

// Re-export client functions for convenience
export * from './client';
