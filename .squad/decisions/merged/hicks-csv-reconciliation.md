# Device Schema Extension for Real-Inventory CSV Import

**Context:** Brian exported his 551-device SharePoint List to `data/Devices.csv` (gitignored). Analysis revealed schema gaps between the CSV and current Device entity.

## Decisions

### D-095: Make BrandId Nullable
**Rationale:** 37% of real devices (homemade, generic, no-name appliances) have no Vendor value in the CSV. Forcing a "Unknown" brand would be semantic pollution. Nullable `Guid?` with `Guard.AgainstOptionalDefault` allows legitimate brand-less devices.

**Alternatives rejected:**
- Creating a synthetic "Unknown" brand entity → defeats referential integrity semantics
- Blocking import → unacceptable UX for real household data

**Implementation:** Changed `Guid BrandId` → `Guid? BrandId` in Device entity, commands, validators, DTOs. Updated repository export to handle null brand lookups. EF migration makes FK nullable.

### D-096: Add 6 New Device Fields
**Rationale:** SharePoint CSV contains high-value fields absent from v1 schema:

| Field | Max | Population | Notes |
|---|---|---|---|
| Purpose | 500 | 94% | E.g., "Master TV", "Given to Parents", "Antenna for Master Bedroom" |
| OperatingSystem | 100 | 47% | E.g., "Windows 11", "iOS 17.4" |
| IpAddress | 45 | 22% | Supports IPv6 (max 45 chars); optional `IPAddress.TryParse` validation |
| MacAddress | 17 | 11% | Format `XX:XX:XX:XX:XX:XX` (case-insensitive, stored uppercase); regex validated |
| ProductUrl | 500 | 6% | Absolute URI; `Uri.TryCreate` validation |
| Version | 50 | ~100% | Firmware/software version; often "1.0" |

**Schema impact:** All nullable, all additive. Migration adds 6 columns; existing rows unaffected. No breaking changes to API contracts (optional fields).

**Validation approach:**
- `Purpose`, `OperatingSystem`, `Version`: length-only (no semantic validation)
- `IpAddress`: length 45 (IPv6); no regex (too brittle for mixed v4/v6/CIDR input)
- `MacAddress`: strict regex `^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$`, normalized to uppercase
- `ProductUrl`: `Uri.TryCreate(value, UriKind.Absolute, out _)`

### D-097: License Key Field Excluded
**Rationale:** SharePoint CSV has "License Key" column with 2% population (10 of 551 rows). Brian decision: security burden (credential storage, encryption-at-rest requirements, audit logging) outweighs utility for <2% coverage.

**Alternatives rejected:**
- Encrypted license key storage → over-engineering for 10 rows
- Plain-text storage → unacceptable security posture per SECURITY.md

**Action:** Column ignored during CSV import. No field added to Device entity.

### D-098: Networking Column Becomes Network Entities (v1 Ergonomics)
**Rationale:** SharePoint "Networking" column contains 12 distinct values:
- Traditional: `gapa-wifi`, `gapa-iot`, `Wired`, `Wireless`
- **Transport-type tokens:** `Bluetooth`, `z-wave`, `Zigbee`, `sonos-net`

**Decision:** Auto-create Network entities for ALL Networking values during import, including transport types. Ontological purity (networks vs. protocols) sacrificed for v1 ergonomics:
- User can rename/merge via UI post-import
- Alternative (hard-coded protocol enum) would block unknown future transports
- Household ergonomics trump technical rigor for single-family self-hosted app

**Migration note:** Import creates Networks; user manually consolidates if desired.

### D-099: Status Mapping via Retired + Purpose Regex
**Rationale:** CSV has binary `Retired` (True/False) + free-text `Purpose`. Analysis found disposal patterns:

**Mapping logic:**
- `Retired == "False"` → `DeviceStatus.Active`
- `Retired == "True"` + Purpose matches `^(.*(sold|given|donated|gifted|disposed|trashed).*)$` (case-insensitive) → `DeviceStatus.Disposed`, set `DisposalMethod = Purpose` (truncated to 500)
- `Retired == "True"` + no pattern match → `DeviceStatus.Retired`

