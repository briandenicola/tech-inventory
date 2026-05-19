# D-133: CORS Configuration for Local Development

**Status:** ✅ Implemented  
**Date:** 2026-05-18  
**Agent:** Hicks  
**Commit:** 908845a

## Context

Brian ran `task dev:up` successfully (API on `:8080`, Web on `:5173`). When signing into the Web UI, the browser blocked the request from `http://localhost:5173` to `http://localhost:8080/api/v1/owners/me` with:

```
Access to fetch at 'http://localhost:8080/api/v1/owners/me' from origin 'http://localhost:5173' 
has been blocked by CORS policy: Response to preflight request doesn't pass access control check: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

## Root Cause

**CORS was not configured at all** in `Program.cs`. There was no `AddCors()` service registration or `UseCors()` middleware call. The API returned no CORS headers, so all cross-origin requests from the Vite dev server failed at preflight.

## Decision

Add **config-driven CORS policy** that:

1. Reads allowed origins from `Cors:AllowedOrigins` configuration array
2. Only applies CORS policy if at least one origin is configured (empty array = no CORS policy)
3. Uses `.AllowAnyHeader()`, `.AllowAnyMethod()`, `.AllowCredentials()` for Entra bearer + cookie flow support
4. Applies `UseCors("ApiCorsPolicy")` in middleware pipeline **before** `UseAuthentication()` (required for preflight OPTIONS requests)

### Configuration

**Development** (`appsettings.Development.json`):
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  }
}
```

**Production** (`appsettings.json`): No CORS section by default. Operators can add specific production origins (e.g., `https://techinventory.example.com`) if deploying Web and API to separate hosts.

### Implementation

**Service registration** (after `AddAuthorizationBuilder()`):
```csharp
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
```

**Middleware pipeline** (after `UseHttpsRedirection()`, before `UseAuthentication()`):
```csharp
app.UseCors("ApiCorsPolicy");
```

## Production Safety

- Production `appsettings.json` unchanged — no origins configured by default
- CORS policy only applies when `Cors:AllowedOrigins` is explicitly configured
- No `AllowAnyOrigin()` — operators must whitelist specific origins
- `.AllowCredentials()` requires explicit origins (incompatible with wildcard)

## Test Results

- `dotnet build -c Release`: ✅ Succeeded (16.1s)
- `dotnet test -c Release`: ✅ 377 passed / 6 skipped / 0 failed (28.4s)
- No behavior regressions detected

## Files Changed

1. `src/TechInventory.Api/Program.cs`:
   - Added CORS service registration (lines 126-138)
   - Added `UseCors("ApiCorsPolicy")` middleware (line 184)

2. `src/TechInventory.Api/appsettings.Development.json`:
   - Added `Cors:AllowedOrigins` with `["http://localhost:5173"]`

3. `src/TechInventory.Api/appsettings.json`:
   - No changes (production origins not configured by default)

## Next Steps

**Brian must restart the API** to pick up the config change:
1. `Ctrl+C` to stop current `task dev:up`
2. `task dev:up` to restart with new CORS config
3. Sign in at `http://localhost:5173` — `/api/v1/owners/me` should now succeed

## References

- Constitution §2: API-first, versioned REST
- MDN Web Docs: [CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- ASP.NET Core: [Enable CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors)
