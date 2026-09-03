/**
 * DeviceForm + DeviceDetailModal — field regression guards (#148).
 *
 * Issue: On iOS WebKit at narrow installed-PWA viewports (320–390 px), native
 * input[type=date] and input[type=number] can assert an intrinsic minimum
 * width wider than their containing column, forcing the form wider than the
 * modal and causing horizontal overflow.
 *
 * Required containment contracts (all source/class assertions — jsdom cannot
 * compute intrinsic sizing or iOS WebKit layout):
 *
 *   DeviceForm:
 *     • purchaseDate input:    date-input-contain, min-w-0, w-full (currently present — tamper-tested)
 *     • purchasePrice input:   min-w-0, w-full (min-w-0 is a PRE-FIX guard for #148)
 *     • notes textarea:        w-full (currently present — tamper-tested)
 *     • purchaseDate label:    max-w-full (PRE-FIX guard — label text must not force overflow)
 *
 *   DeviceDetailModal ancestor chain:
 *     • modal surface div:     overflow-hidden (currently present — tamper-tested)
 *     • scroll region div:     overflow-y-auto (currently present — tamper-tested)
 *
 * PRE-FIX STATUS:
 *   purchasePrice `min-w-0` guard will fail until Vasquez adds it (#148).
 *   purchaseDate label `max-w-full` guard will fail until Vasquez adds it (#148).
 *
 * TAMPER-TESTED guards: date-input-contain, min-w-0, w-full on date input;
 *   notes w-full; modal surface overflow-hidden; scroll region overflow-y-auto.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import DeviceForm from './DeviceForm.svelte';
import type { ReferenceDataState } from '$lib/stores/referenceData';

vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await import('svelte/store');
	return {
		referenceDataStore: writable<ReferenceDataState>({
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

const baseProps = {
	mode: 'create' as const,
	onSubmit: vi.fn(async () => {}),
	onCancel: vi.fn()
};

describe('DeviceForm — #148 date input containment contract', () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The date input must carry `date-input-contain` (scoped CSS class that
	// sets max-width:100%) to cap the control at its column width on iOS.
	it('purchaseDate input carries date-input-contain class', () => {
		render(DeviceForm, { props: baseProps });
		const dateInput = screen.getByLabelText(/devices.columns.purchaseDate/i);
		expect(
			dateInput.className,
			'date input must carry date-input-contain (max-width:100% safety net for iOS WebKit)'
		).toMatch(/\bdate-input-contain\b/);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// min-w-0 removes the browser's minimum-content-size floor that lets
	// input[type=date] assert wider-than-container intrinsic widths on iOS.
	it('purchaseDate input carries min-w-0', () => {
		render(DeviceForm, { props: baseProps });
		const dateInput = screen.getByLabelText(/devices.columns.purchaseDate/i);
		expect(
			dateInput.className,
			'date input must carry min-w-0 to suppress iOS intrinsic minimum-width floor'
		).toMatch(/\bmin-w-0\b/);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// w-full makes the control fill its column; combined with min-w-0 and
	// date-input-contain this is the complete three-class containment set (#148).
	it('purchaseDate input carries w-full', () => {
		render(DeviceForm, { props: baseProps });
		const dateInput = screen.getByLabelText(/devices.columns.purchaseDate/i);
		expect(dateInput.className, 'date input must carry w-full').toMatch(/\bw-full\b/);
	});
});

describe('DeviceForm — #148 price/number input containment contract', () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// input[type=number] also has intrinsic minimum widths on some browsers.
	// w-full ensures it fills its grid column.
	it('purchasePrice input carries w-full', () => {
		render(DeviceForm, { props: baseProps });
		const priceInput = screen.getByLabelText(/devices.columns.purchasePrice/i);
		expect(priceInput.className, 'price input must carry w-full').toMatch(/\bw-full\b/);
	});

	// ── PRE-FIX sentinel ──────────────────────────────────────────────────────
	// min-w-0 prevents number inputs from asserting a wider intrinsic minimum
	// than their grid column. Currently absent — this test WILL FAIL until
	// Vasquez adds it as part of #148.
	it('purchasePrice input carries min-w-0 (pre-fix: will fail until Vasquez lands #148)', () => {
		render(DeviceForm, { props: baseProps });
		const priceInput = screen.getByLabelText(/devices.columns.purchasePrice/i);
		expect(
			priceInput.className,
			'price input must carry min-w-0 to prevent number-input intrinsic-width overflow on iOS'
		).toMatch(/\bmin-w-0\b/);
	});
});

describe('DeviceForm — #148 notes textarea containment contract', () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The notes textarea must fill its column (w-full) so it cannot grow
	// wider than the form on iOS.
	it('notes textarea carries w-full', () => {
		render(DeviceForm, { props: baseProps });
		const notesTextarea = screen.getByLabelText(/devices.columns.notes/i);
		expect(notesTextarea.className, 'notes textarea must carry w-full').toMatch(/\bw-full\b/);
	});
});

describe('DeviceForm — #148 purchaseDate label containment contract', () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	// ── PRE-FIX sentinel ──────────────────────────────────────────────────────
	// The label text for purchaseDate must not force the row wider than the
	// modal.  max-w-full caps the label at its parent's width. Currently absent
	// — this test WILL FAIL until Vasquez adds max-w-full to the label as part
	// of #148.
	it('purchaseDate label carries max-w-full to prevent label text from forcing overflow (pre-fix: will fail until #148 lands)', () => {
		render(DeviceForm, { props: baseProps });
		const dateInput = screen.getByLabelText(/devices.columns.purchaseDate/i);
		const label = dateInput.closest('div')?.querySelector('label[for="purchaseDate"]');
		expect(label, 'purchaseDate label must exist').toBeTruthy();
		expect(
			label!.className,
			'purchaseDate label must carry max-w-full to prevent label-driven overflow at narrow widths'
		).toMatch(/\bmax-w-full\b/);
	});
});
