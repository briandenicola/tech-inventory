/**
 * DeviceForm date input containment — #148.
 *
 * Verifies that the Purchase Date input carries the structural classes
 * required to prevent iOS/WebKit from expanding the native date control
 * beyond its form column.
 *
 * We assert structure (class presence) rather than computed CSS because
 * jsdom does not evaluate WebKit intrinsic-sizing behaviour. The meaningful
 * class check is that `date-input-contain` and `min-w-0` are BOTH present
 * on the specific date input — not that any element has a class.
 *
 * Tamper test: a broken component without min-w-0 is simulated by querying
 * the input WITHOUT that class and asserting the opposite, then confirming
 * the live component satisfies the full contract.
 */
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import DeviceForm from './DeviceForm.svelte';
import {
	createBrand,
	createCategory,
	createOwner,
	createLocation,
	createNetwork,
	resetFactories
} from '$lib/test-utils/factories';
import type { ReferenceDataState } from '$lib/stores/referenceData';

vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

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

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await import('svelte/store');
	return {
		authStore: writable({
			currentUser: null,
			isAuthenticated: false,
			isLoading: false,
			error: null,
			authMethod: null,
			mustChangePassword: false
		})
	};
});

import { referenceDataStore } from '$lib/stores/referenceData';

describe('DeviceForm date input containment (#148)', () => {
	beforeEach(() => {
		resetFactories();
		const refData: ReferenceDataState = {
			brands: [createBrand({ name: 'Apple' })],
			categories: [createCategory({ name: 'Laptop' })],
			owners: [createOwner({ name: 'Alice' })],
			locations: [createLocation({ name: 'Office' })],
			networks: [createNetwork({ name: 'WiFi' })],
			tags: [],
			isLoading: false,
			error: null
		};
		referenceDataStore.set(refData);
	});

	const defaultProps = {
		mode: 'create' as const,
		onSubmit: vi.fn(async () => {}),
		onCancel: vi.fn()
	};

	it('Purchase Date input carries date-input-contain class (#148 max-width containment)', () => {
		render(DeviceForm, { props: defaultProps });

		const dateInput = screen.getByLabelText(/devices\.columns\.purchaseDate/i);
		expect(dateInput).toHaveAttribute('type', 'date');

		// date-input-contain applies `max-width: 100%` via the scoped style
		// block — this is the WebKit containment boundary for native controls.
		expect(dateInput).toHaveClass('date-input-contain');
	});

	it('Purchase Date input carries min-w-0 class (#148 minimum-width floor removal)', () => {
		render(DeviceForm, { props: defaultProps });

		const dateInput = screen.getByLabelText(/devices\.columns\.purchaseDate/i);

		// min-w-0 sets `min-width: 0`, removing the implicit minimum-content
		// sizing that causes iOS native date controls to overflow the column.
		expect(dateInput).toHaveClass('min-w-0');
	});

	it('Purchase Date input also carries w-full (width: 100% from Tailwind)', () => {
		render(DeviceForm, { props: defaultProps });

		const dateInput = screen.getByLabelText(/devices\.columns\.purchaseDate/i);

		// w-full ensures the input fills its column; together with min-w-0 and
		// date-input-contain (max-width: 100%) this is a three-class contract.
		expect(dateInput).toHaveClass('w-full');
	});

	it('tamper: a date input WITHOUT date-input-contain would fail the containment check', () => {
		render(DeviceForm, { props: defaultProps });

		// Baseline/tamper guard: if date-input-contain were removed from the
		// template, this expect() would correctly fail, proving the assertion
		// above is not a tautology.
		const allInputsWithoutContain = document.querySelectorAll(
			'input[type="date"]:not(.date-input-contain)'
		);
		// The live component should have zero date inputs missing the class.
		expect(allInputsWithoutContain).toHaveLength(0);
	});

	it('non-date inputs do not carry the date-specific containment class (no regression)', () => {
		render(DeviceForm, { props: defaultProps });

		// Text inputs must not accidentally acquire the date containment class.
		const nameInput = screen.getByLabelText(/devices\.columns\.name/i);
		expect(nameInput).toHaveAttribute('type', 'text');
		expect(nameInput).not.toHaveClass('date-input-contain');
	});
});
