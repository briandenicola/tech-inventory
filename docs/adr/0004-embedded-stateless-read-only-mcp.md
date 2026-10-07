# ADR 0004: Embedded stateless read-only MCP adapter

- **Status:** Accepted
- **Date:** 2026-10-06
- **Issue:** #198

## Context

Tech Inventory needs a remote Model Context Protocol endpoint for Hermes Agent
and other MCP clients. The inventory API already owns authentication,
authorization, rate limiting, validation, and read models. A second service
would duplicate those controls and create another deployment boundary.

MCP clients are powerful callers. Exposing arbitrary HTTP, database, shell,
filesystem, or generic agent execution would turn an inventory integration
into a broad remote-control surface.

## Decision

Phase 1 embeds the official `ModelContextProtocol.AspNetCore` SDK in the
existing API and maps stateless Streamable HTTP at `POST /api/mcp`.

- The endpoint is disabled by default with `Mcp:Enabled=false`.
- It accepts only existing `Authorization: ApiKey <selector>.<secret>`
  credentials carrying `inventory.read` or `inventory.write`.
- Existing expiry, revocation, live-principal, scope, and per-selector
  rate-limit checks remain authoritative.
- Tools call existing MediatR queries. They do not call controllers, proxy the
  REST API, or access EF Core directly.
- Device responses omit serial numbers, IP and MAC addresses, notes, product
  URLs, and audit metadata.
- The adapter exposes only bounded device, reference-data, and aggregate-report
  tools. It exposes no mutation, audit, settings, API-key administration,
  import/export, arbitrary HTTP, shell, filesystem, prompts, or resources.
- Request bodies are limited to 128 KiB by default. Responses are naturally
  bounded by tool pagination and existing report contracts.
- No separate container, port, database migration, or session affinity is
  introduced.

Phase 2 agentic lifecycle tools require a separate specification and approval.
General-purpose external agent spawning remains out of scope.

## Consequences

Deployments opt in explicitly and can roll back by setting
`Mcp__Enabled=false`. Existing REST clients and authentication behavior remain
unchanged. MCP protocol evolution is delegated to the pinned official SDK, and
upgrades require dependency review plus protocol-level regression tests.
