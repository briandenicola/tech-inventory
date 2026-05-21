const DOWNLOAD_CLEANUP_DELAY_MS = 1000;

export function triggerBlobDownload(blob: Blob, fileName: string): void {
	const objectUrl = URL.createObjectURL(blob);
	const link = document.createElement('a');
	link.href = objectUrl;
	link.download = fileName;
	link.rel = 'noopener';
	link.style.display = 'none';

	document.body.append(link);
	link.click();

	window.setTimeout(() => {
		URL.revokeObjectURL(objectUrl);
		link.remove();
	}, DOWNLOAD_CLEANUP_DELAY_MS);
}
