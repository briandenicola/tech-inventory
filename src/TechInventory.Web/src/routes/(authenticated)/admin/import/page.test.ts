/**
 * /admin/import — C-09 (wizard chrome + step indicator + CSV upload),
 * C-19 (F034 preview columns: Model/Purpose/Notes already implemented in
 * production; this is a test-only gap), C-18 (partial: axe on step 1).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import type { AuthState, CurrentUser } from '$lib/stores/auth';
import type { PreviewImportResult, CommitImportResult } from '$lib/api/types';

const { goto, previewMock, commitMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	previewMock: vi.fn(),
	commitMock: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto }));

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
		imports: { preview: previewMock, commit: commitMock }
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

function setAuth(currentUser: CurrentUser | null) {
	authStore.set({
		currentUser,
		isAuthenticated: currentUser !== null,
		isLoading: false,
		error: null,
		authMethod: currentUser ? 'entra' : null,
		mustChangePassword: false
	});
}


function makeCsvFile(): File {
	const csv = 'Name,Brand,Category\nThinkPad X1,Lenovo,Laptop\n';
	return new File([csv], 'devices.csv', { type: 'text/csv' });
}

describe('/admin/import (C-09, C-19)', () => {
	beforeEach(() => {
		goto.mockReset();
		previewMock.mockReset();
		commitMock.mockReset();
		setAuth(adminUser);
	});

	it('redirects a non-Admin owner away from the import wizard', () => {
		setAuth({ ...adminUser, role: 'Member' });

		render(Page);

		expect(goto).toHaveBeenCalledWith('/devices');
	});

	it('does not redirect an Admin owner', () => {
		render(Page);

		expect(goto).not.toHaveBeenCalled();
	});

	it('renders the 3-step wizard chrome and an upload dropzone on step 1', () => {
		render(Page);

		expect(screen.getByRole('heading', { name: 'Import Devices' })).toBeInTheDocument();
		const progress = screen.getByRole('list', { name: 'Import progress' });
		expect(within(progress).getByText('Upload CSV File')).toBeInTheDocument();
		expect(within(progress).getByText('Preview Import')).toBeInTheDocument();
		expect(within(progress).getByText('Commit Import')).toBeInTheDocument();
		expect(
			screen.getByText('Drag and drop a CSV file here, or click to select')
		).toBeInTheDocument();
	});

	it('has no accessibility violations on the upload step', async () => {
		const { container } = render(Page);

		expect(await axe(container)).toHaveNoViolations();
	});

	it('previews F034 Model/Purpose/Notes columns for valid rows after Next (C-19)', async () => {
		const previewResult: PreviewImportResult = {
			totalRows: 1,
			validRows: [
				{
					rowNumber: 2,
					device: {
						name: 'ThinkPad X1',
						brand: 'Lenovo',
						category: 'Laptop',
						model: 'X1 Carbon Gen 11',
						purpose: 'Kids homework laptop',
						notes: 'Refurbished 2023',
						owner: 'Brian',
						location: 'Home Office',
						status: 'Active'
					}
				}
			],
			invalidRows: [],
			lookupsToCreate: []
		};
		previewMock.mockResolvedValue(previewResult);

		const { container } = render(Page);
		const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
		await fireEvent.change(fileInput, { target: { files: [makeCsvFile()] } });

		await waitFor(() => expect(screen.getByText('devices.csv')).toBeInTheDocument());

		await fireEvent.click(screen.getByRole('button', { name: 'Next' }));

		await waitFor(() => expect(previewMock).toHaveBeenCalledTimes(1));
		expect(screen.getByText('1 valid rows')).toBeInTheDocument();

		// The per-row preview table is inside a collapsed <details> — open it
		// to confirm the F034 columns actually rendered from the API response.
		const details = screen.getByTestId('import-preview-rows');
		await fireEvent.click(within(details).getByText('Preview rows'));

		expect(within(details).getByText('X1 Carbon Gen 11')).toBeInTheDocument();
		expect(within(details).getByText('Kids homework laptop')).toBeInTheDocument();
		expect(within(details).getByText('Refurbished 2023')).toBeInTheDocument();
	});

	it('commits the import and renders the success summary', async () => {
		const previewResult: PreviewImportResult = {
			totalRows: 1,
			validRows: [{ rowNumber: 2, device: { name: 'ThinkPad X1' } }],
			invalidRows: [],
			lookupsToCreate: []
		};
		const commitResult: CommitImportResult = {
			batchId: 'batch-1',
			totalRows: 1,
			importedRows: 1,
			invalidRows: 0,
			failedRows: []
		};
		previewMock.mockResolvedValue(previewResult);
		commitMock.mockResolvedValue(commitResult);

		const { container } = render(Page);
		const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
		await fireEvent.change(fileInput, { target: { files: [makeCsvFile()] } });
		await waitFor(() => expect(screen.getByText('devices.csv')).toBeInTheDocument());
		await fireEvent.click(screen.getByRole('button', { name: 'Next' }));
		await waitFor(() => expect(previewMock).toHaveBeenCalledTimes(1));

		await fireEvent.click(screen.getByRole('button', { name: 'Commit Import' }));

		await waitFor(() => expect(commitMock).toHaveBeenCalledTimes(1));
		expect(screen.getByText('Import completed successfully')).toBeInTheDocument();
	});
});
