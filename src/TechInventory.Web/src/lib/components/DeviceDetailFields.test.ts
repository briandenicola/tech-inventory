import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import DeviceDetailFields from './DeviceDetailFields.svelte';
import { createDeviceResponse } from '$lib/test-utils/factories';

describe('DeviceDetailFields', () => {
	const device = {
		...createDeviceResponse(),
		name: 'MacBook Pro',
		serialNumber: 'ABC123',
		model: '14-inch',
		purchaseDate: '2023-06-15',
		purchasePrice: 1999,
		currencyCode: 'USD',
		createdAt: '2024-01-01T10:00:00Z',
		modifiedAt: '2024-02-01T10:00:00Z',
		createdBy: 'Brian',
		modifiedBy: 'Brian'
	};

	const props = {
		device,
		brandName: 'Apple',
		categoryName: 'Laptop',
		ownerName: 'Brian',
		locationName: 'Home Office',
		networkName: 'Wi-Fi',
		deviceTags: [{ id: 'tag-1', name: 'Portable', color: '#3366ff' }],
		statusClass: 'bg-success-100 text-success-800',
		formatDate: (value: string | null) => value ?? '—',
		formatDateTime: (value: string | null) => value ?? '—'
	};

	it('renders details in a horizontal table layout', () => {
		render(DeviceDetailFields, { props });

		const tables = screen.getAllByRole('table');
		expect(tables.length).toBeGreaterThan(0);
		expect(screen.getByRole('rowheader', { name: 'Brand' })).toBeInTheDocument();
		expect(screen.getByText('Apple')).toBeInTheDocument();
		expect(screen.getByRole('rowheader', { name: 'Location' })).toBeInTheDocument();
		expect(screen.getByText('Home Office')).toBeInTheDocument();
	});

	it('renders tag chips in the value column', () => {
		render(DeviceDetailFields, { props });

		expect(screen.getByText('Portable')).toBeInTheDocument();
		expect(screen.getByRole('rowheader', { name: 'Tags' })).toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(DeviceDetailFields, { props });

		expect(await axe(container)).toHaveNoViolations();
	});

	describe('F034 optional fields (C-08)', () => {
		it('elides operatingSystem/version/ipAddress/macAddress/productUrl/purpose/notes rows when unset', () => {
			// The base `props.device` factory leaves every F034 field null —
			// none of their rows/labels should render at all.
			render(DeviceDetailFields, { props });

			for (const label of [
				'Operating System',
				'Version',
				'IP Address',
				'MAC Address',
				'Product URL',
				'Purpose',
				'Notes'
			]) {
				expect(screen.queryByRole('rowheader', { name: label })).not.toBeInTheDocument();
			}
		});

		it('renders operatingSystem/version/ipAddress/macAddress/productUrl/purpose/notes rows when set', () => {
			const fullDevice = {
				...device,
				operatingSystem: 'Windows 11',
				version: '23H2',
				ipAddress: '192.168.1.42',
				macAddress: 'AA:BB:CC:DD:EE:FF',
				productUrl: 'https://example.com/product',
				purpose: 'Runs the household media server',
				notes: 'Replaced fan in 2024'
			};

			render(DeviceDetailFields, { props: { ...props, device: fullDevice } });

			expect(screen.getByRole('rowheader', { name: 'Operating System' })).toBeInTheDocument();
			expect(screen.getByText('Windows 11')).toBeInTheDocument();

			expect(screen.getByRole('rowheader', { name: 'Version' })).toBeInTheDocument();
			expect(screen.getByText('23H2')).toBeInTheDocument();

			expect(screen.getByRole('rowheader', { name: 'IP Address' })).toBeInTheDocument();
			expect(screen.getByText('192.168.1.42')).toBeInTheDocument();

			expect(screen.getByRole('rowheader', { name: 'MAC Address' })).toBeInTheDocument();
			expect(screen.getByText('AA:BB:CC:DD:EE:FF')).toBeInTheDocument();

			expect(screen.getByRole('rowheader', { name: 'Product URL' })).toBeInTheDocument();
			const productLink = screen.getByRole('link', { name: 'https://example.com/product' });
			expect(productLink).toHaveAttribute('href', 'https://example.com/product');
			expect(productLink).toHaveAttribute('target', '_blank');

			expect(screen.getByRole('rowheader', { name: 'Purpose' })).toBeInTheDocument();
			expect(screen.getByText('Runs the household media server')).toBeInTheDocument();

			expect(screen.getByRole('rowheader', { name: 'Notes' })).toBeInTheDocument();
			expect(screen.getByText('Replaced fan in 2024')).toBeInTheDocument();
		});

		it('always renders the Model row, falling back to an em dash when unset', () => {
			const withoutModel = { ...device, model: null };

			render(DeviceDetailFields, { props: { ...props, device: withoutModel } });

			expect(screen.getByRole('rowheader', { name: 'Model' })).toBeInTheDocument();
			expect(screen.getByText('—')).toBeInTheDocument();
		});
	});
});
