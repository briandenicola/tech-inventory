/**
 * deviceRowActions.svelte.ts controller tests — C-12.
 *
 * The role-gating rules (`canEdit`/`canDelete`/`canViewHistory`) mirror
 * `DeviceDetailModal`'s inline copy exactly (see the module docstring), and
 * are exercised here directly against the composable rather than through a
 * rendered component — the cheapest deterministic place to prove Admin,
 * Member, and Viewer each see the correct affordance set. `canClaim`/
 * `canRelease` are ownership-gated **and** role-gated to Admin/Member —
 * constitution §5.2 / docs/prd.md define Viewer as read-only, so a Viewer
 * must never see claim/release regardless of device ownership (B1/B3 review
 * fix: a prior revision of this file asserted the opposite as "intentional
 * behaviour" — that was the defect, not a documented exception).
 */
import { describe, it, expect, beforeEach } from 'vitest';
import { createDeviceRowActions } from './deviceRowActions.svelte';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';
import type { CurrentUser } from '$lib/stores/auth';

function makeUser(role: CurrentUser['role'], id = 'user-1'): CurrentUser {
	return { id, entraObjectId: null, displayName: 'Test User', role };
}

describe('createDeviceRowActions (C-12 role gating)', () => {
	beforeEach(() => {
		resetFactories();
	});

	it('grants edit to Admin and Member but not Viewer', () => {
		const device = createDeviceResponse();

		const admin = createDeviceRowActions(() => device, () => makeUser('Admin'));
		const member = createDeviceRowActions(() => device, () => makeUser('Member'));
		const viewer = createDeviceRowActions(() => device, () => makeUser('Viewer'));

		expect(admin.canEdit).toBe(true);
		expect(member.canEdit).toBe(true);
		expect(viewer.canEdit).toBe(false);
	});

	it('grants delete and view-history only to Admin', () => {
		const device = createDeviceResponse();

		const admin = createDeviceRowActions(() => device, () => makeUser('Admin'));
		const member = createDeviceRowActions(() => device, () => makeUser('Member'));
		const viewer = createDeviceRowActions(() => device, () => makeUser('Viewer'));

		expect(admin.canDelete).toBe(true);
		expect(admin.canViewHistory).toBe(true);

		expect(member.canDelete).toBe(false);
		expect(member.canViewHistory).toBe(false);

		expect(viewer.canDelete).toBe(false);
		expect(viewer.canViewHistory).toBe(false);
	});

	it('denies every gated action when there is no signed-in user', () => {
		const device = createDeviceResponse();
		const anonymous = createDeviceRowActions(() => device, () => null);

		expect(anonymous.canEdit).toBe(false);
		expect(anonymous.canDelete).toBe(false);
		expect(anonymous.canViewHistory).toBe(false);
	});

	it('gates claim/release by ownership for Admin and Member', () => {
		const unowned = createDeviceResponse({ ownerId: 'someone-else' });
		const owned = createDeviceResponse({ ownerId: 'me' });

		const memberUnowned = createDeviceRowActions(() => unowned, () => makeUser('Member', 'me'));
		expect(memberUnowned.canClaim).toBe(true);
		expect(memberUnowned.canRelease).toBe(false);

		const memberOwned = createDeviceRowActions(() => owned, () => makeUser('Member', 'me'));
		expect(memberOwned.canClaim).toBe(false);
		expect(memberOwned.canRelease).toBe(true);

		const adminUnowned = createDeviceRowActions(() => unowned, () => makeUser('Admin', 'me'));
		expect(adminUnowned.canClaim).toBe(true);
		expect(adminUnowned.canRelease).toBe(false);
	});

	it('denies claim/release to a Viewer regardless of device ownership', () => {
		const unowned = createDeviceResponse({ ownerId: 'someone-else' });
		const viewerActions = createDeviceRowActions(() => unowned, () => makeUser('Viewer', 'me'));

		expect(viewerActions.canClaim).toBe(false);
		expect(viewerActions.canRelease).toBe(false);

		const owned = createDeviceResponse({ ownerId: 'me' });
		const viewerOwnerActions = createDeviceRowActions(() => owned, () => makeUser('Viewer', 'me'));

		expect(viewerOwnerActions.canClaim).toBe(false);
		expect(viewerOwnerActions.canRelease).toBe(false);
	});
});
