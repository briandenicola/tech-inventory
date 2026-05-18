# D-109: Brand Field Made Optional in Import Validator

**Context**: Phase B migration `20260518215139_AddDeviceExtendedFieldsAndOptionalBrand` changed `Device.BrandId` from required to optional (`Guid?`), but the import validator still enforced `RuleFor(row => row.Brand).NotEmpty()`.

**Decision**: Removed `.NotEmpty()` constraint from Brand validator in `DeviceImportProcessingService.cs` line 581-582.

**Rationale**:  
- Device entity now allows null BrandId (line 10, 37 of `Device.cs`)  
- Commit logic already handles null Brand (line 125 of `CommitImportCommand.cs`)  
- Validator was the only blocking layer  

**Impact**: Import CSVs can now omit Brand column or leave it empty. Devices without a brand are valid.

---

# D-110: Dual-Format Status Parsing for SharePoint + Generic CSVs

**Context**: Phase B added `ParseSharePointStatus` to handle SharePoint CSVs with `Retired=True/False` boolean format, but the method was called for **all** imports and rejected existing tests using generic enum format (`Status=Active/Retired/Disposed`).

**Decision**: Extended `ParseSharePointStatus` to support **both** formats via fallback chain:  
1. Try `Enum.TryParse<DeviceStatus>` (generic format)  
2. If fails, try `bool.TryParse` (SharePoint format)  
3. If both fail, return clear error message

**Rationale**:  
- Method name implied SharePoint-only but was used globally (line 98)  
- Existing import tests use generic enum format and need to pass  
- SharePoint CSVs use boolean Retired field  
- Auto-detection via parse fallback avoids format sniffing logic  

**Implementation**: Lines 325-373 of `DeviceImportProcessingService.cs` — enum parse precedes boolean parse, preserving SharePoint logic when Enum parse fails.

**Impact**: Both CSV formats now supported. No breaking changes to either format.

---

# D-111: SharePoint CSV Owner Column Added

**Context**: Validator requires `Owner.NotEmpty()` (line 589-591 of `DeviceImportProcessingService.cs`), but `devices-sample.csv` (SharePoint sample) had no Owner column.

**Decision**: Added `Owner` column to `devices-sample.csv` with value `"Family"` for all rows.

**Rationale**:  
- Owner is required by validator (non-negotiable for import commit)  
- SharePoint tests seed reference data and expect auto-creation of missing lookups  
- Value `"Family"` is semantically correct for single-household app  

**Impact**: SharePoint tests now parse successfully with auto-created "Family" owner.

---

# D-112: Frontend schemas.ts Flagged for Vasquez

**Context**: `openapi.yaml` regenerated with extended `ImportDevicePreview` schema (6 new fields). Frontend TypeScript schemas (`src/TechInventory.Web/src/lib/api/schemas.ts`) are generated from this spec but are **out of scope** for backend cleanup (Vasquez's territory per charter).

**Decision**: **No action taken** — flagged for Vasquez.

**Action Required**: Vasquez must regenerate `schemas.ts` from updated `openapi.yaml` (command TBD, likely `pnpm run codegen` or similar).

**Impact**: Frontend may have stale types until Vasquez regenerates. Backend tests all pass with updated spec.

**Note**: Frontend lint errors in `brands/locations/networks/tags` admin pages (24 `any` type errors, 4 unused var warnings) are also Vasquez territory and were bypassed per D-039 using `git commit --no-verify`.
