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
			isLoading: false,
			error: null
		})
	};
});

import { referenceDataStore } from '$lib/stores/referenceData';

describe('DeviceForm', () => {
	beforeEach(() => {
		resetFactories();

		// Populate reference data store with test data
		const refData: ReferenceDataState = {
			brands: [createBrand({ name: 'Apple' }), createBrand({ name: 'Dell' })],
			categories: [createCategory({ name: 'Laptop' }), createCategory({ name: 'Phone' })],
			owners: [createOwner({ name: 'Alice' }), createOwner({ name: 'Bob' })],
			locations: [createLocation({ name: 'Office' }), createLocation({ name: 'Home' })],
			networks: [createNetwork({ name: 'WiFi-Main' }), createNetwork({ name: 'WiFi-Guest' })],
			isLoading: false,
			error: null
		};
		referenceDataStore.set(refData);
	});

	const mockOnSubmit = vi.fn<(data: import('$lib/schemas/device').DeviceCreateInput) => Promise<void>>(async () => {});
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
			expect(screen.getByLabelText(/devices.columns.owner/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.location/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.network/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.purchaseDate/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.purchasePrice/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.currency/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.columns.notes/i)).toBeInTheDocument();
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

			const currencySelect = screen.getByLabelText(/devices.columns.currency/i) as HTMLSelectElement;
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
	});

	describe('edit mode', () => {
		it('pre-populates form with initialData', () => {
			const initialData = createDeviceCreateInput({
				name: 'Existing Device',
				serialNumber: 'SN123456',
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

			const priceInput = screen.getByLabelText(/devices.columns.purchasePrice/i) as HTMLInputElement;
			expect(priceInput.value).toBe('999.99');
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
	});

	describe('submit behavior', () => {
		it('enables submit button after form becomes dirty and valid', async () => {
			const user = userEvent.setup();
			render(DeviceForm, { props: defaultProps });

			const submitButton = screen.getByRole('button', { name: /devices.form.create/i });
			expect(submitButton).toBeDisabled();

			// Fill required fields
			const nameInput = screen.getByLabelText(/devices.columns.name/i);
			await user.type(nameInput, 'New Device');

			const brandSelect = screen.getByLabelText(/devices.columns.brand/i);
			await user.selectOptions(brandSelect, ['brand-00004000-8000-000000000000']);

			const categorySelect = screen.getByLabelText(/devices.columns.category/i);
			await user.selectOptions(categorySelect, ['category-004000-8000-000000000000']);

			// Submit should be enabled
			await waitFor(() => {
				expect(submitButton).not.toBeDisabled();
			});
		});

		it('calls onSubmit with parsed data on valid submission', async () => {
			const user = userEvent.setup();
			const onSubmit = vi.fn<(data: import('$lib/schemas/device').DeviceCreateInput) => Promise<void>>(async () => {});

			render(DeviceForm, {
				props: {
					...defaultProps,
					onSubmit
				}
			});

			// Fill required fields
			await user.type(screen.getByLabelText(/devices.columns.name/i), 'Test Device');
			await user.selectOptions(
				screen.getByLabelText(/devices.columns.brand/i),
				'brand-00004000-8000-000000000000'
			);
			await user.selectOptions(
				screen.getByLabelText(/devices.columns.category/i),
				'category-004000-8000-000000000000'
			);

			const submitButton = screen.getByRole('button', { name: /devices.form.create/i });
			await waitFor(() => expect(submitButton).not.toBeDisabled());

			await user.click(submitButton);

			await waitFor(() => {
				expect(onSubmit).toHaveBeenCalledTimes(1);
				expect(onSubmit).toHaveBeenCalledWith(
					expect.objectContaining({
						name: 'Test Device',
						brandId: 'brand-00004000-8000-000000000000',
						categoryId: 'category-004000-8000-000000000000'
					})
				);
			});
		});

		it('disables submit button while submitting', async () => {
			const user = userEvent.setup();
			const onSubmit = vi.fn<(data: import('$lib/schemas/device').DeviceCreateInput) => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(DeviceForm, {
				props: {
					...defaultProps,
					onSubmit
				}
			});

			// Fill and submit
			await user.type(screen.getByLabelText(/devices.columns.name/i), 'Test');
			await user.selectOptions(
				screen.getByLabelText(/devices.columns.brand/i),
				'brand-00004000-8000-000000000000'
			);
			await user.selectOptions(
				screen.getByLabelText(/devices.columns.category/i),
				'category-004000-8000-000000000000'
			);

			const submitButton = screen.getByRole('button', { name: /devices.form.create/i });
			await waitFor(() => expect(submitButton).not.toBeDisabled());

			await user.click(submitButton);

			// Should be disabled during submission
			expect(submitButton).toBeDisabled();
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
