import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { writable } from 'svelte/store';
import { axe } from 'vitest-axe';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import TimelineReport from './TimelineReport.svelte';

const mocks = vi.hoisted(() => ({
	timeline: vi.fn(),
	fetchReferenceData: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	default: {
		reports: {
			timeline: mocks.timeline
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

describe('TimelineReport', () => {
	beforeEach(() => {
		mocks.timeline.mockReset();
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

	it('renders loading state while the timeline report is pending', () => {
		mocks.timeline.mockImplementation(() => new Promise(() => {}));
		render(TimelineReport);

		expect(screen.getByText(/building your timeline/i)).toBeInTheDocument();
	});

	it('renders grouped category data and active or disposed bars', async () => {
		mocks.timeline.mockResolvedValue({
			entries: [
				{ deviceName: 'iPhone 15', brand: 'Apple', purchaseDate: '2023-09-22', disposalDate: null, groupLabel: 'Phones', estimatedValue: 999 },
				{ deviceName: 'Surface Laptop', brand: 'Microsoft', purchaseDate: '2020-05-01', disposalDate: '2024-05-01', groupLabel: 'Computers', estimatedValue: 1299 }
			],
			asOfDate: '2026-05-20',
			groupBy: 'Category',
			appliedCategoryId: null
		});

		const { container } = render(TimelineReport);

		expect(await screen.findByText('Surface Laptop')).toBeInTheDocument();
		expect(screen.getByRole('heading', { name: 'Phones' })).toBeInTheDocument();
		expect(screen.getByRole('heading', { name: 'Computers' })).toBeInTheDocument();
		expect(mocks.timeline).toHaveBeenCalledWith({ categoryId: null, groupBy: 'Category' });
		expect(container.querySelector('[data-state="active"]')).toBeTruthy();
		expect(container.querySelector('[data-state="disposed"]')).toBeTruthy();
	});

	it('updates filters and grouping', async () => {
		mocks.timeline.mockResolvedValue({
			entries: [
				{ deviceName: 'ThinkPad X1', brand: 'Lenovo', purchaseDate: '2021-01-01', disposalDate: null, groupLabel: 'Laptops', estimatedValue: 1400 }
			],
			asOfDate: '2026-05-20',
			groupBy: 'Category',
			appliedCategoryId: null
		});

		render(TimelineReport);
		const user = userEvent.setup();

		await screen.findByText('ThinkPad X1');
		await user.selectOptions(screen.getByRole('combobox', { name: /category filter/i }), 'cat-phones');
		await waitFor(() => expect(mocks.timeline).toHaveBeenLastCalledWith({ categoryId: 'cat-phones', groupBy: 'Category' }));

		await user.click(screen.getByRole('button', { name: /by owner/i }));
		await waitFor(() => expect(mocks.timeline).toHaveBeenLastCalledWith({ categoryId: 'cat-phones', groupBy: 'Owner' }));
	});

	it('renders the empty state when no entries are returned', async () => {
		mocks.timeline.mockResolvedValue({ entries: [], asOfDate: '2026-05-20', groupBy: 'Category', appliedCategoryId: null });
		render(TimelineReport);

		expect(await screen.findByText(/no devices with purchase dates to show/i)).toBeInTheDocument();
	});

	it('renders the error state when loading fails', async () => {
		mocks.timeline.mockRejectedValue(new Error('boom'));
		render(TimelineReport);

		expect(await screen.findByText(/couldn't load timeline data/i)).toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		mocks.timeline.mockResolvedValue({
			entries: [
				{ deviceName: 'Nest Hub', brand: 'Google', purchaseDate: '2022-06-01', disposalDate: null, groupLabel: 'Smart Home', estimatedValue: 99 }
			],
			asOfDate: '2026-05-20',
			groupBy: 'Category',
			appliedCategoryId: null
		});

		const { container } = render(TimelineReport);
		await screen.findByText('Smart Home');

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});
