/**
 * columnResize — drag-and-keyboard column resizing for the desktop devices table.
 *
 * Kept as an action rather than inline handlers so the table component stays a
 * renderer, and so the pointer/keyboard contract is testable on its own.
 *
 * Two things drive the design:
 *
 *  1. **Pointer capture, not window listeners.** `setPointerCapture` keeps events
 *     flowing to the handle even when the cursor outruns it (easy to do when
 *     dragging fast), and releases cleanly if the pointer is cancelled — which a
 *     window-level mousemove/mouseup pair gets wrong when the drag ends outside
 *     the window.
 *
 *  2. **Keyboard parity.** A pointer-only resize is unusable without a mouse and
 *     fails WCAG 2.1.1. The handle is a focusable `separator` that responds to
 *     arrow keys, so every pointer gesture has a keyboard equivalent.
 */

export interface ColumnResizeOptions {
	/** Current width in CSS pixels. */
	width: number;
	min: number;
	max: number;
	/** Called continuously while dragging — cheap, not persisted. */
	onResize: (width: number) => void;
	/** Called once when a gesture ends. This is the point to persist. */
	onCommit: (width: number) => void;
	/** Called when the user asks for this column's default back. */
	onReset: () => void;
}

/** Arrow-key step. Shift multiplies it, matching how sliders behave elsewhere. */
const STEP = 16;
const COARSE_STEP = 64;

export function columnResize(node: HTMLElement, options: ColumnResizeOptions) {
	let current = options;
	let startX = 0;
	let startWidth = 0;
	let dragging = false;

	function clamp(width: number): number {
		return Math.min(current.max, Math.max(current.min, Math.round(width)));
	}

	function onPointerDown(event: PointerEvent) {
		// Ignore secondary buttons: a right-click here should open the context menu,
		// not start an invisible drag the user cannot see or cancel.
		if (event.button !== 0) return;

		dragging = true;
		startX = event.clientX;
		startWidth = current.width;
		node.setPointerCapture?.(event.pointerId);

		// Stops the browser turning the drag into a text selection across the header.
		event.preventDefault();
	}

	function onPointerMove(event: PointerEvent) {
		if (!dragging) return;
		current.onResize(clamp(startWidth + (event.clientX - startX)));
	}

	function endDrag(event: PointerEvent) {
		if (!dragging) return;
		dragging = false;

		if (node.hasPointerCapture?.(event.pointerId)) {
			node.releasePointerCapture?.(event.pointerId);
		}

		current.onCommit(clamp(startWidth + (event.clientX - startX)));
	}

	function onKeyDown(event: KeyboardEvent) {
		const step = event.shiftKey ? COARSE_STEP : STEP;
		let next: number;

		switch (event.key) {
			case 'ArrowLeft':
				next = current.width - step;
				break;
			case 'ArrowRight':
				next = current.width + step;
				break;
			case 'Home':
				// Reset rather than jump to the minimum: "get me back to normal" is the
				// thing people actually want from a handle they have overshot with.
				event.preventDefault();
				current.onReset();
				return;
			default:
				return;
		}

		event.preventDefault();
		const clamped = clamp(next);
		current.onResize(clamped);
		current.onCommit(clamped);
	}

	function onDoubleClick(event: MouseEvent) {
		event.preventDefault();
		current.onReset();
	}

	node.addEventListener('pointerdown', onPointerDown);
	node.addEventListener('pointermove', onPointerMove);
	node.addEventListener('pointerup', endDrag);
	node.addEventListener('pointercancel', endDrag);
	node.addEventListener('keydown', onKeyDown);
	node.addEventListener('dblclick', onDoubleClick);

	return {
		update(next: ColumnResizeOptions) {
			current = next;
		},
		destroy() {
			node.removeEventListener('pointerdown', onPointerDown);
			node.removeEventListener('pointermove', onPointerMove);
			node.removeEventListener('pointerup', endDrag);
			node.removeEventListener('pointercancel', endDrag);
			node.removeEventListener('keydown', onKeyDown);
			node.removeEventListener('dblclick', onDoubleClick);
		}
	};
}
