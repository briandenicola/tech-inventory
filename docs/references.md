# Reference Implementations

These are existing applications by the same author that inform the
architectural, operational, and stylistic patterns of this project.
They are **reference material**, not dependencies. Copilot agents and
contributors should consult them when noted in a spec or ADR, but
**must not** copy code wholesale — patterns yes, code no.

> **Provenance discipline**: when borrowing a pattern, cite the source
> repo + commit SHA + path in the commit message or ADR. Do not assume
> these repos are stable; pin to a SHA.

---

## R1: drinks-and-desserts

- **Repo**: https://github.com/briandenicola/drinks-and-desserts
- **Pinned commit**: `<paste-sha-here>` (last reviewed: YYYY-MM-DD)
- **License**: <check repo>
- **Stack**: <fill in: e.g., .NET 8 API + React, Azure, etc.>
- **Status**: production / archived / experimental

### What This Project Demonstrates Well
- <e.g., Clean Architecture layering — see `src/Application/`>
- <e.g., FluentValidation usage>
- <e.g., GitHub Actions workflow shape>
- <e.g., EF Core migration discipline>

### Patterns to Borrow
| Pattern | Source Path | Adopt For |
|---|---|---|
| MediatR handler structure | `src/Application/Features/` | Spec 001 Core API |
| Problem Details middleware | `src/Api/Middleware/` | Spec 001 error model |
| Health check setup | `src/Api/Program.cs` | Spec 001 ops endpoints |

### Patterns NOT to Borrow
- <e.g., "Uses Azure App Configuration — we're self-hosted, use env vars">
- <e.g., "Pre-.NET 10 minimal API shape — we use newer patterns">
- <e.g., "No tenancy code — irrelevant for household-scope app">

### When to Consult
- During Spec 001 (Core API) planning
- When designing the error response shape
- When wiring up MediatR + FluentValidation

---

## R2: coin-collection-app

- **Repo**: https://github.com/briandenicola/coin-collection-app
- **Pinned commit**: `<paste-sha-here>` (last reviewed: YYYY-MM-DD)
- **License**: <check repo>
- **Stack**: <fill in>
- **Status**: production / archived / experimental

### What This Project Demonstrates Well
- <e.g., Domain modeling for a personal inventory system — directly analogous>
- <e.g., CSV import workflow>
- <e.g., Photo attachment handling>
- <e.g., Self-hosted Docker deployment shape>

### Patterns to Borrow
| Pattern | Source Path | Adopt For |
|---|---|---|
| Inventory domain shape | `src/Domain/` | Spec 001 device model |
| CSV import pipeline | `src/Application/Import/` | Spec 001 ingest endpoint |
| Attachment storage | `src/Infrastructure/Attachments/` | F018 (photo AI) |
| Docker Compose layout | `deploy/` | Spec 003 deployment |

### Patterns NOT to Borrow
- <e.g., "Single-user — we have multi-user with Entra ID">
- <e.g., "No audit log — we require it (constitution §4)">
- <e.g., "Free-text categories — we use FK reference data (§4.5)">

### When to Consult
- During Spec 001, especially data model and CSV import
- When designing attachment storage for F018
- When planning the Docker Compose structure

---

## How AI Agents Should Use This Document

1. When working a spec or task that names a reference (e.g., "see R2 for
   CSV import pattern"), the agent **must**:
   - Fetch the referenced repo at the pinned SHA
   - Read only the cited paths (not the whole repo)
   - Summarize what's adoptable in 3–5 bullets before writing code
   - Cite source path + SHA in the commit message

2. The agent **must not**:
   - Copy code verbatim without attribution + license check
   - Wander into uncited parts of the reference repo
   - Assume the reference is canonical — this project's constitution wins

3. When a reference and the constitution conflict, the **constitution wins**.
   Note the conflict in the relevant ADR.

---

## Updating This File

- Pin a new SHA when reviewing a reference repo for fresh patterns
- Add a row to History when references are added, retired, or re-pinned
- Retired references move to a `## Archived References` section, not deleted

## History
- YYYY-MM-DD: created with R1 (drinks-and-desserts), R2 (coin-collection-app)