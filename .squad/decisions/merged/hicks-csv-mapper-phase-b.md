# CSV Import Mapper — Phase B Design Decisions

**Context**: SharePoint Devices.csv import support (PRD §F1, D-095..D-102).  
**Date**: 2025-05-18  
**Author**: Hicks (backend agent)  
**Status**: Implemented  

---

## D-103: SharePoint Status Mapping via Boolean + Regex

**Decision**: Retired column (True/False) combined with Purpose field regex drives DeviceStatus mapping.

**Rationale**:
- Brian's CSV has `Retired=True/False` instead of Status enum
- `Retired=False` → `DeviceStatus.Active`
- `Retired=True` + Purpose matching `/sold|given|donated|gifted|disposed|trashed/i` → `DeviceStatus.Disposed` (DisposalMethod = Purpose value)
- `Retired=True` otherwise → `DeviceStatus.Retired` (RetiredDate fallback to PurchaseDate per D-100)
- Single parse function `ParseSharePointStatus` in `DeviceImportProcessingService` handles all 3 branches
- Regex allows flexible user phrasing ("sold to neighbor", "given to John", etc.)

**Alternatives Rejected**:
- Hard-coded keyword list (less flexible)
- Manual Status column requirement (breaks existing CSV compatibility)
- Case-sensitive matching (user-hostile)

**Implementation**: `src/TechInventory.Application/Imports/DeviceImportProcessingService.cs` lines ~309-347

---

## D-104: Network Auto-Creation Enabled (Reversal of Phase 1 Decision)

**Decision**: Network entities auto-create on import, same as Brand/Category/Location/Owner.

**Rationale**:
- Brian's Networking column has **37 distinct values** including transport types ("Bluetooth", "Wired", "z-wave")
- Manually pre-creating 37 networks before import is user-hostile
- Idempotent cache (Dictionary keyed by normalized name) prevents duplicates within same batch
- User can rename/merge via R6a admin UI post-import
- Aligns with D-101 auto-create policy for reference data

**Phase 1 Context**: Original design set `allowCreate: false` for Networks due to ambiguity concerns. Brian's real CSV proved those concerns were unfounded — he wants **all** networking values as entities.

**Implementation**: 
- `DeviceImportProcessingService.cs` line ~172: `allowCreate: true`
- `CommitImportCommand.cs` lines ~45, ~98: Network auto-creation case + cache

---

## D-105: "N/A" Networking → Null Association

**Decision**: Exact-string "N/A" (case-insensitive) in Networking column results in null NetworkId. Any other value creates/references a Network entity.

**Rationale**:
- Brian uses "N/A" to explicitly mark devices with no network (e.g., offline switches)
- Alternative interpretations ("Not Applicable", "None", blank) NOT treated as N/A — only exact match
- Simplest unambiguous rule; no regex/fuzzy matching overhead
- Aligns with D-098

**Implementation**: `NormalizeNetworking` helper in `DeviceImportProcessingService.cs` line ~349

---

## D-106: MAC Address Normalization to Colon-Separated Format

**Decision**: All MAC addresses normalized to `XX:XX:XX:XX:XX:XX` (uppercase, colon-separated) regardless of input format.

**Rationale**:
- Brian's CSV has mixed delimiters: `AA:BB:CC:DD:EE:FF`, `00-11-22-33-44-55`, `aabbccddeeff`
- Domain validator (`DeviceValidationRules`) expects colon format
- Normalization strips all delimiters (`-`, `:`, `.`, space), validates 12 hex digits, then re-formats with colons
- Invalid MACs rejected with clear error message
- Consistent storage format simplifies queries/reporting

**Implementation**: `NormalizeMacAddress` helper in `DeviceImportProcessingService.cs` lines ~356-371

---

## D-107: URL Validation as Absolute HTTP/HTTPS Only

**Decision**: ProductUrl column validated as absolute URI with http/https scheme. File URLs, relative URLs, and non-HTTP protocols rejected.

**Rationale**:
- Brian's URL column points to manufacturer product pages (all web URLs)
- `Uri.TryCreate(..., UriKind.Absolute, ...)` + scheme check catches malformed/dangerous inputs
- Prevents `file://`, `javascript:`, `data:` injection vectors
- Blank/null URLs allowed (6% populated per analysis)
- Error message includes problematic URL for easy correction

**Implementation**: `NormalizeProductUrl` helper in `DeviceImportProcessingService.cs` lines ~373-388

---

## D-108: Mapper Integrated into Existing DeviceImportProcessingService

**Decision**: Phase B mapper logic added directly to existing `DeviceImportProcessingService` via helper methods, not as separate mapper class.

**Rationale**:
- Existing service already owns CSV parsing + lookup catalog
- 6 new extended fields (Purpose, OperatingSystem, IpAddress, MacAddress, ProductUrl, Version) passed through candidate → preview → commit pipeline with **zero architectural changes**
- SharePoint-specific logic isolated to 4 helper methods: `ParseSharePointStatus`, `NormalizeNetworking`, `NormalizeMacAddress`, `NormalizeProductUrl`
- Avoids duplication of field-reading + validation infrastructure
- ImportDeviceCandidate record extended with 6 new properties; ImportDevicePreview extended identically
- All existing preview/commit tests remain green (374 passing before Phase B)

**Alternatives Rejected**:
- Separate `SharePointCsvMapper` class (unnecessary abstraction; would duplicate 90% of parsing logic)
- Dual-parser architecture (Phase 1 vs Phase B) with factory pattern (premature; no evidence of other CSV schemas)

**Modified Files**:
- `ImportFieldNames.cs`: Added 11 aliases (Vendor→Brand, DeviceType→Category, etc.)
- `DeviceImportProcessingService.cs`: Extended candidate/preview records + 4 helpers (~120 new lines)
- `CommitImportCommand.cs`: Extended Device.Create call with 6 new parameters + Network auto-creation
- `ImportContracts.cs`: Extended ImportDevicePreview record signature

---

## Decision Summary

| ID | Topic | Core Decision |
|----|-------|---------------|
| D-103 | Status mapping | Retired bool + Purpose regex → 3-way status branch |
| D-104 | Network auto-create | Enabled (reversal of Phase 1 `allowCreate: false`) |
| D-105 | "N/A" handling | Exact match "N/A" → null NetworkId |
| D-106 | MAC normalization | Always `XX:XX:XX:XX:XX:XX` colon format |
| D-107 | URL validation | Absolute HTTP/HTTPS only |
| D-108 | Mapper placement | Integrated into existing service via helpers |

---

**Next Steps** (for Brian/Coordinator):
1. Integration tests at `tests/.../SharePointCsvImportTests.cs` need household seeding fix (all 3 failing with 409 Conflict due to missing Household seed)
2. Synthetic sample CSV (`devices-sample.csv`) committed; real `data/Devices.csv` remains gitignored
3. Unit tests for individual mappers (NormalizeMacAddress, ParseSharePointStatus, etc.) deferred to follow-up
4. Coverage delta TBD once integration tests pass

**Git Blame Trail**: Commits `46f6042` (Phase A schema), `a273faa` (Phase A decision drop), `<PHASE_B_SHA>` (this implementation)
