<script lang="ts">
	import { t } from '$lib/i18n';
	import type { Snippet } from 'svelte';

	type RefreshHandler = () => void | Promise<void>;

	let { children, onRefresh }: { children?: Snippet; onRefresh?: RefreshHandler } = $props();

	const pullThreshold = 60;
	const maxPullDistance = 120;
	const pullResistance = 0.5;

	let container = $state<HTMLDivElement | null>(null);
	let isCoarsePointer = $state(false);
	let startY = $state<number | null>(null);
	let pullDistance = $state(0);
	let isPulling = $state(false);
	const pullDeadzone = 10;
	let isRefreshing = $state(false);
	let canRefresh = $state(false);

	const isEnabled = $derived(isCoarsePointer && typeof onRefresh === 'function');
	const indicatorHeight = $derived(isRefreshing ? pullThreshold : pullDistance);
	const isActive = $derived(isPulling || indicatorHeight > 0);
	const progress = $derived(Math.min(indicatorHeight / pullThreshold, 1));
	const statusMessage = $derived.by(() => {
		if (isRefreshing) {
			return t('pullToRefresh.refreshing');
		}

		if (canRefresh) {
			return t('pullToRefresh.release');
		}

		return t('pullToRefresh.pull');
	});

	function resetPullState(): void {
		startY = null;
		pullDistance = 0;
		isPulling = false;
		canRefresh = false;
	}

	function shouldTrackPull(target: EventTarget | null): boolean {
		if (!isEnabled || isRefreshing || typeof window === 'undefined' || window.scrollY > 0) {
			return false;
		}

		if (!(target instanceof Element)) {
			return true;
		}

		return !target.closest(
			'input, textarea, select, button, a, [contenteditable="true"], [role="dialog"], [data-pull-refresh-ignore="true"]'
		);
	}

	function handleTouchStart(event: TouchEvent): void {
		if (event.touches.length !== 1 || !shouldTrackPull(event.target)) {
			resetPullState();
			return;
		}

		startY = event.touches[0]?.clientY ?? null;
		pullDistance = 0;
		canRefresh = false;
	}

	function handleTouchMove(event: TouchEvent): void {
		if (!isEnabled || isRefreshing || startY === null) {
			return;
		}

		const currentY = event.touches[0]?.clientY;
		if (typeof currentY !== 'number') {
			return;
		}

		if (typeof window !== 'undefined' && window.scrollY > 0) {
			resetPullState();
			return;
		}

		const delta = currentY - startY;
		if (delta <= 0) {
			resetPullState();
			return;
		}

		// Deadzone: don't claim the gesture until the user has clearly committed
		// to pulling down. This prevents iOS WebKit from blocking native scroll
		// when a micro-movement (common at touch start) briefly goes positive.
		if (delta < pullDeadzone) {
			return;
		}

		isPulling = true;
		pullDistance = Math.min(maxPullDistance, delta * pullResistance);
		canRefresh = pullDistance >= pullThreshold;
		event.preventDefault();
	}

	async function handleTouchEnd(): Promise<void> {
		if (!isEnabled || isRefreshing) {
			resetPullState();
			return;
		}

		if (!canRefresh || !onRefresh) {
			resetPullState();
			return;
		}

		isPulling = false;
		isRefreshing = true;
		startY = null;
		canRefresh = false;

		try {
			await onRefresh();
		} finally {
			isRefreshing = false;
			resetPullState();
		}
	}

	$effect(() => {
		if (!container || typeof window === 'undefined') {
			return;
		}

		const node = container;
		const mediaQuery = window.matchMedia('(pointer: coarse)');
		const updatePointerMode = () => {
			isCoarsePointer = mediaQuery.matches;
			if (!mediaQuery.matches) {
				resetPullState();
			}
		};
		const touchStartListener = (event: Event) => {
			handleTouchStart(event as TouchEvent);
		};
		const touchMoveListener = (event: Event) => {
			handleTouchMove(event as TouchEvent);
		};
		const touchEndListener = () => {
			void handleTouchEnd();
		};

		updatePointerMode();
		node.addEventListener('touchstart', touchStartListener, { passive: true });
		node.addEventListener('touchmove', touchMoveListener, { passive: false });
		node.addEventListener('touchend', touchEndListener);
		node.addEventListener('touchcancel', touchEndListener);

		mediaQuery.addEventListener('change', updatePointerMode);

		return () => {
			node.removeEventListener('touchstart', touchStartListener);
			node.removeEventListener('touchmove', touchMoveListener);
			node.removeEventListener('touchend', touchEndListener);
			node.removeEventListener('touchcancel', touchEndListener);
			mediaQuery.removeEventListener('change', updatePointerMode);
		};
	});
</script>

<div bind:this={container} class="overscroll-y-contain">
	<div class="sr-only" aria-live="polite" aria-atomic="true">
		{#if isRefreshing}
			{statusMessage}
		{/if}
	</div>

	<div aria-hidden="true" class="pointer-events-none relative z-0">
		<div
			class="flex justify-center overflow-hidden transition-all duration-200 ease-out"
			style={`height: ${indicatorHeight}px;`}
		>
			<div
				class="inline-flex items-center gap-3 px-4 py-2 text-sm font-medium"
				style="color: var(--color-text-secondary);"
			>
				<span
					class="inline-flex h-5 w-5 items-center justify-center transition-opacity duration-150 ease-out"
					class:animate-spin={isRefreshing}
					data-refreshing={isRefreshing}
					style={`opacity: ${isRefreshing || isPulling ? Math.max(progress, 0.25) : 0};`}
				>
					<span
						class="h-full w-full rounded-full border-2"
						style="border-color: var(--color-border); border-top-color: var(--color-primary-500);"
					></span>
				</span>
				<span style:color={canRefresh ? 'var(--color-text)' : 'var(--color-text-secondary)'}>
					{statusMessage}
				</span>
			</div>
		</div>
	</div>

	<div
		data-testid="pull-to-refresh-content"
		class="transition-transform duration-200 ease-out"
		class:will-change-transform={isActive}
		class:transition-none={isPulling}
		style={isActive ? `transform: translateY(${indicatorHeight}px);` : ''}
	>
		{#if children}
			{@render children()}
		{/if}
	</div>
</div>
