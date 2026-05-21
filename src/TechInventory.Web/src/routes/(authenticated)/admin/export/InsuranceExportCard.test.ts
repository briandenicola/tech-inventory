import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import InsuranceExportCard from './InsuranceExportCard.svelte';

const mocks = vi.hoisted(() => ({
	insurance: vi.fn(),
	triggerBlobDownload: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	default: {
		reports: {
			insurance: mocks.insurance
		}
	}
}));

vi.mock('$lib/utils/blobDownload', () => ({
	triggerBlobDownload: mocks.triggerBlobDownload
}));

describe('InsuranceExportCard', () => {
	beforeEach(() => {
		mocks.insurance.mockReset();
		mocks.triggerBlobDownload.mockReset();
	});

	it('downloads the insurance export with the selected location filter', async () => {
		const blob = new Blob(['device,total\nPhone,1'], { type: 'text/csv' });
		mocks.insurance.mockResolvedValue({
			blob,
			fileName: 'insurance-2026-05-21.csv',
			contentType: 'text/csv'
		});

		render(InsuranceExportCard, {
			props: {
				locations: [
					{ id: 'loc-basement', name: 'Basement' },
					{ id: 'loc-office', name: 'Office' }
				]
			}
		});
		const user = userEvent.setup();

		await user.selectOptions(screen.getByRole('combobox', { name: /location filter/i }), 'loc-office');
		await user.click(screen.getByRole('button', { name: /download insurance csv/i }));

		await waitFor(() => {
			expect(mocks.insurance).toHaveBeenCalledWith({ LocationId: 'loc-office' });
		});
		expect(mocks.triggerBlobDownload).toHaveBeenCalledWith(blob, 'insurance-2026-05-21.csv');
		expect(
			await screen.findByText(/insurance export ready to save: insurance-2026-05-21\.csv/i)
		).toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(InsuranceExportCard, {
			props: {
				locations: [{ id: 'loc-basement', name: 'Basement' }]
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});
});
