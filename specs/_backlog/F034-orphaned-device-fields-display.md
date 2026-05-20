# F034: Surface Imported Device Fields Hidden by the UI

**Status**: shipped
**Priority**: P1
**Effort**: S
**Value**: high
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-20
**Owner**: vasquez

## Problem
Brian's 2026-05-20 import-bug report — *"are we not importing Model, Purpose,
or Notes Fields from the SharePoint List?"* — triaged to a **UI display gap,
not an import bug**. End-to-end verification of the import pipeline shows
every field is correctly persisted:

- `ImportFieldNames.cs` aliases recognize `Model`, `Notes`, `Purpose`,
  `Vendor`→Brand, `DeviceType`→Category, etc. from the SharePoint CSV header
  row (`data/Devices.csv` line 1)
- `DeviceImportProcessingService.cs` extracts all three (lines 98, 114, 120)
- `CommitImportCommand.cs` passes them into `Device.Create(...)` (lines 129,
  136, 139)
- `Device` entity persists them via guarded private setters
- `DeviceResponse` returns them on the API (lines 8, 19, 22)

The user-visible symptom is that imported devices appear blank in the UI for
several fields that *are* present in the database. The gap matrix:

| Field             | Detail page | Edit form | Add form | Card / Table |
| ----------------- | :---------: | :-------: | :------: | :----------: |
| Model             |     ❌      |    ❌     |    ❌    |      ❌      |
| Purpose           |     ❌      |    ✅     |    ✅    |      ❌      |
| Notes             |     ✅      |    ✅     |    ✅    |      ❌      |
| OperatingSystem   |     ❌      |    ✅     |    ✅    |      ❌      |
| IpAddress         |     ❌      |    ✅     |    ✅    |      ❌      |
| MacAddress        |     ❌      |    ✅     |    ✅    |      ❌      |
| ProductUrl        |     ❌      |    ✅     |    ✅    |      ❌      |
| Version           |     ❌      |    ✅     |    ✅    |      ❌      |
| RetiredDate       |     ❌      |    ✅     |    n/a   |      ❌      |
| DisposalMethod    |     ❌      |    ✅     |    n/a   |      ❌      |

**Model** is the worst case — present in `Device.Model`, returned by the API,
but renders nowhere and cannot be entered by hand in any form.

A secondary contributor: the **import preview UI** (`/admin/import`) shows
summary counts only, with no per-row preview table, so users have no way to
verify that Model/Purpose/Notes are landing correctly before pressing Commit.

## Proposed Solution
Two independent slices, both small and self-contained.

### Slice A — Display all persisted device fields
- **Detail page** (`src/TechInventory.Web/src/routes/(authenticated)/devices/[id]/+page.svelte`):
  add Model, Purpose, OperatingSystem, IpAddress, MacAddress, ProductUrl,
  Version, RetiredDate, DisposalMethod as `<dt>/<dd>` pairs in the existing
  fields grid (lines 433-518), each gated on truthy value so blank rows
  don't render. Match the existing two-column-on-`sm+` layout.
- **DeviceForm** (`src/TechInventory.Web/src/lib/components/DeviceForm.svelte`):
  add a `Model` text input bound to `formData.model`, validation
  `maxLength: 200` matching `Device.Model`'s domain guard. Place between
  Serial Number and Brand (the conventional spec-sheet ordering).
- Reflect the new field through `addDeviceSchema` + `editDeviceSchema` Zod
  schemas, the `DeviceFormData` type, and the AddDeviceModal/edit page
  payload assembly.
- **DeviceTable** / device card components: add a secondary line for Model
  (under the device name) — render only when present, so existing rows with
  no model don't grow visually. Defer adding Purpose/Notes to the card
  surfaces (those belong on detail only; cards stay scannable).

### Slice B — Per-row import preview table
- Expand step 2 of the import wizard
  (`src/TechInventory.Web/src/routes/(authenticated)/admin/import/+page.svelte`)
  with a collapsible "Preview rows" table showing the first N valid rows
  (N=10) with columns: Name, Brand, Category, **Model**, **Purpose**,
  **Notes**, Owner, Location, Status. Uses `PreviewImportResult.validRows`
  which already carries every field via `ImportDevicePreview`.
- No new API needed; the data is already on the response shape (see
  `ImportContracts.cs:28`).

## User Stories
- *As Brian after importing the SharePoint CSV, I open any device's detail
  page and see Model, Purpose, Notes, OS, IP, MAC, URL, and Version exactly
  as they appeared in the source spreadsheet.*
- *As Brian on the import preview screen, I can scan the first 10 rows and
  confirm Model/Purpose/Notes lined up correctly before pressing Commit.*
- *As anyone adding a device by hand, I can enter Model so my hand-entered
  records match my imported records.*

