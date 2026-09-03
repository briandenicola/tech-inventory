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

	it('renders the Edit link with the given editHref target', async () => {
		const user = userEvent.setup();

		render(DeviceActionsMenu, {
			props: {
				editHref: '/devices/device-1/edit'
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		expect(screen.getByRole('menuitem', { name: 'Edit' })).toHaveAttribute(
			'href',
			'/devices/device-1/edit'
		);
	});

	it('invokes onRelease when the Release action is clicked', async () => {
		const user = userEvent.setup();
		const onRelease = vi.fn();

		render(DeviceActionsMenu, {
			props: {
				onRelease
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));
		await user.click(screen.getByRole('menuitem', { name: 'Release Ownership' }));

		expect(onRelease).toHaveBeenCalledOnce();
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

	it('shows the Change Status action when provided (#127 — replaces the old Active-only Retire / Retired-only Unretire pair)', async () => {
		const user = userEvent.setup();
		const onChangeStatus = vi.fn();

		render(DeviceActionsMenu, {
			props: {
				onChangeStatus
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		expect(screen.getByText('Change Status')).toBeInTheDocument();

		await user.click(screen.getByRole('menuitem', { name: 'Change Status' }));
		expect(onChangeStatus).toHaveBeenCalledOnce();
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
				onChangeStatus: vi.fn(),
				onDelete: vi.fn()
			}
		});

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		expect(await axe(container)).toHaveNoViolations();
	});
});
