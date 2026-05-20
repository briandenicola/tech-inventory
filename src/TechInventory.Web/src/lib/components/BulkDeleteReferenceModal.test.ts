import { describe, expect, it, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';

const mocks = vi.hoisted(() => ({
	fetchReferenceDeviceCount: vi.fn(),
	bulkDelete: vi.fn(),
	addToast: vi.fn()
}));

vi.mock('$lib/utils/referenceMerge', () => ({
	fetchReferenceDeviceCount: mocks.fetchReferenceDeviceCount
}));

vi.mock('$lib/stores/toast', () => ({
	addToast: mocks.addToast
}));

vi.mock('$lib/api/client', () => ({
	default: {
		brands: { bulkDelete: mocks.bulkDelete },
		categories: { bulkDelete: mocks.bulkDelete },
		locations: { bulkDelete: mocks.bulkDelete },
		networks: { bulkDelete: mocks.bulkDelete }
	}
}));

import BulkDeleteReferenceModal from './BulkDeleteReferenceModal.svelte';

describe('BulkDeleteReferenceModal', () => {
	beforeEach(() => {
		mocks.fetchReferenceDeviceCount.mockReset();
		mocks.bulkDelete.mockReset();
		mocks.addToast.mockReset();
	});

	it('warns when any selected item still has devices assigned', async () => {
		mocks.fetchReferenceDeviceCount.mockResolvedValueOnce(2).mockResolvedValueOnce(0);
		render(BulkDeleteReferenceModal, {
			props: {
				entityType: 'brand',
				items: [
					{ id: 'brand-1', name: 'Apple' },
					{ id: 'brand-2', name: 'Dell' }
				],
				isOpen: true,
				onDeleted: vi.fn(),
				onCancel: vi.fn()
			}
		});

		expect(
			await screen.findByText(/still have devices assigned and cannot be deleted/i)
		).toBeInTheDocument();
		await waitFor(() =>
			expect(screen.getByRole('button', { name: /delete selected/i })).toBeDisabled()
		);
	});

	it('calls the bulk delete endpoint and success callback when there are no conflicts', async () => {
		const user = userEvent.setup();
		const onDeleted = vi.fn().mockResolvedValue(undefined);
		const onCancel = vi.fn();
		mocks.fetchReferenceDeviceCount.mockResolvedValue(0);
		mocks.bulkDelete.mockResolvedValue({ deletedCount: 2 });

		render(BulkDeleteReferenceModal, {
			props: {
				entityType: 'network',
				items: [
					{ id: 'network-1', name: 'Main Wi-Fi' },
					{ id: 'network-2', name: 'Guest Wi-Fi' }
				],
				isOpen: true,
				onDeleted,
				onCancel
			}
		});

		await waitFor(() =>
			expect(screen.getByRole('button', { name: /delete selected/i })).toBeEnabled()
		);
		await user.click(screen.getByRole('button', { name: /delete selected/i }));

		await waitFor(() =>
			expect(mocks.bulkDelete).toHaveBeenCalledWith({ ids: ['network-1', 'network-2'] })
		);
		expect(onCancel).toHaveBeenCalledOnce();
		expect(onDeleted).toHaveBeenCalledOnce();
		expect(mocks.addToast).toHaveBeenCalledWith(
			expect.objectContaining({
				type: 'success'
			})
		);
	});

	it('has no accessibility violations', async () => {
		mocks.fetchReferenceDeviceCount.mockResolvedValue(0);
		const { container } = render(BulkDeleteReferenceModal, {
			props: {
				entityType: 'location',
				items: [{ id: 'location-1', name: 'Office' }],
				isOpen: true,
				onDeleted: vi.fn(),
				onCancel: vi.fn()
			}
		});

		await screen.findByRole('dialog');
		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});
