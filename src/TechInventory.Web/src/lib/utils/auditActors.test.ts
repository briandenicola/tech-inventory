import { describe, expect, it } from 'vitest';
import { buildActorDisplayNameMap, formatActor } from './auditActors';

describe('auditActors', () => {
	it('maps owner ids and Entra object ids to display names', () => {
		const map = buildActorDisplayNameMap([
			{
				id: '11111111-1111-1111-1111-111111111111',
				entraObjectId: '98abb71d-84a4-4e7d-ba3b-db375e15a10e',
				displayName: 'Brian Denicola',
				role: 'Admin',
				isActive: true
			}
		]);

		expect(formatActor('11111111-1111-1111-1111-111111111111', map)).toBe('Brian Denicola');
		expect(formatActor('98ABB71D-84A4-4E7D-BA3B-DB375E15A10E', map)).toBe('Brian Denicola');
	});

	it('falls back to the original actor when no owner matches', () => {
		expect(formatActor('localuser:admin', {})).toBe('localuser:admin');
		expect(formatActor(null, {})).toBe('—');
	});
});
