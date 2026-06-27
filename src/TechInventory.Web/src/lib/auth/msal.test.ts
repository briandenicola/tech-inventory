import { describe, expect, it } from 'vitest';
import { BrowserCacheLocation } from '@azure/msal-browser';
import { loginRequest, msalConfig } from './msal';

describe('MSAL configuration', () => {
	it('keeps tokens in sessionStorage', () => {
		expect(msalConfig.cache?.cacheLocation).toBe(BrowserCacheLocation.SessionStorage);
	});

	it('requests offline access for refresh behavior', () => {
		expect(loginRequest.scopes).toContain('offline_access');
	});
});
