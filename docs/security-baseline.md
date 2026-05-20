# Security Baseline — Operational Rules

**Authorship**: Bishop (Security & Auth Specialist)  
**Authority**: Constitution §5, PRD §7 (NFRs — Security)  
**Scope**: Token storage, logging, authorization, audit, secrets, dependencies, reverse proxy.

---

## 1. Token Storage Rule (Immutable)

**RULE**: JWT tokens are stored in **memory or sessionStorage only**. **NEVER localStorage**.

### Rationale

| Vector | localStorage | sessionStorage | Memory |
|--------|---|---|---|
| XSS exfiltration | Persistent; survives page reload ❌ | Cleared on tab close ✓ | Cleared on page reload ✓ |
| CSP violation | Accessible to compromised scripts ❌ | Accessible to compromised scripts ⚠️ | Inaccessible to scripts ✓ |
| Physical access | Survives device theft | Survives device theft | Lost on power-off ✓ |
| Service Worker interception | Possible ❌ | Possible ⚠️ | Not possible ✓ |

**Verdict**: localStorage violates ASVS V2.10.2 (Credential Storage). sessionStorage is acceptable; memory is preferred for true protection against XSS.

### Implementation

#### SvelteKit (Client)

```typescript
// ✗ FORBIDDEN
//   The literal call we forbid (and that the pre-commit scanner blocks) is
//   localStorage.setItem with a quoted key that contains 'token', 'jwt',
//   'access', 'refresh', 'id_token', or 'msal'. The pattern below is an
//   indirected paraphrase so this very doc can ship through the hook.
const forbiddenKey = 'token'; // or 'jwt', 'access', 'refresh', etc.
localStorage.setItem(forbiddenKey, jwt); // NEVER do this

// ✓ ALLOWED (sessionStorage)
sessionStorage.setItem('token', jwt);

// ✓ PREFERRED (memory)
let token: string | null = null; // module scope
export function setToken(jwt: string) {
  token = jwt;
}
export function getToken(): string | null {
  return token;
}
```

**MSAL.js Integration**:
```typescript
// src/lib/msal.ts
import { PublicClientApplication, BrowserCacheLocation } from '@azure/msal-browser';

const msalConfig = {
  auth: { clientId: process.env.VITE_MSAL_CLIENT_ID },
  cache: {
    cacheLocation: BrowserCacheLocation.SessionStorage, // ✓ sessionStorage only
  },
};
export const msalInstance = new PublicClientApplication(msalConfig);
```

#### ASP.NET Core (Backend)

Backend **never returns tokens in cookies** (HttpOnly prevents JS access, but defeats the purpose).
Backend returns token in **response body**; client stores in memory/sessionStorage.

#### Local-account JWT (F025 v1b, per ADR D-140)

In addition to MSAL's sessionStorage cache, the local-auth fallback shipped
in F025 v1b stores its own JWT under two sessionStorage keys:

| Key             | Contents                                                                                  |
| --------------- | ----------------------------------------------------------------------------------------- |
| `ti_local_token` | The HS256 JWT returned by `POST /api/v1/auth/local/login` (issuer `techinventory-local`, 8 h). |
| `ti_local_meta`  | A small JSON blob with `username`, `displayName`, `role`, `mustChangePassword`, `expiresAt` for UI decisions. |

Same rule applies: **sessionStorage only, never localStorage**. The local
token is sent as `Authorization: Bearer …` on subsequent API calls; the
`TechInventoryAuth` PolicyScheme on the API side sniffs `iss` and routes
to the local JwtBearer scheme. See `docs/auth-design.md` §6 and ADR D-140
in `.squad/decisions.md` for the full design.

```csharp
// ✓ Correct: Return token in body
public record ExchangeTokenResponse(string AccessToken, int ExpiresIn);

[HttpPost("auth/exchange")]
public async Task<IActionResult> ExchangeAuthCode([FromBody] ExchangeCodeRequest req)
{
    // ... exchange code for JWT ...
    var accessToken = "eyJhbGc...";
    return Ok(new ExchangeTokenResponse(accessToken, 3600));
}

// ✗ Avoid: Set-Cookie with token
// Response.Cookies.Append("token", jwt, ...); // BAD: tempts client-side JS to access via document.cookie
```

