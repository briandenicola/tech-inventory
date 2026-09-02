/**
 * deviceCreate.ts — F045 §5.2 cross-route "Add" bridge.
 *
 * `AppBottomNav`'s Add action lives in the root authenticated layout, but the
 * create-device modal state lives on the devices page. Mirrors the
 * `pullToRefresh` registration pattern: the devices page registers a handler
 * while mounted on `/devices`; the layout calls it directly when present, or
 * navigates to `/devices?add=1` (which the devices page interprets as
 * "open the create modal") when the user is elsewhere.
 */
import { get, writable } from 'svelte/store';

export type DeviceCreateHandler = () => void;

interface DeviceCreateRegistration {
	routePath: string;
	onOpenCreate: DeviceCreateHandler;
}

export const activeDeviceCreate = writable<DeviceCreateRegistration | null>(null);

export function registerDeviceCreateHandler(
	routePath: string,
	onOpenCreate: DeviceCreateHandler
): () => void {
	activeDeviceCreate.set({ routePath, onOpenCreate });

	return () => {
		const current = get(activeDeviceCreate);
		if (current?.routePath === routePath && current.onOpenCreate === onOpenCreate) {
			activeDeviceCreate.set(null);
		}
	};
}
