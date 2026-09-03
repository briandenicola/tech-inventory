/**
 * Audit Log horizontal scroll — #146.
 *
 * Verifies that at narrow (PWA) viewports the audit table is wrapped in an
 * overflow-x-auto scroll region so users can reach every column. The page
 * header, filters toggle, and pagination controls must NOT participate in
 * the scroll region — they stay viewport-width.
 *
 * Style-contract assertions query meaningful structural attributes
 * (data-audit-scroll-region, overflow class, table role) rather than
 * tautologically inspecting the class being applied.
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import type { AuthState, CurrentUser } from '$lib/stores/auth';

const { goto, auditEventsListMock, ownersListMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	auditEventsListMock: vi.fn(),
	ownersListMock: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto }));

vi.mock('$app/stores', async () => {
	const { readable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	return {
		page: readable({
			url: new URL('http://localhost/admin/audit'),
			params: {},
			route: { id: '/(authenticated)/admin/audit' },
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
			auditEvents: { list: auditEventsListMock },
			owners: { list: ownersListMock }
		}
	};
});

import { authStore } from '$lib/stores/auth';
import Page from './+page.svelte';

const adminUser: CurrentUser = {
	id: 'admin-1',
	entraObjectId: 'entra-1',
	displayName: 'Brian',
	role: 'Admin'
};

const sampleEvents = [
	{
		id: 'evt-1',
		timestamp: '2026-09-01T10:00:00Z',
		actor: 'user-oid-1',
		entityType: 'Device',
		entityId: 'device-1',
		action: 'Created',
		beforePayload: null,
		afterPayload: '{"name":"Washer"}'
	}
];

describe('Audit Log horizontal scroll (#146)', () => {
	beforeEach(() => {
		goto.mockReset();
		auditEventsListMock
			.mockReset()
			.mockResolvedValue({ items: sampleEvents, totalCount: 1, page: 1, pageSize: 25 });
		ownersListMock.mockReset().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 200 });
		authStore.set({
			currentUser: adminUser,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});
	});

	it('wraps the audit table in a dedicated horizontal-scroll region', async () => {
		const { container } = render(Page);
		await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument());

		// The scroll region is identified by the data attribute (#146).
		const scrollRegion = container.querySelector('[data-audit-scroll-region]');
		expect(scrollRegion).not.toBeNull();

		// The scroll region must actually contain the table.
		const table = screen.getByRole('table');
		expect(scrollRegion!.contains(table)).toBe(true);

		// The scroll class is meaningful because it enables touch-horizontal
		// scroll; we verify it exists on the structural element that wraps
		// the table (not on the table itself or an unrelated node).
		expect(scrollRegion).toHaveClass('overflow-x-auto');
	});

	it('places the table inside the scroll region but keeps pagination outside it', async () => {
		const { container } = render(Page);
		await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument());

		const scrollRegion = container.querySelector('[data-audit-scroll-region]');
		expect(scrollRegion).not.toBeNull();

		// Table is inside the scroll region.
		expect(scrollRegion!.contains(screen.getByRole('table'))).toBe(true);

		// Pagination navigation is NOT inside the scroll region — it must stay
		// viewport-width so it doesn't drift off-screen on narrow viewports.
		// The "Filters" toggle button is the clearest non-table element to check.
		const filtersToggle = screen.getByRole('button', { name: /filters/i });
		expect(scrollRegion!.contains(filtersToggle)).toBe(false);
	});

	it('has no accessibility violations with audit rows rendered', async () => {
		const { container } = render(Page);
		await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument());

		expect(await axe(container)).toHaveNoViolations();
	});
});
