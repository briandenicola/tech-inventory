/**
 * Type definitions for vitest-axe matchers
 * Extends Vitest's expect interface with axe accessibility matchers.
 */

import type { AxeMatchers } from 'vitest-axe/matchers';

declare module 'vitest' {
	// eslint-disable-next-line @typescript-eslint/no-empty-object-type, @typescript-eslint/no-unused-vars
	interface Assertion<T = unknown> extends AxeMatchers {}
	// eslint-disable-next-line @typescript-eslint/no-empty-object-type
	interface AsymmetricMatchersContaining extends AxeMatchers {}
}
