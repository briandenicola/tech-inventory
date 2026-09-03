/**
 * /devices/new — C-16 (mutation failure surfaces an error toast, never a
 * silent success). The production POST is `NetworkOnly` per the Workbox
 * config (see vite.config.ts / C-15); Vitest cannot register a real service
 * worker, so the offline-refusal path is modelled the same way it manifests
 * to the page: `devices.create()` rejects. This proves the resulting UI
 * always surfaces an error — the same catch block that runs for a genuine
 * offline `NetworkOnly` refusal.
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import type { ReferenceDataState } from '$lib/stores/referenceData';

const { goto, createMock, syncTagsMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	createMock: vi.fn(),
	syncTagsMock: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto, beforeNavigate: vi.fn() }));

vi.mock('$app/stores', async () => {
	const { readable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	return {
		page: readable({
			url: new URL('http://localhost/devices/new'),
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
		devices: { ...actual.devices, create: createMock, syncTags: syncTagsMock }
	};
});

const testCategoryId = '00000000-0000-4000-8000-000000000101';
const testOwnerId = '00000000-0000-4000-8000-000000000102';
const testLocationId = '00000000-0000-4000-8000-000000000103';

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	const initialState: ReferenceDataState = {
		brands: [{ id: '00000000-0000-4000-8000-000000000100', name: 'Apple' }],
		categories: [{ id: '00000000-0000-4000-8000-000000000101', name: 'Laptop' }],
		owners: [{ id: '00000000-0000-4000-8000-000000000102', name: 'Brian' }],
		locations: [{ id: '00000000-0000-4000-8000-000000000103', name: 'Home Office' }],
		networks: [],
		tags: [],
		isLoading: false,
		error: null
	};

	return {
		referenceDataStore: writable(initialState),
		fetchReferenceData: vi.fn(async () => {})
	};
});

import { toasts, clearToasts } from '$lib/stores/toast';
import { get } from 'svelte/store';
import Page from './+page.svelte';

async function fillRequiredFields(user: ReturnType<typeof userEvent.setup>) {
	await user.type(screen.getByLabelText(/^Name/), 'ThinkPad X1');
	await user.selectOptions(screen.getByLabelText(/^Category/), testCategoryId);
	await user.selectOptions(screen.getByLabelText(/^Owner/), testOwnerId);
	await user.selectOptions(screen.getByLabelText(/^Location/), testLocationId);
}

describe('/devices/new (C-16 offline/mutation error surfacing)', () => {
	beforeEach(() => {
		goto.mockReset();
		createMock.mockReset();
		syncTagsMock.mockReset();
		clearToasts();
	});

	it('surfaces an error toast and keeps the user on the page when the create mutation is refused', async () => {
		const user = userEvent.setup();
		createMock.mockRejectedValue(new Error('Failed to fetch'));

		render(Page);
		await fillRequiredFields(user);

		const saveButton = screen.getByRole('button', { name: 'Save' });
		await waitFor(() => expect(saveButton).not.toBeDisabled());
		await user.click(saveButton);

		await waitFor(() => expect(createMock).toHaveBeenCalledTimes(1));
		// Never a silent success: no redirect away from the form, and the
		// error toast renders with the failure message.
		expect(goto).not.toHaveBeenCalled();
		await waitFor(() =>
			expect(get(toasts).some((toast) => toast.type === 'error' && toast.message === 'Failed to fetch')).toBe(
				true
			)
		);
	});

	it('redirects to the new device on a successful create (control case)', async () => {
		const user = userEvent.setup();
		createMock.mockResolvedValue({ id: 'device-1' });
		syncTagsMock.mockResolvedValue(undefined);

		render(Page);
		await fillRequiredFields(user);

		const saveButton = screen.getByRole('button', { name: 'Save' });
		await waitFor(() => expect(saveButton).not.toBeDisabled());
		await user.click(saveButton);

		await waitFor(() => expect(goto).toHaveBeenCalledWith('/devices/device-1'));
	});
});
