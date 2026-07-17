/**
 * Reduced-motion helper for Svelte transition directives.
 *
 * `transition:`/`in:`/`out:` directives run via the Web Animations API in
 * JS, so the `prefers-reduced-motion` override in app.css (which only
 * affects CSS transitions/animations) doesn't reach them. Wrap a
 * directive's duration with this so it collapses to 0 under the same OS
 * preference.
 */
export function motionDuration(duration: number): number {
	if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return duration;
	return window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : duration;
}
