import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import BackToTopFab from './BackToTopFab.svelte';

// F045 §5.7: the bottom-offset must clear AppBottomNav's pill+bubble in
// standalone-PWA mode. `displayMode` is a module-level singleton, so it's
// mocked here rather than injected as a prop.
let mockIsPwa = false;
vi.mock('$lib/stores/displayMode.svelte', () => ({
	get displayMode() {
		return { isPwa: mockIsPwa };
	}
}));

describe('BackToTopFab', () => {
	afterEach(() => {
		mockIsPwa = false;
	});

	it('does not render when hidden', () => {
		render(BackToTopFab, {
			props: {
				visible: false,
				label: 'Back to top',
				onClick: vi.fn()
			}
		});

		expect(screen.queryByRole('button', { name: /Back to top/i })).not.toBeInTheDocument();
	});

	it('calls onClick when activated', async () => {
		const user = userEvent.setup();
		const onClick = vi.fn();

		render(BackToTopFab, {
			props: {
				visible: true,
				label: 'Back to top',
				onClick
			}
		});

		await user.click(screen.getByRole('button', { name: /Back to top/i }));

		expect(onClick).toHaveBeenCalledOnce();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(BackToTopFab, {
			props: {
				visible: true,
				label: 'Back to top',
				onClick: vi.fn()
			}
		});

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});

	it('uses the default bottom offset outside standalone-PWA mode', () => {
		mockIsPwa = false;
		render(BackToTopFab, { props: { visible: true, label: 'Back to top', onClick: vi.fn() } });

		const style = screen.getByRole('button', { name: /Back to top/i }).getAttribute('style') ?? '';
		expect(style).not.toContain('5.5rem');
		expect(style).toContain('--space-6');
	});

	it('raises the bottom offset to clear AppBottomNav in standalone-PWA mode', () => {
		mockIsPwa = true;
		render(BackToTopFab, { props: { visible: true, label: 'Back to top', onClick: vi.fn() } });

		// jsdom's CSSOM reformats/reorders multi-term calc()+env() values on
		// assignment, so assert on the distinguishing literal rather than the
		// exact serialized string.
		const style = screen.getByRole('button', { name: /Back to top/i }).getAttribute('style') ?? '';
		expect(style).toContain('5.5rem');
	});
});