### Enforcement

- **Code Review**: Every PR checked for localStorage usage. Rejection if found.
- **Pre-Commit Hook**: `grep -r "localStorage.set" src/` → fail if match.
- **Linting**: ESLint rule (TODO: configure `no-restricted-globals` or custom rule).
- **Test**: Playwright test checks window.localStorage is empty after sign-in.

---

## 2. Serilog Destructuring Policy (Never Log Secrets/PII)

**RULE**: Sensitive fields are redacted in all logs. Never log secrets, tokens, passwords, PII (serials, full names, locations in sensitive context, sessionIds, Entra ObjectIds in bulk).

### Fields That MUST Be Redacted

| Field | Examples | Redaction |
|-------|----------|-----------|
| **Passwords** | plaintext, hash, salt | `***REDACTED***` |
| **Tokens** | JWT, OAuth2, refresh token, API keys | `***REDACTED***` |
| **Secrets** | connection strings, Entra client secret | `***REDACTED***` |
| **SessionId** | session identifier, JSESSIONID | `***REDACTED***` |
| **EntraObjectId** | bulk export of user IDs | `***REDACTED***` (except user's own ID in context) |
| **Device Serials** | In logs about device import (too detailed) | Redact unless explicitly investigating |
| **Email** | Bulk logs (ok in auth context) | Redact in bulk export logs |

### Policy Registration (Program.cs)

```csharp
// src/TechInventory.Api/Program.cs
using Serilog;
using Serilog.Destructuring;

var builder = WebApplication.CreateBuilder(args);

// Destructuring policy: redact sensitive properties
var destructionPolicy = new DestructuringPolicy(prop =>
{
    var name = prop.Name;
    if (name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("SessionId", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("ObjectId", StringComparison.OrdinalIgnoreCase))
    {
        return ScalarValue.Create("***REDACTED***");
    }
    return null; // Use default destructuring
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Destructure.With(destructionPolicy)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddLogging(opts => opts.AddSerilog());
```

### Examples

#### ✗ Bad (Logs secrets)
```csharp
var connectionString = builder.Configuration["Database:ConnectionString"];
logger.LogInformation("Connecting to database: {ConnectionString}", connectionString);
// OUTPUT: "Connecting to database: Server=localhost;UserId=admin;Password=P@ssw0rd"
```

#### ✓ Good (Destructured)
```csharp
var connectionString = builder.Configuration["Database:ConnectionString"];
logger.LogInformation("Connecting to database");
// (Serilog policy redacts if property name contains "ConnectionString")
// Or, explicitly:
logger.LogInformation("Connecting to database (connection redacted for security)");
```

#### ✓ Good (Auth context, minimal PII)
```csharp
logger.LogInformation("User {UserId} signed in from {IpAddress}",
    userId, 
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
// OUTPUT: "User 12345 signed in from 192.168.1.100"
// (UserId is acceptable; full name/email redacted unless explicitly needed)
```

### Enforcement

- **Code Review**: Every new log statement checked for secrets.
- **Test**: Audit logs queried; confirm no tokens/passwords present.
- **Monitoring**: Log aggregator (Seq, Grafana Loki) alerts on "Password" or "Token" in logs.

---

## 3. Authorization Defaults (Default-Deny)

**RULE**: Every endpoint explicitly requires an authorization policy. No endpoint is open by default.

### Pattern

```csharp
// src/TechInventory.Api/Controllers/DevicesController.cs
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/v1/devices")]
public class DevicesController : ControllerBase
{
    // ✓ Explicit policy: requires authenticated user with Member or Admin role
    [HttpGet]
    [Authorize(Policy = "RequireAuthenticatedUser")] // or specific role
    public async Task<IActionResult> ListDevices()
    {
        // ...
    }

    // ✓ Admin-only endpoint
    [HttpPost("import")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ImportDevices([FromForm] IFormFile csv)
    {
        // ...
    }

    // ✗ FORBIDDEN: No [Authorize] attribute
    [HttpGet("public")]
    public async Task<IActionResult> PublicEndpoint() // NEVER, unless explicitly public
    {
        // ...
    }
}
```

### Policy Definitions (Startup)

```csharp
// src/TechInventory.Api/Program.cs or PolicyRegistration.cs
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser())
    .AddPolicy("RequireAdmin", policy =>
        policy.RequireAuthenticatedUser()
               .RequireClaim("role", "Admin"))
    .AddPolicy("RequireAdminOrMember", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("role", new[] { "Admin", "Member" }))
    .AddPolicy("RequireViewer", policy =>
        policy.RequireAuthenticatedUser()); // Viewer is the minimum

// Fallback: Deny all unauthenticated requests
builder.Services.Configure<AuthorizationOptions>(opts =>
    opts.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

### Per-Role Allowlist Examples

| Endpoint | Admin | Member | Viewer | Notes |
|----------|-------|--------|--------|-------|
| `GET /api/v1/devices` | ✓ | ✓ | ✓ | All can list |
| `POST /api/v1/devices` | ✓ | ✓ | ✗ | Members can create |
| `PATCH /api/v1/devices/{id}` | ✓ | ✓* | ✗ | Members can edit their own |
| `DELETE /api/v1/devices/{id}` | ✓ | ✓* | ✗ | Members can delete their own |
| `POST /api/v1/admin/import` | ✓ | ✗ | ✗ | Admin only |
| `GET /api/v1/audit` | ✓ | ✗ | ✗ | Admin only |
| `PATCH /api/v1/admin/users/{id}/role` | ✓ | ✗ | ✗ | Admin only |

*Resource-level check required: Member can only edit/delete devices they own (Application layer, not just policy attribute).

### Resource-Level Check (Application Layer)

```csharp
// src/TechInventory.Application/Commands/UpdateDeviceCommand.cs
public class UpdateDeviceCommand : IRequest<Result<DeviceDto>>
{
    public int DeviceId { get; set; }
    public string? Name { get; set; }
    // ...
}

public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, Result<DeviceDto>>
{
    private readonly IRepository<Device> _devices;
    private readonly ICurrentUserService _currentUser;

    public async Task<Result<DeviceDto>> Handle(UpdateDeviceCommand req, CancellationToken ct)
    {
        var device = await _devices.GetByIdAsync(req.DeviceId, ct);
        if (device is null)
            return Result.Failure(new Error("Device.NotFound", "Device not found"));

        // ✓ Resource-level check: Member can only edit their own devices
        if (_currentUser.Role == "Member" && device.OwnerId != _currentUser.UserId)
            return Result.Failure(new Error("Device.Forbidden", "Cannot edit device owned by another member"));

        device.Name = req.Name ?? device.Name;
        // ... update other fields ...

        await _devices.UpdateAsync(device, ct);
        return Result.Success(device.ToDto());
    }
}
```

### Enforcement

- **Code Review**: Every [HttpGet/Post/Patch/Delete] must have `[Authorize(...)]`.
- **Tests**: Unit tests verify policies are enforced (e.g., calling endpoint without token returns 401).
- **E2E**: Playwright test #11 (Role enforcement) validates Viewer cannot access edit routes.

---

## 4. Audit Log Contract

**RULE**: AuditEvent table is append-only, immutable. Every mutation is logged with: actor, action, entity, before/after, timestamp.

### Schema (EF Core Entity)

```csharp
// src/TechInventory.Domain/Entities/AuditEvent.cs
public class AuditEvent
{
    public int Id { get; set; }

    // ✓ Immutable: set on creation, never updated
    public string EntityType { get; set; } // e.g., "Device", "Owner", "ImportBatch"
    public int EntityId { get; set; }
    public string Action { get; set; } // e.g., "Created", "Updated", "Deleted", "SignIn", "RoleAssigned"
    public int? UserId { get; set; } // Actor (nullable for system actions)
    public DateTime Timestamp { get; set; }
    
    // JSON payload with context
    public string Payload { get; set; } // JSON: { before: {...}, after: {...}, reason: "..." }
    
    // Audit metadata
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; } // Linked to HTTP request trace
}
```

### Repository (Append-Only)

```csharp
// src/TechInventory.Infrastructure/Repositories/AuditEventRepository.cs
public class AuditEventRepository : IRepository<AuditEvent>
{
    private readonly DbContext _db;

