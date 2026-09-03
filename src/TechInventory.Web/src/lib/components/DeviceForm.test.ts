/**
 * DeviceForm Component Tests — Form validation and behavior (T23)
 *
 * Test create/edit modes, validation, disabledFields, submit/cancel handlers.
 * Constitution §3.4: axe-core zero violations
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import DeviceForm from './DeviceForm.svelte';
import {
	createBrand,
	createCategory,
	createOwner,
	createLocation,
	createNetwork,
	createTag,
	createDeviceCreateInput,
	resetFactories
} from '$lib/test-utils/factories';
import type { ReferenceDataState } from '$lib/stores/referenceData';

// Mock i18n
vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

// Mock reference data store
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

import { referenceDataStore } from '$lib/stores/referenceData';

const testBrandId = '00000000-0000-4000-8000-000000000301';
const testCategoryId = '00000000-0000-4000-8000-000000000201';
const testOwnerId = '00000000-0000-4000-8000-000000000401';
const testLocationId = '00000000-0000-4000-8000-000000000501';

describe('DeviceForm', () => {
	beforeEach(() => {
		resetFactories();

		// Populate reference data store with test data
		const refData: ReferenceDataState = {
			brands: [
				createBrand({ id: testBrandId, name: 'Apple' }),
				createBrand({ id: '00000000-0000-4000-8000-000000000302', name: 'Dell' })
			],
			categories: [
				createCategory({ id: testCategoryId, name: 'Laptop' }),
				createCategory({ id: '00000000-0000-4000-8000-000000000202', name: 'Phone' })
			],
			owners: [
				createOwner({ id: testOwnerId, name: 'Alice' }),
				createOwner({ name: 'Bob' })
			],
			locations: [
				createLocation({ id: testLocationId, name: 'Office' }),
				createLocation({ name: 'Home' })
			],
			networks: [createNetwork({ name: 'WiFi-Main' }), createNetwork({ name: 'WiFi-Guest' })],
			tags: [
				createTag({ id: '00000000-0000-4000-8000-000000000101', name: 'Travel', color: '#0ea5e9' }),
				createTag({
					id: '00000000-0000-4000-8000-000000000102',
					name: 'Critical',
					color: '#ef4444'
				})
			],
			isLoading: false,
			error: null
		};
		referenceDataStore.set(refData);
	});

	const mockOnSubmit = vi.fn<
		(data: import('$lib/schemas/device').DeviceFormInput) => Promise<void>
	>(async () => {});
	const mockOnCancel = vi.fn();

	const defaultProps = {
		mode: 'create' as const,
		onSubmit: mockOnSubmit,
		onCancel: mockOnCancel
	};

	describe('create mode', () => {
		it('renders form with all expected fields', () => {
			render(DeviceForm, { props: defaultProps });

			// Required fields
			expect(screen.getByLabelText(/devices.columns.name/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.brand/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.category/i)).toBeInTheDocument();

			// Optional fields
			expect(screen.getByLabelText(/devices.columns.serial/i)).toBeInTheDocument();
			// F034: model is rendered as a top-level optional field so imported devices
			// surface their model number without burying it inside the collapsible
			// "Additional details" section.
			expect(screen.getByLabelText(/devices.columns.model/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.owner/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.location/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.network/i)).toBeInTheDocument();
			expect(screen.getByRole('group', { name: /devices.form.tags/i })).toBeInTheDocument();
			expect(screen.getByLabelText('Travel')).toBeInTheDocument();
			expect(screen.getByLabelText('Critical')).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.purchaseDate/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.purchasePrice/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.currency/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.notes/i)).toBeInTheDocument();
		});

		it('renders exactly one tag selector (regression: duplicate tag UI)', () => {
			// Regression test: User reported duplicate tag pickers appearing in the
			// new item view. DeviceForm should render exactly one DeviceTagSelector.
			// Callers (like AddDeviceModal) must NOT add a second tag picker above the form.
			render(DeviceForm, { props: defaultProps });

			// Should find exactly one role="group" with name matching tags
			const tagGroups = screen.getAllByRole('group', { name: /devices.form.tags/i });
			expect(tagGroups).toHaveLength(1);

			// Tag checkboxes should appear once
			const travelCheckboxes = screen.getAllByLabelText('Travel');
			expect(travelCheckboxes).toHaveLength(1);

			const criticalCheckboxes = screen.getAllByLabelText('Critical');
			expect(criticalCheckboxes).toHaveLength(1);
		});

		it('starts with empty form', () => {
			render(DeviceForm, { props: defaultProps });

			const nameInput = screen.getByLabelText(/devices.columns.name/i) as HTMLInputElement;
			expect(nameInput.value).toBe('');
		});

		it('submit button is NOT disabled initially in create mode (can submit partial forms)', () => {
			render(DeviceForm, { props: defaultProps });

			const submitButton = screen.getByRole('button', { name: /common\.actions\.save/i });
			expect(submitButton).not.toBeDisabled();
		});

		it('currency defaults to USD (D-070)', () => {
			render(DeviceForm, { props: defaultProps });

			const currencySelect = screen.getByLabelText(
				/devices.columns.currency/i
			) as HTMLSelectElement;
			expect(currencySelect.value).toBe('USD');
		});

		it('reference data dropdowns populate from store', () => {
			render(DeviceForm, { props: defaultProps });

			const brandSelect = screen.getByLabelText(/devices.columns.brand/i);
			expect(brandSelect).toBeInTheDocument();

			// Should have at least the brands from mock
			const options = Array.from(brandSelect.querySelectorAll('option'));
			const brandNames = options.map((opt) => opt.textContent);
			expect(brandNames).toContain('Apple');
			expect(brandNames).toContain('Dell');
		});

		it('does not render a Status control in create mode (new devices are always Active server-side)', () => {
			render(DeviceForm, { props: defaultProps });

			expect(screen.queryByLabelText(/devices.columns.status/i)).not.toBeInTheDocument();
		});
	});

	describe('edit mode', () => {
		it('pre-populates form with initialData', () => {
			const initialData = createDeviceCreateInput({
				name: 'Existing Device',
				serialNumber: 'SN123456',
				tagIds: ['00000000-0000-4000-8000-000000000102'],
				purchasePrice: 999.99
			});

			const props = {
				...defaultProps,
				mode: 'edit' as const,
				initialData
			};

			render(DeviceForm, { props });

			const nameInput = screen.getByLabelText(/devices.columns.name/i) as HTMLInputElement;
			expect(nameInput.value).toBe('Existing Device');

			const serialInput = screen.getByLabelText(/devices.columns.serial/i) as HTMLInputElement;
			expect(serialInput.value).toBe('SN123456');

			const priceInput = screen.getByLabelText(
				/devices.columns.purchasePrice/i
			) as HTMLInputElement;
			expect(priceInput.value).toBe('999.99');

			const selectedTag = screen.getByLabelText('Critical') as HTMLInputElement;
			expect(selectedTag).toBeChecked();
		});

		it('submit button disabled initially in edit mode when not dirty', () => {
			const initialData = createDeviceCreateInput({ name: 'Existing Device' });

			const props = {
				...defaultProps,
				mode: 'edit' as const,
				initialData
			};

			render(DeviceForm, { props });

			const submitButton = screen.getByRole('button', { name: /common\.actions\.save/i });
			expect(submitButton).toBeDisabled();
		});

		// #133: the edit form previously had no Status control at all, so
		// devices.update always omitted `status`. UpdateDeviceRequest.Status
		// defaults to Active when omitted, so any edit to an InRepair/Lent/
		// Retired device silently reset it back to Active. These three cases
		// lock in that the form now pre-populates and preserves each status.
		it.each(['InRepair', 'Lent', 'Retired'] as const)(
			'pre-populates the Status control with the device\'s current status (%s)',
			(status) => {
				const initialData = createDeviceCreateInput({ name: 'Existing Device', status });

				render(DeviceForm, {
					props: { ...defaultProps, mode: 'edit' as const, initialData }
				});

				const statusSelect = screen.getByLabelText(
					/devices.columns.status/i
				) as HTMLSelectElement;
				expect(statusSelect.value).toBe(status);
			}
		);

		it.each(['InRepair', 'Lent', 'Retired'] as const)(
			'preserves the current status (%s) in the submitted payload when the user does not touch it',
			async (status) => {
				const user = userEvent.setup();
				const onSubmit = vi.fn<
					(data: import('$lib/schemas/device').DeviceFormInput) => Promise<void>
				>(async () => {});
				const initialData = createDeviceCreateInput({
					name: 'Existing Device',
					brandId: testBrandId,
					categoryId: testCategoryId,
					ownerId: testOwnerId,
					locationId: testLocationId,
					status
				});

				render(DeviceForm, {
					props: { ...defaultProps, onSubmit, mode: 'edit' as const, initialData }
				});

				// Touch an unrelated field so the form becomes dirty and submittable.
				const nameInput = screen.getByLabelText(/devices.columns.name/i);
				await user.clear(nameInput);
				await user.type(nameInput, 'Existing Device Renamed');

				const submitButton = screen.getByRole('button', { name: /common\.actions\.save/i });
				await waitFor(() => expect(submitButton).toBeEnabled());
				await user.click(submitButton);

				await waitFor(() => expect(onSubmit).toHaveBeenCalled());
				const submitted = onSubmit.mock.calls[0][0];
				expect(submitted.status).toBe(status);
			}
		);

		it('allows selecting a different status and submits the newly selected value', async () => {
			const user = userEvent.setup();
			const onSubmit = vi.fn<
				(data: import('$lib/schemas/device').DeviceFormInput) => Promise<void>
			>(async () => {});
			const initialData = createDeviceCreateInput({
				name: 'Existing Device',
				brandId: testBrandId,
				categoryId: testCategoryId,
				ownerId: testOwnerId,
				locationId: testLocationId,
				status: 'Active'
			});

			render(DeviceForm, {
				props: { ...defaultProps, onSubmit, mode: 'edit' as const, initialData }
			});

			const statusSelect = screen.getByLabelText(/devices.columns.status/i);
			await user.selectOptions(statusSelect, 'InRepair');

			const submitButton = screen.getByRole('button', { name: /common\.actions\.save/i });
			await waitFor(() => expect(submitButton).toBeEnabled());
			await user.click(submitButton);

			await waitFor(() => expect(onSubmit).toHaveBeenCalled());
			const submitted = onSubmit.mock.calls[0][0];
			expect(submitted.status).toBe('InRepair');
		});
	});

	describe('validation', () => {
		it('shows error on blur when name is empty', async () => {
			const user = userEvent.setup();
			render(DeviceForm, { props: defaultProps });

			const nameInput = screen.getByLabelText(/devices.columns.name/i);

			// Focus and blur without entering text
			await user.click(nameInput);
			await user.tab();

			// Should show error after blur
			await waitFor(() => {
				expect(screen.getByText(/required/i)).toBeInTheDocument();
			});
		});

		it('does not show error before field is touched', () => {
			render(DeviceForm, { props: defaultProps });

			// No error message initially
			expect(screen.queryByText(/required/i)).not.toBeInTheDocument();
		});

		it('clears error when valid input entered', async () => {
			const user = userEvent.setup();
			render(DeviceForm, { props: defaultProps });

			const nameInput = screen.getByLabelText(/devices.columns.name/i);

			// Trigger error
			await user.click(nameInput);
			await user.tab();
			await waitFor(() => {
				expect(screen.getByText(/required/i)).toBeInTheDocument();
			});

			// Enter valid text
			await user.clear(nameInput);
			await user.type(nameInput, 'Valid Device Name');
			await user.tab();

			// Error should clear
			await waitFor(() => {
				expect(screen.queryByText(/required/i)).not.toBeInTheDocument();
			});
		});

		it('shows accessible owner-required error on submit when owner is omitted (regression: owner/location-missing create failure, F-02)', async () => {
			// Regression test: the API requires ownerId/locationId (backend
			// DeviceValidationRules.ApplyRequiredReferenceRules) but the form
			// used to show no asterisk and no field-level error for them,
			// only a generic toast. This test ensures the form validates
			// ownerId and displays a clear accessible error message.
			const user = userEvent.setup();
			const onSubmit = vi.fn<
				(data: import('$lib/schemas/device').DeviceFormInput) => Promise<void>
			>(async () => {});

			render(DeviceForm, {
				props: {
					...defaultProps,
					onSubmit
				}
			});

			// Fill name and category (required fields), select a location, but omit owner
			const nameInput = screen.getByLabelText(/devices.columns.name/i);
			await user.type(nameInput, 'Test Device');

			const categorySelect = screen.getByLabelText(/devices.columns.category/i);
			await user.selectOptions(categorySelect, [testCategoryId]);

			const locationSelect = screen.getByLabelText(/devices.columns.location/i);
			await user.selectOptions(locationSelect, [testLocationId]);

			// Submit form without selecting owner
			const submitButton = screen.getByRole('button', { name: /common.actions.save/i });
			await user.click(submitButton);

			// Should show owner-required error (Zod schema: "Owner is required")
			await waitFor(() => {
				expect(screen.getByText(/Owner is required/i)).toBeInTheDocument();
			});

			// Verify error appears below the owner field for accessibility
			const ownerSelect = screen.getByLabelText(/devices.columns.owner/i);
			const ownerField = ownerSelect.closest('div');
			const ownerFieldError = ownerField?.querySelector('p.text-danger-600, p.text-danger-400');
			expect(ownerFieldError).toBeInTheDocument();
			expect(ownerFieldError).toHaveTextContent('Owner is required');

			// onSubmit should NOT have been called because validation failed
			expect(onSubmit).not.toHaveBeenCalled();
		});

		it('shows accessible owner-required error on blur when owner is omitted (regression: owner/location-missing create failure, F-02)', async () => {
			// Second assertion: owner field should also validate on blur so the
			// user gets immediate feedback before submit.
			const user = userEvent.setup();
			render(DeviceForm, { props: defaultProps });

			const ownerSelect = screen.getByLabelText(/devices.columns.owner/i);

			// Focus owner field and blur without selecting a value
			await user.click(ownerSelect);
			await user.tab();

			// Should show error after blur
			await waitFor(() => {
				const errorMsg = screen.getByText('Owner is required');
				expect(errorMsg).toBeInTheDocument();
				expect(errorMsg).toHaveClass('text-danger-600');
			});
		});
	});

	describe('submit behavior', () => {
		it('enables submit button after form becomes dirty and valid', async () => {
			const user = userEvent.setup();
			render(DeviceForm, { props: defaultProps });

			const submitButton = screen.getByRole('button', { name: /common.actions.save/i });
			// In create mode, button is enabled by default (only disabled when submitting)
			// Validation happens on submit, not via button disabled state
			expect(submitButton).not.toBeDisabled();

			// Fill required fields (use IDs from resetFactories: first brand=0, first category=0)
			const nameInput = screen.getByLabelText(/devices.columns.name/i);
			await user.type(nameInput, 'New Device');

			const brandSelect = screen.getByLabelText(/devices.columns.brand/i);
			await user.selectOptions(brandSelect, [testBrandId]);

			const categorySelect = screen.getByLabelText(/devices.columns.category/i);
			await user.selectOptions(categorySelect, [testCategoryId]);

			// Submit should still be enabled (validation on submit, not button state)
			await waitFor(() => {
				expect(submitButton).not.toBeDisabled();
			});
		});

		it('calls onSubmit with selected tagIds on valid submission', async () => {
			const user = userEvent.setup();
			const onSubmit = vi.fn<
				(data: import('$lib/schemas/device').DeviceFormInput) => Promise<void>
			>(async () => {});
			const initialData = {
				name: 'Existing Device',
				serialNumber: 'SN123456',
				brandId: testBrandId,
				categoryId: testCategoryId,
				ownerId: testOwnerId,
				locationId: testLocationId,
				networkId: '',
				tagIds: ['00000000-0000-4000-8000-000000000101'],
				purchaseDate: '',
				purchasePrice: null,
				currencyCode: 'USD',
				notes: '',
				purpose: '',
				operatingSystem: '',
				ipAddress: '',
				macAddress: '',
				productUrl: '',
				version: ''
			};
			referenceDataStore.update((state) => ({
				...state,
				brands: [createBrand({ id: testBrandId, name: 'Valid Brand' })],
				categories: [createCategory({ id: testCategoryId, name: 'Valid Category' })]
			}));

			render(DeviceForm, {
				props: {
					...defaultProps,
					mode: 'edit',
					initialData,
					onSubmit
				}
			});

			const criticalCheckbox = screen.getByLabelText('Critical');
			await user.click(criticalCheckbox);

			const submitButton = screen.getByRole('button', { name: /common.actions.save/i });
			await waitFor(() => expect(submitButton).not.toBeDisabled());
			await user.click(submitButton);

			await waitFor(() => {
				expect(onSubmit).toHaveBeenCalledTimes(1);
				expect(onSubmit).toHaveBeenCalledWith(
					expect.objectContaining({
						name: 'Existing Device',
						brandId: testBrandId,
						categoryId: testCategoryId,
						tagIds: ['00000000-0000-4000-8000-000000000101', '00000000-0000-4000-8000-000000000102']
					})
				);
			});
		});

		it('disables submit button while submitting', async () => {
			const user = userEvent.setup();
			const onSubmit = vi.fn<
				(data: import('$lib/schemas/device').DeviceFormInput) => Promise<void>
			>(async () => new Promise((resolve) => setTimeout(resolve, 100)));

			render(DeviceForm, {
				props: {
					...defaultProps,
					onSubmit
				}
			});

			// Fill every required field (owner/location included) so client-side
			// validation actually lets the async onSubmit run — an earlier version
			// of this test omitted owner/location, so validation blocked submission
			// and isSubmitting never flipped true; that looked like a jsdom
			// select-binding bug but was just an incomplete fixture (T23 skip note
			// was stale, not a real jsdom limitation).
			await user.type(screen.getByLabelText(/devices.columns.name/i), 'Test');
			await user.selectOptions(screen.getByLabelText(/devices.columns.brand/i), testBrandId);
			await user.selectOptions(screen.getByLabelText(/devices.columns.category/i), testCategoryId);
			await user.selectOptions(screen.getByLabelText(/devices.columns.owner/i), testOwnerId);
			await user.selectOptions(screen.getByLabelText(/devices.columns.location/i), testLocationId);

			const submitButton = screen.getByRole('button', { name: /common.actions.save/i });
			await waitFor(() => expect(submitButton).not.toBeDisabled());

			await user.click(submitButton);

			// Should be disabled during submission
			await waitFor(() => expect(submitButton).toBeDisabled());
		});
	});

	describe('cancel behavior', () => {
		it('calls onCancel when cancel button clicked', async () => {
			const user = userEvent.setup();
			const onCancel = vi.fn();

			render(DeviceForm, {
				props: {
					...defaultProps,
					onCancel
				}
			});

			const cancelButton = screen.getByRole('button', { name: /common\.actions\.cancel/i });
			await user.click(cancelButton);

			expect(onCancel).toHaveBeenCalledTimes(1);
		});

		it('discards unsaved edits without submitting (C-05: cancel-without-save)', async () => {
			// Regression guard: cancelling a dirty, unsubmitted form must never
			// trigger the save mutation — only onCancel fires, and onSubmit is
			// never invoked with the in-progress edits.
			const user = userEvent.setup();
			const onCancel = vi.fn();
			const onSubmit = vi.fn<
				(data: import('$lib/schemas/device').DeviceFormInput) => Promise<void>
			>(async () => {});

			render(DeviceForm, {
				props: {
					...defaultProps,
					onCancel,
					onSubmit
				}
			});

			await user.type(screen.getByLabelText(/devices.columns.name/i), 'Unsaved Device Name');
			await user.selectOptions(screen.getByLabelText(/devices.columns.brand/i), testBrandId);

			const cancelButton = screen.getByRole('button', { name: /common\.actions\.cancel/i });
			await user.click(cancelButton);

			expect(onCancel).toHaveBeenCalledTimes(1);
			expect(onSubmit).not.toHaveBeenCalled();
		});
	});

	describe('disabledFields prop (retired device guard)', () => {
		it('disables name field when in disabledFields', () => {
			render(DeviceForm, {
				props: {
					...defaultProps,
					disabledFields: ['name']
				}
			});

			const nameInput = screen.getByLabelText(/devices.columns.name/i) as HTMLInputElement;
			expect(nameInput).toBeDisabled();
		});

		it('disables serialNumber when in disabledFields', () => {
			render(DeviceForm, {
				props: {
					...defaultProps,
					disabledFields: ['serialNumber']
				}
			});

			const serialInput = screen.getByLabelText(/devices.columns.serial/i) as HTMLInputElement;
			expect(serialInput).toBeDisabled();
		});

		it('disables multiple fields when specified', () => {
			render(DeviceForm, {
				props: {
					...defaultProps,
					disabledFields: ['name', 'serialNumber', 'brandId']
				}
			});

			expect(screen.getByLabelText(/devices.columns.name/i)).toBeDisabled();
			expect(screen.getByLabelText(/devices.columns.serial/i)).toBeDisabled();
			expect(screen.getByLabelText(/devices.columns.brand/i)).toBeDisabled();
		});

		it('does not disable fields not in disabledFields', () => {
			render(DeviceForm, {
				props: {
					...defaultProps,
					disabledFields: ['name']
				}
			});

			// Name should be disabled
			expect(screen.getByLabelText(/devices.columns.name/i)).toBeDisabled();

			// Other fields should remain enabled
			expect(screen.getByLabelText(/devices.columns.serial/i)).not.toBeDisabled();
			expect(screen.getByLabelText(/devices.columns.brand/i)).not.toBeDisabled();
			expect(screen.getByLabelText('Travel')).not.toBeDisabled();
		});

		it('disables tag checkboxes when tagIds is disabled', () => {
			render(DeviceForm, {
				props: {
					...defaultProps,
					disabledFields: ['tagIds']
				}
			});

			expect(screen.getByLabelText('Travel')).toBeDisabled();
			expect(screen.getByLabelText('Critical')).toBeDisabled();
		});

		// #133: the Status control must respect the same generic disabledFields
		// mechanism as every other field — callers (e.g. role/permission gating
		// on the edit route) disable it the same way they disable name/brandId/etc.
		it('disables the Status control when "status" is in disabledFields (edit mode)', () => {
			const initialData = createDeviceCreateInput({ name: 'Existing Device', status: 'Retired' });

			render(DeviceForm, {
				props: {
					...defaultProps,
					mode: 'edit' as const,
					initialData,
					disabledFields: ['status']
				}
			});

			expect(screen.getByLabelText(/devices.columns.status/i)).toBeDisabled();
		});

		it('leaves the Status control enabled when it is not in disabledFields (edit mode)', () => {
			const initialData = createDeviceCreateInput({ name: 'Existing Device', status: 'Active' });

			render(DeviceForm, {
				props: {
					...defaultProps,
					mode: 'edit' as const,
					initialData,
					disabledFields: ['name']
				}
			});

			expect(screen.getByLabelText(/devices.columns.status/i)).not.toBeDisabled();
		});
	});

	describe('accessibility', () => {
		it('has no axe violations in create mode (empty form)', async () => {
			const { container } = render(DeviceForm, { props: defaultProps });

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations in edit mode with data', async () => {
			const initialData = createDeviceCreateInput({
				name: 'Test Device',
				serialNumber: 'SN123'
			});

			const { container } = render(DeviceForm, {
				props: {
					...defaultProps,
					mode: 'edit',
					initialData
				}
			});

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations in edit mode with the Status control on a non-default status', async () => {
			const initialData = createDeviceCreateInput({
				name: 'Test Device',
				serialNumber: 'SN123',
				status: 'InRepair'
			});

			const { container } = render(DeviceForm, {
				props: {
					...defaultProps,
					mode: 'edit',
					initialData,
					disabledFields: ['status']
				}
			});

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with validation errors', async () => {
			const user = userEvent.setup();
			const { container } = render(DeviceForm, { props: defaultProps });

			// Trigger validation error
			const nameInput = screen.getByLabelText(/devices.columns.name/i);
			await user.click(nameInput);
			await user.tab();

			await waitFor(() => {
				expect(screen.getByText(/required/i)).toBeInTheDocument();
			});

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with disabled fields', async () => {
			const { container } = render(DeviceForm, {
				props: {
					...defaultProps,
					disabledFields: ['name', 'serialNumber']
				}
			});

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});
