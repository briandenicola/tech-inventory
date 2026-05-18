# Hicks Decision Note — ProblemDetails shape

- **Date:** 2026-05-18
- **Scope:** `src/TechInventory.Api/ExceptionHandling/ApiExceptionHandler.cs`
- **Related tasks:** T41
- **Related authority:** `specs/001-core-api/plan.md` §4.2, D-020, D-021

## Decision

Use `IExceptionHandler` + `ProblemDetailsFactory` as the single API failure serializer.

- Validation failures (`Error.Code == "Validation"`) become `ValidationProblemDetails` with an `errors` dictionary keyed by property name
- `NotFound` becomes 404 ProblemDetails
- `Conflict` becomes 409 ProblemDetails
- Other expected failures default to 400 ProblemDetails
- Unhandled exceptions become 500 ProblemDetails; only the exception message is surfaced in Development, never stack traces outside Development

## Rationale

Application handlers already normalize expected failures into `Result`/`Error`, so the API layer should translate that contract once rather than per action. Using `ProblemDetailsFactory` keeps RFC 7807 fields (`type`, `title`, `status`, `detail`, `instance`) aligned with ASP.NET conventions while preserving the D-020 validation error dictionary shape.
