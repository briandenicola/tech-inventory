/**
 * /admin/audit — C-18 (route-level axe harness; one of the three routes not
 * otherwise composed in a Vitest test — see coverage-migration.md C-18).
 * Only the default (empty) view + Admin-gating are exercised — the filter
 * form/pagination/diff-drawer interactions are pre-existing, richer surface
 * area out of scope for this gap-closing pass.
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
		default: { ...actual.default, auditEvents: { list: auditEventsListMock }, owners: { list: ownersListMock } }
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

describe('/admin/audit (C-18 axe)', () => {
	beforeEach(() => {
		goto.mockReset();
		auditEventsListMock.mockReset().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 25 });
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

	it('renders the audit log title and an empty-state message when there are no events', async () => {
		render(Page);

		expect(screen.getByRole('heading', { name: 'Audit Log' })).toBeInTheDocument();
		await waitFor(() => expect(auditEventsListMock).toHaveBeenCalled());
		expect(goto).not.toHaveBeenCalled();
	});

	it('has no accessibility violations on the default (empty) view', async () => {
		const { container } = render(Page);
		await waitFor(() => expect(auditEventsListMock).toHaveBeenCalled());

		expect(await axe(container)).toHaveNoViolations();
	});
});
