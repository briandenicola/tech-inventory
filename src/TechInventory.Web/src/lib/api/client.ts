/**
 * API Client — Typed fetch wrapper for Tech Inventory API
 * 
 * Constitution §6.5.2: "TypeScript API client generated from OpenAPI — no hand-written fetch calls"
 * Spec §4.2: Wrapper configures base URL + auth headers; ready for MSAL token injection (T05)
 */

import type { paths } from './generated/types';

/**
 * Type helpers to extract request/response types from OpenAPI paths
 */
type GetResponse<T> = T extends { get: { responses: { 200: { content: infer C } } } }
	? C extends { 'application/json': infer R }
		? R
		: never
	: never;

type PostRequestBody<T> = T extends { post: { requestBody?: infer B } }
	? B extends { content: { 'application/json': infer R } }
		? R
		: B extends { content: { 'multipart/form-data': unknown } }
			? FormData
			: never
	: never;

type PostResponse<T> = T extends { post: { responses: { 201: { content: infer C } } } }
	? C extends { 'application/json': infer R }
		? R
		: never
	: T extends { post: { responses: { 200: { content: infer C } } } }
		? C extends { 'application/json': infer R }
			? R
			: never
		: never;

type PutRequestBody<T> = T extends { put: { requestBody?: infer B } }
	? B extends { content: { 'application/json': infer R } }
		? R
		: never
	: never;

type PutResponse<T> = T extends { put: { responses: { 200: { content: infer C } } } }
	? C extends { 'application/json': infer R }
		? R
		: never
	: never;

/**
 * ProblemDetails error from API (RFC 7807)
 */
export class ApiError extends Error {
	constructor(
		public status: number,
		public title: string,
		public detail?: string,
		public instance?: string,
		public errors?: Record<string, string[]>
	) {
		super(`${title}${detail ? `: ${detail}` : ''}`);
		this.name = 'ApiError';
	}
}

/**
 * API client configuration
 */
interface ApiClientConfig {
	baseUrl?: string;
	getAuthToken?: () => Promise<string | null>;
}

/**
 * Default base URL from environment
 */
const DEFAULT_BASE_URL =
	import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

/**
 * Global client config (can be overridden by setApiConfig)
 */
let clientConfig: ApiClientConfig = {
	baseUrl: DEFAULT_BASE_URL
};

/**
 * Set API client configuration (e.g., for wiring MSAL token provider in T05)
 */
export function setApiConfig(config: Partial<ApiClientConfig>): void {
	clientConfig = { ...clientConfig, ...config };
}

/**
 * Typed fetch helper
 */
async function apiFetch<TResponse>(
	path: string,
	options: RequestInit = {}
): Promise<TResponse> {
	const url = `${clientConfig.baseUrl}${path}`;
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
		...(options.headers as Record<string, string> | undefined)
	};

	// T05: Inject auth header (Bearer token) from MSAL.js before every API call
	if (clientConfig.getAuthToken) {
		const token = await clientConfig.getAuthToken();
		if (token) {
			headers['Authorization'] = `Bearer ${token}`;
		}
		// If no token (user not signed in), proceed without auth header
		// Round 2 route guard (T12) will redirect unauthenticated users before protected API calls
	}

	const response = await fetch(url, {
		...options,
		headers
	});

	if (!response.ok) {
		// Parse ProblemDetails error (RFC 7807)
		let errorBody: unknown;
		try {
			errorBody = await response.json();
		} catch {
			throw new ApiError(
				response.status,
				response.statusText,
				undefined,
				url
			);
		}

		const problem = errorBody as {
			title?: string;
			detail?: string;
			status?: number;
			instance?: string;
			errors?: Record<string, string[]>;
		};

		throw new ApiError(
			problem.status ?? response.status,
			problem.title ?? response.statusText,
			problem.detail,
			problem.instance,
			problem.errors
		);
	}

	// Handle 204 No Content
	if (response.status === 204) {
		return undefined as TResponse;
	}

	return response.json();
}

