/**
 * DeviceTable component tests
 * 
 * Per T18: Test table rendering, sortable columns (D-054 2-state), column order (D-038), accessibility.
 * Constitution §3.4: axe-core with zero violations.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import DeviceTable from './DeviceTable.svelte';
import { createDeviceList, resetFactories } from '$lib/test-utils/factories';

describe('DeviceTable', () => {
	beforeEach(() => {
		resetFactories();
	});

	const defaultProps = {
		devices: createDeviceList(3),
		currentSort: undefined as 'name' | 'purchaseDate' | 'createdAt' | undefined,
		sortDir: 'asc' as 'asc' | 'desc',
		onSort: vi.fn()
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
			const headerTexts = headers.map(h => h.textContent?.trim());
			
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

	describe('accessibility', () => {
		it('has no accessibility violations', async () => {
			const { container } = render(DeviceTable, { props: defaultProps });
			
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has scope="col" on all column headers', () => {
			render(DeviceTable, { props: defaultProps });
			
			const headers = screen.getAllByRole('columnheader');
			headers.forEach(header => {
				expect(header).toHaveAttribute('scope', 'col');
			});
		});
	});
});
