/**
 * headerStacking.ts — F045 B1 regression guard.
 *
 * The authenticated shell's `<header>` raises its own z-index while
 * `AppMenuPopover` is open so the popover panel (`--z-popover`, 60) clears
 * everything below it. The QC-rejected regression demoted the *closed*
 * state from `--z-fixed` (30) to `--z-sticky` (20): seven pages
 * (`devices` + six `admin/*`) carry an opaque `sticky top-[73px] z-20`
 * sub-header that is later in DOM order, so an equal z-index hands them the
 * stacking tie and the desktop user-menu dropdown — the sole desktop nav
 * entry point — paints beneath them instead of above.
 *
 * Extracted to a pure function so the regression has a fast, dependency-free
 * unit test instead of requiring the full authenticated-layout mount
 * (MSAL/auth store/reference-data graph).
 */
export function headerZIndexToken(popoverOpen: boolean): string {
	return popoverOpen ? 'var(--z-popover)' : 'var(--z-fixed)';
}
