# Plan: Read-only MCP server

1. Pin `ModelContextProtocol.AspNetCore` and embed stateless HTTP transport.
2. Add a default-off persisted household setting, Admin-only Settings UI,
   request guard, API-key-only policy, and the narrow `/api/mcp` scope-policy
   allowance.
3. Implement bounded MediatR-backed tools and sanitized device contracts.
4. Add protocol-level integration tests and tamper tests for the guards.
5. Add exact nginx routing plus request-size environment configuration.
6. Publish the portable skill and client/deployment documentation.
7. Run targeted tests, full verification, dependency audit, security review,
   and post-major-work QC audit.
