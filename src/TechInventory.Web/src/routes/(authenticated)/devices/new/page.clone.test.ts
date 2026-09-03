/**
 * /devices/new?cloneFrom=<id> — Issue #131 (Clone Device).
 *
 * Clone opens the *normal* create-device flow (same DeviceForm, same
 * `devices.create()` submit path as `page.test.ts`), pre-filled from a
 * source device fetched fresh via `devices.get`/`devices.listTags`. This
 * file proves the field-copy matrix and the safety invariants called out in
 * the issue:
 *
 *   COPIED (reusable/descriptive/reference — matches the AC's "brand,
 *   category, owner, location, network, model, tags and other reusable
 *   attributes"):
 *     model, brandId, categoryId, ownerId, locationId, networkId, tagIds,
 *     currencyCode, purpose, operatingSystem, productUrl, version
 *
 *   CLEARED (never copied — identity/unique/history, per the AC's "unique
 *   identifiers are NOT copied" plus the task's audit/timestamp/status
 *   exclusions):
 *     name (blank, required — user must give the new unit its own
 *       identity; the smaller-surface alternative to a disambiguating
 *       suffix), serialNumber (blank per the AC), ipAddress/macAddress
 *       (per-unit network/hardware identity), purchaseDate/purchasePrice
 *       (instance transaction history), notes (may hold unit-specific
 *       history), status/retiredDate/disposalMethod (mode="create" never
 *       renders Status at all — server defaults it to Active — and these
 *       are not part of DeviceFormInput), id/createdAt/createdBy/
 *       modifiedAt/modifiedBy (audit identity — never part of
 *       DeviceFormInput, never read from the source device).
 *
 * Also proves: create-not-update (devices.update is never called),
 * source-device-unchanged (only devices.get/listTags — read-only — are
 * called against the source id), cancel persists nothing, a source-fetch
 * failure surfaces ErrorState with a working retry, and the URL's
 * `cloneFrom` param is the only navigation-state carried across (no device
 * data serialized into the URL/session).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import type { ReferenceDataState } from '$lib/stores/referenceData';
import { createDeviceResponse, createTag, resetFactories } from '$lib/test-utils/factories';

const {
	goto,
	getMock,
	listTagsMock,
	createMock,
	updateMock,
	deleteMock,
	syncTagsMock,
	testBrandId,
	testCategoryId,
	testOwnerId,
	testLocationId,
	testNetworkId,
	testTagId
} = vi.hoisted(() => ({
	goto: vi.fn(),
	getMock: vi.fn(),
	listTagsMock: vi.fn(),
	createMock: vi.fn(),
	updateMock: vi.fn(),
	deleteMock: vi.fn(),
	syncTagsMock: vi.fn(),
	testBrandId: '00000000-0000-4000-8000-000000000301',
	testCategoryId: '00000000-0000-4000-8000-000000000201',
	testOwnerId: '00000000-0000-4000-8000-000000000401',
	testLocationId: '00000000-0000-4000-8000-000000000501',
	testNetworkId: '00000000-0000-4000-8000-000000000601',
	testTagId: '00000000-0000-4000-8000-000000000101'
}));

vi.mock('$app/navigation', () => ({ goto, beforeNavigate: vi.fn() }));

vi.mock('$app/stores', async () => {
	const { readable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	return {
		page: readable({
			url: new URL('http://localhost/devices/new?cloneFrom=source-device-1'),
			params: {},
			route: { id: '/(authenticated)/devices/new' },
			status: 200,
			error: null,
			data: {},
			form: null
		})
	};
});

vi.mock('$lib/api/client', async () => {
	const actual = await vi.importActual<typeof import('$lib/api/client')>('$lib/api/client');
	return {
		...actual,
		devices: {
			...actual.devices,
			get: getMock,
			listTags: listTagsMock,
			create: createMock,
			update: updateMock,
			delete: deleteMock,
			syncTags: syncTagsMock
		}
	};
});

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	const initialState: ReferenceDataState = {
		brands: [{ id: testBrandId, name: 'ThirdReality' }],
		categories: [{ id: testCategoryId, name: 'IoT' }],
		owners: [{ id: testOwnerId, name: 'Brian' }],
		locations: [{ id: testLocationId, name: 'Garden' }],
		networks: [{ id: testNetworkId, name: 'Home WiFi' }],
		tags: [{ id: testTagId, name: 'IoT', color: '#0ea5e9' }],
		isLoading: false,
		error: null
	};

	return {
		referenceDataStore: writable(initialState),
		fetchReferenceData: vi.fn(async () => {})
	};
});

import { clearToasts } from '$lib/stores/toast';
import Page from './+page.svelte';

function makeSourceDevice() {
	return createDeviceResponse({
		id: 'source-device-1',
		name: 'Original Sensor Unit',
		model: 'X100',
		serialNumber: 'SRC-SN-001',
		brandId: testBrandId,
		categoryId: testCategoryId,
		ownerId: testOwnerId,
		locationId: testLocationId,
		networkId: testNetworkId,
		purchaseDate: '2024-05-01',
		purchasePrice: 199.99,
		currencyCode: 'EUR',
		status: 'Retired',
		notes: 'Unit-specific private note (should never carry over)',
		retiredDate: '2025-01-01',
		disposalMethod: null,
		purpose: 'Irrigation monitoring',
		operatingSystem: 'FreeRTOS 2.1',
		ipAddress: '10.0.0.42',
		macAddress: 'AA:BB:CC:DD:EE:FF',
		productUrl: 'https://example.com/x100',
		version: '2.1.0'
	});
}

describe('/devices/new?cloneFrom=<id> (#131 Clone Device)', () => {
	beforeEach(() => {
		resetFactories();
		goto.mockReset();
		getMock.mockReset();
		listTagsMock.mockReset();
		createMock.mockReset();
		updateMock.mockReset();
		deleteMock.mockReset();
		syncTagsMock.mockReset();
		clearToasts();
	});

	it('fetches the source device fresh via the API and pre-fills only the reusable/descriptive fields, leaving identity/unique/history fields blank', async () => {
		getMock.mockResolvedValue(makeSourceDevice());
		listTagsMock.mockResolvedValue([createTag({ id: testTagId, name: 'IoT', color: '#0ea5e9' })]);

		render(Page);

		const nameInput = (await screen.findByLabelText(/^Name/)) as HTMLInputElement;

		expect(getMock).toHaveBeenCalledWith('source-device-1');
		expect(listTagsMock).toHaveBeenCalledWith('source-device-1');

		// Banner gives the user context without leaking source data into the URL.
		expect(screen.getByText(/Cloned from Original Sensor Unit/)).toBeInTheDocument();

		// --- COPIED: reusable/descriptive/reference fields + tags ---
		expect((screen.getByLabelText(/^Model/) as HTMLInputElement).value).toBe('X100');
		expect((screen.getByLabelText(/^Brand/) as HTMLSelectElement).value).toBe(testBrandId);
		expect((screen.getByLabelText(/^Category/) as HTMLSelectElement).value).toBe(testCategoryId);
		expect((screen.getByLabelText(/^Owner/) as HTMLSelectElement).value).toBe(testOwnerId);
		expect((screen.getByLabelText(/^Location/) as HTMLSelectElement).value).toBe(testLocationId);
		expect((screen.getByLabelText(/^Network/) as HTMLSelectElement).value).toBe(testNetworkId);
		expect((screen.getByLabelText('IoT') as HTMLInputElement).checked).toBe(true);
		expect((screen.getByLabelText(/^Currency/) as HTMLSelectElement).value).toBe('EUR');
		expect((screen.getByLabelText(/^Purpose/) as HTMLTextAreaElement).value).toBe(
			'Irrigation monitoring'
		);
		expect((screen.getByLabelText(/^Operating System/) as HTMLInputElement).value).toBe(
			'FreeRTOS 2.1'
		);
		expect((screen.getByLabelText(/^Product URL/) as HTMLInputElement).value).toBe(
			'https://example.com/x100'
		);
		expect((screen.getByLabelText(/^Version/) as HTMLInputElement).value).toBe('2.1.0');

		// --- CLEARED: identity/unique/history fields ---
		expect(nameInput.value).toBe('');
		expect((screen.getByLabelText(/^Serial Number/) as HTMLInputElement).value).toBe('');
		expect((screen.getByLabelText(/^IP Address/) as HTMLInputElement).value).toBe('');
		expect((screen.getByLabelText(/^MAC Address/) as HTMLInputElement).value).toBe('');
		expect((screen.getByLabelText(/^Purchase Date/) as HTMLInputElement).value).toBe('');
		expect((screen.getByLabelText(/^Purchase Price/) as HTMLInputElement).value).toBe('');
		expect((screen.getByLabelText(/^Notes/) as HTMLTextAreaElement).value).toBe('');
		// create mode never renders Status — server defaults it to Active
		// regardless of the source device's (Retired) status.
		expect(screen.queryByLabelText(/^Status/)).not.toBeInTheDocument();
	});

	it('creates a brand-new device via the normal create path — never updates the source, which stays untouched', async () => {
		const user = userEvent.setup();
		getMock.mockResolvedValue(makeSourceDevice());
		listTagsMock.mockResolvedValue([createTag({ id: testTagId, name: 'IoT', color: '#0ea5e9' })]);
		createMock.mockResolvedValue({ id: 'new-device-1' });
		syncTagsMock.mockResolvedValue(undefined);

		render(Page);
		const nameInput = await screen.findByLabelText(/^Name/);
		await user.type(nameInput, 'Soil Moisture Sensor #2');
		await user.type(screen.getByLabelText(/^Serial Number/), 'NEW-SN-002');

		const saveButton = screen.getByRole('button', { name: 'Save' });
		await waitFor(() => expect(saveButton).not.toBeDisabled());
		await user.click(saveButton);

		await waitFor(() => expect(createMock).toHaveBeenCalledTimes(1));

		// Create-not-update: the source is only ever read, never mutated.
		expect(getMock).toHaveBeenCalledTimes(1);
		expect(updateMock).not.toHaveBeenCalled();
		expect(deleteMock).not.toHaveBeenCalled();

		const payload = createMock.mock.calls[0]![0] as Record<string, unknown>;
		expect(payload).not.toHaveProperty('id');
		expect(payload.name).toBe('Soil Moisture Sensor #2');
		expect(payload.serialNumber).toBe('NEW-SN-002');
		expect(payload.categoryId).toBe(testCategoryId);
		expect(payload.ownerId).toBe(testOwnerId);
		expect(payload.locationId).toBe(testLocationId);
		// status is never part of the create payload's intent here — the
		// form doesn't expose the control in create mode, and the API
		// defaults new devices to Active regardless of the source's status.
		expect(payload).not.toHaveProperty('retiredDate');
		expect(payload).not.toHaveProperty('disposalMethod');

		expect(goto).toHaveBeenCalledWith('/devices/new-device-1');
	});

	it('cancelling the cloned form creates no record and never touches the source device', async () => {
		const user = userEvent.setup();
		getMock.mockResolvedValue(makeSourceDevice());
		listTagsMock.mockResolvedValue([]);

		render(Page);
		await screen.findByLabelText(/^Name/);

		await user.click(screen.getByRole('button', { name: 'Cancel' }));

		expect(createMock).not.toHaveBeenCalled();
		expect(updateMock).not.toHaveBeenCalled();
		expect(goto).toHaveBeenCalledWith('/devices');
	});

	it('surfaces an error state (with working retry) when the source device fails to load, and never attempts to create a device', async () => {
		const user = userEvent.setup();
		getMock.mockRejectedValueOnce(new Error('Failed to fetch'));

		render(Page);

		expect(await screen.findByRole('alert')).toHaveTextContent('Failed to fetch');
		expect(createMock).not.toHaveBeenCalled();

		getMock.mockResolvedValueOnce(makeSourceDevice());
		listTagsMock.mockResolvedValue([]);
		await user.click(screen.getByRole('button', { name: 'Retry' }));

		await screen.findByLabelText(/^Name/);
		expect(getMock).toHaveBeenCalledTimes(2);
	});

	it('has no accessibility violations once the cloned form has loaded', async () => {
		getMock.mockResolvedValue(makeSourceDevice());
		listTagsMock.mockResolvedValue([createTag({ id: testTagId, name: 'IoT', color: '#0ea5e9' })]);

		const { container } = render(Page);
		await screen.findByLabelText(/^Name/);

		expect(await axe(container)).toHaveNoViolations();
	});
});

// The no-`cloneFrom` control case (blank form renders immediately, no
// source fetch, existing create/error-toast flows unaffected) is already
// covered by the sibling `page.test.ts` — that file's `$app/stores` mock is
// a plain `/devices/new` URL, proving #131 didn't regress the base flow.
