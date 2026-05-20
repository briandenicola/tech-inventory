import { describe, expect, it } from 'vitest';
import {
	clearReferenceSelection,
	getVisibleReferenceSelectionState,
	toggleAllVisibleReferenceSelections,
	toggleReferenceSelection
} from './referenceSelection';

describe('referenceSelection', () => {
	it('toggles individual ids on and off', () => {
		let selectedIds = new Set<string>();

		selectedIds = toggleReferenceSelection(selectedIds, 'brand-1');
		expect(Array.from(selectedIds)).toEqual(['brand-1']);

		selectedIds = toggleReferenceSelection(selectedIds, 'brand-1');
		expect(Array.from(selectedIds)).toEqual([]);
	});

	it('toggles all visible ids together', () => {
		const visibleIds = ['brand-1', 'brand-2'];
		let selectedIds = new Set<string>(['brand-3']);

		selectedIds = toggleAllVisibleReferenceSelections(selectedIds, visibleIds);
		expect(Array.from(selectedIds).sort()).toEqual(['brand-1', 'brand-2', 'brand-3']);

		selectedIds = toggleAllVisibleReferenceSelections(selectedIds, visibleIds);
		expect(Array.from(selectedIds)).toEqual(['brand-3']);
	});

	it('reports all/some visible selection states', () => {
		const visibleIds = ['category-1', 'category-2', 'category-3'];

		expect(getVisibleReferenceSelectionState(new Set<string>(), visibleIds)).toEqual({
			allVisibleSelected: false,
			someVisibleSelected: false
		});
		expect(getVisibleReferenceSelectionState(new Set<string>(['category-1']), visibleIds)).toEqual({
			allVisibleSelected: false,
			someVisibleSelected: true
		});
		expect(
			getVisibleReferenceSelectionState(
				new Set<string>(['category-1', 'category-2', 'category-3']),
				visibleIds
			)
		).toEqual({
			allVisibleSelected: true,
			someVisibleSelected: false
		});
	});

	it('clears the selection', () => {
		expect(clearReferenceSelection().size).toBe(0);
	});
});
