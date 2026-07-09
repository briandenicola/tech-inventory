import type { components } from '$lib/api/generated/types';

type OwnerResponse = components['schemas']['OwnerResponse'];

export function buildActorDisplayNameMap(owners: OwnerResponse[]): Record<string, string> {
	const next: Record<string, string> = {};
	for (const owner of owners) {
		if (!owner.displayName) {
			continue;
		}
		if (owner.id) {
			next[owner.id.toLowerCase()] = owner.displayName;
		}
		if (owner.entraObjectId) {
			next[owner.entraObjectId.toLowerCase()] = owner.displayName;
		}
	}
	return next;
}

export function formatActor(actor: string | null | undefined, actorDisplayNames: Record<string, string>): string {
	if (!actor) {
		return '—';
	}
	return actorDisplayNames[actor.toLowerCase()] ?? actor;
}