/**
 * Build query string from params object
 */
function buildQueryString(
	params: Record<string, unknown> | undefined
): string {
	if (!params) return '';

	const searchParams = new URLSearchParams();
	Object.entries(params).forEach(([key, value]) => {
		if (value !== undefined && value !== null) {
			searchParams.append(key, String(value));
		}
	});

	const query = searchParams.toString();
	return query ? `?${query}` : '';
}

/**
 * Type-safe API client functions
 */

// Devices
export const devices = {
	list: async (
		params?: paths['/api/v1/devices']['get']['parameters']['query']
	) => apiFetch<GetResponse<paths['/api/v1/devices']>>(`/api/v1/devices${buildQueryString(params)}`),

	get: async (id: string) =>
		apiFetch<GetResponse<paths['/api/v1/devices/{id}']>>(`/api/v1/devices/${encodeURIComponent(id)}`),

	create: async (body: PostRequestBody<paths['/api/v1/devices']>) =>
		apiFetch<PostResponse<paths['/api/v1/devices']>>(`/api/v1/devices`, {
			method: 'POST',
			body: JSON.stringify(body)
		}),

	update: async (id: string, body: PutRequestBody<paths['/api/v1/devices/{id}']>) =>
		apiFetch<PutResponse<paths['/api/v1/devices/{id}']>>(`/api/v1/devices/${encodeURIComponent(id)}`, {
			method: 'PUT',
			body: JSON.stringify(body)
		}),

	delete: async (id: string, reason: string) =>
		apiFetch<void>(`/api/v1/devices/${encodeURIComponent(id)}`, {
			method: 'DELETE',
			body: JSON.stringify({ reason })
		})
};

// Brands
export const brands = {
	list: async (
		params?: paths['/api/v1/brands']['get']['parameters']['query']
	) => apiFetch<GetResponse<paths['/api/v1/brands']>>(`/api/v1/brands${buildQueryString(params)}`),

	get: async (id: string) =>
		apiFetch<GetResponse<paths['/api/v1/brands/{id}']>>(`/api/v1/brands/${encodeURIComponent(id)}`),

	create: async (body: PostRequestBody<paths['/api/v1/brands']>) =>
		apiFetch<PostResponse<paths['/api/v1/brands']>>(`/api/v1/brands`, {
			method: 'POST',
			body: JSON.stringify(body)
		}),

	update: async (id: string, body: PutRequestBody<paths['/api/v1/brands/{id}']>) =>
		apiFetch<PutResponse<paths['/api/v1/brands/{id}']>>(`/api/v1/brands/${encodeURIComponent(id)}`, {
			method: 'PUT',
			body: JSON.stringify(body)
		}),

	deactivate: async (id: string) =>
		apiFetch<void>(
			`/api/v1/brands/${encodeURIComponent(id)}/deactivate`,
			{
				method: 'PATCH'
			}
		)
};

// Categories
export const categories = {
	list: async (
		params?: paths['/api/v1/categories']['get']['parameters']['query']
	) => apiFetch<GetResponse<paths['/api/v1/categories']>>(`/api/v1/categories${buildQueryString(params)}`),

	get: async (id: string) =>
		apiFetch<GetResponse<paths['/api/v1/categories/{id}']>>(`/api/v1/categories/${encodeURIComponent(id)}`),

	create: async (body: PostRequestBody<paths['/api/v1/categories']>) =>
		apiFetch<PostResponse<paths['/api/v1/categories']>>(`/api/v1/categories`, {
			method: 'POST',
			body: JSON.stringify(body)
		}),

	update: async (id: string, body: PutRequestBody<paths['/api/v1/categories/{id}']>) =>
		apiFetch<PutResponse<paths['/api/v1/categories/{id}']>>(`/api/v1/categories/${encodeURIComponent(id)}`, {
			method: 'PUT',
			body: JSON.stringify(body)
		}),

	deactivate: async (id: string) =>
		apiFetch<void>(
			`/api/v1/categories/${encodeURIComponent(id)}/deactivate`,
			{
				method: 'PATCH'
			}
		)
};

