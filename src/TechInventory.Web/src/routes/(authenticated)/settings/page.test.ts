/**
 * /settings — #135: the shipped "Coming soon" (F020b) placeholder card has
 * been removed. This asserts the placeholder is gone, the surrounding
 * Profile/Appearance/table-column sections still render without an orphaned
 * gap, and no other "coming soon"/internal feature-code text leaks into the
 * page.
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import type { AuthState, CurrentUser } from '$lib/stores/auth';

const { ownersMeMock, updateMyProfileMock } = vi.hoisted(() => ({
	ownersMeMock: vi.fn(),
	updateMyProfileMock: vi.fn()
}));

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

	return {
		authStore: writable(initialAuthState),
		updateCurrentUserDisplayName: vi.fn()
	};
});

vi.mock('$lib/api/client', async () => {
	const actual = await vi.importActual<typeof import('$lib/api/client')>('$lib/api/client');
	return {
		...actual,
		default: {
			...actual.default,
			owners: { ...actual.default.owners, me: ownersMeMock, updateMyProfile: updateMyProfileMock }
		}
	};
});

import { authStore } from '$lib/stores/auth';
import Page from './+page.svelte';

const adminUser: CurrentUser = {
	id: 'owner-1',
	entraObjectId: 'entra-1',
	displayName: 'Brian',
	role: 'Admin'
};

describe('/settings (#135 placeholder removal)', () => {
	beforeEach(() => {
		ownersMeMock.mockReset().mockResolvedValue({ id: 'owner-1', displayName: 'Brian' });
		updateMyProfileMock.mockReset();
		authStore.set({
			currentUser: adminUser,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		Object.defineProperty(window, 'matchMedia', {
			writable: true,
			value: vi.fn().mockImplementation(() => ({
				matches: false,
				media: '(prefers-color-scheme: dark)',
				addEventListener: vi.fn(),
				removeEventListener: vi.fn()
			}))
		});
	});

	it('does not render the "Coming soon" placeholder card or the F020b feature code', async () => {
		render(Page);

		await waitFor(() => expect(ownersMeMock).toHaveBeenCalled());
		await screen.findByRole('heading', { name: 'Profile' });

		expect(screen.queryByText('Coming soon')).not.toBeInTheDocument();
		expect(screen.queryByText(/F020b/)).not.toBeInTheDocument();
		expect(
			screen.queryByText(/avatar upload/i, { selector: 'p, h2' })
		).not.toBeInTheDocument();
	});

	it('keeps the surrounding Profile and Appearance sections intact with no orphaned gap', async () => {
		render(Page);

		await waitFor(() => expect(ownersMeMock).toHaveBeenCalled());

		expect(await screen.findByRole('heading', { name: 'Profile' })).toBeInTheDocument();
		expect(screen.getByRole('heading', { name: 'Appearance' })).toBeInTheDocument();
		expect(screen.getByLabelText('Display name')).toBeInTheDocument();
	});
});
