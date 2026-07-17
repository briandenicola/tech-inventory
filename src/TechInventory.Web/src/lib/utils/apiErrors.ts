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
