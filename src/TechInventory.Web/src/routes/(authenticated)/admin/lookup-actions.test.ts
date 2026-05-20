import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen } from '@testing-library/svelte';
import type {
	BrandResponse,
	CategoryResponse,
	LocationResponse,
	NetworkResponse
} from '$lib/api/types';

const mocks = vi.hoisted(() => {
	const createStore = (value: unknown) => ({
		subscribe(run: (next: unknown) => void) {
			run(value);
			return () => undefined;
		}
	});

	return {
		goto: vi.fn(),
		registerPullToRefresh: vi.fn(() => () => undefined),
		addToast: vi.fn(),
		fetchReferenceData: vi.fn(),
		listBrands: vi.fn(),
		listCategories: vi.fn(),
		listLocations: vi.fn(),
		listNetworks: vi.fn(),
		page: createStore({ url: new URL('http://localhost/admin') }),
		authStore: createStore({ currentUser: { role: 'Admin', displayName: 'Admin' } }),
		referenceDataStore: createStore({
			brands: [],
			categories: [],
			locations: [],
			networks: []
		})
	};
});

vi.mock('$app/navigation', () => ({ goto: mocks.goto }));
vi.mock('$app/stores', () => ({ page: mocks.page }));
vi.mock('$lib/stores/auth', () => ({ authStore: mocks.authStore }));
vi.mock('$lib/stores/pullToRefresh', () => ({
	registerPullToRefresh: mocks.registerPullToRefresh
}));
vi.mock('$lib/stores/toast', () => ({ addToast: mocks.addToast }));
vi.mock('$lib/stores/referenceData', () => ({
	fetchReferenceData: mocks.fetchReferenceData,
	referenceDataStore: mocks.referenceDataStore
}));
vi.mock('$lib/api/client', () => ({
	default: {
		brands: { list: mocks.listBrands },
		categories: { list: mocks.listCategories },
		locations: { list: mocks.listLocations },
		networks: { list: mocks.listNetworks }
	}
}));

import BrandsPage from './brands/+page.svelte';
import CategoriesPage from './categories/+page.svelte';
import LocationsPage from './locations/+page.svelte';
import NetworksPage from './networks/+page.svelte';

const activeBrand: BrandResponse = {
	id: 'brand-1',
	name: 'Apple',
	website: 'https://apple.example',
	notes: 'Phones',
	isActive: true
};

const activeCategory: CategoryResponse = {
	id: 'category-1',
	name: 'Phones',
	parentId: null,
	icon: '📱',
	depth: 1,
	isActive: true
};

const activeLocation: LocationResponse = {
	id: 'location-1',
	name: 'Office',
	type: 'Home',
	isActive: true
};

const activeNetwork: NetworkResponse = {
	id: 'network-1',
	name: 'Main Wi-Fi',
	description: 'Primary SSID',
	isActive: true
};

async function expectLookupRowActions(pageComponent: typeof BrandsPage) {
	render(pageComponent);

	expect(await screen.findAllByRole('button', { name: /^Edit$/i })).not.toHaveLength(0);
	expect(await screen.findAllByRole('button', { name: /^Deactivate$/i })).not.toHaveLength(0);
	expect(screen.queryByRole('button', { name: /^Merge$/i })).not.toBeInTheDocument();
}

describe('admin lookup row actions', () => {
	beforeEach(() => {
		mocks.goto.mockReset();
		mocks.registerPullToRefresh.mockClear();
		mocks.addToast.mockReset();
		mocks.fetchReferenceData.mockReset();
		mocks.listBrands.mockReset();
		mocks.listCategories.mockReset();
		mocks.listLocations.mockReset();
		mocks.listNetworks.mockReset();

		mocks.listBrands.mockResolvedValue({ items: [activeBrand], totalCount: 1 });
		mocks.listCategories.mockResolvedValue({ items: [activeCategory] });
		mocks.listLocations.mockResolvedValue({ items: [activeLocation], totalCount: 1 });
		mocks.listNetworks.mockResolvedValue({ items: [activeNetwork], totalCount: 1 });
	});

	afterEach(() => {
		cleanup();
	});

	it('shows edit and deactivate only on brand rows', async () => {
		await expectLookupRowActions(BrandsPage);
	});

	it('shows edit and deactivate only on category rows', async () => {
		await expectLookupRowActions(CategoriesPage);
	});

	it('shows edit and deactivate only on location rows', async () => {
		await expectLookupRowActions(LocationsPage);
	});

	it('shows edit and deactivate only on network rows', async () => {
		await expectLookupRowActions(NetworksPage);
	});
});
