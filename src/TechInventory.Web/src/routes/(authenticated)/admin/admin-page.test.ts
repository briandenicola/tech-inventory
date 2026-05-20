import { describe, expect, it } from 'vitest';
import { load } from './+page';

describe('admin index route', () => {
	it('redirects to the audit log page', async () => {
		const event = {} as Parameters<typeof load>[0];

		await expect(load(event)).rejects.toMatchObject({
			status: 307,
			location: '/admin/audit'
		});
	});
});
