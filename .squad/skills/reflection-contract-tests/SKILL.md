# Reflection Contract Tests

Use this pattern when QA needs to lock a contract on a type whose public surface matters more than the implementation details.

## AuditEvent append-only contract
1. Resolve the target type by full name from the Domain assembly.
2. Create the instance through reflection with deterministic sample values.
3. Assert constructor capture of actor/action/timestamp/before/after.
4. Assert UTC timestamps, required-field guards, and zero public setters or mutator methods.

## Repository interface contract
1. Resolve each repository interface by full name from `TechInventory.Application.Abstractions.Repositories`.
2. Use `Substitute.For(new[] { interfaceType }, Array.Empty<object>())` to prove the interface is mockable without compile-time coupling.
3. Reflect every method signature: require a `CancellationToken`, forbid `IQueryable`, and lock the surface to async result/list/paged-result shapes only.
4. For `IAuditEventRepository`, assert `AppendAsync` exists and any `Update|Delete|Remove` method name is absent.

## Current project helpers
- `tests/TechInventory.UnitTests/Support/ContractReflectionAssertions.cs`
- `tests/TechInventory.UnitTests/Domain/AuditEventTests.cs`
- `tests/TechInventory.UnitTests/Application/Abstractions/RepositoryInterfaceContractTests.cs`
