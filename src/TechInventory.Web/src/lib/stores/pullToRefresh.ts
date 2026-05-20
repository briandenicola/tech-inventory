import { get, writable } from 'svelte/store';

export type PullToRefreshCallback = () => void | Promise<void>;

interface PullToRefreshRegistration {
	routePath: string;
	onRefresh: PullToRefreshCallback;
}

export const activePullToRefresh = writable<PullToRefreshRegistration | null>(null);

export function registerPullToRefresh(
	routePath: string,
	onRefresh: PullToRefreshCallback
): () => void {
	activePullToRefresh.set({ routePath, onRefresh });

	return () => {
		const currentRegistration = get(activePullToRefresh);
		if (
			currentRegistration?.routePath === routePath &&
			currentRegistration.onRefresh === onRefresh
		) {
			activePullToRefresh.set(null);
		}
	};
}
