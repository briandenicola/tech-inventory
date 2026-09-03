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


	describe('compact PWA variant', () => {
		// The default (roomy) variant stacks label above value inside a padded
		// card with a 21.25px row gap — right for a desktop page, but on a phone
		// it turns a dozen short values into a long, mostly-blank scroll. The
		// installed-PWA shell passes `compact` to switch to a flush inset list:
		// label left / value right on one line, hairline dividers, no grid.
		//
		// Tamper evidence, each mutation applied and reverted in isolation
		// against DeviceDetailFields.svelte (21 tests in the suite):
		//   · `{#if compact}` → `{#if false}`, so every caller got the roomy
		//     variant: "opts out of the roomy two-column grid", "lays
		//     single-value fields out as one-line label/value rows", and
		//     "separates rows with hairline dividers" all failed.
		//   · dropped `divide-y` from both compact <dl>s: "separates rows with
		//     hairline dividers" failed (1 failed | 20 passed).
		// Green again after restore.
		const compactProps = { ...props, compact: true };

		function rowFor(container: HTMLElement, label: string): HTMLElement | undefined {
			return Array.from(container.querySelectorAll<HTMLElement>('dl > div')).find(
				(div) => div.querySelector('dt')?.textContent?.trim() === label
			);
		}

		it('opts out of the roomy two-column grid entirely', () => {
			const { container } = render(DeviceDetailFields, { props: compactProps });

			for (const dl of Array.from(container.querySelectorAll('dl'))) {
				expect(dl.className).not.toContain('grid');
				expect(dl.className).not.toContain('sm:grid-cols-2');
				expect(dl.className).not.toContain('gap-y-5');
			}
		});

		it('lays single-value fields out as one-line label/value rows', () => {
			const { container } = render(DeviceDetailFields, { props: compactProps });

			for (const label of ['Brand', 'Category', 'Owner', 'Location']) {
				const row = rowFor(container, label);
				expect(row, `missing compact row for "${label}"`).toBeDefined();
				// Single line: label and value are flex siblings pushed apart,
				// not a stacked dt-over-dd pair.
				expect(row?.className).toContain('flex');
				expect(row?.className).toContain('justify-between');
				expect(row?.querySelector('dd')?.className).toContain('text-right');
			}
		});

		it('separates rows with hairline dividers instead of whitespace', () => {
			const { container } = render(DeviceDetailFields, { props: compactProps });

			const dls = Array.from(container.querySelectorAll('dl'));
			expect(dls.length).toBeGreaterThanOrEqual(2);
			for (const dl of dls) {
				expect(dl.className).toContain('divide-y');
			}

			// Row padding stays tight — py-2 (8.5px rendered at the 17px root),
			// not the roomy card's p-4/p-6.
			const brandRow = rowFor(container, 'Brand');
			expect(brandRow?.className).toContain('py-2');
			expect(brandRow?.className).not.toContain('py-4');
		});

		it('keeps long free-text fields stacked, since they cannot share a line', () => {
			const fullDevice = {
				...device,
				purpose: 'Runs the household media server',
				notes: 'Replaced fan in 2024'
			};
			const { container } = render(DeviceDetailFields, {
				props: { ...compactProps, device: fullDevice }
			});

			for (const label of ['Purpose', 'Notes']) {
				const row = rowFor(container, label);
				expect(row, `missing compact row for "${label}"`).toBeDefined();
				expect(row?.className).not.toContain('justify-between');
			}
		});

		it('renders the same fields, in the same order, as the roomy variant', () => {
			const fullDevice = {
				...device,
				operatingSystem: 'Windows 11',
				ipAddress: '192.168.1.42',
				purpose: 'Runs the household media server'
			};
			const roomy = render(DeviceDetailFields, {
				props: { ...props, device: fullDevice }
			});
			const roomyLabels = Array.from(roomy.container.querySelectorAll('dt')).map((el) =>
				el.textContent?.trim()
			);
			roomy.unmount();

			const compact = render(DeviceDetailFields, {
				props: { ...compactProps, device: fullDevice }
			});
			const compactLabels = Array.from(compact.container.querySelectorAll('dt')).map((el) =>
				el.textContent?.trim()
			);

			expect(compactLabels).toEqual(roomyLabels);
		});

		it('keeps <dl>/<dt>/<dd> semantics and the audit <time> elements', () => {
			const { container } = render(DeviceDetailFields, { props: compactProps });

			expect(container.querySelector('dl')).toBeInTheDocument();
			expect(container.querySelector('table')).not.toBeInTheDocument();
			expect(container.querySelectorAll('time').length).toBeGreaterThanOrEqual(2);
			expect(screen.getAllByText('by Brian')).toHaveLength(2);
		});

		it('has no accessibility violations', async () => {
			const { container } = render(DeviceDetailFields, { props: compactProps });

			expect(await axe(container)).toHaveNoViolations();
		});
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
