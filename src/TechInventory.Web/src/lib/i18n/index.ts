import en from './en.json';

// TODO: Replace with a proper i18n library when multi-language support is needed
// For now, a simple function that returns the English catalog
export function getTranslations() {
	return en;
}

// Type-safe translation key helper
export type TranslationKey = string;

export function t(key: TranslationKey): string {
	const translations = getTranslations();
	const keys = key.split('.');
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let value: any = translations;

	for (const k of keys) {
		value = value[k];
		if (value === undefined) {
			console.warn(`Translation key not found: ${key}`);
			return key;
		}
	}

	return String(value);
}
