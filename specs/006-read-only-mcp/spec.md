# Specification: Read-only MCP server

**Issue:** #198  
**Status:** Complete  
**Authoritative decision:** `docs/adr/0004-embedded-stateless-read-only-mcp.md`

## Goal

Expose Tech Inventory to Hermes Agent and compatible MCP clients through a
default-off, API-key-authenticated, read-only protocol adapter.

## Functional requirements

1. `POST /api/mcp` uses stateless Streamable HTTP.
2. Only active API keys with `inventory.read` or `inventory.write` may connect.
   Interactive bearer tokens are not accepted.
3. Discovery returns exactly these tools:
   `search_devices`, `get_device`, `list_reference_data`,
   `inventory_summary`, `warranty_report`, `spending_report`, `era_report`,
   and `timeline_report`.
4. Every tool delegates to an existing Application-layer MediatR query.
5. Device outputs exclude serial number, IP address, MAC address, notes,
   product URL, and audit metadata.
6. Search returns at most 50 devices per call. Reference data returns at most
   100 entries. Inputs are explicitly bounded and validated.
7. Disabled deployments return 503. Missing or invalid credentials return 401;
   authenticated non-API-key credentials and insufficient scopes return 403;
   oversized requests return 413; rate limits return 429.
8. Deployment and client documentation covers opt-in configuration, HTTPS,
   proxy behavior, key lifecycle, Hermes Agent, generic clients, and
   troubleshooting.

## Non-goals

- Mutation or API-key administration
- Audit log, settings, insurance CSV, import, or export tools
- Prompts, resources, roots, sampling, or stateful MCP sessions
- Arbitrary HTTP, database, filesystem, or shell access
- External or general-purpose agent spawning
- Phase 2 application-owned agentic capabilities

## Acceptance

- Protocol integration tests prove authentication, discovery, representative
  calls, stateless method handling, default-off behavior, and mutation absence.
- Existing API-key negative and rate-limit suites remain green.
- The official SDK is pinned and dependency scanning reports no known
  vulnerabilities.
- `task verify` passes.
