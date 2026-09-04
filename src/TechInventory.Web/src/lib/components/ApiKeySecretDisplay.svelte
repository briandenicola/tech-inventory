<!--
	ApiKeySecretDisplay.svelte — #149 / spec 005 US-6.

	Shows the plaintext API key exactly once, immediately after creation.

	This is the only moment the credential exists outside the caller's own storage:
	the server keeps an HMAC verifier and nothing else, so if the user navigates away
	without copying it, the key is unrecoverable and their only option is to revoke and
	create another. That is why the warning is part of the heading rather than a
	footnote, why the value sits in a read-only input the user can select manually, and
	why dismissing is an explicit action rather than something a stray click triggers.

	The clipboard write can fail for reasons the page cannot control (insecure context,
	Safari permission prompts, an older WebView). Failure is reported inline and the
	value stays visible and selectable, so the copy button is never the only way out.
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		secret: string;
		onDismiss: () => void;
	}

	let { secret, onDismiss }: Props = $props();

	type CopyState = 'idle' | 'copied' | 'failed';
	let copyState = $state<CopyState>('idle');
	let inputElement = $state<HTMLInputElement | null>(null);

	async function copy() {
		try {
			await navigator.clipboard.writeText(secret);
			copyState = 'copied';
		} catch {
			// Selecting the text gives the user a manual path when the API is unavailable.
			copyState = 'failed';
			inputElement?.select();
		}
	}
</script>

<div
	class="rounded-xl border border-success-300 bg-success-50 p-4 dark:border-success-800 dark:bg-success-950"
	data-testid="api-key-secret"
>
	<p class="text-sm font-medium text-success-800 dark:text-success-200">
		{t('settings.apiKeys.secret.heading')}
	</p>

	<div class="mt-3 flex flex-col gap-2 sm:flex-row sm:items-center">
		<label class="sr-only" for="api-key-secret-value">{t('settings.apiKeys.secret.label')}</label>
		<input
			id="api-key-secret-value"
			bind:this={inputElement}
			type="text"
			readonly
			value={secret}
			onfocus={(event) => event.currentTarget.select()}
			class="w-full min-w-0 rounded-lg border border-success-300 bg-white px-3 py-2 font-mono text-xs text-neutral-900 dark:border-success-800 dark:bg-neutral-950 dark:text-neutral-100"
		/>
		<div class="flex shrink-0 gap-2">
			<button
				type="button"
				onclick={copy}
				class="inline-flex h-11 items-center justify-center rounded-lg bg-success-600 px-4 text-sm font-medium text-white transition-colors hover:bg-success-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-success-500 focus-visible:ring-offset-2"
			>
				{copyState === 'copied' ? t('settings.apiKeys.secret.copied') : t('settings.apiKeys.secret.copy')}
			</button>
			<button
				type="button"
				onclick={onDismiss}
				class="inline-flex h-11 items-center justify-center rounded-lg border border-neutral-300 px-4 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
			>
				{t('settings.apiKeys.secret.dismiss')}
			</button>
		</div>
	</div>

	<!-- Announced politely: the user has just acted, so this confirms rather than interrupts. -->
	<p class="mt-2 text-xs text-success-700 dark:text-success-300" role="status" aria-live="polite">
		{#if copyState === 'copied'}
			{t('settings.apiKeys.secret.copied')}
		{:else if copyState === 'failed'}
			{t('settings.apiKeys.secret.copyFailed')}
		{/if}
	</p>
</div>
