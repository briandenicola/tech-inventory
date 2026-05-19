// Pre-render so the service worker can serve this shell from cache when the
// network is unreachable. T52 navigateFallback points here.
export const prerender = true;
export const ssr = true;
