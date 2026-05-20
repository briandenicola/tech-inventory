# F033: Device Images, Room Capture & Auto-Creation from Photos

**Status**: backlog
**Priority**: P3
**Effort**: XL
**Value**: high
**Risk**: high
**Target release**: v1.3+
**Created**: 2026-05-19
**Owner**: ripley

## Problem
Brian wants to attach images to devices, and — beyond that — to do a "Room
Capture" pass: walk through a room with the phone, snap photos, and have the
PWA propose device records (with tags) auto-extracted from each photo. This
sits at the intersection of three concerns:

1. **Plain image upload + display** per device (the PRD already lists
   photos under detail view, marked v2).
2. **Auto-creation of device candidates from photos** — overlaps with the
   existing backlog item **F018 (Photo-to-device AI draft)**.
3. **Room Capture flow** — a new mobile-first capture journey that groups
   photos by Location, runs them through the candidate-extraction pipeline,
   and presents a review/approve queue.

Today there is **no image storage**, **no image API**, **no on-device or
server-side vision integration**, and **no capture flow**. Every piece is
greenfield.

## Proposed Solution
This is an umbrella entry. It should be split into multiple specs once
promoted from backlog — but capture the framing here so we don't lose it.

### Slice A — Image upload + display (foundation)
- New `DeviceImage` entity: `Id`, `DeviceId`, `StorageKey`, `MimeType`,
  `SizeBytes`, `Width`, `Height`, `CapturedAt`, `UploadedBy`, `CreatedAt`.
- Storage abstraction `IObjectStorage` with two implementations:
  - `FileSystemObjectStorage` (default, writes under
    `${TI_DATA_DIR}/images/{deviceId}/{imageId}.{ext}`).
  - Future swap-in for S3-compatible if/when needed.
- API:
  - `POST /api/v1/devices/{id}/images` (multipart, single file, Admin/Member).
  - `GET /api/v1/devices/{id}/images` (list).
  - `GET /api/v1/images/{id}` (binary, with `Cache-Control` + ETag).
  - `DELETE /api/v1/images/{id}` (Admin only, soft-delete).
- Frontend: photo gallery on device detail; upload button; mobile camera
  capture via `<input type="file" accept="image/*" capture="environment">`.
- Thumbnails: pre-generate 256 px JPEG on upload (server-side, single
  image manipulation library; Ripley ADR for choice).

### Slice B — Auto-creation candidates (consolidates with F018)
- After upload, optional pipeline step extracts:
  - Brand candidates (logo/text OCR).
  - Category candidate (image classifier).
  - Suggested tags.
  - Possibly model number from visible labels.
- Produces a `DeviceCandidate` record reviewed by the user before commit.
- Inference: **on-device first when feasible** (privacy contract per PRD),
  cloud only as opt-in with an ADR; Bishop reviews any cloud egress.
- Coexists with F018 — F033 is the capture/upload surface; F018 owns the
  inference quality and model selection.

### Slice C — Room Capture flow
- New `/devices/capture` mobile-first route.
- User picks a Location (or creates one).
- Camera capture in a loop: snap → thumbnail appears in a tray → snap next.
- After capture: review queue runs each photo through Slice B's candidate
  extractor; user accepts/edits each candidate; bulk-commit creates devices
  pinned to the chosen Location with the uploaded photo attached.

## User Stories
- *As anyone, I attach photos to a device when I create or edit it.*
- *As Brian, I walk through the office, snap 12 photos, and the PWA proposes
  12 device candidates with brand/category/tag suggestions for me to
  approve.*
- *As Admin, photo storage stays on my host — nothing leaves the network
  unless I opt in to cloud inference.*

## Acceptance Criteria (per slice; full list deferred to spec promotion)

### Slice A — must-haves
- [ ] `DeviceImage` migration + repository tests
- [ ] Upload, list, download, delete endpoints with OpenAPI contract
- [ ] File-size cap + MIME-type allow-list enforced on upload (Bishop
      threat-model row updated)
- [ ] Image gallery on device detail page with mobile camera capture
- [ ] Thumbnail pre-generation
- [ ] Deletion is soft-delete; binary stays until retention job purges
      (separate ops decision)
- [ ] Hudson: container volume sizing + backup strategy includes images

### Slice B/C — captured here, fully fleshed when promoted
- [ ] Coordinate with F018 so we don't ship two competing pipelines
- [ ] Bishop sign-off on any cloud-egress path
- [ ] Ripley ADR for inference strategy (local vs. cloud, model choice)

## Out of Scope
- Generic file attachments (manuals, receipts) — separate backlog.
- Video capture — separate backlog.
- Real-time AR overlays / live device recognition.
- Sharing photos outside the household.

## Dependencies
- **F018** — existing backlog item for photo-to-device AI; F033 is the
  capture/storage half. Reconcile at promotion time.
- Ripley: ADR(s) for storage abstraction, image library, inference strategy.
- Bishop: file-upload threat model (size/type, virus scan, content-type
  sniffing); cloud-egress review if/when applicable.
- Hudson: object-store sizing, backup inclusion, healthcheck for the new
  storage volume.
- Drake: camera/capture-flow glyphs and visual rhythm.

## Open Questions
- Storage backend default: filesystem under `${TI_DATA_DIR}` is simplest;
  S3-compatible (MinIO sidecar) is more scalable. Default to filesystem;
  abstraction permits a swap.
- On-device inference is privacy-clean but model-quality is limited. Cloud
  is the opposite. Bishop + Ripley decide via ADR; default position is
  "on-device only for v1.3, cloud as a future opt-in."
- Do we generate multiple thumbnail sizes (responsive)? Default to one
  256 px JPEG for v1.3.

## Notes / Research
- This is intentionally an **umbrella** entry. When promoted to
  `specs/00X-*/`, it should be split into Slice A (foundation) and a
  separate spec for Slice B+C (capture + AI), each with its own plan/tasks.
- PRD §7 (Privacy) sets the constraint: "Data never leaves the host"
  unless explicitly opted in.
- Memory: *"AuditEvent is append-only"* — image upload/delete should
  emit AuditEvent rows for accountability (same `IAuditable` pattern as
  other mutations).

## History
- 2026-05-19: created from Brian's PWA field-test feedback (item 18 in
  session plan). Explicitly cross-references existing F018 so they get
  reconciled rather than racing.
