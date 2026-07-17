import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import DeviceActionsMenu from './DeviceActionsMenu.svelte';

describe('DeviceActionsMenu', () => {
	it('reveals actions from the overflow trigger', async () => {
		const user = userEvent.setup();
		const onClaim = vi.fn();

		render(DeviceActionsMenu, {
			props: {
				editHref: '/devices/device-1/edit',
				onClaim
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		expect(screen.getAllByText('Edit').length).toBeGreaterThan(0);
		expect(screen.getAllByText('Claim Ownership').length).toBeGreaterThan(0);
	});

	it('invokes action callbacks', async () => {
		const user = userEvent.setup();
		const onDelete = vi.fn();

		render(DeviceActionsMenu, {
			props: {
				onDelete
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));
		await user.click(screen.getAllByRole('menuitem', { name: 'Delete' })[0]!);

		expect(onDelete).toHaveBeenCalledOnce();
	});

	it('shows retire action when provided', async () => {
		const user = userEvent.setup();
		const onRetire = vi.fn();

		render(DeviceActionsMenu, {
			props: {
				onRetire
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));
		
		expect(screen.getByText('Retire Device')).toBeInTheDocument();

		await user.click(screen.getByRole('menuitem', { name: 'Retire Device' }));
		expect(onRetire).toHaveBeenCalledOnce();
	});

	it('shows unretire action when provided', async () => {
		const user = userEvent.setup();
		const onUnretire = vi.fn();

		render(DeviceActionsMenu, {
			props: {
				onUnretire
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		expect(screen.getByText('Unretire Device')).toBeInTheDocument();

		await user.click(screen.getByRole('menuitem', { name: 'Unretire Device' }));
		expect(onUnretire).toHaveBeenCalledOnce();
	});

	it('closes on escape', async () => {
		const user = userEvent.setup();

		render(DeviceActionsMenu, {
			props: {
				onViewHistory: vi.fn()
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));
		expect(screen.getAllByText('View change history').length).toBeGreaterThan(0);

		await user.keyboard('{Escape}');

		expect(screen.queryByText('Device actions')).not.toBeInTheDocument();
	});

	it('has no accessibility violations when expanded', async () => {
		const user = userEvent.setup();
		const { container } = render(DeviceActionsMenu, {
			props: {
				editHref: '/devices/device-1/edit',
				onRelease: vi.fn(),
				onRetire: vi.fn(),
				onUnretire: vi.fn(),
				onDelete: vi.fn()
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		expect(await axe(container)).toHaveNoViolations();
	});
});