**Rationale for regex:** "Given to Parents", "Sold To Alex Smart", "Donated to Goodwill" are semantically disposal events, not generic retirement. Regex extracts intent from free text.

**Edge case:** False negatives acceptable (user can manually transition Retired → Disposed via UI).

### D-100: RetiredDate Heuristic
**Rationale:** CSV lacks explicit `RetiredDate` or `DisposedDate`. When `Retired == "True"`:

**Heuristic:** Set `RetiredDate = PurchaseDate` if no better signal exists.

**Rationale:**
- Better than NULL (enables "device lifespan" calculations)
- Conservative assumption (device likely retired closer to purchase than present day for truly old devices)
- User can manually correct via UI if they remember actual retirement date

**Limitations:** Acknowledged inaccuracy; alternative (leave NULL) loses analytical value.

### D-101: Reference Data Auto-Create on Import (Idempotent Name-Match)
**Rationale:** CSV references Brands/Categories/Locations/Networks by name (strings), not IDs. Import must resolve or create.

**Policy:**
- **Find by name (case-insensitive):** `StringComparer.OrdinalIgnoreCase`
- **Create if missing:** Auto-create inactive=false entity
- **Batch-local cache:** Single resolution per import batch (avoid N round-trips for shared references)

**Idempotency:** Re-importing same file produces no duplicate reference entities (name-match deduplication).

**Application to columns:**
- `DeviceName` (Device.Name) → never auto-created (Device is top-level)
- `Vendor` (Brand.Name) → auto-create if missing; nullable if blank
- `DeviceType` (Category.Name) → auto-create if missing
- `Owner` (Owner.DisplayName) → auto-create if missing
- `Location` (Location.Name) → auto-create if missing
- `Networking` (Network.Name) → auto-create if missing; NULL if "N/A"

**Contrast with existing Phase 1 import:** Phase 1 allowed Network auto-create=false (user must pre-create). Phase 2 CSV mapper allows auto-create=true for all reference entities to match SharePoint export ergonomics.

### D-102: Synthetic Sample Fixture Pattern
**Rationale:** `data/Devices.csv` (551 rows) is gitignored (real household PII: IP addresses, serial numbers, room names). Integration tests need committed fixtures.

**Solution:** `tests/.../SampleData/devices-sample.csv` (10 synthetic rows) committed to repo. Covers:
- All status mappings (Active, Retired, Disposed via regex)
- All Networking variants (N/A→null, valid values→Network entities)
- Blank Vendor (nullable Brand)
- Populated Vendor (auto-create Brand)
- All 6 new fields populated in at least 2 rows
- Edge case: whitespace-only Purpose (should map to null after trim)

**Naming convention:** Real-world plausible but clearly fabricated (e.g., DeviceName="Living Room Roku Express", Location="Demo Living Room", Brand="Roku").

**Coverage verification:** Integration test asserts device count, status distribution, reference entity counts, zero errors.

## Implementation Artifacts

**Migration:** `20260518215139_AddDeviceExtendedFieldsAndOptionalBrand`
- 6 new nullable columns (Purpose, OperatingSystem, IpAddress, MacAddress, ProductUrl, Version)
- BrandId FK changed to nullable
- All additive; no data migration required

**Commit:** `46f6042` (Phase A - schema extension)
**Tests:** 374 passing / 6 skipped / 0 failed
**Coverage:** Domain 100.00% / Application 91.58%+ / Infrastructure 94.33%+ / Api 91.63%+

## Phase B Scope (Deferred)

CSV mapper implementation (`DevicesCsvMapper.cs`) + integration tests deferred to separate session due to time constraints. Schema foundation complete; import logic follows naturally from decisions above.

**Mapping rules documented in charter:**
- CsvHelper configuration (trim, case-insensitive headers, ignore bad data)
- Column name aliases (e.g., "IP Address" → IpAddress, "Device Name" → Name)
- Whitespace + "N/A" normalization
- Status regex (D-099)
- RetiredDate heuristic (D-100)
- Reference data auto-create (D-101)

**Sample fixture stub:** 10-row CSV structure defined; full integration test implementation pending.

---
**Scribe note:** Renumber D-095..D-102 if inbox has higher-numbered pending decisions at merge time.
