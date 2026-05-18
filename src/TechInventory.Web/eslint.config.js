/**
 * Four-gate auth token storage enforcement:
 * 1. ESLint blocks token-like localStorage access in the web app.
 * 2. Pre-commit scanning catches localStorage + token patterns before push.
 * 3. Playwright asserts real auth flows never persist tokens to localStorage.
 * 4. Security review checklist keeps the policy visible during code review.
 */
import js from '@eslint/js';
import ts from 'typescript-eslint';
import svelte from 'eslint-plugin-svelte';
import prettier from 'eslint-config-prettier';
import globals from 'globals';

const tokenLikeStorageKeyPattern = /token|jwt|access|refresh|id_token|msal/i;
const restrictedLocalStoragePathPattern = /\/src\/lib\/(?:auth|api)\//;
const restrictedLocalStorageMethods = new Set(['setItem', 'getItem', 'removeItem']);

function normalizePath(value) {
	return value.replace(/\\/g, '/');
}

function unwrapChainExpression(node) {
	return node?.type === 'ChainExpression' ? node.expression : node;
}

function isIdentifierNamed(node, name) {
	const target = unwrapChainExpression(node);
	return target?.type === 'Identifier' && target.name === name;
}

function isWindowLocalStorageMember(node) {
	const target = unwrapChainExpression(node);

	return (
		target?.type === 'MemberExpression' &&
		!target.computed &&
		isIdentifierNamed(target.object, 'window') &&
		isIdentifierNamed(target.property, 'localStorage')
	);
}

function isLocalStorageReference(node) {
	const target = unwrapChainExpression(node);
	return isIdentifierNamed(target, 'localStorage') || isWindowLocalStorageMember(target);
}

function getMemberPropertyName(node) {
	const target = unwrapChainExpression(node);

	if (target?.type !== 'MemberExpression') {
		return null;
	}

	if (!target.computed && target.property.type === 'Identifier') {
		return target.property.name;
	}

	if (target.computed && target.property.type === 'Literal' && typeof target.property.value === 'string') {
		return target.property.value;
	}

	return null;
}

function getStaticStringValue(node) {
	const target = unwrapChainExpression(node);

	if (!target) {
		return null;
	}

	if (target.type === 'Literal' && typeof target.value === 'string') {
		return target.value;
	}

	if (target.type === 'TemplateLiteral' && target.expressions.length === 0) {
		return target.quasis[0]?.value.cooked ?? null;
	}

	return null;
}

const securityPlugin = {
	rules: {
		'no-auth-token-localstorage': {
			meta: {
				type: 'problem',
				docs: {
					description: 'Forbid localStorage for auth data and auth/api modules.'
				},
				schema: [],
				messages: {
					noLocalStorageInRestrictedModules:
						'localStorage is forbidden in src/lib/auth and src/lib/api. Use sessionStorage or in-memory state instead.',
					noTokenLikeKey:
						'Auth-related localStorage key "{{key}}" is forbidden. Store tokens in sessionStorage or memory only.'
				}
			},
			create(context) {
				const normalizedFilename = normalizePath(context.getFilename());
				const restrictAllLocalStorage = restrictedLocalStoragePathPattern.test(normalizedFilename);

				return {
					CallExpression(node) {
						const callee = unwrapChainExpression(node.callee);

						if (callee?.type !== 'MemberExpression' || !isLocalStorageReference(callee.object)) {
							return;
						}

						const methodName = getMemberPropertyName(callee);

						if (methodName === null || !restrictedLocalStorageMethods.has(methodName)) {
							return;
						}

						if (restrictAllLocalStorage) {
							context.report({
								node,
								messageId: 'noLocalStorageInRestrictedModules'
							});
							return;
						}

						const key = getStaticStringValue(node.arguments[0]);

						if (key !== null && tokenLikeStorageKeyPattern.test(key)) {
							context.report({
								node: node.arguments[0],
								messageId: 'noTokenLikeKey',
								data: { key }
							});
						}
					},
					Identifier(node) {
						if (!restrictAllLocalStorage || node.name !== 'localStorage') {
							return;
						}

						const parent = node.parent;

						if (parent?.type === 'MemberExpression') {
							if (parent.property === node && !parent.computed) {
								return;
							}

							if (parent.object === node) {
								const grandparent = parent.parent;

								if (grandparent?.type === 'CallExpression' && grandparent.callee === parent) {
									return;
								}

								if (grandparent?.type === 'MemberExpression' && grandparent.object === parent) {
									return;
								}
							}
						}

						context.report({
							node,
							messageId: 'noLocalStorageInRestrictedModules'
						});
					},
					MemberExpression(node) {
						if (!restrictAllLocalStorage || !isWindowLocalStorageMember(node)) {
							return;
						}

						const parent = node.parent;

						if (parent?.type === 'MemberExpression' && parent.object === node) {
							const grandparent = parent.parent;

							if (grandparent?.type === 'CallExpression' && grandparent.callee === parent) {
								return;
							}
						}

						context.report({
							node,
							messageId: 'noLocalStorageInRestrictedModules'
						});
					}
				};
			}
		}
	}
};

export default [
	js.configs.recommended,
	...ts.configs.recommended,
	...svelte.configs['flat/recommended'],
	{
		plugins: {
			security: securityPlugin
		},
		rules: {
			'security/no-auth-token-localstorage': 'error'
		}
	},
	prettier,
	...svelte.configs['flat/prettier'],
	{
		languageOptions: {
			globals: {
				...globals.browser,
				...globals.node
			}
		}
	},
	{
		files: ['**/*.svelte'],
		languageOptions: {
			parserOptions: {
				parser: ts.parser
			}
		},
		rules: {
			// Downgrade Svelte compiler warnings to ESLint warnings (Constitution D-072: intentional runes patterns)
			'svelte/valid-compile': 'warn'
		}
	},
	{
		ignores: ['build/', '.svelte-kit/', 'dist/']
	}
];
