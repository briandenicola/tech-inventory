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
});
