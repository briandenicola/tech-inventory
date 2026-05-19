/**
 * API test helpers for Playwright E2E.
 *
 * The F025 local-account auth path is in force for E2E (see
 * tests/e2e/fixtures/auth.ts): the `authenticated` test variant exchanges
 * the seeded admin's credentials at POST /api/v1/auth/local/login for an
 * HS256 JWT and overrides Playwright's default `request` fixture so the
 * resulting `APIRequestContext` carries `Authorization: Bearer <token>` on
 * every call. The web container reverse-proxies /api/* to the backend over
 * the compose network; the API's TechInventoryAuth PolicyScheme routes the
 * local-issuer token to the HMAC validator and stamps the seeded admin's
 * identity onto the request.
 *
 * These helpers seed reference data + devices on demand so journey specs don't
 * need to depend on a pre-populated database. Each helper returns the created
 * entity (with `id`) so the caller can chain dependent requests. Tests that
 * call them MUST destructure `request` from the `authenticated` fixture (not
 * the vanilla `test`) so the seeds are authorised.
 *
 * IMPORTANT: tests own their data. Use a unique suffix (Date.now() + random)
 * on every seed so parallel workers don't collide on unique-name validation.
 */

import type { APIRequestContext } from '@playwright/test';

function unique(prefix: string): string {
  return `${prefix} ${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`;
}

export interface SeededEntity {
  id: string;
  name: string;
}

export async function seedBrand(
  request: APIRequestContext,
  overrides: Partial<{ name: string; website: string; notes: string }> = {}
): Promise<SeededEntity> {
  const body = { name: unique('E2E Brand'), ...overrides };
  const res = await request.post('/api/v1/brands', { data: body });
  if (!res.ok()) {
    throw new Error(`seedBrand failed (${res.status()}): ${await res.text()}`);
  }
  const json = await res.json();
  return { id: json.id, name: json.name ?? body.name };
}

export async function seedCategory(
  request: APIRequestContext,
  overrides: Partial<{ name: string; parentId: string; icon: string }> = {}
): Promise<SeededEntity> {
  const body = { name: unique('E2E Category'), ...overrides };
  const res = await request.post('/api/v1/categories', { data: body });
  if (!res.ok()) {
    throw new Error(`seedCategory failed (${res.status()}): ${await res.text()}`);
  }
  const json = await res.json();
  return { id: json.id, name: json.name ?? body.name };
}

export interface SeededDevice extends SeededEntity {
  brandId: string;
  categoryId: string;
}

/**
 * Seed a fully wired device with fresh brand + category dependencies.
 *
 * The frontend's DeviceForm requires both a brand and a category, and the
 * API enforces those as foreign keys. This helper handles the full chain so
 * a journey spec can just say `const device = await seedDevice(request)`.
 */
export async function seedDevice(
  request: APIRequestContext,
  overrides: Partial<{
    name: string;
    brandId: string;
    categoryId: string;
    serialNumber: string;
    notes: string;
  }> = {}
): Promise<SeededDevice> {
  const brandId = overrides.brandId ?? (await seedBrand(request)).id;
  const categoryId = overrides.categoryId ?? (await seedCategory(request)).id;
  const body = {
    name: overrides.name ?? unique('E2E Device'),
    brandId,
    categoryId,
    serialNumber: overrides.serialNumber,
    notes: overrides.notes,
    status: 'Active'
  };
  const res = await request.post('/api/v1/devices', { data: body });
  if (!res.ok()) {
    throw new Error(`seedDevice failed (${res.status()}): ${await res.text()}`);
  }
  const json = await res.json();
  return { id: json.id, name: json.name ?? body.name, brandId, categoryId };
}
