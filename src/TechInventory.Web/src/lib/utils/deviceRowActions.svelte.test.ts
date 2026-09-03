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
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createDeviceRowActions } from './deviceRowActions.svelte';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';
import type { CurrentUser } from '$lib/stores/auth';

const mocks = vi.hoisted(() => ({
	bulkUpdate: vi.fn(),
	invalidateDevicesCache: vi.fn(),
	showToast: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	devices: { bulkUpdate: mocks.bulkUpdate }
}));

vi.mock('$lib/queries/devices.svelte', () => ({
	invalidateDevicesCache: mocks.invalidateDevicesCache
}));

vi.mock('$lib/stores/toast', () => ({
	showToast: mocks.showToast
}));

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

describe('createDeviceRowActions — canChangeStatus (#127)', () => {
	beforeEach(() => {
		resetFactories();
	});

	it('grants Admin the change-status affordance on any non-Disposed device', () => {
		const device = createDeviceResponse({ status: 'Retired', ownerId: 'someone-else' });
		const admin = createDeviceRowActions(() => device, () => makeUser('Admin', 'me'));
		expect(admin.canChangeStatus).toBe(true);
	});

	it('grants an owning Member the change-status affordance but not a non-owning Member', () => {
		const owned = createDeviceResponse({ ownerId: 'me' });
		const unowned = createDeviceResponse({ ownerId: 'someone-else' });

		expect(createDeviceRowActions(() => owned, () => makeUser('Member', 'me')).canChangeStatus).toBe(
			true
		);
		expect(createDeviceRowActions(() => unowned, () => makeUser('Member', 'me')).canChangeStatus).toBe(
			false
		);
	});

	it('denies a Viewer regardless of ownership', () => {
		const owned = createDeviceResponse({ ownerId: 'me' });
		expect(createDeviceRowActions(() => owned, () => makeUser('Viewer', 'me')).canChangeStatus).toBe(
			false
		);
	});

	it('denies changing status on an already-Disposed device even for Admin', () => {
		const disposed = createDeviceResponse({ status: 'Disposed' });
		expect(createDeviceRowActions(() => disposed, () => makeUser('Admin', 'me')).canChangeStatus).toBe(
			false
		);
	});
});

describe('createDeviceRowActions — handleChangeStatus (#127)', () => {
	beforeEach(() => {
		resetFactories();
		mocks.bulkUpdate.mockReset();
		mocks.invalidateDevicesCache.mockReset();
		mocks.showToast.mockReset();
	});

	it('reuses devices.bulkUpdate scoped to this device id, invalidates the cache, toasts success, and clears the open modal', async () => {
		mocks.bulkUpdate.mockResolvedValueOnce({ correlationId: 'corr-1', affectedCount: 1 });
		const device = createDeviceResponse({ id: 'device-42', name: 'Aqua Flosser', status: 'Active' });
		const onChanged = vi.fn();
		const actions = createDeviceRowActions(() => device, () => makeUser('Admin', 'me'), { onChanged });
		actions.openModal = 'changeStatus';

		await actions.handleChangeStatus('Retired');

		expect(mocks.bulkUpdate).toHaveBeenCalledWith({
			deviceIds: ['device-42'],
			changes: { status: 'Retired' }
		});
		expect(mocks.invalidateDevicesCache).toHaveBeenCalledOnce();
		expect(mocks.showToast).toHaveBeenCalledWith({
			type: 'success',
			message: expect.stringContaining('Aqua Flosser')
		});
		expect(onChanged).toHaveBeenCalledOnce();
		expect(actions.openModal).toBeNull();
	});

	it('shows an error toast and still clears the open modal when the API call fails', async () => {
		mocks.bulkUpdate.mockRejectedValueOnce(new Error('boom'));
		const device = createDeviceResponse({ status: 'Active' });
		const onChanged = vi.fn();
		const actions = createDeviceRowActions(() => device, () => makeUser('Admin', 'me'), { onChanged });
		actions.openModal = 'changeStatus';

		await actions.handleChangeStatus('Retired');

		expect(mocks.showToast).toHaveBeenCalledWith(
			expect.objectContaining({ type: 'error' })
		);
		expect(onChanged).not.toHaveBeenCalled();
		expect(actions.openModal).toBeNull();
	});
});
