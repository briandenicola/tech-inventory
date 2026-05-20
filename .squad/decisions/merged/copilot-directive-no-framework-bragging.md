### 2026-05-18T20:56-05:00: User directive — No framework attribution in UI
**By:** brian.denicolafamily (via Copilot)
**What:** Do NOT include "Built with X" / framework attribution strings (e.g., "Built with SvelteKit") anywhere in user-facing UI. Quote: "Don't put 'Built with SvelteKit'. No one cares."
**Why:** User preference — captured for team memory. Applies to all future footer / about / splash-screen copy.
**Affected files:** `src/TechInventory.Web/src/lib/i18n/en.json` (footer.builtWith removed), `src/TechInventory.Web/src/routes/(authenticated)/+layout.svelte` (footer span removed).
