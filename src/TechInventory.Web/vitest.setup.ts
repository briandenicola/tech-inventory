import '@testing-library/jest-dom/vitest';
import 'vitest-axe/extend-expect';

// Polyfill Web Animations API for Svelte transitions (jsdom doesn't support it)
if (typeof Element.prototype.animate === 'undefined') {
	Element.prototype.animate = function () {
		return {
			cancel: () => {},
			finish: () => {},
			pause: () => {},
			play: () => {},
			reverse: () => {},
			playbackRate: 1,
			startTime: 0,
			currentTime: 0,
			onfinish: null,
			oncancel: null
		} as Animation;
	};
}
