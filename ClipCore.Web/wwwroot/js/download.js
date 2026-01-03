export async function downloadFiles(urls) {
    if (!urls || urls.length === 0) return;

    for (const url of urls) {
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', '');
        link.style.display = 'none';
        document.body.appendChild(link);

        link.click();

        document.body.removeChild(link);

        // Small delay to prevent browser from blocking multiple downloads
        await new Promise(resolve => setTimeout(resolve, 1000));
    }
}
