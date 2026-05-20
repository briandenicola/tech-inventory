import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';

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

import DeviceFilters from './DeviceFilters.svelte';

const defaultFilters = {
	page: 1,
	pageSize: 25
};

describe('DeviceFilters', () => {
	beforeEach(() => {
		document.body.style.overflow = '';
	});

	it('renders an accessible mobile drawer with a sticky header and footer when open', () => {
		const { container } = render(DeviceFilters, {
			props: {
				filters: defaultFilters,
				onFiltersChange: vi.fn(),
				isOpen: true,
				onClose: vi.fn()
			}
		});

		const dialog = screen.getByRole('dialog', { name: 'Filters' });
		expect(dialog).toHaveClass('h-dvh');
		expect(dialog).toHaveAttribute('aria-modal', 'true');
		expect(container.querySelector('div.sticky.top-0')).toBeInTheDocument();
		expect(container.querySelector('div.sticky.bottom-0')).toBeInTheDocument();
		expect(container.querySelector('div.flex-1.overflow-y-auto')).toBeInTheDocument();
		expect(container.innerHTML).toContain('safe-area-inset-top');
		expect(container.innerHTML).toContain('safe-area-inset-bottom');
	});

	it('focuses the close button and closes on Escape', async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();

		render(DeviceFilters, {
			props: {
				filters: defaultFilters,
				onFiltersChange: vi.fn(),
				isOpen: true,
				onClose
			}
		});

		const closeButton = screen.getByRole('button', { name: 'Close Filters' });
		expect(closeButton).toHaveFocus();

		await user.keyboard('{Escape}');

		expect(onClose).toHaveBeenCalledOnce();
	});

	it('has no accessibility violations when open', async () => {
		const { container } = render(DeviceFilters, {
			props: {
				filters: defaultFilters,
				onFiltersChange: vi.fn(),
				isOpen: true,
				onClose: vi.fn()
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});
});
