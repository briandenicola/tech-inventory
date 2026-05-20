export function toggleReferenceSelection(selectedIds: Set<string>, id: string): Set<string> {
	const next = new Set(selectedIds);
	if (next.has(id)) {
		next.delete(id);
	} else {
		next.add(id);
	}

	return next;
}

export function toggleAllVisibleReferenceSelections(
	selectedIds: Set<string>,
	visibleIds: string[]
): Set<string> {
	const next = new Set(selectedIds);
	const allVisibleSelected = visibleIds.length > 0 && visibleIds.every((id) => next.has(id));

	if (allVisibleSelected) {
		visibleIds.forEach((id) => next.delete(id));
	} else {
		visibleIds.forEach((id) => next.add(id));
	}

	return next;
}

export function clearReferenceSelection(): Set<string> {
	return new Set<string>();
}

export function getVisibleReferenceSelectionState(
	selectedIds: Set<string>,
	visibleIds: string[]
): {
	allVisibleSelected: boolean;
	someVisibleSelected: boolean;
} {
	const allVisibleSelected = visibleIds.length > 0 && visibleIds.every((id) => selectedIds.has(id));
	const someVisibleSelected = !allVisibleSelected && visibleIds.some((id) => selectedIds.has(id));

	return {
		allVisibleSelected,
		someVisibleSelected
	};
}