// Owners
export const owners = {
	list: async (
		params?: paths['/api/v1/owners']['get']['parameters']['query']
	) => apiFetch<GetResponse<paths['/api/v1/owners']>>(`/api/v1/owners${buildQueryString(params)}`),

	get: async (id: string) =>
		apiFetch<GetResponse<paths['/api/v1/owners/{id}']>>(`/api/v1/owners/${encodeURIComponent(id)}`),

	create: async (body: PostRequestBody<paths['/api/v1/owners']>) =>
		apiFetch<PostResponse<paths['/api/v1/owners']>>(`/api/v1/owners`, {
			method: 'POST',
			body: JSON.stringify(body)
		}),

	update: async (id: string, body: PutRequestBody<paths['/api/v1/owners/{id}']>) =>
		apiFetch<PutResponse<paths['/api/v1/owners/{id}']>>(`/api/v1/owners/${encodeURIComponent(id)}`, {
			method: 'PUT',
			body: JSON.stringify(body)
		}),

	deactivate: async (id: string) =>
		apiFetch<void>(
			`/api/v1/owners/${encodeURIComponent(id)}/deactivate`,
			{
				method: 'PATCH'
			}
		)
};

// Locations
export const locations = {
	list: async (
		params?: paths['/api/v1/locations']['get']['parameters']['query']
	) => apiFetch<GetResponse<paths['/api/v1/locations']>>(`/api/v1/locations${buildQueryString(params)}`),

	get: async (id: string) =>
		apiFetch<GetResponse<paths['/api/v1/locations/{id}']>>(`/api/v1/locations/${encodeURIComponent(id)}`),

	create: async (body: PostRequestBody<paths['/api/v1/locations']>) =>
		apiFetch<PostResponse<paths['/api/v1/locations']>>(`/api/v1/locations`, {
			method: 'POST',
			body: JSON.stringify(body)
		}),

	update: async (id: string, body: PutRequestBody<paths['/api/v1/locations/{id}']>) =>
		apiFetch<PutResponse<paths['/api/v1/locations/{id}']>>(`/api/v1/locations/${encodeURIComponent(id)}`, {
			method: 'PUT',
			body: JSON.stringify(body)
		}),

	deactivate: async (id: string) =>
		apiFetch<void>(
			`/api/v1/locations/${encodeURIComponent(id)}/deactivate`,
			{
				method: 'PATCH'
			}
		)
};

// Imports
export const imports = {
	preview: async (formData: FormData) =>
		apiFetch<PostResponse<paths['/api/v1/imports/preview']>>(`/api/v1/imports/preview`, {
			method: 'POST',
			headers: {}, // Let browser set Content-Type with boundary
			body: formData
		}),

	commit: async (body: PostRequestBody<paths['/api/v1/imports/commit']>) =>
		apiFetch<PostResponse<paths['/api/v1/imports/commit']>>(`/api/v1/imports/commit`, {
			method: 'POST',
			body: JSON.stringify(body)
		})
};

// Exports
export const exports = {
	devices: async (
		params?: paths['/api/v1/exports/devices']['get']['parameters']['query']
	) => {
		const url = `/api/v1/exports/devices${buildQueryString(params)}`;
		const response = await fetch(`${clientConfig.baseUrl}${url}`, {
			headers: {
				// T05: Inject auth header for export endpoint (blob download)
				...(clientConfig.getAuthToken
					? { Authorization: `Bearer ${await clientConfig.getAuthToken()}` }
					: {})
			}
		});

		if (!response.ok) {
			throw new ApiError(
				response.status,
				response.statusText,
				undefined,
				url
			);
		}

		return response.blob();
	}
};

// Audit Events
export const auditEvents = {
	list: async (
		params?: paths['/api/v1/audit-events']['get']['parameters']['query']
	) => apiFetch<GetResponse<paths['/api/v1/audit-events']>>(`/api/v1/audit-events${buildQueryString(params)}`)
};
