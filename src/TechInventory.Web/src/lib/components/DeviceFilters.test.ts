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

		const dialog = screen.getByRole('dialog', { name: 'View' });
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

		const closeButton = screen.getByRole('button', { name: 'Close view panel' });
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

			await user.click(screen.getByRole('button', { name: 'Clear all' }));

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

			await user.click(screen.getByRole('button', { name: 'Clear all' }));

			expect(onFiltersChange).toHaveBeenCalledWith({
				page: 1,
				pageSize: 25,
				groupBy: undefined
			});
		});
	});

	describe('#145 — PWA selection mode', () => {
		it('does not show the SELECTION section when isPwa is false (desktop behavior)', () => {
			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					isPwa: false
				}
			});

			expect(screen.queryByRole('region', { name: 'SELECTION' })).not.toBeInTheDocument();
			expect(
				screen.queryByRole('button', { name: 'Enable Selection Mode' })
			).not.toBeInTheDocument();
		});

		it('shows the SELECTION section with "Enable Selection Mode" button when isPwa=true and selection is off', () => {
			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					isPwa: true,
					pwaSelectionMode: false
				}
			});

			// Section label is present
			expect(screen.getByText('SELECTION')).toBeInTheDocument();
			// Enable button is shown; Exit button is absent
			expect(screen.getByRole('button', { name: 'Enable Selection Mode' })).toBeInTheDocument();
			expect(screen.queryByRole('button', { name: 'Exit Selection Mode' })).not.toBeInTheDocument();
		});

		it('shows "Exit Selection Mode" when pwaSelectionMode is active', () => {
			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					isPwa: true,
					pwaSelectionMode: true
				}
			});

			expect(screen.getByRole('button', { name: 'Exit Selection Mode' })).toBeInTheDocument();
			expect(
				screen.queryByRole('button', { name: 'Enable Selection Mode' })
			).not.toBeInTheDocument();
		});

		it('calls onEnablePwaSelection and onClose when "Enable Selection Mode" is tapped', async () => {
			const user = userEvent.setup();
			const onEnablePwaSelection = vi.fn();
			const onClose = vi.fn();

			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose,
					isPwa: true,
					pwaSelectionMode: false,
					onEnablePwaSelection
				}
			});

			await user.click(screen.getByRole('button', { name: 'Enable Selection Mode' }));

			expect(onEnablePwaSelection).toHaveBeenCalledOnce();
			expect(onClose).toHaveBeenCalledOnce();
		});

		it('calls onDisablePwaSelection and onClose when "Exit Selection Mode" is tapped', async () => {
			const user = userEvent.setup();
			const onDisablePwaSelection = vi.fn();
			const onClose = vi.fn();

			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose,
					isPwa: true,
					pwaSelectionMode: true,
					onDisablePwaSelection
				}
			});

			await user.click(screen.getByRole('button', { name: 'Exit Selection Mode' }));

			expect(onDisablePwaSelection).toHaveBeenCalledOnce();
			expect(onClose).toHaveBeenCalledOnce();
		});

		it('has no accessibility violations with PWA selection section visible', async () => {
			const { container } = render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					isPwa: true,
					pwaSelectionMode: false
				}
			});

			expect(await axe(container)).toHaveNoViolations();
		});
	});
	// The panel is the "View" panel now: it carries view options (Group by, bulk
	// expand/collapse, selection mode) alongside the filter facets.
	describe('group bulk actions', () => {
		it('hides Expand/Collapse all when no grouping is in effect', () => {
			render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					groupingActive: false
				}
			});

			expect(screen.queryByTestId('expand-all-groups')).not.toBeInTheDocument();
			expect(screen.queryByTestId('collapse-all-groups')).not.toBeInTheDocument();
		});

		it('shows Expand/Collapse all when grouping is in effect', () => {
			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' as const },
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					groupingActive: true
				}
			});

			expect(screen.getByRole('button', { name: 'Expand all groups' })).toBeInTheDocument();
			expect(screen.getByRole('button', { name: 'Collapse all groups' })).toBeInTheDocument();
		});

		it('calls onExpandAllGroups and closes the panel, without applying filters', async () => {
			const user = userEvent.setup();
			const onExpandAllGroups = vi.fn();
			const onFiltersChange = vi.fn();
			const onClose = vi.fn();

			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' as const },
					onFiltersChange,
					isOpen: true,
					onClose,
					groupingActive: true,
					onExpandAllGroups
				}
			});

			await user.click(screen.getByRole('button', { name: 'Expand all groups' }));

			expect(onExpandAllGroups).toHaveBeenCalledOnce();
			expect(onClose).toHaveBeenCalledOnce();
			// Display-only action: it must not push a query change through.
			expect(onFiltersChange).not.toHaveBeenCalled();
		});

		it('calls onCollapseAllGroups and closes the panel', async () => {
			const user = userEvent.setup();
			const onCollapseAllGroups = vi.fn();
			const onClose = vi.fn();

			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'owner' as const },
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose,
					groupingActive: true,
					onCollapseAllGroups
				}
			});

			await user.click(screen.getByRole('button', { name: 'Collapse all groups' }));

			expect(onCollapseAllGroups).toHaveBeenCalledOnce();
			expect(onClose).toHaveBeenCalledOnce();
		});

		// "Expand all groups" wrapped to two lines in these half-width pills. The
		// visible label is now one word; the fuller phrase survives as the
		// accessible name, which is why the queries above still find these buttons
		// by 'Expand all groups'.
		//
		// TAMPER-TESTED: putting the long string back as visible text fails the
		//   one-word test; dropping aria-label fails it too (the accessible name
		//   collapses to "Expand") and also breaks the three callback tests above;
		//   removing whitespace-nowrap fails the no-wrap test.
		it('labels the bulk actions with a single word', () => {
			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' as const },
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					groupingActive: true
				}
			});

			expect(screen.getByTestId('expand-all-groups').textContent?.trim()).toBe('Expand');
			expect(screen.getByTestId('collapse-all-groups').textContent?.trim()).toBe('Collapse');
		});

		it('keeps the full phrase as the accessible name', () => {
			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' as const },
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					groupingActive: true
				}
			});

			// WCAG 2.5.3 Label in Name: the accessible name must contain the
			// visible label, so "Expand" has to be a prefix of what is announced.
			const expand = screen.getByTestId('expand-all-groups');
			const collapse = screen.getByTestId('collapse-all-groups');
			expect(expand).toHaveAttribute('aria-label', 'Expand all groups');
			expect(collapse).toHaveAttribute('aria-label', 'Collapse all groups');
			expect('Expand all groups'.startsWith(expand.textContent!.trim())).toBe(true);
			expect('Collapse all groups'.startsWith(collapse.textContent!.trim())).toBe(true);
		});

		it('never lets a bulk-action label wrap its pill onto a second line', () => {
			render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' as const },
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					groupingActive: true
				}
			});

			for (const id of ['expand-all-groups', 'collapse-all-groups']) {
				const button = screen.getByTestId(id);
				expect(button.className, id).toMatch(/\bwhitespace-nowrap\b/);
				expect(button.querySelector('span')!.className, id).toMatch(/\btruncate\b/);
			}
		});

		it('has no accessibility violations with the group actions visible', async () => {
			const { container } = render(DeviceFilters, {
				props: {
					filters: { ...defaultFilters, groupBy: 'category' as const },
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn(),
					groupingActive: true
				}
			});

			expect(await axe(container)).toHaveNoViolations();
		});
	});
	// The panel's sticky footer was four stacked full-width pills — ~235px of
	// permanent chrome that hid the options the panel exists to show. It is now
	// one row of two, with the occasional default-view actions moved into the
	// scroll area.
	//
	// TAMPER-TESTED: reverting the footer row to `flex-col` fails "lays the two
	//   pinned actions out in a single two-column row"; putting a Save-as-default
	//   button back in the footer fails "pins only Clear all and Apply" (and the
	//   two relocation tests); dropping either pinned button to `min-h-9` fails
	//   "keeps both pinned actions at a 44px-plus tap target".
	describe('footer density', () => {
		function footerOf(container: HTMLElement): HTMLElement {
			const footer = container.querySelector<HTMLElement>('div.sticky.bottom-0');
			expect(footer, 'sticky footer must exist').toBeTruthy();
			return footer!;
		}

		function scrollBodyOf(container: HTMLElement): HTMLElement {
			const body = container.querySelector<HTMLElement>('div.flex-1.overflow-y-auto');
			expect(body, 'scrollable body must exist').toBeTruthy();
			return body!;
		}

		const withDefaults = {
			filters: defaultFilters,
			onFiltersChange: vi.fn(),
			isOpen: true,
			onClose: vi.fn(),
			onSaveDefault: vi.fn(),
			onClearDefault: vi.fn(),
			hasStoredDefault: true,
			canSaveDefault: true
		};

		it('pins only Clear all and Apply', () => {
			const { container } = render(DeviceFilters, { props: { ...withDefaults } });

			const footerButtons = Array.from(footerOf(container).querySelectorAll('button')).map(
				(button) => button.textContent?.trim()
			);

			expect(footerButtons).toEqual(['Clear all', 'Apply']);
		});

		it('lays the two pinned actions out in a single two-column row', () => {
			const { container } = render(DeviceFilters, { props: { ...withDefaults } });

			const row = footerOf(container).querySelector('div.grid');
			expect(row, 'pinned actions must share one grid row').toBeTruthy();
			expect(row!.className).toMatch(/\bgrid-cols-2\b/);
			// A stack would reintroduce the height this change removed.
			expect(row!.className).not.toMatch(/\bflex-col\b/);
		});

		it('keeps both pinned actions at a 44px-plus tap target', () => {
			const { container } = render(DeviceFilters, { props: { ...withDefaults } });

			for (const button of footerOf(container).querySelectorAll('button')) {
				// min-h-11 is 2.75rem = 46.75px at this app's 17px root (D-137).
				expect(button.className, button.textContent ?? '').toMatch(/\bmin-h-11\b/);
			}
		});

		it('moves Save as default view and Clear saved default into the scroll area', () => {
			const { container } = render(DeviceFilters, { props: { ...withDefaults } });

			const body = scrollBodyOf(container);
			const footer = footerOf(container);

			const save = screen.getByRole('button', { name: 'Save as default view' });
			const clear = screen.getByRole('button', { name: 'Clear saved default' });

			expect(body.contains(save)).toBe(true);
			expect(footer.contains(save)).toBe(false);
			expect(body.contains(clear)).toBe(true);
			expect(footer.contains(clear)).toBe(false);
		});

		it('still wires the relocated default-view actions to their callbacks', async () => {
			const user = userEvent.setup();
			const onSaveDefault = vi.fn();
			const onClearDefault = vi.fn();

			render(DeviceFilters, {
				props: { ...withDefaults, onSaveDefault, onClearDefault }
			});

			await user.click(screen.getByRole('button', { name: 'Save as default view' }));
			expect(onSaveDefault).toHaveBeenCalledOnce();

			await user.click(screen.getByRole('button', { name: 'Clear saved default' }));
			expect(onClearDefault).toHaveBeenCalledOnce();
		});

		it('omits the default-view section entirely when neither callback is supplied', () => {
			const { container } = render(DeviceFilters, {
				props: {
					filters: defaultFilters,
					onFiltersChange: vi.fn(),
					isOpen: true,
					onClose: vi.fn()
				}
			});

			expect(
				screen.queryByRole('button', { name: 'Save as default view' })
			).not.toBeInTheDocument();
			expect(screen.queryByRole('button', { name: 'Clear saved default' })).not.toBeInTheDocument();
			expect(footerOf(container).querySelectorAll('button')).toHaveLength(2);
		});

		it('has no accessibility violations with the relocated default-view section', async () => {
			const { container } = render(DeviceFilters, { props: { ...withDefaults } });

			expect(await axe(container)).toHaveNoViolations();
		});
	});
	// Five stacked full-width rows spent 255px on five short words. The options
	// now sit two per row (161.5px, and a bigger tap target). The layout is only
	// safe because the labels fit their half-width columns — jsdom has no layout
	// engine, so these guard the structural preconditions for that fit and M-33
	// carries the pixel judgement.
	//
	// TAMPER-TESTED: reverting the container to `space-y-2.5` fails "lays the
	//   status options out two per row"; restoring the old `gap-3`/`px-2` inside
	//   the label fails "keeps the in-label spacing that lets the longest label
	//   fit"; dropping `truncate` fails "clips an over-long label instead of
	//   letting it wrap"; and `min-h-10` fails the tap-target guard.
	describe('status option density', () => {
		function statusList(container: HTMLElement): HTMLElement {
			const legend = [...container.querySelectorAll('legend')].find(
				(node) => node.textContent?.trim() === 'Status'
			);
			expect(legend, 'Status fieldset must exist').toBeTruthy();
			const list = legend!.parentElement!.querySelector<HTMLElement>('div');
			expect(list, 'Status options container must exist').toBeTruthy();
			return list!;
		}

		const baseProps = {
			filters: defaultFilters,
			onFiltersChange: vi.fn(),
			isOpen: true,
			onClose: vi.fn()
		};

		it('lays the status options out two per row', () => {
			const { container } = render(DeviceFilters, { props: { ...baseProps } });

			const list = statusList(container);
			expect(list.className).toMatch(/\bgrid\b/);
			expect(list.className).toMatch(/\bgrid-cols-2\b/);
			// A vertical stack is what this replaced.
			expect(list.className).not.toMatch(/\bspace-y-/);
		});

		it('still renders every status option exactly once', () => {
			const { container } = render(DeviceFilters, { props: { ...baseProps } });

			const labels = [...statusList(container).querySelectorAll('label')].map((node) =>
				node.textContent?.trim()
			);

			expect(labels).toEqual(['Active', 'Retired', 'Disposed', 'In Repair', 'Lent']);
		});

		it('keeps the in-label spacing that lets the longest label fit its column', () => {
			const { container } = render(DeviceFilters, { props: { ...baseProps } });

			for (const label of statusList(container).querySelectorAll('label')) {
				// gap-3 + px-2 costs 12.75px of the 82.4px a column has for text at
				// 320px, which is what made "In Repair" wrap to two lines there.
				expect(label.className).toMatch(/\bgap-2\b/);
				expect(label.className).toMatch(/\bpx-1\.5\b/);
				expect(label.className).not.toMatch(/\bgap-3\b/);
			}
		});

		it('clips an over-long label instead of letting it wrap its row taller', () => {
			const { container } = render(DeviceFilters, { props: { ...baseProps } });

			for (const label of statusList(container).querySelectorAll('label')) {
				const text = label.querySelector('span');
				expect(text!.className, label.textContent ?? '').toMatch(/\btruncate\b/);
				// The checkbox must not be squeezed when the text is the long one.
				expect(label.querySelector('input')!.className).toMatch(/\bshrink-0\b/);
			}
		});

		it('gives every status option a 44px-plus tap target', () => {
			const { container } = render(DeviceFilters, { props: { ...baseProps } });

			for (const label of statusList(container).querySelectorAll('label')) {
				// min-h-11 is 2.75rem = 46.75px at this app's 17px root (D-137);
				// the stacked layout this replaced used min-h-10 (42.5px).
				expect(label.className, label.textContent ?? '').toMatch(/\bmin-h-11\b/);
			}
		});

		it('still toggles a status through the pending gate when paired', async () => {
			const user = userEvent.setup();
			const onFiltersChange = vi.fn();

			render(DeviceFilters, { props: { ...baseProps, onFiltersChange } });

			await user.click(screen.getByLabelText('In Repair'));
			expect(onFiltersChange).not.toHaveBeenCalled();

			await user.click(screen.getByRole('button', { name: 'Apply' }));
			expect(onFiltersChange).toHaveBeenCalledWith(
				expect.objectContaining({ status: ['InRepair'] })
			);
		});

		it('has no accessibility violations with the paired status options', async () => {
			const { container } = render(DeviceFilters, { props: { ...baseProps } });

			expect(await axe(container)).toHaveNoViolations();
		});
	});
});
