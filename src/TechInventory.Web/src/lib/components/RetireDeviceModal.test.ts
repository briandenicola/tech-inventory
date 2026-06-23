import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import RetireDeviceModal from './RetireDeviceModal.svelte';

describe('RetireDeviceModal', () => {
	it('renders with device name', () => {
		const onConfirm = vi.fn();
		const onCancel = vi.fn();

		render(RetireDeviceModal, {
			props: {
				deviceName: 'Test Device',
				onConfirm,
				onCancel
			}
		});

		expect(screen.getByText('Retire Device')).toBeInTheDocument();
		expect(screen.getByText('Test Device')).toBeInTheDocument();
		expect(screen.getByText(/mark this device as retired/i)).toBeInTheDocument();
	});

	it('calls onConfirm when retire button is clicked', async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn().mockResolvedValue(undefined);
		const onCancel = vi.fn();

		render(RetireDeviceModal, {
			props: {
				deviceName: 'Test Device',
				onConfirm,
				onCancel
			}
		});

		await user.click(screen.getByRole('button', { name: /retire/i }));

		await waitFor(() => {
			expect(onConfirm).toHaveBeenCalledOnce();
		});
	});

	it('calls onCancel when cancel button is clicked', async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		const onCancel = vi.fn();

		render(RetireDeviceModal, {
			props: {
				deviceName: 'Test Device',
				onConfirm,
				onCancel
			}
		});

		await user.click(screen.getByRole('button', { name: /cancel/i }));

		expect(onCancel).toHaveBeenCalledOnce();
		expect(onConfirm).not.toHaveBeenCalled();
	});

	it('calls onCancel when escape key is pressed', async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		const onCancel = vi.fn();

		render(RetireDeviceModal, {
			props: {
				deviceName: 'Test Device',
				onConfirm,
				onCancel
			}
		});

		await user.keyboard('{Escape}');

		expect(onCancel).toHaveBeenCalledOnce();
	});

	it('shows processing state when submitting', async () => {
		const user = userEvent.setup();
		let resolveConfirm: () => void;
		const onConfirm = vi.fn(() => new Promise<void>((resolve) => { resolveConfirm = resolve; }));
		const onCancel = vi.fn();

		render(RetireDeviceModal, {
			props: {
				deviceName: 'Test Device',
				onConfirm,
				onCancel
			}
		});

		await user.click(screen.getByRole('button', { name: /retire/i }));

		expect(screen.getByText(/processing/i)).toBeInTheDocument();

		resolveConfirm!();
		
		await waitFor(() => {
			expect(onConfirm).toHaveBeenCalledOnce();
		});
	});

	it('has no accessibility violations', async () => {
		const onConfirm = vi.fn();
		const onCancel = vi.fn();

		const { container } = render(RetireDeviceModal, {
			props: {
				deviceName: 'Test Device',
				onConfirm,
				onCancel
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});
});
