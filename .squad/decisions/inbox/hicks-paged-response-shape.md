# Hicks Decision Inbox — PagedResponse Shape

- **Date:** 2026-05-18
- **Related:** `src/TechInventory.Application/Common/Paging/PagedResponse.cs`, `specs/001-core-api/tasks.md` T21–T27, constitution §4.2

## Proposal
Adopt `PagedResponse<T>` in Application as the standard list-query DTO shape with:
- `Items`
- `TotalCount`
- `Page`
- `PageSize`

## Why
Repository seams already use `PagedResult<T>` internally, but handler/query DTOs needed an outward-facing response type that matches the API acceptance criteria (`totalCount`, `page`, `pageSize`) without leaking repository implementation details.

## Implemented In
- `ListDevicesQuery`
- `ListBrandsQuery`
- `ListCategoriesQuery`
- `ListOwnersQuery`
- `ListLocationsQuery`
- `ListNetworksQuery`
- `ListTagsQuery`

## Notes
For hierarchical categories, pagination applies to root nodes while each returned root preserves its full descendant tree.