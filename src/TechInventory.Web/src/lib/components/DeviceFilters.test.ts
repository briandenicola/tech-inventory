import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await import('svelte/store');
	return {
		referenceDataStore: writable({
			brands: [],
			categories: [],
			owners: [],
			locations: [],
			networks: [],
			tags: [],
			isLoading: false,
			error: null
		}),
		fetchReferenceData: vi.fn(async () => {})
	};
});

import DeviceFilters from './DeviceFilters.svelte';

const defaultFilters = {
	page: 1,
	pageSize: 25
};

describe('DeviceFilters', () => {
	beforeEach(() => {
		document.body.style.overflow = '';
	});

	it('renders an accessible mobile drawer with a sticky header and footer when open', () => {
		const { container } = render(DeviceFilters, {
			props: {
				filters: defaultFilters,
				onFiltersChange: vi.fn(),
				isOpen: true,
				onClose: vi.fn()
			}
		});

		const dialog = screen.getByRole('dialog', { name: 'Filters' });
		expect(dialog).toHaveClass('h-dvh');
		expect(dialog).toHaveAttribute('aria-modal', 'true');
		expect(container.querySelector('div.sticky.top-0')).toBeInTheDocument();
		expect(container.querySelector('div.sticky.bottom-0')).toBeInTheDocument();
		expect(container.querySelector('div.flex-1.overflow-y-auto')).toBeInTheDocument();
		expect(container.innerHTML).toContain('safe-area-inset-top');
		expect(container.innerHTML).toContain('safe-area-inset-bottom');
	});

	it('focuses the close button and closes on Escape', async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();

		render(DeviceFilters, {
			props: {
				filters: defaultFilters,
				onFiltersChange: vi.fn(),
				isOpen: true,
				onClose
			}
		});

		const closeButton = screen.getByRole('button', { name: 'Close Filters' });
		expect(closeButton).toHaveFocus();

		await user.keyboard('{Escape}');

		expect(onClose).toHaveBeenCalledOnce();
	});

	it('has no accessibility violations when open', async () => {
		const { container } = render(DeviceFilters, {
			props: {
				filters: defaultFilters,
				onFiltersChange: vi.fn(),
				isOpen: true,
				onClose: vi.fn()
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});

	describe('groupBy select (F045 B2)', () => {
		it('selects "None" by default when no groupBy is set and no implicit default is active', () => {
			render(DeviceFilters, {
				props: { filters: defaultFilters, onFiltersChange: vi.fn() }
			});

			expect(screen.getByLabelText('Group by')).toHaveValue('none');
		});

		it('sends the explicit "none" sentinel — not undefined — when the user picks None', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			render(DeviceFilters, {
				props: { filters: { ...defaultFilters, groupBy: 'category' }, onFiltersChange }
			});

			await user.selectOptions(screen.getByLabelText('Group by'), 'None');

			expect(onFiltersChange).toHaveBeenCalledWith(
				expect.objectContaining({ groupBy: 'none', page: 1 })
			);
		});

		it('sends the explicit dimension when the user picks a grouping option', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			render(DeviceFilters, {
				props: { filters: defaultFilters, onFiltersChange }
			});

			await user.selectOptions(screen.getByLabelText('Group by'), 'Owner');

			expect(onFiltersChange).toHaveBeenCalledWith(
				expect.objectContaining({ groupBy: 'owner', page: 1 })
			);
		});

		it('shows Category selected and renders the implicit-default note when implicitGroupingActive is true (dead i18n key follow-up)', () => {
			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					implicitGroupingActive: true
				}
			});

			expect(screen.getByLabelText('Group by')).toHaveValue('category');
			expect(screen.getByText('Grouped by category')).toBeInTheDocument();
		});

		it('does not render the implicit-default note when a groupBy value is already explicit', () => {
			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' },
					onFiltersChange: vi.fn(),
					implicitGroupingActive: false
				}
			});

			expect(screen.queryByText('Grouped by category')).not.toBeInTheDocument();
		});
	});

	describe('Clear All (C-06)', () => {
		it('resets every filter dimension but preserves page size and groupBy', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			const dirtyFilters = {
				page: 3,
				pageSize: 50,
				search: 'iPhone',
				brandId: 'brand-1',
				categoryId: 'category-1',
				ownerId: 'owner-1',
				locationId: 'location-1',
				networkId: 'network-1',
				status: ['Active' as const],
				purchaseYearMin: 2020,
				purchaseYearMax: 2023,
				groupBy: 'owner' as const
			};

			render(DeviceFilters, {
				props: { filters: dirtyFilters, onFiltersChange, isOpen: true, onClose: vi.fn() }
			});

			await user.click(screen.getByRole('button', { name: 'Clear All Filters' }));

			// pageSize and groupBy (F045 B2 user choice) survive Clear All;
			// every other filter dimension — including status — comes back
			// undefined. Whether "undefined status" ends up as the F026
			// STATUS_ALL_SENTINEL ('all') in the URL is buildDevicesUrlParams's
			// concern (see deviceFilterUrl.test.ts), not this component's.
			expect(onFiltersChange).toHaveBeenCalledWith({
				page: 1,
				pageSize: 50,
				groupBy: 'owner'
			});
		});

		it('falls back to a pageSize of 25 when the current filters have none', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();

			render(DeviceFilters, {
				props: {
					filters: { page: 2, search: 'router' },
					onFiltersChange,
					isOpen: true,
					onClose: vi.fn()
				}
			});

			await user.click(screen.getByRole('button', { name: 'Clear All Filters' }));

			expect(onFiltersChange).toHaveBeenCalledWith({
				page: 1,
				pageSize: 25,
				groupBy: undefined
			});
		});
	});
});
