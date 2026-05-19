/**
 * Re-exports of generated OpenAPI response/request schemas as named types.
 *
 * Hand-rolling these aliases keeps consumer code clean (`BrandResponse`)
 * instead of leaking the generated `components['schemas'][...]` indirection.
 * Constitution §6.5.2: API client is generated from OpenAPI — these names
 * mirror the backend DTOs verbatim.
 */
import type { components } from './generated/types';

type Schemas = components['schemas'];

export type BrandResponse = Schemas['BrandResponse'];
export type CategoryResponse = Schemas['CategoryResponse'];
export type DeviceResponse = Schemas['DeviceResponse'];
export type LocationResponse = Schemas['LocationResponse'];
export type NetworkResponse = Schemas['NetworkResponse'];
export type OwnerResponse = Schemas['OwnerResponse'];
export type TagResponse = Schemas['TagResponse'];

export type OwnerRole = Schemas['OwnerRole'];
export type LocationType = Schemas['LocationType'];
export type DeviceStatus = Schemas['DeviceStatus'];