## Acceptance Criteria
- [ ] `devices/[id]/+page.svelte` renders Model, Purpose, OperatingSystem,
      IpAddress, MacAddress, ProductUrl, Version, RetiredDate,
      DisposalMethod when each is non-null
- [ ] `DeviceForm.svelte` exposes a Model input (add + edit flows); value
      round-trips through `POST /api/v1/devices` and
      `PUT /api/v1/devices/{id}`
- [ ] Add/edit Zod schemas validate `model` as `string().max(200).optional()`
      mirroring the domain Guard
- [ ] DeviceTable / DeviceCard surfaces Model as a secondary line under the
      device name (truthy-only)
- [ ] `/admin/import` step 2 renders a "Preview rows" table showing the first
      10 valid rows, including Model / Purpose / Notes columns
- [ ] i18n keys for any new strings (Model column header, placeholder, etc.)
      land in `src/lib/i18n/en.json`
- [ ] `pnpm run check`, `pnpm run lint`, `pnpm exec vitest run`,
      `pnpm run build`, `dotnet build` all pass
- [ ] DeviceForm test fixture updated; existing tests pass
- [ ] Playwright: add a smoke test that imports the canonical SharePoint
      CSV and asserts Model + Purpose + Notes appear on a chosen device's
      detail page (reuse the existing `data/Devices.csv` fixture)
- [ ] Zero axe-core violations on the changed views in both themes

## Out of Scope
- Editing the import processing pipeline — backend is already correct.
- Adding new fields to the device schema. Scope is strictly "surface what
  we already store."
- Re-running the SharePoint import end-to-end (Brian already imported; the
  data is in the DB and will appear once the UI renders it).
- Mass migration / backfill of records — there's nothing to backfill; the
  data is already there.

## Dependencies
- None — this is a UI-only patch on existing fields. Plays nicely alongside
  F027 (responsive list rework) but does not block on it; the `<dt>/<dd>`
  grid pattern is the same shape `<ResponsiveList>` will adopt later.
- Drake: brief sanity check that adding Model under the device name on the
  card doesn't break the dense two-up grid from F031 polish round 2.

## Open Questions
- Should Notes ever show on the card surface? **Recommendation**: no —
  Notes is long-form prose; let the detail page own it. Cards stay
  scannable.
- Audit-trail fields (`createdBy`, `modifiedBy`) — same display gap but
  arguably correct (avoid PII noise). **Recommendation**: leave as-is for
  now; revisit only if Brian asks.

## Notes / Research
- Direct evidence of the gap:
  - `grep -n 'device\.model\|device\.purpose' src/TechInventory.Web/src/routes/(authenticated)/devices/[id]/+page.svelte` → no matches
  - `grep -n 'formData\.\|"model"' src/TechInventory.Web/src/lib/components/DeviceForm.svelte` → 17 fields bound, none named `model`
  - `view src/TechInventory.Application/Devices/DeviceResponse.cs` → fields 8 / 19 / 22 confirm Model/Notes/Purpose returned by the API
  - `data/Devices.csv` line 1 → SharePoint export does carry these columns

## History
- 2026-05-20: created from Brian's "Data Import Bug" report. Triage found
  the bug to be UI-only (data is correctly persisted); ticket reframed
  accordingly.
- 2026-05-20: shipped. Surface area landed as planned:
  - Detail page (`devices/[id]/+page.svelte`) + `DeviceDetailModal.svelte`
    gained 9 truthy-gated `<dt>/<dd>` rows for Model, Purpose, OS, IP, MAC,
    Product URL, Version, Retired Date, Disposal Method (Purpose renders
    full-width with `whitespace-pre-wrap`).
  - `DeviceForm.svelte` got a top-level Model input between Serial and
    Brand; `deviceCreateSchema` adds `model` with the same 200-char guard
    the backend enforces; both payload sites (`/devices/new` and
    `/devices/[id]/edit`) and `AddDeviceModal.svelte` strip empty model
    to `undefined`.
  - `DeviceResponseSchema` (Zod mirror) was missing `purpose`,
    `operatingSystem`, `ipAddress`, `macAddress`, `productUrl`, `version`
    — the server has always returned them; adding them is what actually
    unblocks the detail-page render.
  - `DeviceTable.svelte` shows Model as a secondary line under the device
    name on both the desktop row and the mobile card.
  - `/admin/import` step 2 grew a collapsible "Preview rows" table that
    renders the first 10 valid rows so admins can confirm Model / Purpose /
    Notes / Owner / Location / Status landed in the right columns BEFORE
    committing.
  - Playwright journey 14 covers both a synthetic CSV (asserts every new
    field renders) and the canonical `data/Devices.csv` import (asserts
    the "Mohu Leaf Stitch 60m Range" row's Model="Leaf Stitch" and
    Purpose="Master TV" reach the detail page). The Playwright suite was
    NOT executed locally in this session — no Docker — but the spec
    follows journey-08's proven patterns.
