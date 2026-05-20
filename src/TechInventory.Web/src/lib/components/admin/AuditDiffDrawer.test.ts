/**
 * AuditDiffDrawer.test.ts — axe-core regression test for F029 diff contrast.
 *
 * Verifies the JSON diff rendering in the audit drawer meets WCAG AA (≥4.5:1)
 * in both light and dark themes using Drake's semantic tokens.
 */
import { describe, it, expect, vi } from 'vitest';
import { render } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import AuditDiffDrawer from './AuditDiffDrawer.svelte';
import type { components } from '$lib/api/generated/types';

type AuditEvent = components['schemas']['AuditEventResponse'];

// Mock i18n
vi.mock('$lib/i18n', () => ({
	t: (key: string) => {
		const translations: Record<string, string> = {
			'common.actions.close': 'Close',
			'admin.audit.diff.title': 'Change Details',
			'admin.audit.columns.timestamp': 'Timestamp',
			'admin.audit.columns.actor': 'Actor',
			'admin.audit.columns.entityType': 'Entity Type',
			'admin.audit.columns.action': 'Action',
			'admin.audit.columns.entityId': 'Entity ID',
			'admin.audit.diff.changes': 'Changes',
			'admin.audit.diff.showUnchanged': 'Show unchanged',
			'admin.audit.diff.added': 'Added',
			'admin.audit.diff.removed': 'Removed',
			'admin.audit.diff.changed': 'Changed',
			'admin.audit.diff.unchanged': 'Unchanged',
			'admin.audit.diff.before': 'Before',
			'admin.audit.diff.after': 'After'
		};
		return translations[key] || key;
	}
}));

describe('AuditDiffDrawer diff contrast (F029)', () => {
	const mockEvent: AuditEvent = {
		id: '00000000-0000-0000-0000-000000000001',
		timestamp: '2026-05-20T10:00:00Z',
		entityType: 'Device',
		entityId: '00000000-0000-0000-0000-000000000002',
		action: 'Updated',
		actor: 'test-user@example.com',
		beforePayload: JSON.stringify({ name: 'Old Name', status: 'Active', owner: 'Alice' }),
		afterPayload: JSON.stringify({ name: 'New Name', status: 'Retired', location: 'Storage' })
	};

	it('renders diff with no accessibility violations (light theme default)', async () => {
		const { container } = render(AuditDiffDrawer, {
			props: { event: mockEvent, onClose: vi.fn() }
		});
		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});

	it('renders diff with no accessibility violations (dark theme)', async () => {
		// Force dark mode
		document.documentElement.dataset.theme = 'dark';
		document.documentElement.classList.add('dark');

		const { container } = render(AuditDiffDrawer, {
			props: { event: mockEvent, onClose: vi.fn() }
		});
		const results = await axe(container);
		expect(results).toHaveNoViolations();

		// Cleanup
		delete document.documentElement.dataset.theme;
		document.documentElement.classList.remove('dark');
	});
});
