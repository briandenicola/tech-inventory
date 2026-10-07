import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';

const mocks = vi.hoisted(() => ({
	get: vi.fn(),
	update: vi.fn(),
	addToast: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	mcpSettings: {
		get: mocks.get,
		update: mocks.update
	}
}));

vi.mock('$lib/stores/toast', () => ({
	addToast: mocks.addToast
}));

import McpSettings from './McpSettings.svelte';

describe('McpSettings', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.get.mockResolvedValue({ enabled: false });
		mocks.update.mockImplementation(async ({ enabled }: { enabled?: boolean }) => ({ enabled }));
	});

	it('loads the disabled state by default', async () => {
		render(McpSettings);

		const toggle = await screen.findByRole('switch', { name: /enable mcp server/i });
		expect(toggle).not.toBeChecked();
	});

	it('enables MCP through the settings API', async () => {
		const user = userEvent.setup();
		render(McpSettings);

		const toggle = await screen.findByRole('switch', { name: /enable mcp server/i });
		await user.click(toggle);

		await waitFor(() => expect(mocks.update).toHaveBeenCalledWith({ enabled: true }));
		expect(toggle).toBeChecked();
		expect(mocks.addToast).toHaveBeenCalledWith({
			type: 'success',
			message: 'MCP server enabled.'
		});
	});

	it('restores the previous state when saving fails', async () => {
		const user = userEvent.setup();
		mocks.update.mockRejectedValue({});
		render(McpSettings);

		const toggle = await screen.findByRole('switch', { name: /enable mcp server/i });
		await user.click(toggle);

		await waitFor(() => expect(toggle).not.toBeChecked());
		expect(mocks.addToast).toHaveBeenCalledWith({
			type: 'error',
			message: 'Could not update the MCP setting.'
		});
	});

	it('shows a retryable load error', async () => {
		const user = userEvent.setup();
		mocks.get.mockRejectedValueOnce({}).mockResolvedValueOnce({ enabled: true });
		render(McpSettings);

		expect(await screen.findByRole('alert')).toHaveTextContent('Could not load the MCP setting.');
		await user.click(screen.getByRole('button', { name: /retry/i }));

		expect(await screen.findByRole('switch', { name: /enable mcp server/i })).toBeChecked();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(McpSettings);
		await screen.findByRole('switch', { name: /enable mcp server/i });

		expect(await axe(container)).toHaveNoViolations();
	});
});
