/**
 * DevicePwaRow component tests — F045 §5.4 / Drake D-F.
 *
 * Covers: line-1/line-2 content and order, brand/model/status degradation
 * when data is missing, the reused DeviceActionsMenu ellipsis (present only
 * when the current user has at least one permitted action), row selection
 * checkbox, the open-device callback vs. default navigation fallback, and
 * axe cleanliness.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import DevicePwaRow from './DevicePwaRow.svelte';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';
import type { CurrentUser } from '$lib/stores/auth';

function makeUser(role: CurrentUser['role']): CurrentUser {
	return { id: 'user-1', entraObjectId: null, displayName: 'Test User', role };
}

describe('DevicePwaRow', () => {
	beforeEach(() => {
		resetFactories();
	});

	it('renders the device name on line one and brand/model + status on line two, in that order', () => {
		const device = createDeviceResponse({ name: 'Living Room TV', model: 'OLED77', status: 'Active' });
		render(DevicePwaRow, { props: { device, currentUser: null } });

		const title = screen.getByText('Living Room TV');
		const lineTwo = screen.getByText(/OLED77/);
		const status = screen.getByText('Active');

		expect(title.compareDocumentPosition(lineTwo) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
		expect(lineTwo.compareDocumentPosition(status) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
	});

	it('falls back to an em dash for the title when the device has no name', () => {
		const device = createDeviceResponse({ name: null });
		render(DevicePwaRow, { props: { device, currentUser: null } });

		expect(screen.getByText('—')).toBeInTheDocument();
	});

	it('shows only the model when brand cannot be resolved (no reference data loaded)', () => {
		const device = createDeviceResponse({ model: 'SoloModel' });
		render(DevicePwaRow, { props: { device, currentUser: null } });

		// referenceDataStore is empty in tests, so brand always resolves to
		// '—' and is omitted rather than rendered as "— · SoloModel".
		expect(screen.getByText('SoloModel')).toBeInTheDocument();
		expect(screen.queryByText(/— · /)).not.toBeInTheDocument();
	});

	it('shows an em dash on line two when both brand and model are unavailable', () => {
		const device = createDeviceResponse({ model: null });
		render(DevicePwaRow, { props: { device, currentUser: null } });

		const dashes = screen.getAllByText('—');
		expect(dashes.length).toBeGreaterThan(0);
	});

	it('does not render the actions ellipsis when the current user has no permitted actions', () => {
		const device = createDeviceResponse();
		render(DevicePwaRow, { props: { device, currentUser: null } });

		expect(screen.queryByRole('button', { name: /more actions/i })).not.toBeInTheDocument();
	});

	it('reuses DeviceActionsMenu and reveals it for an Admin user', () => {
		const device = createDeviceResponse();
		render(DevicePwaRow, { props: { device, currentUser: makeUser('Admin') } });

		expect(screen.getByRole('button', { name: /more actions/i })).toBeInTheDocument();
	});

	it('includes a Clone Device link pointing at /devices/new?cloneFrom=<id> for an Admin user (#131)', async () => {
		const user = userEvent.setup();
		const device = createDeviceResponse();
		render(DevicePwaRow, { props: { device, currentUser: makeUser('Admin') } });

		await user.click(screen.getByRole('button', { name: /more actions/i }));

		expect(screen.getByRole('menuitem', { name: 'Clone Device' })).toHaveAttribute(
			'href',
			`/devices/new?cloneFrom=${device.id}`
		);
	});

	it('does not nest an interactive checkbox/actions-menu inside the row body (no nested interactive elements)', () => {
		const device = createDeviceResponse();
		const { container } = render(DevicePwaRow, {
			props: { device, currentUser: makeUser('Admin'), selectable: true, onOpenDevice: vi.fn() }
		});

		const titleButton = container.querySelector('button.flex.min-w-0');
		expect(titleButton?.querySelector('input')).toBeNull();
		expect(titleButton?.querySelector('button')).toBeNull();
	});

	it('renders a real <a href> anchor (not a button faking navigation) when no onOpenDevice callback is supplied', () => {
		const device = createDeviceResponse({ name: 'Linkable Device' });
		const { container } = render(DevicePwaRow, { props: { device, currentUser: null } });

		// Preserves middle-click/ctrl-click "open in new tab", right-click
		// "copy link address", and native <a> semantics for assistive tech —
		// a button + window.location.href fallback loses all of these.
		const link = screen.getByRole('link', { name: /linkable device/i });
		expect(link).toHaveAttribute('href', `/devices/${device.id}`);
		expect(container.querySelector('button.flex.min-w-0')).toBeNull();
	});

	it('renders a selection checkbox when selectable, wired to onToggleSelect without opening the device', async () => {
		const user = userEvent.setup();
		const device = createDeviceResponse({ name: 'Selectable Device' });
		const onToggleSelect = vi.fn();
		const onOpenDevice = vi.fn();

		render(DevicePwaRow, {
			props: { device, currentUser: null, selectable: true, onToggleSelect, onOpenDevice }
		});

		const checkbox = screen.getByRole('checkbox', { name: /selectable device/i });
		await user.click(checkbox);

		expect(onToggleSelect).toHaveBeenCalledWith(device.id);
		expect(onOpenDevice).not.toHaveBeenCalled();
	});

	it('calls onOpenDevice with the device id when the row is clicked', async () => {
		const user = userEvent.setup();
		const device = createDeviceResponse({ name: 'Clickable Device' });
		const onOpenDevice = vi.fn();

		render(DevicePwaRow, { props: { device, currentUser: null, onOpenDevice } });

		await user.click(screen.getByText('Clickable Device'));
		expect(onOpenDevice).toHaveBeenCalledWith(device.id);
	});

	it('has no accessibility violations', async () => {
		const device = createDeviceResponse();
		const { container } = render(DevicePwaRow, { props: { device, currentUser: makeUser('Admin') } });

		expect(await axe(container)).toHaveNoViolations();
	});
});
