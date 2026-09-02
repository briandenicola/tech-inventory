/**
 * Maps an ApiError's field-level validation errors (RFC 7807 `errors`,
 * PascalCase keys like `OwnerId`) onto the camelCase field keys DeviceForm
 * (and friends) key their `errors` state by, taking the first message per
 * field.
 */
export function mapApiFieldErrors(
	errors: Record<string, string[]> | undefined
): Record<string, string> {
	if (!errors) return {};

	return Object.fromEntries(
		Object.entries(errors)
			.filter(([, messages]) => messages.length > 0)
			.map(([field, messages]) => [
				field.charAt(0).toLowerCase() + field.slice(1),
				messages[0]
			])
	);
}

/**
 * Extracts a human-readable message from anything thrown by the API client.
 *
 * This is the single supported way to turn a caught error into toast/banner
 * text. Do not re-derive it at the call site.
 *
 * Why it exists: `ApiError` declares `detail` as a constructor parameter
 * property, so TypeScript always emits `this.detail = detail` — the own
 * property exists on *every* instance even when the server sent no `detail`.
 * That makes the tempting `'detail' in err ? err.detail : fallback` check
 * always take the first branch, yielding `undefined` and a blank red toast.
 * ProblemDetails without a `detail` member is routine: ASP.NET's automatic
 * model-binding 400s omit it, 401/403 bodies are empty, and an nginx 502/504
 * or an offline PWA fetch never produces JSON at all.
 *
 * Resolution order: ProblemDetails `detail` → first field-level validation
 * message → `Error.message` → caller's fallback. The result is never blank.
 */
export function getApiErrorMessage(err: unknown, fallback: string): string {
	const candidates: unknown[] = [];

	if (typeof err === 'object' && err !== null) {
		const problem = err as { detail?: unknown; errors?: unknown; message?: unknown };
		candidates.push(problem.detail);

		const fieldMessages = Object.values(mapApiFieldErrors(
			problem.errors as Record<string, string[]> | undefined
		));
		candidates.push(fieldMessages[0]);

		candidates.push(problem.message);
	} else if (typeof err === 'string') {
		candidates.push(err);
	}

	for (const candidate of candidates) {
		if (typeof candidate === 'string' && candidate.trim().length > 0) {
			return candidate;
		}
	}

	return fallback;
}
