import { fireEvent, render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import { describe, expect, it, vi } from 'vitest';
import WarrantyExpiryPanel from './WarrantyExpiryPanel.svelte';

describe('WarrantyExpiryPanel', () => {
	const baseProps = {
		items: [{
			deviceId: '00000000-0000-4000-8000-000000000001',
			deviceName: 'MacBook Pro',
			brandName: 'Apple',
			purchaseDate: '2024-01-15',
			warrantyExpiry: '2026-07-15',
			daysUntilExpiry: 55,
			tone: 'warning' as const
		}],
		totalCount: 1,
		selectedWindow: 90,
		isLoading: false,
		error: null,
		sortDirection: 'asc' as const,
		onWindowChange: vi.fn(),
		onRetry: vi.fn(),
		onSortToggle: vi.fn()
	};

	it('renders device links and filter controls', async () => {
		render(WarrantyExpiryPanel, { props: baseProps });

		expect(screen.getByRole('link', { name: /macbook pro/i })).toHaveAttribute(
			'href',
			'/devices/00000000-0000-4000-8000-000000000001'
		);

		await fireEvent.click(screen.getByRole('button', { name: /30 days/i }));
		expect(baseProps.onWindowChange).toHaveBeenCalledWith(30);
	});

	it('has no accessibility violations', async () => {
		const { container } = render(WarrantyExpiryPanel, { props: baseProps });
		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});
