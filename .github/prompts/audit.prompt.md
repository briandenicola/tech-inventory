Run audit:
1. Read `.specify/memory/constitution.md`
2. Check codebase against each constitution principle
3. Verify OWASP API Top 10 (2023) compliance per endpoint
4. Entra ID token validation correctness
5. EF Core: parameterization, N+1 queries, missing indexes
6. Container: non-root, read-only FS, no exposed secrets
7. Review recent commits for drift from spec
8. Write `audits/$(date +%Y-%m-%d).md` with findings (severity, file:line, fix)
9. Open GitHub issues for High/Critical findings
