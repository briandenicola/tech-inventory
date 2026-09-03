import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { tick } from 'svelte';
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

		it('stages the explicit "none" sentinel — not undefined — as a pending edit until Apply is pressed', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' },
					onFiltersChange,
					isOpen: true,
					onClose: vi.fn()
				}
			});

			await user.selectOptions(screen.getByLabelText('Group by'), 'None');
			expect(onFiltersChange).not.toHaveBeenCalled();

			await user.click(screen.getByRole('button', { name: 'Apply' }));

			expect(onFiltersChange).toHaveBeenCalledWith(
				expect.objectContaining({ groupBy: 'none', page: 1 })
			);
		});

		it('stages the explicit dimension as a pending edit and only applies it once Apply is pressed', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			render(DeviceFilters, {
				props: { filters: defaultFilters, onFiltersChange, isOpen: true, onClose: vi.fn() }
			});

			await user.selectOptions(screen.getByLabelText('Group by'), 'Owner');
			expect(onFiltersChange).not.toHaveBeenCalled();

			await user.click(screen.getByRole('button', { name: 'Apply' }));

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

	describe('#128 pending/apply model', () => {
		it('does not call onFiltersChange, update rows, or move the applied badge while edits are only pending', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			render(DeviceFilters, {
				props: { filters: defaultFilters, onFiltersChange, isOpen: true, onClose: vi.fn() }
			});

			await user.click(screen.getByLabelText('Active'));
			await user.type(screen.getByLabelText('From Year'), '2020');

			expect(onFiltersChange).not.toHaveBeenCalled();
		});

		it('shows a visually distinct pending-changes banner only once an edit has been made', async () => {
			const user = userEvent.setup();
			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn()
				}
			});

			expect(
				screen.queryByText('You have unapplied filter changes. Press Apply to update the results.')
			).not.toBeInTheDocument();

			await user.click(screen.getByLabelText('Active'));

			expect(
				screen.getByText('You have unapplied filter changes. Press Apply to update the results.')
			).toBeInTheDocument();
		});

		it('sends the full pending payload — including multiple staged edits — only when Apply is pressed', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			render(DeviceFilters, {
				props: { filters: defaultFilters, onFiltersChange, isOpen: true, onClose: vi.fn() }
			});

			await user.click(screen.getByLabelText('Active'));
			await user.click(screen.getByLabelText('Lent'));
			await user.type(screen.getByLabelText('From Year'), '2019');

			expect(onFiltersChange).not.toHaveBeenCalled();

			await user.click(screen.getByRole('button', { name: 'Apply' }));

			expect(onFiltersChange).toHaveBeenCalledTimes(1);
			expect(onFiltersChange).toHaveBeenCalledWith(
				expect.objectContaining({
					status: expect.arrayContaining(['Active', 'Lent']),
					purchaseYearMin: 2019,
					page: 1
				})
			);
		});

		it('re-baselines pending edits from the applied filters whenever the panel is reopened', async () => {
			const onFiltersChange = vi.fn();
			const { rerender } = render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange,
					isOpen: false,
					onClose: vi.fn()
				}
			});

			// Re-open with a different applied filter set (e.g. Apply happened
			// while the panel was closed via some other path, or the URL
			// changed underneath it) — the panel must reflect that, not a
			// stale pending edit from before it closed. Uses purchaseYearMin
			// (a plain number input) rather than a reference-data select,
			// since the mocked referenceDataStore has no brand/category
			// options for a <select> to resolve against.
			await rerender({
				filters: { ...defaultFilters, purchaseYearMin: 2019 },
				onFiltersChange,
				isOpen: true,
				onClose: vi.fn()
			});
			await tick();

			expect(screen.getByLabelText('From Year')).toHaveValue(2019);
		});

		it('reachable via keyboard and has zero axe violations while a pending change is staged', async () => {
			const user = userEvent.setup();
			const { container } = render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn()
				}
			});

			await user.click(screen.getByLabelText('Active'));

			const applyButton = screen.getByRole('button', { name: 'Apply' });
			applyButton.focus();
			expect(applyButton).toHaveFocus();

			expect(await axe(container)).toHaveNoViolations();
		});
	});

	describe('Save as default view (#128)', () => {
		it('disables Save as default view while there are unapplied pending changes', async () => {
			const user = userEvent.setup();
			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					onSaveDefault: vi.fn(),
					canSaveDefault: true
				}
			});

			const saveButton = screen.getByRole('button', { name: 'Save as default view' });
			expect(saveButton).not.toBeDisabled();

			await user.click(screen.getByLabelText('Active'));

			expect(saveButton).toBeDisabled();
			expect(
				screen.getByText('Apply your pending changes before saving them as the default view.')
			).toBeInTheDocument();
		});

		it('re-enables Save as default view once Apply reconciles pending with applied filters', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();
			const onSaveDefault = vi.fn();
			const { rerender } = render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange,
					isOpen: true,
					onClose: vi.fn(),
					onSaveDefault,
					canSaveDefault: true
				}
			});

			await user.click(screen.getByLabelText('Active'));
			expect(screen.getByRole('button', { name: 'Save as default view' })).toBeDisabled();

			await user.click(screen.getByRole('button', { name: 'Apply' }));
			const appliedFilters = onFiltersChange.mock.calls.at(-1)?.[0];

			await rerender({
				filters: appliedFilters,
				onFiltersChange,
				isOpen: true,
				onClose: vi.fn(),
				onSaveDefault,
				canSaveDefault: true
			});
			await tick();

			expect(screen.getByRole('button', { name: 'Save as default view' })).not.toBeDisabled();
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
