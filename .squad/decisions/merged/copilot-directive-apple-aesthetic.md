### 2026-05-18T20:56-05:00: User directive — Apple-elegant visual language is the design target
**By:** brian.denicolafamily (via Copilot)
**What:** UI should feel "Apple elegant." Specific complaints from the first authenticated screen:
- Left nav panel feels cramped/off
- Font sizes are too small for comfortable reading
- Dropdowns / select inputs are too tight (insufficient vertical padding, line-height)
- Overall lacks the polished, generous, refined character of Apple's design language (think macOS Settings, App Store, Apple Music)

**Why:** Single-household production app — Brian uses it daily, polish matters more than density. This is the canonical reference aesthetic for all future UI/UX work.

**Implications for future work:**
- Default to larger base font (16px → 17/18px body, generous heading scale)
- Generous vertical rhythm on form controls (44px minimum touch target — also iOS HIG standard)
- Sidebar/nav should breathe (more padding, more whitespace, larger labels)
- Prefer SF-Pro-like system fonts or Inter; round shapes; subtle shadows over hard borders
- Soft, desaturated palette; restrained color use; high contrast on text

**Open items (NOT YET DECIDED):**
- When to schedule the visual overhaul — Brian asked whether to address now or defer to dedicated theming round. Awaiting his answer.
