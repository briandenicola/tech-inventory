<script lang="ts">
	import { t } from '$lib/i18n';
</script>

<div class="container">
	<h1 class="title">{t('app.title')}</h1>
	<p class="tagline">{t('app.tagline')}</p>
</div>

<style>
	.container {
		padding: var(--space-2xl);
		text-align: center;
	}
	
	.title {
		font-size: var(--text-3xl);
		color: var(--color-text);
		margin-bottom: var(--space-md);
	}
	
	.tagline {
		font-size: var(--text-lg);
		color: var(--color-text-secondary);
	}
</style>
