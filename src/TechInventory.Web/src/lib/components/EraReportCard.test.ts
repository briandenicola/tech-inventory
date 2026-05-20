import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { writable } from 'svelte/store';
import { axe } from 'vitest-axe';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import EraReportCard from './EraReportCard.svelte';

const mocks = vi.hoisted(() => ({
	eras: vi.fn(),
	fetchReferenceData: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	default: {
		reports: {
			eras: mocks.eras
		}
	}
}));

vi.mock('$lib/stores/referenceData', () => ({
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
	fetchReferenceData: mocks.fetchReferenceData
}));

import { referenceDataStore } from '$lib/stores/referenceData';

describe('EraReportCard', () => {
	beforeEach(() => {
		mocks.eras.mockReset();
		mocks.fetchReferenceData.mockReset();
		referenceDataStore.set({
			brands: [],
			categories: [
				{ id: 'cat-phones', name: 'Phones' },
				{ id: 'cat-laptops', name: 'Laptops' }
			],
			owners: [],
			locations: [],
			networks: [],
			tags: [],
			isLoading: false,
			error: null
		});
	});

	it('renders loading state while the era report is pending', () => {
		mocks.eras.mockImplementation(() => new Promise(() => {}));
		render(EraReportCard);

		expect(screen.getByText(/loading…/i)).toBeInTheDocument();
	});

	it('renders era decades data correctly', async () => {
		mocks.eras.mockResolvedValue({
			decades: [
				{
					decade: '2020s',
					startYear: 2020,
					endYear: 2029,
					deviceCount: 5,
					totalValue: 3450,
					sampleDevices: ['iPhone 14 Pro', 'MacBook Pro 14']
				}
			],
			asOfDate: '2026-05-20',
			appliedCategoryId: null
		});

		render(EraReportCard);

		expect(await screen.findAllByText('2020s')).not.toHaveLength(0);
		expect(screen.getAllByText('$3,450.00')).not.toHaveLength(0);
		expect(screen.getAllByText('iPhone 14 Pro')).not.toHaveLength(0);
		expect(mocks.eras).toHaveBeenCalledWith(null);
	});

	it('toggles the category filter API call', async () => {
		mocks.eras.mockResolvedValue({
			decades: [{ decade: '2020s', startYear: 2020, endYear: 2029, deviceCount: 2, totalValue: 1500, sampleDevices: ['ThinkPad X1'] }],
			asOfDate: '2026-05-20',
			appliedCategoryId: null
		});

		render(EraReportCard);
		const user = userEvent.setup();

		await screen.findAllByText('2020s');
		await user.selectOptions(screen.getByRole('combobox', { name: /category filter/i }), 'cat-phones');

		await waitFor(() => expect(mocks.eras).toHaveBeenLastCalledWith('cat-phones'));

		await user.click(screen.getByRole('button', { name: /all categories/i }));
		await waitFor(() => expect(mocks.eras).toHaveBeenLastCalledWith(null));
	});

	it('renders the empty state when no decades are returned', async () => {
		mocks.eras.mockResolvedValue({ decades: [], asOfDate: '2026-05-20', appliedCategoryId: null });
		render(EraReportCard);

		expect(await screen.findByText(/no devices found for this era/i)).toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		mocks.eras.mockResolvedValue({
			decades: [
				{ decade: '1990s', startYear: 1990, endYear: 1999, deviceCount: 3, totalValue: 900, sampleDevices: ['Game Boy Color'] }
			],
			asOfDate: '2026-05-20',
			appliedCategoryId: null
		});

		const { container } = render(EraReportCard);
		await screen.findAllByText('1990s');

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});
