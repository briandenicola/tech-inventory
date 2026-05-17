# Threat Model (STRIDE)

## Assets
- Family device inventory (PII-adjacent)
- Entra ID tokens
- Backup files

## Trust Boundaries
Internet ↔ Caddy ↔ API ↔ SQL
                  ↘ Entra ID

## Threats & Mitigations
| ID | STRIDE | Threat | Mitigation |
|----|--------|--------|------------|
| T01 | S | Token replay | Short JWT TTL, PKCE |
| T02 | T | SQL tampering | Parameterized queries, EF Core |
| T03 | R | Mutation w/o trace | Audit log table |
| T04 | I | Exposed serials | Authz per device owner |
| T05 | D | Container DoS | Rate limiting, resource limits |
| T06 | E | Member → Admin | Role check on every endpoint |
