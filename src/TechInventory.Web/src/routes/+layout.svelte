<script lang="ts">
	import '../app.css';
	import { t } from '$lib/i18n';

	let { children } = $props();
</script>

<a href="#main-content" class="skip-link">{t('app.nav.skipToContent')}</a>

<main id="main-content">
	{@render children()}
</main>

<style>
	.skip-link {
		position: absolute;
		left: -9999px;
		top: 0;
		z-index: 10000;
	}
	.skip-link:focus {
		left: 0;
	}
</style>
