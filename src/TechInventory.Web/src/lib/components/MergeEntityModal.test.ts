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
