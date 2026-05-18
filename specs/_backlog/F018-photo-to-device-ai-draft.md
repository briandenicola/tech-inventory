# F018: AI Photo-to-Device Draft Entry

**Status**: backlog
**Priority**: P3
**Effort**: L
**Value**: high
**Risk**: medium
**Target release**: TBD (post-v1)
**Created**: 2026-05-17
**Owner**: unassigned

## Problem
Adding a device to the inventory is currently a tedious, multi-field form: name,
brand, model, category, serial number, location, purchase date, etc. For a
family member who just unboxed a new gadget or is cataloging existing items,
this friction is the single largest barrier to keeping the inventory accurate
and complete. Many devices never get added simply because the entry process
takes longer than the perceived value of doing it.

Photos already capture most of this information visually — brand logos, model
numbers, serial labels, device shape — but a human currently has to transcribe
that into form fields. This is exactly the kind of pattern-matching work that
local multimodal AI can do well, and offloading it removes the largest friction
point in the core data entry workflow.

## Proposed Solution
Allow a Member or Admin to take or upload one or more photos of a device. The
photos are sent to a **local Ollama instance** running a multimodal model
(e.g., `llava`, `llama3.2-vision`, `qwen2-vl`) which analyzes the image(s) and
returns a **draft device record** with as many fields pre-populated as the
model can confidently infer.

The user is then shown a **review screen** with the draft, with every
AI-suggested field clearly flagged (e.g., a small ✨ icon and confidence
indicator) so they know what to verify. They can edit any field, accept, or
discard the draft. Nothing is written to the inventory until the user explicitly
saves.

The original photos are stored as attachments on the resulting device record
(per existing Attachment entity in PRD §8), creating a visual audit trail.

**Privacy guarantee**: all inference runs against the household's own Ollama
container. No image, prompt, or response ever leaves the local network. This
is enforced architecturally (no external HTTP egress from the AI service)
and verified by network policy.

## User Stories
- **U-AI1**: As a Member, I take a photo of a new device with my phone and
  upload it; within seconds I see a draft entry with brand, model, category,
  and any visible serial/identifiers already filled in, so I only need to
  confirm and add household-specific fields (location, owner).
- **U-AI2**: As a Member, I upload multiple photos of the same device (front,
  back, serial label, packaging) and the AI consolidates them into a single
  draft, using the clearest source for each field.
- **U-AI3**: As a Member, I clearly see which fields were AI-suggested vs.
  empty vs. user-edited, so I know what to verify before saving.
- **U-AI4**: As a Member, when the AI cannot identify the device, I receive a
  graceful "couldn't auto-fill — please enter manually" message with the photo
  pre-attached, so my work isn't lost.
- **U-AI5**: As an Admin, I configure which Ollama model is used and view its
  performance (average latency, success rate) so I can tune the experience.
- **U-AI6**: As an Admin, I can disable the AI draft feature entirely via a
  setting, so households that don't run Ollama have a clean UI without broken
  buttons.
- **U-AI7**: As any user, the AI feature gracefully degrades when Ollama is
  unreachable — the photo upload still works, the draft fields are just empty,
  and the manual form is presented.
- **U-AI8**: As a Viewer, I do not see the photo-to-draft feature (creation is
  Member+ per PRD §4).

## Acceptance Criteria
- [ ] User can upload 1–5 photos (JPEG, PNG, HEIC) up to 10 MB each per draft
- [ ] Photos are submitted to a local Ollama endpoint configured via env var
- [ ] No HTTP traffic from the AI service container reaches any external host
      (verified by network policy and audit)
- [ ] Returned draft includes structured fields: `name`, `brand`, `model`,
      `category` (matched to existing reference data when possible), `serial`,
      `notes`
- [ ] AI-suggested values are tagged with a confidence score (low/medium/high)
      and visually distinguished in the UI
- [ ] User can edit any field before save; edits override AI suggestions
- [ ] Original photos are saved as Attachments linked to the created device
- [ ] If Ollama is unreachable, request fails fast (< 2s timeout to detect)
      and falls back to empty manual form with photos pre-attached
- [ ] If Ollama responds but cannot identify the device, a clear "no
      suggestions" message is shown, manual form is presented
- [ ] Audit log records: who uploaded, when, model used, fields suggested,
      fields user changed before save (per constitution §4 Audit)
- [ ] AI inference latency target: p50 < 10s, p95 < 30s on reference hardware
      (documented in deployment guide)
- [ ] Feature flag (`features.aiPhotoIngest`) disables the entire flow at the
      API and UI levels; no broken buttons remain
- [ ] Category match attempts to map to existing reference data
      (per F012 reference data); falls back to "Uncategorized" if no match
- [ ] OpenAPI documents the new endpoints: `POST /api/v1/devices/draft-from-photos`
      returns 202 + draft preview, then `POST /api/v1/devices` accepts the
      reviewed draft
- [ ] WCAG 2.2 AA compliant per constitution §6.5.6 (keyboard, screen reader,
      contrast, focus, error announcements)
