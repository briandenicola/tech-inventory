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

	describe('semantic definition-list structure', () => {
		it('renders a definition list, not a table', () => {
			const { container } = render(DeviceDetailFields, { props });

			expect(container.querySelector('dl')).toBeInTheDocument();
			expect(container.querySelector('table')).not.toBeInTheDocument();
		});

		it('renders field labels as <dt> and values as <dd>', () => {
			const { container } = render(DeviceDetailFields, { props });

			const dts = Array.from(container.querySelectorAll('dt')).map(
				(el) => el.textContent?.trim()
			);
			expect(dts).toContain('Brand');
			expect(dts).toContain('Location');
			expect(dts).toContain('Tags');

			expect(screen.getByText('Apple')).toBeInTheDocument();
			expect(screen.getByText('Home Office')).toBeInTheDocument();
		});

		it('renders tag chips under the Tags term', () => {
			const { container } = render(DeviceDetailFields, { props });

			const dts = Array.from(container.querySelectorAll('dt')).map(
				(el) => el.textContent?.trim()
			);
			expect(dts).toContain('Tags');
			expect(screen.getByText('Portable')).toBeInTheDocument();
		});
	});

	describe('responsive grid layout', () => {
		it('applies single-column base and two-column desktop classes to the main field list', () => {
			const { container } = render(DeviceDetailFields, { props });

			const mainDl = container.querySelector('dl');
			expect(mainDl?.className).toContain('grid-cols-1');
			expect(mainDl?.className).toContain('sm:grid-cols-2');
		});

		it('applies the same grid classes to the audit trail definition list', () => {
			const { container } = render(DeviceDetailFields, { props });

			const allDls = Array.from(container.querySelectorAll('dl'));
			// One for main fields, one for audit trail
			expect(allDls.length).toBeGreaterThanOrEqual(2);

			const auditDl = allDls[allDls.length - 1];
			expect(auditDl?.className).toContain('grid-cols-1');
			expect(auditDl?.className).toContain('sm:grid-cols-2');
		});

		it('multiline fields span the full grid width', () => {
			const fullDevice = {
				...device,
				purpose: 'Runs the household media server',
				notes: 'Replaced fan in 2024'
			};
			const { container } = render(DeviceDetailFields, {
				props: { ...props, device: fullDevice }
			});

			const fullWidthDivs = Array.from(container.querySelectorAll('dl > div')).filter((div) =>
				div.className.includes('col-span-full')
			);
			// purpose + notes + tags = at least 3 full-width cells
			expect(fullWidthDivs.length).toBeGreaterThanOrEqual(3);
		});

		it('tags always span the full grid width', () => {
			const { container } = render(DeviceDetailFields, { props });

			const tagsGroup = Array.from(container.querySelectorAll('dl > div')).find((div) =>
				div.querySelector('dt')?.textContent?.trim() === 'Tags'
			);
			expect(tagsGroup?.className).toContain('col-span-full');
		});
	});

	describe('audit trail consistency', () => {
		it('renders Created and Last Modified in the audit section', () => {
			const { container } = render(DeviceDetailFields, { props });

			const dts = Array.from(container.querySelectorAll('dt')).map(
				(el) => el.textContent?.trim()
			);
			expect(dts).toContain('Created');
			expect(dts).toContain('Last Modified');
		});

		it('renders audit actor attribution when provided', () => {
			render(DeviceDetailFields, { props });

			// "by Brian" appears for both created and modified
			expect(screen.getAllByText('by Brian')).toHaveLength(2);
		});

		it('renders audit timestamps in <time> elements', () => {
			const { container } = render(DeviceDetailFields, { props });

			const timeEls = container.querySelectorAll('time');
			expect(timeEls.length).toBeGreaterThanOrEqual(2);
		});
	});

	it('has no accessibility violations', async () => {
		const { container } = render(DeviceDetailFields, { props });

		expect(await axe(container)).toHaveNoViolations();
	});

	describe('F034 optional fields (C-08)', () => {
		it('elides operatingSystem/version/ipAddress/macAddress/productUrl/purpose/notes terms when unset', () => {
			// The base `props.device` factory leaves every F034 field null —
			// none of their labels should render as <dt> at all.
			const { container } = render(DeviceDetailFields, { props });

			const dtLabels = Array.from(container.querySelectorAll('dt')).map(
				(el) => el.textContent?.trim()
			);
			for (const label of [
				'Operating System',
				'Version',
				'IP Address',
				'MAC Address',
				'Product URL',
				'Purpose',
				'Notes'
			]) {
				expect(dtLabels).not.toContain(label);
			}
		});

		it('renders operatingSystem/version/ipAddress/macAddress/productUrl/purpose/notes terms when set', () => {
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

			const { container } = render(DeviceDetailFields, { props: { ...props, device: fullDevice } });

			const dtLabels = Array.from(container.querySelectorAll('dt')).map(
				(el) => el.textContent?.trim()
			);
			expect(dtLabels).toContain('Operating System');
			expect(screen.getByText('Windows 11')).toBeInTheDocument();

			expect(dtLabels).toContain('Version');
			expect(screen.getByText('23H2')).toBeInTheDocument();

			expect(dtLabels).toContain('IP Address');
			expect(screen.getByText('192.168.1.42')).toBeInTheDocument();

			expect(dtLabels).toContain('MAC Address');
			expect(screen.getByText('AA:BB:CC:DD:EE:FF')).toBeInTheDocument();

			expect(dtLabels).toContain('Product URL');
			const productLink = screen.getByRole('link', { name: 'https://example.com/product' });
			expect(productLink).toHaveAttribute('href', 'https://example.com/product');
			expect(productLink).toHaveAttribute('target', '_blank');

			expect(dtLabels).toContain('Purpose');
			expect(screen.getByText('Runs the household media server')).toBeInTheDocument();

			expect(dtLabels).toContain('Notes');
			expect(screen.getByText('Replaced fan in 2024')).toBeInTheDocument();
		});

		it('always renders the Model term, falling back to an em dash when unset', () => {
			const withoutModel = { ...device, model: null };

			const { container } = render(DeviceDetailFields, { props: { ...props, device: withoutModel } });

			const dtLabels = Array.from(container.querySelectorAll('dt')).map(
				(el) => el.textContent?.trim()
			);
			expect(dtLabels).toContain('Model');
			expect(screen.getByText('—')).toBeInTheDocument();
		});
	});
});
