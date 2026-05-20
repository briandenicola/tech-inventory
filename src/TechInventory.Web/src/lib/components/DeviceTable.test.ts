/**
 * DeviceTable component tests
 *
 * Per T18: Test table rendering, sortable columns (D-054 2-state), column order (D-038), accessibility.
 * Constitution §3.4: axe-core with zero violations.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, within } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import DeviceTable from './DeviceTable.svelte';
import DeviceTableHarness from './DeviceTableHarness.svelte';
import { createDeviceList, resetFactories } from '$lib/test-utils/factories';

describe('DeviceTable', () => {
	beforeEach(() => {
		resetFactories();
	});

	const defaultProps = {
		devices: createDeviceList(3),
		currentSort: undefined as 'name' | 'purchaseDate' | 'createdAt' | undefined,
		sortDir: 'asc' as 'asc' | 'desc',
		onSort: vi.fn(),
		onOpenDevice: undefined as ((deviceId: string) => void) | undefined
	};

	describe('table structure', () => {
		it('renders semantic table with caption', () => {
			render(DeviceTable, { props: defaultProps });

			const table = screen.getByRole('table');
			expect(table).toBeInTheDocument();

			// Should have sr-only caption per accessibility
			const caption = table.querySelector('caption');
			expect(caption).toBeInTheDocument();
			expect(caption).toHaveClass('sr-only');
		});

		it('renders devices data in rows', () => {
			const devices = createDeviceList(2);
			const props = { ...defaultProps, devices };
			render(DeviceTable, { props });

			// Check that device names are rendered (may appear in both desktop table and mobile cards)
			const name1Elements = screen.getAllByText(devices[0].name!);
			const name2Elements = screen.getAllByText(devices[1].name!);

			expect(name1Elements.length).toBeGreaterThan(0);
			expect(name2Elements.length).toBeGreaterThan(0);
		});

		it('renders empty tbody when no devices', () => {
			render(DeviceTable, { props: { ...defaultProps, devices: [] } });

			const table = screen.getByRole('table');
			const tbody = table.querySelector('tbody');
			expect(tbody).toBeInTheDocument();
			expect(tbody?.children.length).toBe(0);
		});
	});

	describe('column order (D-038)', () => {
		it('renders columns in correct order: Name, Brand, Category, Owner, Status, Purchase Date', () => {
			render(DeviceTable, { props: defaultProps });

			const headers = screen.getAllByRole('columnheader');
			// Extract text content from headers
			const headerTexts = headers.map((h) => h.textContent?.trim());

			// Should match D-038 order (sorting indicators will be in the text)
			expect(headerTexts[0]).toContain('Name');
			expect(headerTexts[1]).toBe('Brand');
			expect(headerTexts[2]).toBe('Category');
			expect(headerTexts[3]).toBe('Owner');
			expect(headerTexts[4]).toBe('Status');
			expect(headerTexts[5]).toContain('Purchase Date');
		});
	});

	describe('sorting (D-054 2-state cycle)', () => {
		it('renders sortable columns with buttons', () => {
			render(DeviceTable, { props: defaultProps });

			// Name, Purchase Date, Created At should be sortable (buttons)
			const nameButton = screen.getByRole('button', { name: /Name/i });
			expect(nameButton).toBeInTheDocument();

			const purchaseDateButton = screen.getByRole('button', { name: /Purchase Date/i });
			expect(purchaseDateButton).toBeInTheDocument();
		});

		it('sets aria-sort="none" on unsorted columns', () => {
			render(DeviceTable, { props: defaultProps });

			const headers = screen.getAllByRole('columnheader');
			const nameHeader = headers[0]; // Name column

			expect(nameHeader).toHaveAttribute('aria-sort', 'none');
		});

		it('sets aria-sort="ascending" on sorted asc column', () => {
			render(DeviceTable, {
				props: { ...defaultProps, currentSort: 'name', sortDir: 'asc' }
			});

			const headers = screen.getAllByRole('columnheader');
			const nameHeader = headers[0];

			expect(nameHeader).toHaveAttribute('aria-sort', 'ascending');
		});

		it('sets aria-sort="descending" on sorted desc column', () => {
			render(DeviceTable, {
				props: { ...defaultProps, currentSort: 'name', sortDir: 'desc' }
			});

			const headers = screen.getAllByRole('columnheader');
			const nameHeader = headers[0];

			expect(nameHeader).toHaveAttribute('aria-sort', 'descending');
		});

		it('calls onSort when clicking sortable column', async () => {
			const user = userEvent.setup();
			const onSort = vi.fn();

			render(DeviceTable, { props: { ...defaultProps, onSort } });

			const nameButton = screen.getByRole('button', { name: /Name/i });
			await user.click(nameButton);

			expect(onSort).toHaveBeenCalledWith('name');
		});

		it('calls onSort with correct column when clicking Purchase Date', async () => {
			const user = userEvent.setup();
			const onSort = vi.fn();

			render(DeviceTable, { props: { ...defaultProps, onSort } });

			const purchaseDateButton = screen.getByRole('button', { name: /Purchase Date/i });
			await user.click(purchaseDateButton);

			expect(onSort).toHaveBeenCalledWith('purchaseDate');
		});
	});

	describe('date formatting', () => {
		it('formats purchase date with Intl.DateTimeFormat', () => {
			const devices = createDeviceList(1);
			devices[0].purchaseDate = '2024-01-15';
			devices[0].name = 'Unique Test Device Name';

			const { container } = render(DeviceTable, { props: { ...defaultProps, devices } });

			// Should format the date (not show raw ISO string or em-dash)
			// Check that "2024-01-15" is NOT in the output (should be formatted)
			expect(container.textContent).not.toContain('2024-01-15');
			// And should contain a year
			expect(container.textContent).toContain('2024');
		});

		it('renders em-dash for null purchase date', () => {
			const devices = createDeviceList(1);
			devices[0].purchaseDate = null;

			render(DeviceTable, { props: { ...defaultProps, devices } });

			// Should render "—" (em-dash) - may appear multiple times for other null fields
			const emDashes = screen.getAllByText('—');
			expect(emDashes.length).toBeGreaterThan(0);
		});
	});

	describe('modal navigation hook', () => {
		it('uses the open-device callback for mobile cards when provided', async () => {
			const user = userEvent.setup();
			const onOpenDevice = vi.fn();
			const devices = createDeviceList(1);

			render(DeviceTable, {
				props: {
					...defaultProps,
					devices,
					onOpenDevice
				}
			});

			const row = screen.getAllByText(devices[0].name!)[0]?.closest('tr');
			expect(row).not.toBeNull();

			await user.click(row!);

			expect(onOpenDevice).toHaveBeenCalledWith(devices[0].id);
		});
	});

	describe('responsive mobile cards', () => {
		it('renders stacked cards for mobile and keeps the desktop table wrapper', () => {
			const { container } = render(DeviceTableHarness);

			const mobileList = screen.getByRole('list', { name: 'Devices' });
			const table = screen.getByRole('table');
			const tableWrapper = container.querySelector('.hidden.md\\:block');

			expect(mobileList).toBeInTheDocument();
			expect(mobileList).toHaveClass('md:hidden');
			expect(mobileList).not.toHaveClass('hidden');
			expect(tableWrapper).not.toBeNull();
			expect(tableWrapper).toContainElement(table);
			expect(screen.getByRole('heading', { name: 'Kitchen Hub' })).toBeInTheDocument();
			expect(container.querySelector('.md\\:hidden dl')).not.toBeNull();
		});

		it('renders device details as definition-list pairs inside the mobile card', () => {
			render(DeviceTableHarness);

			const mobileList = screen.getByRole('list', { name: 'Devices' });
			const definitionList = mobileList.querySelector('dl');
			expect(definitionList).not.toBeNull();
			const scoped = within(definitionList as HTMLElement);

			expect(scoped.getByText('Brand')).toBeInTheDocument();
			expect(scoped.getByText('Google')).toBeInTheDocument();
			expect(scoped.getByText('Category')).toBeInTheDocument();
			expect(scoped.getByText('Smart Display')).toBeInTheDocument();
			expect(scoped.getByText('Owner')).toBeInTheDocument();
			expect(scoped.getByText('Brian')).toBeInTheDocument();
			expect(scoped.getByText('Status')).toBeInTheDocument();
			expect(scoped.getByText('Active')).toBeInTheDocument();
			expect(scoped.getByText('Purchase Date')).toBeInTheDocument();
		});

		it('keeps mobile and desktop checkboxes in sync through the shared row selection state', async () => {
			const user = userEvent.setup();
			render(DeviceTableHarness);

			const mobileList = screen.getByRole('list', { name: 'Devices' });
			await user.click(within(mobileList).getByRole('checkbox', { name: 'Select Kitchen Hub' }));

			const rowCheckboxes = screen.getAllByRole('checkbox', { name: 'Select Kitchen Hub' });
			rowCheckboxes.forEach((checkbox) => {
				expect(checkbox).toBeChecked();
			});
		});
	});

	describe('accessibility', () => {
		it('has no accessibility violations', async () => {
			const { container } = render(DeviceTableHarness);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has scope="col" on all column headers', () => {
			render(DeviceTable, { props: defaultProps });

			const headers = screen.getAllByRole('columnheader');
			headers.forEach((header) => {
				expect(header).toHaveAttribute('scope', 'col');
			});
		});
	});
});