- [ ] Lighthouse and axe-core clean on the review screen
- [ ] Test coverage ≥ 85% on the AI orchestration layer; integration tests use
      a mocked Ollama client (no model required in CI)
- [ ] At least one E2E happy path test with a real photo + locally-hosted
      Ollama (gated to local dev / nightly CI, not PR CI)

## Out of Scope
- Real-time camera / live AR overlay (this is a post-capture flow only)
- Automatic device identification from product barcodes / QR codes (separate
  feature; could combine later)
- AI-generated description, market value lookup, or external enrichment
  (would require internet egress; violates privacy stance)
- Re-running AI on existing devices to backfill fields (future enhancement)
- Multiple-device-per-photo (one shoebox of cables → many devices) — too
  ambiguous for v1; one photo set = one device
- Voice input or natural language description (separate feature)
- Training, fine-tuning, or selecting between multiple models at runtime
  per request — admin picks one model globally
- GPU detection or auto-configuration of Ollama — operator's responsibility
- Mobile camera capture optimizations (image rotation, EXIF, HEIC conversion)
  beyond what the browser provides natively in v1

## Dependencies
- **F001 Core API + Domain** must be complete (Device, Category, Attachment
  entities exist)
- **F012 Reference Data Admin** should exist so Category mapping has targets
  (soft dependency — can ship with category-as-string fallback)
- **Ollama** running and reachable on the local network. Operator's
  responsibility; we provide a reference `docker-compose.ollama-example.yml`
- **Attachment storage** infrastructure (filesystem or S3-compatible volume)
  must be in place
- **Authentication** (F002) — only Members and Admins can create devices
- **Audit log** infrastructure (per constitution §4) for tracking AI usage
- New ADR: `docs/adr/00XX-local-ai-inference-via-ollama.md` documenting the
  decision to depend on Ollama vs. embedded models

## Open Questions
- **Q1**: Which Ollama model is the default recommendation? `llava:13b`,
  `llama3.2-vision`, `qwen2-vl`? Needs benchmarking on reference hardware
  for accuracy + latency + RAM footprint.
- **Q2**: How do we match free-text category output to existing Category
  reference data? Fuzzy match? Embedding similarity? Strict lookup with
  "Uncategorized" fallback?
- **Q3**: Should multi-photo input be merged in a single Ollama call (longer
  prompt) or sequential calls reconciled by us? Latency vs. accuracy tradeoff.
- **Q4**: What's the prompt template? Needs to be:
  (a) deterministic enough to parse, (b) JSON-shaped output, (c) versioned
  so we can iterate without breaking existing audit records.
- **Q5**: Do we cache results by image hash to avoid redundant inference if
  a user re-uploads the same photo?
- **Q6**: Image preprocessing — do we resize/recompress before sending to
  Ollama to keep latency down? At what resolution?
- **Q7**: Confidence scores — does the model emit them, or do we synthesize
  them from response heuristics (field present vs. "unknown")?
- **Q8**: Storage of original photos — full resolution, or downscaled? EXIF
  stripped? (Privacy: EXIF often contains GPS.)
- **Q9**: Rate limiting — should we throttle inference requests to protect
  shared Ollama capacity (e.g., if family member is mass-importing)?
- **Q10**: Does this need a dedicated AI service container, or can the API
  call Ollama directly? Pros of a dedicated service: cleaner separation,
  easier to evolve prompts. Cons: another container to operate.

## Notes / Research
- Ollama supports multimodal models via the same `/api/generate` and
  `/api/chat` endpoints used for text; image is base64-encoded in the request
- Reference candidate models (as of late 2024):
  - `llava:13b` — well-tested, broad device knowledge, ~8GB VRAM
  - `llama3.2-vision:11b` — newer, strong instruction-following
  - `qwen2-vl:7b` — smaller footprint, multilingual
- Recommendation: ship with a documented "verified" model list and let the
  admin pick; default to the smallest reliable option
- Prompt engineering: use structured-output / JSON-schema prompting; consider
  Ollama's `format: "json"` parameter when supported
- Security: the AI service container should be on the `app` network only,
  with **no route to the internet** — verify via `docker network inspect`
  and a startup self-test that attempts an external DNS resolution and logs
  a critical failure if it succeeds
- Constitution alignment:
  - §4 (Persistence): AI suggestions are *drafts* — not persisted until user
    saves
  - §4.5 (Reference Data): AI must respect curated Category/Location lists
  - §5 (Security): no external egress; image content treated as user data
    (encrypted at rest if attachment encryption is enabled)
  - §6.5.4 (Component Discipline): the review screen needs loading / empty /
    error / success states all defined
  - §10.2 (Never): "invent APIs or package names" — prompts must be designed
    so hallucinated brands/models are caught (low confidence → flagged)
- UX inspiration: Apple Photos "people" suggestions — AI proposes, human
  confirms, never auto-files

## History
- 2025-01-XX: created