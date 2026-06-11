import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import AddDeviceModal from './AddDeviceModal.svelte';

const mocks = vi.hoisted(() => ({
	create: vi.fn(),
	syncTags: vi.fn(),
	showToast: vi.fn(),
	invalidateDevicesCache: vi.fn(),
	fetchReferenceData: vi.fn()
}));

vi.mock('$lib/i18n', () => ({
	t: (key: string, params?: Record<string, string>) =>
		params?.name ? `${key} ${params.name}` : key
}));

vi.mock('$lib/api/client', () => ({
	devices: {
		create: mocks.create,
		syncTags: mocks.syncTags
	}
}));

vi.mock('$lib/stores/toast', () => ({
	showToast: mocks.showToast
}));

vi.mock('$lib/queries/devices.svelte', () => ({
	invalidateDevicesCache: mocks.invalidateDevicesCache
}));

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await import('svelte/store');
	return {
		referenceDataStore: writable({
			brands: [
				{
					id: '00000000-0000-4000-8000-000000000301',
					name: 'Dyson'
				}
			],
			categories: [
				{
					id: '00000000-0000-4000-8000-000000000201',
					name: 'Fan'
				}
			],
			owners: [],
			locations: [],
			networks: [],
			tags: [
				{
					id: '00000000-0000-4000-8000-000000000101',
					name: 'Cooling',
					color: '#0ea5e9'
				}
			],
			isLoading: false,
			error: null
		}),
		fetchReferenceData: mocks.fetchReferenceData
	};
});

describe('AddDeviceModal', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.create.mockResolvedValue({ id: '00000000-0000-4000-8000-000000000501' });
		mocks.syncTags.mockResolvedValue([]);
	});

	it('uses DeviceForm as the single tag UI and syncs selected tags after create', async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();
		const onCreated = vi.fn();

		render(AddDeviceModal, {
			props: {
				onClose,
				onCreated
			}
		});

		expect(screen.queryByPlaceholderText('Type to find or add tags...')).not.toBeInTheDocument();
		expect(screen.getAllByRole('group', { name: /devices.form.tags/i })).toHaveLength(1);

		await user.type(screen.getByLabelText(/devices.columns.name/i), 'Dyson fan');
		await user.selectOptions(
			screen.getByLabelText(/devices.columns.brand/i),
			'00000000-0000-4000-8000-000000000301'
		);
		await user.selectOptions(
			screen.getByLabelText(/devices.columns.category/i),
			'00000000-0000-4000-8000-000000000201'
		);
		await user.click(screen.getByLabelText('Cooling'));
		await user.click(screen.getByRole('button', { name: /common.actions.save/i }));

		await waitFor(() => {
			expect(mocks.create).toHaveBeenCalledTimes(1);
		});

		expect(mocks.create).toHaveBeenCalledWith(
			expect.not.objectContaining({
				tagIds: expect.any(Array)
			})
		);
		expect(mocks.syncTags).toHaveBeenCalledWith('00000000-0000-4000-8000-000000000501', [
			'00000000-0000-4000-8000-000000000101'
		]);
		expect(onCreated).toHaveBeenCalledWith('00000000-0000-4000-8000-000000000501');
		expect(onClose).toHaveBeenCalledTimes(1);
	});
});
