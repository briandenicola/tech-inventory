/**
 * /admin/export — C-10 (Admin-gated export hub: filters wired to
 * referenceDataStore, download flow calls exportsApi.devices()), C-18
 * (partial: axe on the default view).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import type { AuthState, CurrentUser } from '$lib/stores/auth';
import type { ReferenceDataState } from '$lib/stores/referenceData';

const { goto, devicesExportMock, fetchReferenceDataMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	devicesExportMock: vi.fn(),
	fetchReferenceDataMock: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto }));

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	const initialAuthState: AuthState = {
		currentUser: null,
		isAuthenticated: false,
		isLoading: false,
		error: null,
		authMethod: null,
		mustChangePassword: false
	};

	return { authStore: writable(initialAuthState) };
});

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	const initialState: ReferenceDataState = {
		brands: [{ id: 'brand-1', name: 'Apple' }],
		categories: [{ id: 'category-1', name: 'Laptop' }],
		owners: [{ id: 'owner-1', name: 'Brian' }],
		locations: [{ id: 'location-1', name: 'Home Office' }],
		networks: [{ id: 'network-1', name: 'Wi-Fi' }],
		tags: [],
		isLoading: false,
		error: null
	};

	return {
		referenceDataStore: writable(initialState),
		fetchReferenceData: fetchReferenceDataMock
	};
});

vi.mock('$lib/api/client', async () => {
	const actual = await vi.importActual<typeof import('$lib/api/client')>('$lib/api/client');
	return {
		...actual,
		exports: { devices: devicesExportMock }
	};
});

import { authStore } from '$lib/stores/auth';
import { toasts, clearToasts } from '$lib/stores/toast';
import { get } from 'svelte/store';
import Page from './+page.svelte';

const adminUser: CurrentUser = {
	id: 'owner-1',
	entraObjectId: 'entra-1',
	displayName: 'Brian',
	role: 'Admin'
};

function setAuth(currentUser: CurrentUser | null) {
	authStore.set({
		currentUser,
		isAuthenticated: currentUser !== null,
		isLoading: false,
		error: null,
		authMethod: currentUser ? 'entra' : null,
		mustChangePassword: false
	});
}

describe('/admin/export (C-10)', () => {
	beforeEach(() => {
		goto.mockReset();
		devicesExportMock.mockReset();
		fetchReferenceDataMock.mockReset();
		setAuth(adminUser);
		clearToasts();

		// jsdom does not implement Blob URL creation/revocation.
		vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:mock-url');
		vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
	});

	it('redirects a non-Admin owner away from the export hub', () => {
		setAuth({ ...adminUser, role: 'Viewer' });

		render(Page);

		expect(goto).toHaveBeenCalledWith('/devices');
	});

	it('does not redirect an Admin owner and loads reference data for the filter dropdowns', () => {
		render(Page);

		expect(goto).not.toHaveBeenCalled();
		expect(fetchReferenceDataMock).toHaveBeenCalled();
		expect(screen.getByRole('option', { name: 'Apple' })).toBeInTheDocument();
		// "Home Office" also appears in InsuranceExportCard's own location
		// select further down the page — scope to the export filter select.
		expect(
			within(screen.getByLabelText('Location') as HTMLElement).getByRole('option', {
				name: 'Home Office'
			})
		).toBeInTheDocument();
	});

	it('renders the export hub with format choice and download action', () => {
		render(Page);

		expect(screen.getByRole('heading', { name: 'Export Devices' })).toBeInTheDocument();
		expect(screen.getByRole('radio', { name: 'CSV' })).toBeChecked();
		expect(screen.getByRole('radio', { name: 'JSON' })).not.toBeChecked();
		expect(screen.getByRole('button', { name: 'Download Export' })).toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(Page);

		expect(await axe(container)).toHaveNoViolations();
	});

	it('calls exportsApi.devices() with the selected filters and triggers a download', async () => {
		const blob = new Blob(['id,name'], { type: 'text/csv' });
		devicesExportMock.mockResolvedValue(blob);

		render(Page);

		await fireEvent.input(screen.getByLabelText('Search'), { target: { value: 'router' } });
		await fireEvent.change(screen.getByLabelText('Brand'), { target: { value: 'brand-1' } });

		await fireEvent.click(screen.getByRole('button', { name: 'Download Export' }));

		await waitFor(() => expect(devicesExportMock).toHaveBeenCalledTimes(1));
		expect(devicesExportMock).toHaveBeenCalledWith(
			expect.objectContaining({ Format: 'Csv', Search: 'router', BrandId: 'brand-1' })
		);
		expect(get(toasts).some((toast) => toast.message === 'Export downloaded successfully')).toBe(
			true
		);
	});

	it('surfaces an error message when the download fails (mutation error, C-16)', async () => {
		devicesExportMock.mockRejectedValue(new Error('network unreachable'));

		render(Page);

		await fireEvent.click(screen.getByRole('button', { name: 'Download Export' }));

		await waitFor(() => expect(devicesExportMock).toHaveBeenCalledTimes(1));
		expect(await screen.findByRole('alert')).toHaveTextContent('network unreachable');
	});
});
