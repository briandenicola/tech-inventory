# STRIDE Threat Modeling Skill

**Skill ID**: `stride-framework`  
**Author**: Bishop  
**Project**: Tech Inventory  
**Date Created**: 2026-05-18

---

## Overview

STRIDE is a systematic threat modeling framework for identifying security threats across system surfaces.

- **S** = Spoofing (identity fraud)
- **T** = Tampering (data/code modification)
- **R** = Repudiation (deny actions)
- **I** = Information Disclosure (leaks)
- **D** = Denial of Service (availability)
- **E** = Elevation of Privilege (RBAC bypass)

## When to Use

- Early design phase (before coding begins).
- After major architectural changes.
- Per-surface threat identification (web client, API, database, auth, infrastructure).
- To validate compliance with OWASP ASVS L2 / NIST standards.

## Methodology

### 1. Identify Surfaces

List all system boundaries where threats can occur. Examples:
- Web Client (SvelteKit PWA)
- API (ASP.NET Core)
- Database (SQLite)
- Auth Provider (Entra ID)
- Container Host (Docker)
- Reverse Proxy (TLS termination)
- Backup Destination (off-site)

### 2. Per-Surface STRIDE Analysis

For each surface, analyze six threat categories:

| Category | Questions |
|----------|-----------|
| **Spoofing** | Can an attacker impersonate a legitimate user/system? Can identities be forged? |
| **Tampering** | Can an attacker modify data, code, or configuration in flight or at rest? |
| **Repudiation** | Can an actor deny performing an action (lack of audit trail)? |
| **Information Disclosure** | Can sensitive data be leaked (secrets, tokens, PII)? |
| **Denial of Service** | Can the system be made unavailable (crash, resource exhaustion, rate-limit bypass)? |
| **Elevation of Privilege** | Can a low-privilege user gain admin rights or bypass access controls? |

### 3. For Each Threat, Document

- **Threat description**: What is the specific attack?
- **Mitigation**: What controls reduce or eliminate the threat?
- **Residual risk**: After mitigations, how likely/severe is the threat? (Critical / High / Medium / Low)

### 4. Cross-Reference Standards

Link threats to OWASP standards:
- **ASVS**: OWASP Application Security Verification Standard (e.g., V2.10.2 for credential storage).
- **API Top 10 (2023)**: OWASP API Security risks (e.g., API1:2023 BOLA).
- **CWE Top 25**: Common Weakness Enumeration (e.g., CWE-352 CSRF).

---

## Template (Markdown)

```markdown
# Threat Model — [System Name] [Version]

**Scope**: [One-sentence overview]
**Methodology**: STRIDE per surface
**Baseline**: [Standards: OWASP ASVS L2, OWASP API Top 10 (2023), etc.]

---

## 1. System Surfaces

| # | Surface | Technology | Owner | Notes |
|---|---------|-----------|-------|-------|
| 1 | [Name] | [Tech Stack] | [Owner] | [Brief purpose] |

---

## 2. Surface-by-Surface STRIDE Analysis

### Surface 1: [Name]

#### Spoofing (Identity Fraud)
- **Threat**: [Describe attack]
- **Mitigation**: [Controls]
- **Residual Risk**: [Level]

#### Tampering (Modification)
- **Threat**: [Describe attack]
- **Mitigation**: [Controls]
- **Residual Risk**: [Level]

#### Repudiation (Deny Actions)
- **Threat**: [Describe attack]
- **Mitigation**: [Controls]
- **Residual Risk**: [Level]

#### Information Disclosure (Leaks)
- **Threat**: [Describe attack]
- **Mitigation**: [Controls]
- **Residual Risk**: [Level]

#### Denial of Service (Availability)
- **Threat**: [Describe attack]
- **Mitigation**: [Controls]
- **Residual Risk**: [Level]

#### Elevation of Privilege (RBAC Bypass)
- **Threat**: [Describe attack]
- **Mitigation**: [Controls]
- **Residual Risk**: [Level]

---

## 3. Summary by Risk Level

### Critical
[List threats]

### High
[List threats]

### Medium
[List threats]

### Low
[List threats]

---

## 4. OWASP Alignment

| Standard | Control / Risk | Status | Notes |
|----------|---|---|---|
| ASVS V2.10.2 | [Control name] | ✓ | [Implementation] |
| API1:2023 | BOLA | ✓ | [Implementation] |

---

## 5. Next Steps

- [ ] Architecture review
- [ ] Implementation planning
- [ ] Post-implementation verification

---

## 6. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | YYYY-MM-DD | [Author] | Initial analysis |
```

---

## Example: Web Client (SvelteKit PWA)

**Spoofing**
- *Threat*: Attacker injects fake JWT token or hijacks MSAL.js session.
- *Mitigation*: MSAL.js validates issuer, audience, signature cryptographically. Tokens stored in memory (not localStorage) to prevent XSS exfiltration.
- *Residual Risk*: Low.

**Tampering**
- *Threat*: XSS attack injects malicious script to modify DOM or exfiltrate data.
- *Mitigation*: CSP `strict-dynamic` + SRI on external scripts. No `dangerouslySetInnerHTML`. Output encoding default.
- *Residual Risk*: Low.

**Repudiation**
- *Threat*: User claims they didn't perform an action (e.g., device edit).
- *Mitigation*: All mutations logged server-side (audit trail). Request correlation ID linked to backend logs.
- *Residual Risk*: Very Low.

**Information Disclosure**
- *Threat*: XSS steals session tokens, device serials, or user emails.
- *Mitigation*: Tokens in memory/sessionStorage (cleared on page reload). HTTPS only. Rate limiting on API.
- *Residual Risk*: Medium (XSS is still a risk until CSP hardening is complete).

**Denial of Service**
- *Threat*: Attacker floods client with requests or consumes all storage.
- *Mitigation*: Service Worker caches app shell. Debouncing on search. API rate limiting per user.
- *Residual Risk*: Low.

**Elevation of Privilege**
- *Threat*: Viewer role sees/edits Admin-only affordances or makes API calls as Admin.
- *Mitigation*: UI hides edit/delete for Viewer (client-side only). API re-validates on every request (server-side policy).
- *Residual Risk*: Very Low (server-side policy is mandatory).

---

## Lessons Learned

1. **STRIDE works best with cross-functional team**: Architect (surfaces), Security (threats), Developer (mitigations).
2. **Residual risk is honest**: Document what remains after mitigations, not just the ideal state.
3. **Standards alignment is key**: Link threats to ASVS/NIST to justify priority and effort.
4. **Iterate per phase**: Threat model v1.0 at design time; update post-implementation and per major change.
5. **Don't over-analyze**: Focus on plausible attacks for the threat model to stay actionable.

---

## References

- OWASP STRIDE: https://owasp.org/www-community/attacks/STRIDE_Model
- OWASP ASVS: https://owasp.org/www-project-application-security-verification-standard/
- OWASP API Security Top 10 (2023): https://owasp.org/www-project-api-security/
- NIST SSDF: https://csrc.nist.gov/projects/ssdf
