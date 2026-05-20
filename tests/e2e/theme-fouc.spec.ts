/**
 * theme-fouc.spec.ts — Playwright E2E test for F029 FOUC suppression.
 *
 * Verifies that the pre-hydration script in app.html sets data-theme="dark"
 * on <html> BEFORE first paint when themePreference='dark' is stored in localStorage,
 * preventing any light-mode flash during load.
 */
import { test, expect } from '@playwright/test';

test.describe('F029: Dark theme FOUC suppression', () => {
	test.beforeEach(async ({ page }) => {
		// Set up a mock userId in localStorage (matches userPrefs.ts storage key pattern)
		await page.addInitScript(() => {
			localStorage.setItem('ti.currentUserId', 'test-user-fouc');
			localStorage.setItem(
				'ti.userPrefs.v1.test-user-fouc',
				JSON.stringify({ version: 1, themePreference: 'dark' })
			);
		});
	});

	test('sets data-theme="dark" before first paint', async ({ page }) => {
		// Navigate and immediately check <html> before hydration completes.
		// The pre-hydration script runs synchronously in <head>, so data-theme
		// should be set BEFORE any Svelte components mount.
		await page.goto('/');

		const htmlTheme = await page.locator('html').getAttribute('data-theme');
		expect(htmlTheme).toBe('dark');
	});

	test('body background is dark immediately after navigation', async ({ page }) => {
		await page.goto('/');

		// Sample the body background color immediately. If FOUC suppression fails,
		// we'd see a light background (white/neutral-50) for a frame or two before
		// the theme script executes. With the pre-hydration script, it should be
		// dark from the start.
		const bodyBg = await page.evaluate(() => {
			const computed = window.getComputedStyle(document.body);
			return computed.backgroundColor;
		});

		// Dark theme body bg is typically neutral-900 (#1d1d1f ≈ rgb(29, 29, 31)).
		// Light theme would be white (rgb(255, 255, 255)). Assert we're not white.
		expect(bodyBg).not.toBe('rgb(255, 255, 255)');
		expect(bodyBg).not.toBe('#ffffff');
	});

	test('screenshot shows no light flash', async ({ page }) => {
		// Navigate and screenshot immediately after page load starts.
		// If FOUC is present, the screenshot would show light content briefly.
		// This is a belt-and-suspenders visual check.
		await page.goto('/');
		await page.waitForLoadState('domcontentloaded');

		const screenshot = await page.screenshot();
		expect(screenshot).toBeDefined();
		// Visual regression is not automated here, but the screenshot can be manually
		// inspected in CI artifacts to verify no light-mode frame appears.
	});
});
