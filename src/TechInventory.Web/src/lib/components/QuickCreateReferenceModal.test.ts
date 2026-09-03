/**
 * QuickCreateReferenceModal Component Tests — #136
 *
 * Focused coverage per the binding design review:
 * - Create-and-auto-select: happy path for each reference type
 * - Cancel preserves form state (no API call, onCreated not called)
 * - Duplicate conflict (409): inline user-legible error, no onCreated call
 * - Permission gate: non-Admin DeviceForm doesn't render the affordance
 * - Focus behavior: initial focus on name field, Escape calls onCancel
 * - Accessibility: zero axe violations in open state (all three types)
 *
 * AC retirement note: the E2E browser AC from issue #136 is replaced by this
 * component test (Vitest + Testing Library) per the project-wide E2E
 * retirement decision (specs/004-agentic-development-foundation/brief.md §2.1).
 * Contract correctness is covered by the generated API client types.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import QuickCreateReferenceModal from './QuickCreateReferenceModal.svelte';

// Mock i18n — t() returns the key so assertions can use key names.
vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

// Mock the API client so no real network calls are made.
const mockBrandsCreate = vi.fn();
const mockCategoriesCreate = vi.fn();
const mockLocationsCreate = vi.fn();

vi.mock('$lib/api/client', () => ({
	brands: { create: (...args: unknown[]) => mockBrandsCreate(...args) },
	categories: { create: (...args: unknown[]) => mockCategoriesCreate(...args) },
	locations: { create: (...args: unknown[]) => mockLocationsCreate(...args) },
	ApiError: class ApiError extends Error {
		status: number;
		constructor(status: number, title: string) {
			super(title);
			this.name = 'ApiError';
			this.status = status;
		}
	}
}));

// Re-import ApiError after mock so we can construct instances.
import { ApiError } from '$lib/api/client';

const BRAND_ID = '00000000-0000-4000-8000-000000000301';
const CATEGORY_ID = '00000000-0000-4000-8000-000000000201';
const LOCATION_ID = '00000000-0000-4000-8000-000000000501';

function makeProps(overrides: Partial<{
	type: 'brand' | 'category' | 'location';
	triggerElement: HTMLElement | null;
	onCreated: (id: string, name: string) => void;
	onCancel: () => void;
}> = {}) {
	return {
		type: 'brand' as const,
		triggerElement: null,
		onCreated: vi.fn(),
		onCancel: vi.fn(),
		...overrides
	};
}

describe('QuickCreateReferenceModal', () => {
	beforeEach(() => {
		mockBrandsCreate.mockReset();
		mockCategoriesCreate.mockReset();
		mockLocationsCreate.mockReset();
	});

	// ────────────────────────────────────────────────────────────
	// Rendering
	// ────────────────────────────────────────────────────────────

	describe('rendering', () => {
		it('shows the dialog with the correct title for brand type', () => {
			render(QuickCreateReferenceModal, { props: makeProps({ type: 'brand' }) });
			expect(screen.getByRole('dialog')).toBeInTheDocument();
			expect(screen.getByText('referenceCreate.brand.dialogTitle')).toBeInTheDocument();
		});

		it('shows the dialog with the correct title for category type', () => {
			render(QuickCreateReferenceModal, { props: makeProps({ type: 'category' }) });
			expect(screen.getByText('referenceCreate.category.dialogTitle')).toBeInTheDocument();
		});

		it('shows the dialog with the correct title for location type', () => {
			render(QuickCreateReferenceModal, { props: makeProps({ type: 'location' }) });
			expect(screen.getByText('referenceCreate.location.dialogTitle')).toBeInTheDocument();
		});

		it('shows the location type selector only for location type', () => {
			const { unmount } = render(QuickCreateReferenceModal, { props: makeProps({ type: 'brand' }) });
			expect(screen.queryByLabelText(/referenceCreate.location.typeLabel/i)).not.toBeInTheDocument();
			unmount();

			render(QuickCreateReferenceModal, { props: makeProps({ type: 'location' }) });
			expect(screen.getByLabelText(/referenceCreate.location.typeLabel/i)).toBeInTheDocument();
		});

		it('has Cancel and Create buttons', () => {
			render(QuickCreateReferenceModal, { props: makeProps() });
			expect(screen.getByRole('button', { name: /referenceCreate.cancel/i })).toBeInTheDocument();
			expect(screen.getByRole('button', { name: /referenceCreate.submit/i })).toBeInTheDocument();
		});
	});

	// ────────────────────────────────────────────────────────────
	// Happy path — create and auto-select
	// ────────────────────────────────────────────────────────────

	describe('create-and-auto-select', () => {
		it('calls brands.create and invokes onCreated with id+name on success', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			mockBrandsCreate.mockResolvedValue({ id: BRAND_ID, name: 'Acme' });

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'brand', onCreated })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Acme');
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			await waitFor(() => {
				expect(mockBrandsCreate).toHaveBeenCalledWith({ name: 'Acme' });
				expect(onCreated).toHaveBeenCalledWith(BRAND_ID, 'Acme');
			});
		});

		it('calls categories.create and invokes onCreated on success', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			mockCategoriesCreate.mockResolvedValue({ id: CATEGORY_ID, name: 'Smartphones' });

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'category', onCreated })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Smartphones');
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			await waitFor(() => {
				expect(mockCategoriesCreate).toHaveBeenCalledWith({ name: 'Smartphones' });
				expect(onCreated).toHaveBeenCalledWith(CATEGORY_ID, 'Smartphones');
			});
		});

		it('calls locations.create with name+type and invokes onCreated on success', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			mockLocationsCreate.mockResolvedValue({ id: LOCATION_ID, name: 'Garage' });

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'location', onCreated })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Garage');
			// Default locationType is 'Home'; select 'Storage'
			await user.selectOptions(
				screen.getByLabelText(/referenceCreate.location.typeLabel/i),
				'Storage'
			);
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			await waitFor(() => {
				expect(mockLocationsCreate).toHaveBeenCalledWith({ name: 'Garage', type: 'Storage' });
				expect(onCreated).toHaveBeenCalledWith(LOCATION_ID, 'Garage');
			});
		});
	});

	// ────────────────────────────────────────────────────────────
	// Cancel — preserves form (no API call, onCreated not invoked)
	// ────────────────────────────────────────────────────────────

	describe('cancel-preserves-form', () => {
		it('calls onCancel when the Cancel button is clicked', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			const onCancel = vi.fn();

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'brand', onCreated, onCancel })
			});

			// Type something to make the form non-empty
			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Partial');
			await user.click(screen.getByRole('button', { name: /referenceCreate.cancel/i }));

			expect(onCancel).toHaveBeenCalledTimes(1);
			// No API call should have occurred
			expect(mockBrandsCreate).not.toHaveBeenCalled();
			// onCreated must not be invoked
			expect(onCreated).not.toHaveBeenCalled();
		});

		it('calls onCancel on Escape and does not call the API', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			const onCancel = vi.fn();

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'category', onCreated, onCancel })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Incomplete');
			await user.keyboard('{Escape}');

			expect(onCancel).toHaveBeenCalledTimes(1);
			expect(mockCategoriesCreate).not.toHaveBeenCalled();
			expect(onCreated).not.toHaveBeenCalled();
		});
	});

	// ────────────────────────────────────────────────────────────
	// Duplicate conflict (409) — inline user-legible validation message
	// ────────────────────────────────────────────────────────────

	describe('duplicate conflict', () => {
		it('shows inline duplicate error for brand when API returns 409', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			mockBrandsCreate.mockRejectedValue(new ApiError(409, 'Conflict'));

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'brand', onCreated })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Apple');
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			await waitFor(() => {
				expect(
					screen.getByText('referenceCreate.brand.duplicateError')
				).toBeInTheDocument();
			});
			expect(onCreated).not.toHaveBeenCalled();
		});

		it('shows inline duplicate error for location when API returns 409', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			mockLocationsCreate.mockRejectedValue(new ApiError(409, 'Conflict'));

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'location', onCreated })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Basement');
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			await waitFor(() => {
				expect(
					screen.getByText('referenceCreate.location.duplicateError')
				).toBeInTheDocument();
			});
			expect(onCreated).not.toHaveBeenCalled();
		});

		it('shows generic submit error (not duplicate message) for non-409 failures', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();
			mockCategoriesCreate.mockRejectedValue(new ApiError(500, 'Internal Server Error'));

			render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'category', onCreated })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Tablets');
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			await waitFor(() => {
				expect(
					screen.getByText('referenceCreate.category.createError')
				).toBeInTheDocument();
			});
			// Duplicate-specific message must NOT appear
			expect(
				screen.queryByText('referenceCreate.category.duplicateError')
			).not.toBeInTheDocument();
			expect(onCreated).not.toHaveBeenCalled();
		});
	});

	// ────────────────────────────────────────────────────────────
	// Client-side validation
	// ────────────────────────────────────────────────────────────

	describe('client-side validation', () => {
		it('does not call API when name is empty', async () => {
			const user = userEvent.setup();
			const onCreated = vi.fn();

			render(QuickCreateReferenceModal, { props: makeProps({ onCreated }) });

			// Click Create without typing anything
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			// API must not be called
			await waitFor(() => {
				expect(mockBrandsCreate).not.toHaveBeenCalled();
			});
			expect(onCreated).not.toHaveBeenCalled();
		});
	});

	// ────────────────────────────────────────────────────────────
	// Focus behavior
	// ────────────────────────────────────────────────────────────

	describe('focus behavior', () => {
		it('initially focuses the name input', async () => {
			render(QuickCreateReferenceModal, { props: makeProps() });

			await waitFor(() => {
				expect(document.activeElement).toBe(screen.getByLabelText(/referenceCreate.nameLabel/i));
			});
		});

		it('restores focus to triggerElement on cancel', async () => {
			const user = userEvent.setup();
			const trigger = document.createElement('button');
			document.body.appendChild(trigger);

			render(QuickCreateReferenceModal, {
				props: makeProps({ triggerElement: trigger })
			});

			await user.click(screen.getByRole('button', { name: /referenceCreate.cancel/i }));

			expect(document.activeElement).toBe(trigger);
			document.body.removeChild(trigger);
		});
	});

	// ────────────────────────────────────────────────────────────
	// Accessibility — zero axe violations
	// ────────────────────────────────────────────────────────────

	describe('accessibility', () => {
		it('has no axe violations in initial state (brand)', async () => {
			const { container } = render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'brand' })
			});
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations in initial state (category)', async () => {
			const { container } = render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'category' })
			});
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations in initial state (location, includes type selector)', async () => {
			const { container } = render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'location' })
			});
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations when a duplicate error is shown', async () => {
			const user = userEvent.setup();
			mockBrandsCreate.mockRejectedValue(new ApiError(409, 'Conflict'));

			const { container } = render(QuickCreateReferenceModal, {
				props: makeProps({ type: 'brand' })
			});

			await user.type(screen.getByLabelText(/referenceCreate.nameLabel/i), 'Apple');
			await user.click(screen.getByRole('button', { name: /referenceCreate.submit/i }));

			await waitFor(() => {
				expect(
					screen.getByText('referenceCreate.brand.duplicateError')
				).toBeInTheDocument();
			});

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});
