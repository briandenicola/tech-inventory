import { fireEvent, render, screen } from '@testing-library/svelte';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import MergeEntityModal from './MergeEntityModal.svelte';

describe('MergeEntityModal', () => {
	it('renders the confirmation text for the selected target', async () => {
		const onConfirm = vi.fn().mockResolvedValue(undefined);
		render(MergeEntityModal, {
			props: {
				entityType: 'brand',
				sourceEntity: { id: 'source-id', name: 'Source Brand', deviceCount: 3 },
				entities: [
					{ id: 'source-id', name: 'Source Brand' },
					{ id: 'target-id', name: 'Target Brand' }
				],
				isOpen: true,
				onConfirm,
				onCancel: vi.fn()
			}
		});

		await fireEvent.change(screen.getByLabelText(/merge into/i), {
			target: { value: 'target-id' }
		});
		await fireEvent.click(screen.getByRole('button', { name: /confirm/i }));

		expect(screen.getByText(/move 3 devices from Source Brand to Target Brand/i)).toBeInTheDocument();
		expect(onConfirm).toHaveBeenCalledWith('target-id');
	});

	it('supports selecting a merge target from a multi-select batch', async () => {
		const onConfirm = vi.fn().mockResolvedValue(undefined);
		render(MergeEntityModal, {
			props: {
				entityType: 'network',
				sourceEntities: [
					{ id: 'network-1', name: 'Office Wi-Fi', deviceCount: 2 },
					{ id: 'network-2', name: 'Main Wi-Fi', deviceCount: 1 },
					{ id: 'network-3', name: 'Guest Wi-Fi', deviceCount: 0 }
				],
				entities: [
					{ id: 'network-1', name: 'Office Wi-Fi' },
					{ id: 'network-2', name: 'Main Wi-Fi' },
					{ id: 'network-3', name: 'Guest Wi-Fi' }
				],
				isOpen: true,
				onConfirm,
				onCancel: vi.fn()
			}
		});

		await fireEvent.change(screen.getByLabelText(/merge into/i), {
			target: { value: 'network-2' }
		});
		await fireEvent.click(screen.getByRole('button', { name: /confirm/i }));

		expect(screen.getByRole('list', { name: /selected networks/i })).toBeInTheDocument();
		expect(screen.getByText(/merge 2 selected networks into main wi-fi/i)).toBeInTheDocument();
		expect(onConfirm).toHaveBeenCalledWith('network-2');
	});

	it('has no accessibility violations', async () => {
		const { container } = render(MergeEntityModal, {
			props: {
				entityType: 'location',
				sourceEntity: { id: 'source-id', name: 'Hallway', deviceCount: 1 },
				entities: [
					{ id: 'source-id', name: 'Hallway' },
					{ id: 'target-id', name: 'Office' }
				],
				isOpen: true,
				onConfirm: vi.fn().mockResolvedValue(undefined),
				onCancel: vi.fn()
			}
		});

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});
