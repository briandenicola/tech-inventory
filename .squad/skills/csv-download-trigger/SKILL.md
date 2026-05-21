# Skill: CSV Download Trigger

**Trigger:** Authenticated UI needs to download a server-generated CSV and preserve the server-issued filename when possible.

## Pattern

1. **Fetch through the typed API client**, not ad-hoc `fetch`, so auth headers and endpoint typing stay centralized.
2. **Return both the `Blob` and parsed `Content-Disposition` filename** from the client seam.
3. **Use a temporary anchor element** with `URL.createObjectURL(blob)` and `download = fileName`.
4. **Append the anchor to `document.body` before clicking it**.
5. **Delay `URL.revokeObjectURL(...)` cleanup** (about 1 second) instead of revoking in the same tick; this avoids WebKit/iOS races where the download can be interrupted.
6. **Keep inline UI state** (`idle` / `loading` / `success` / `error`) so failed exports are never silent.

## Notes

- Prefer RFC 5987 `filename*=` parsing first, then fall back to plain `filename=`.
- If the server omits `Content-Disposition`, provide a deterministic fallback filename in UI copy.
- Role-gate the trigger in the UI even when the backend already enforces authorization.

## Applied In

- `src/TechInventory.Web/src/lib/api/client.ts`
- `src/TechInventory.Web/src/lib/utils/blobDownload.ts`
- `src/TechInventory.Web/src/lib/components/InsuranceReportCard.svelte`
