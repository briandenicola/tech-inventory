<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		createdAt: string | null;
		createdBy?: string | null;
		modifiedAt: string | null;
		modifiedBy?: string | null;
	}

	let { createdAt, createdBy = null, modifiedAt, modifiedBy = null }: Props = $props();

	function formatDateTime(dateStr: string | null): string {
		if (!dateStr) return '—';

		const date = new Date(dateStr);
		return date.toLocaleString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit'
		});
	}
</script>

<section
	class="rounded-lg p-5 shadow-sm"
	style="background: var(--color-bg); border: 1px solid var(--color-border);"
	aria-labelledby="device-audit-trail-title"
>
	<h2
		class="m-0 text-sm font-semibold leading-normal"
		style="color: var(--color-text);"
		id="device-audit-trail-title"
	>
		{t('devices.detail.audit.title')}
	</h2>

	<dl class="mt-3 grid gap-3">
		<div class="grid gap-1">
			<dt class="font-medium" style="color: var(--color-text-secondary);">
				{t('devices.detail.audit.created')}
			</dt>
			<dd class="m-0 flex flex-wrap gap-1" style="color: var(--color-text);">
				{#if createdAt}
					<time datetime={createdAt} title={formatDateTime(createdAt)}>
						{formatDateTime(createdAt)}
					</time>
				{:else}
					<span>—</span>
				{/if}

				{#if createdBy}
					<span style="color: var(--color-text-secondary);">
						{t('devices.detail.audit.by', { actor: createdBy })}
					</span>
				{/if}
			</dd>
		</div>

		<div class="grid gap-1">
			<dt class="font-medium" style="color: var(--color-text-secondary);">
				{t('devices.detail.audit.lastModified')}
			</dt>
			<dd class="m-0 flex flex-wrap gap-1" style="color: var(--color-text);">
				{#if modifiedAt}
					<time datetime={modifiedAt} title={formatDateTime(modifiedAt)}>
						{formatDateTime(modifiedAt)}
					</time>
				{:else}
					<span>—</span>
				{/if}

				{#if modifiedBy}
					<span style="color: var(--color-text-secondary);">
						{t('devices.detail.audit.by', { actor: modifiedBy })}
					</span>
				{/if}
			</dd>
		</div>
	</dl>
</section>