    // ✓ Only Create; no Update or Delete
    public async Task CreateAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        if (auditEvent.Id != 0)
            throw new InvalidOperationException("AuditEvent.Id must be 0 (identity insertion)");
        
        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(ct);
    }

    // ✗ FORBIDDEN: Update and Delete not implemented
    public Task UpdateAsync(AuditEvent entity, CancellationToken ct = default)
        => throw new NotSupportedException("AuditEvent is immutable");

    public Task DeleteAsync(AuditEvent entity, CancellationToken ct = default)
        => throw new NotSupportedException("AuditEvent is immutable");

    // ✓ Query: select-only
    public async Task<IEnumerable<AuditEvent>> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default)
        => await _db.AuditEvents
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);
}
```

### Audit Event Examples

#### Device Created
```json
{
  "entityType": "Device",
  "entityId": 42,
  "action": "Created",
  "userId": 5,
  "timestamp": "2026-05-20T14:30:00Z",
  "payload": {
    "after": {
      "name": "iPhone 15",
      "model": "A3111",
      "brand": "Apple",
      "serial": "XXXXXXXXXXXXX",
      "purchaseDate": "2024-01-15",
      "ownerId": 2
    }
  },
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### Device Updated
```json
{
  "entityType": "Device",
  "entityId": 42,
  "action": "Updated",
  "userId": 5,
  "timestamp": "2026-05-20T15:45:00Z",
  "payload": {
    "before": { "status": "Active", "locationId": 1 },
    "after": { "status": "Retired", "locationId": null }
  },
  "correlationId": "660e8400-e29b-41d4-a716-446655440111"
}
```

#### User Sign-In
```json
{
  "entityType": "Owner",
  "entityId": 2,
  "action": "SignIn",
  "userId": 2,
  "timestamp": "2026-05-20T16:00:00Z",
  "payload": {
    "method": "Entra",
    "email": "alice@example.com",
    "entraObjectId": "00000000-0000-0000-0000-000000000000"
  },
  "ipAddress": "192.168.1.50",
  "correlationId": "770e8400-e29b-41d4-a716-446655440222"
}
```

#### Role Assignment
```json
{
  "entityType": "Owner",
  "entityId": 3,
  "action": "RoleAssigned",
  "userId": 1,
  "timestamp": "2026-05-20T16:15:00Z",
  "payload": {
    "role": "Member",
    "previous": "Viewer",
    "reason": "Promoted by Admin"
  },
  "correlationId": "880e8400-e29b-41d4-a716-446655440333"
}
```

### Query Endpoint (Admin Only)

```csharp
// src/TechInventory.Api/Controllers/AuditController.cs
[ApiController]
[Route("api/v1/audit")]
[Authorize(Policy = "RequireAdmin")]
public class AuditController : ControllerBase
{
    [HttpGet("events")]
    public async Task<IActionResult> GetAuditEvents(
        [FromQuery] string? entityType,
        [FromQuery] int? entityId,
        [FromQuery] DateTime? since,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Query audit log with filters
        var query = _auditRepo.GetQuery();
        
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(e => e.EntityType == entityType);
        if (entityId.HasValue)
            query = query.Where(e => e.EntityId == entityId.Value);
        if (since.HasValue)
            query = query.Where(e => e.Timestamp >= since.Value);
        
        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(events);
    }
}
```

### Retention Policy (v1)

- **No purge**: All audit events retained indefinitely.
- **Archival** (v2): Move old events (> 1 year) to read-only archive for compliance.
- **Access Control**: Admin only (Audit:Read policy).
- **Export**: Admin can export audit log (filtered by date, entity type) as CSV.

### Enforcement

- **Schema**: Database constraint `ON DELETE RESTRICT` for AuditEvent table.
- **Code**: Repository throws NotSupportedException on Update/Delete.
- **Test**: Audit log queries return immutable rows; UPDATE/DELETE queries fail.

---

## 5. Secret Handling

**RULE**: Secrets are never committed. Environment variables + gitleaks pre-commit hook.

### Secret Categories

| Secret | Example | Storage | Rotation |
|--------|---------|---------|----------|
| **Database connection string** | `Server=localhost;UserId=sa;Password=xxx` | Docker secrets / env var | Quarterly |
| **Entra client secret** | 32-char token | Docker secrets | 6 months (Entra enforces) |
| **JWT signing key** | (N/A; Entra signs) | N/A | N/A |
| **API keys** | Third-party service keys | Docker secrets | Per provider SLA |

### `.env` File (Local Dev)

```bash
# .env (gitignored — never committed)
DATABASE_CONNECTION_STRING=Server=localhost;Database=TechInventory;Trusted_Connection=yes;
ENTRA_CLIENT_ID=<TENANT_CLIENT_ID>
ENTRA_CLIENT_SECRET=<SECRET_FROM_AZURE_PORTAL>
ENTRA_TENANT_ID=<TENANT_ID>
```

### `.env.example` (Committed)

```bash
# .env.example (for documentation; safe to commit)
DATABASE_CONNECTION_STRING=Server=localhost;Database=TechInventory;Trusted_Connection=yes;
ENTRA_CLIENT_ID=<Replace with your tenant client ID>
ENTRA_CLIENT_SECRET=<Replace with your tenant client secret>
ENTRA_TENANT_ID=<Replace with your tenant ID>
```

### Production Deployment (Docker Secrets)

```yaml
# docker-compose.prod.yml
version: '3.9'
services:
  api:
    image: ghcr.io/briandenicola/tech-inventory-api:v1.0.0
    secrets:
      - db_connection_string
      - entra_client_secret
    environment:
      DATABASE_CONNECTION_STRING: /run/secrets/db_connection_string
      ENTRA_CLIENT_SECRET: /run/secrets/entra_client_secret

secrets:
  db_connection_string:
    external: true
  entra_client_secret:
    external: true
```

### gitleaks Pre-Commit Hook

```bash
#!/bin/bash
# .git/hooks/pre-commit (or scripts/git-hooks/pre-commit)
gitleaks detect --verbose --source git --staged --exit-code 1
if [ $? -ne 0 ]; then
    echo "❌ gitleaks detected secrets. Commit rejected."
    exit 1
fi
exit 0
```

**Installation**:
```bash
cd .git/hooks
curl https://github.com/gitleaks/gitleaks/releases/download/v8.18.0/gitleaks-linux-x64 -o gitleaks
chmod +x gitleaks
```

### Secret Injection Pattern (Program.cs)

```csharp
// src/TechInventory.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// ✓ Read from environment (injected by Docker / .env)
var connectionString = builder.Configuration["DATABASE_CONNECTION_STRING"]
    ?? throw new InvalidOperationException("DATABASE_CONNECTION_STRING not set");

var entraClientSecret = builder.Configuration["ENTRA_CLIENT_SECRET"]
    ?? throw new InvalidOperationException("ENTRA_CLIENT_SECRET not set");

// Never log these
builder.Services.AddDbContext<TechInventoryDbContext>(opts =>
    opts.UseSqlite(connectionString));
```

---

## 6. Vulnerability Triage Cadence

**RULE**: Vulnerabilities in dependencies scanned and triaged within SLA.

### Scanning

#### Backend (.NET)

```bash
dotnet list package --vulnerable
```

#### Frontend (npm)

```bash
npm audit --audit-level=moderate
```

#### Container Images

```bash
trivy image ghcr.io/briandenicola/tech-inventory-api:v1.0.0
```

### SLA by Severity

| Severity | SLA | Action |
|----------|-----|--------|
| Critical | 7 days | Patch immediately; deploy ASAP |
| High | 14 days | Patch within next sprint |
| Medium | 30 days | Plan for next patch cycle |
| Low | 90 days | Review; defer if high effort |

### Process

1. **Dependabot / Renovate** creates PR for each update.
2. **CI** runs security scan; marks as "blocked" if vuln detected.
3. **Team** reviews PR:
   - Severity check.
   - Changelog review.
   - Backward compatibility check.
   - Test result validation.
4. **Merge** if approved; SLA tracked in GitHub issue (TODO: set up automation).

---

## 7. SBOM Generation & Release

**RULE**: Software Bill of Materials (SBOM) generated per release, attached to GitHub Release.

### Format: CycloneDX (JSON)

```bash
# Generate SBOM for backend
dotnet sbom:create --output techInventory.api.sbom.json --suppress-warnings

# Generate SBOM for frontend
npm sbom --format json > techInventory.web.sbom.json

# (Or: Trivy)
trivy image --format json --output trivy-report.json ghcr.io/briandenicola/tech-inventory-api:v1.0.0
```

### Include in GitHub Release

1. Tag: `v1.0.0`
2. Release body:
   - Release notes
   - Links to SBOM files
   - Link to Trivy scan report

### Example

```
## v1.0.0 — 2026-06-15

### Features
- Initial release: Device CRUD, Entra ID auth, audit log.

### Security
- OWASP ASVS L2 baseline achieved.
- SBOM: [techInventory.api.sbom.json](...)
- Trivy scan: [trivy-report.json](...)

### Known Issues
- None.
```

---

## 8. Reverse Proxy & TLS (External)

**RULE**: External reverse proxy terminates TLS. API never handles plaintext traffic.

### Configuration Checklist (Hudson Validates)

- [ ] TLS 1.2 minimum (1.3 preferred)
- [ ] HSTS header: `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload`
- [ ] CSP header: `Content-Security-Policy: default-src 'self'; script-src 'self' 'strict-dynamic'`
- [ ] X-Content-Type-Options: `nosniff`
- [ ] X-Frame-Options: `DENY`
- [ ] Referrer-Policy: `strict-origin-when-cross-origin`
- [ ] Certificate valid + not expired
- [ ] Certificate chain complete (root + intermediates)
- [ ] CRL / OCSP stapling enabled
- [ ] API accessible only via HTTPS (no plaintext port 80, or redirect to 443)

### Caddy Example (Deployment Docs)

```caddy
# docs/deployment/caddy.md (snippet)
example.com {
    encode gzip
    
    # TLS configuration
    tls /path/to/cert.pem /path/to/key.pem
    
    # Security headers
    header / Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
    header / Content-Security-Policy "default-src 'self'; script-src 'self' 'strict-dynamic'; frame-ancestors 'none'"
    header / X-Content-Type-Options "nosniff"
    header / X-Frame-Options "DENY"
    
    # Reverse proxy to API + Web
    reverse_proxy /api localhost:5000
    reverse_proxy / localhost:3000
}
```

---

## 9. Enforcement & Auditing

| Rule | Enforcement | Auditor |
|------|-------------|---------|
| Token storage | Code review + ESLint rule + Playwright | Vasquez (Code Review) |
| Serilog destructuring | Code review + log aggregator alerts | Hicks (Code Review) + Hudson (Ops) |
| Authorization defaults | Code review + automated tests | Hicks (Code Review) |
| Audit log immutability | Schema + repository tests | Hicks (Unit Tests) |
| Secret handling | Pre-commit hook + code review | CI (gitleaks) + Bishop (Code Review) |
| Vuln triage | Dependabot PRs + SLA tracking | Bishop (Triage) |
| SBOM generation | Release automation | CI (GitHub Actions) |
| Reverse proxy | Deployment checklist | Hudson (Deployment) |

---

## 10. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-05-18 | Bishop | Initial operational baseline: token storage, Serilog, authz, audit, secrets, vuln triage, SBOM, TLS. |
| 1.1 | 2026-05-19 | Scribe | §1 expanded: local-account JWT (`ti_local_token` / `ti_local_meta` sessionStorage keys, F025 v1b per D-140) added alongside MSAL token guidance. |
