import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';

const mocks = vi.hoisted(() => {
	function createStore<T>(initialValue: T) {
		let value = initialValue;
		const subscribers = new Set<(next: T) => void>();

		return {
			subscribe(run: (next: T) => void) {
				run(value);
				subscribers.add(run);
				return () => subscribers.delete(run);
			},
			set(next: T) {
				value = next;
				subscribers.forEach((subscriber) => subscriber(value));
			}
		};
	}

	return {
		authStore: createStore({
			currentUser: {
				id: 'owner-1',
				entraObjectId: null,
				displayName: 'Test User',
				role: 'Admin' as 'Admin' | 'Member' | 'Viewer'
			},
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra' as const,
			mustChangePassword: false
		})
	};
});

vi.mock('$lib/stores/auth', () => ({ authStore: mocks.authStore }));

import DeviceListAddActions from './DeviceListAddActions.svelte';

function setCurrentUser(role: 'Admin' | 'Member' | 'Viewer'): void {
	mocks.authStore.set({
		currentUser: {
			id: 'owner-1',
			entraObjectId: null,
			displayName: 'Test User',
			role
		},
		isAuthenticated: true,
		isLoading: false,
		error: null,
		authMethod: 'entra',
		mustChangePassword: false
	});
}

describe('DeviceListAddActions', () => {
	afterEach(() => {
		cleanup();
	});

	beforeEach(() => {
		setCurrentUser('Admin');
	});

	it.each(['Admin', 'Member'] as const)('renders the add-device FAB for %s users', (role) => {
		setCurrentUser(role);
		render(DeviceListAddActions);

		const fab = screen.getByLabelText('Add device');
		expect(fab).toBeInTheDocument();
		expect(fab.tagName).toBe('A');
		expect(fab).toHaveAttribute('href', '/devices/new');
	});

	it('hides the add-device FAB for Viewer users', () => {
		setCurrentUser('Viewer');
		render(DeviceListAddActions);

		expect(screen.queryByLabelText('Add device')).not.toBeInTheDocument();
	});

	it('hides the FAB while a device detail modal is open', () => {
		render(DeviceListAddActions, { props: { detailOpen: true } });

		expect(screen.queryByLabelText('Add device')).not.toBeInTheDocument();
	});

	it('has no accessibility violations for authorized users', async () => {
		const { container } = render(DeviceListAddActions);

		expect(await axe(container)).toHaveNoViolations();
	});
});
