/**
 * API Client Bootstrap
 * 
 * Wires MSAL token acquisition into the API client's auth header injection.
 * Side-effect import in +layout.svelte configures the client at app startup.
 */

import { setApiConfig } from './client';
import { acquireApiToken } from '$lib/auth';

// T05: Configure API client to inject Bearer tokens from MSAL.js
setApiConfig({
	getAuthToken: acquireApiToken
});

// Re-export client functions for convenience
export * from './client';
