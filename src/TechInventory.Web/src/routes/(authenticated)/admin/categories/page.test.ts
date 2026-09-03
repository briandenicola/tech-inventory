/**
 * /admin/categories — #132 (children/inactive categories were invisible
 * because the page consumed the paginated, root-only `/api/v1/categories`
 * endpoint and discarded each root's nested `children`; search inherited the
 * same blind spot since it only ever scanned that root-only flat array).
 *
 * Covers: full-tree visibility for children/grandchildren, inactive-category
 * rendering (badge + Show Inactive gating), recursive case-insensitive
 * partial search with ancestor-context preservation, the inline 409
 * duplicate-conflict message, the Show Inactive toggle wiring, and a zero
 * axe-violations check on the loaded (non-empty) tree view.
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import type { AuthState, CurrentUser } from '$lib/stores/auth';
import type { components } from '$lib/api/generated/types';

type CategoryResponse = components['schemas']['CategoryResponse'];

const { goto, categoriesTreeMock, categoriesCreateMock, categoriesUpdateMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	categoriesTreeMock: vi.fn(),
	categoriesCreateMock: vi.fn(),
	categoriesUpdateMock: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto }));

vi.mock('$app/stores', async () => {
	const { readable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	return {
		page: readable({
			url: new URL('http://localhost/admin/categories'),
			params: {},
			route: { id: '/(authenticated)/admin/categories' },
			status: 200,
			error: null,
			data: {},
			form: null
		})
	};
});

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	const initialAuthState: AuthState = {
		currentUser: null,
		isAuthenticated: false,
		isLoading: false,
		error: null,
		authMethod: null,
		mustChangePassword: false
	};
	return { authStore: writable(initialAuthState) };
});

vi.mock('$lib/api/client', async () => {
	const actual = await vi.importActual<typeof import('$lib/api/client')>('$lib/api/client');
	return {
		...actual,
		default: {
			...actual.default,
			categories: {
				...actual.default.categories,
				tree: categoriesTreeMock,
				create: categoriesCreateMock,
				update: categoriesUpdateMock
			}
		}
	};
});

import { authStore } from '$lib/stores/auth';
import { ApiError } from '$lib/api/client';
import Page from './+page.svelte';

const adminUser: CurrentUser = {
	id: 'admin-1',
	entraObjectId: 'entra-1',
	displayName: 'Brian',
	role: 'Admin'
};

// A three-level tree — root "Electronics" -> child "Computers" (inactive) ->
// grandchild "Laptops" — exactly the shape the paginated root-only endpoint
// could never surface past the first level.
const ELECTRONICS_ID = '11111111-1111-4111-8111-111111111111';
const COMPUTERS_ID = '22222222-2222-4222-8222-222222222222';
const LAPTOPS_ID = '33333333-3333-4333-8333-333333333333';
const PHONES_ID = '44444444-4444-4444-8444-444444444444';

function buildTree(): CategoryResponse[] {
	return [
		{
			id: ELECTRONICS_ID,
			name: 'Electronics',
			parentId: null,
			depth: 1,
			icon: '🔌',
			isActive: true,
			children: [
				{
					id: COMPUTERS_ID,
					name: 'Computers',
					parentId: ELECTRONICS_ID,
					depth: 2,
					icon: '💻',
					isActive: false,
					children: [
						{
							id: LAPTOPS_ID,
							name: 'Laptops',
							parentId: COMPUTERS_ID,
							depth: 3,
							icon: '💻',
							isActive: true,
							children: []
						}
					]
				},
				{
					id: PHONES_ID,
					name: 'Phones',
					parentId: ELECTRONICS_ID,
					depth: 2,
					icon: '📱',
					isActive: true,
					children: []
				}
			]
		}
	];
}

describe('/admin/categories (#132)', () => {
	beforeEach(() => {
		goto.mockReset();
		categoriesTreeMock.mockReset().mockResolvedValue(buildTree());
		categoriesCreateMock.mockReset();
		categoriesUpdateMock.mockReset();
		authStore.set({
			currentUser: adminUser,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});
	});

	it('calls the full tree endpoint rather than the paginated root-only list', async () => {
		render(Page);

		await waitFor(() => expect(categoriesTreeMock).toHaveBeenCalled());
		expect(categoriesTreeMock).toHaveBeenCalledWith({ includeInactive: false });
	});

	it('renders every level of the tree, including nested children and grandchildren', async () => {
		render(Page);

		await waitFor(() => expect(screen.getByText('Electronics')).toBeInTheDocument());
		// Computers is inactive but includeInactive defaults false; the mock
		// still returns it (as a real API would honor includeInactive
		// server-side) so we can assert the *rendering* path handles it once
		// present — the toggle-wiring test below covers the request param.
		expect(screen.getByText('Computers')).toBeInTheDocument();
		expect(screen.getByText('Laptops')).toBeInTheDocument();
		expect(screen.getByText('Phones')).toBeInTheDocument();
	});

	it('renders an "Inactive" badge on deactivated categories without hiding them', async () => {
		render(Page);

		await waitFor(() => expect(screen.getByText('Computers')).toBeInTheDocument());
		const computersRow = screen.getByText('Computers').closest('div');
		expect(computersRow).not.toBeNull();
		expect(within(computersRow as HTMLElement).getByText('Inactive')).toBeInTheDocument();
	});

	it('toggles includeInactive via the Show Inactive checkbox and re-requests the tree', async () => {
		const user = userEvent.setup();
		render(Page);

		await waitFor(() => expect(categoriesTreeMock).toHaveBeenCalledTimes(1));
		const checkbox = screen.getByLabelText('Show Inactive');
		await user.click(checkbox);

		expect(goto).toHaveBeenCalledWith('?includeInactive=true', {
			replaceState: true,
			keepFocus: true,
			noScroll: true
		});
	});

	it('search partially and case-insensitively matches descendants at any depth, preserving ancestor context', async () => {
		const user = userEvent.setup();
		render(Page);

		await waitFor(() => expect(screen.getByText('Laptops')).toBeInTheDocument());

		const search = screen.getByPlaceholderText('Search categories...');
		// Partial, mixed-case, and matching only the grandchild — the bug
		// this covers is search never seeing past root-level items at all.
		await user.type(search, 'LAP');

		await waitFor(() => expect(screen.getByText('Laptops')).toBeInTheDocument());
		// Ancestors of the match must still be shown to preserve hierarchy
		// context, even though "Electronics" and "Computers" don't match "LAP".
		expect(screen.getByText('Electronics')).toBeInTheDocument();
		expect(screen.getByText('Computers')).toBeInTheDocument();
		// A sibling that doesn't match and isn't an ancestor of a match must
		// be filtered out.
		expect(screen.queryByText('Phones')).not.toBeInTheDocument();
	});

	it('shows an inline duplicate-conflict message under the Name field on a 409 response', async () => {
		const user = userEvent.setup();
		categoriesCreateMock.mockRejectedValue(
			new ApiError(409, 'Conflict', "Category with name 'Phones' already exists under the selected parent.")
		);
		render(Page);

		await waitFor(() => expect(screen.getByText('Electronics')).toBeInTheDocument());
		await user.click(screen.getByRole('button', { name: 'Add Category' }));

		await user.type(screen.getByLabelText('Category Name *'), 'Phones');
		const parentSelect = screen.getByLabelText('Parent Category (optional)');
		await user.selectOptions(parentSelect, ELECTRONICS_ID);
		await user.click(screen.getByRole('button', { name: 'Save' }));

		expect(
			await screen.findByText('A category named "Phones" already exists under Electronics.')
		).toBeInTheDocument();
	});

	it('has no accessibility violations once the tree has loaded', async () => {
		const { container } = render(Page);
		await waitFor(() => expect(screen.getByText('Electronics')).toBeInTheDocument());

		expect(await axe(container)).toHaveNoViolations();
	});
});
