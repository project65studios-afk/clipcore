/**
 * upload-direct.js
 * Finalized Direct Upload Script (V20 Stable Implementation)
 * - Uses AwsS3 plugin for robust signing/upload.
 * - Monkeypatches XMLHttpRequest to handle Mux ETag CORS header missing.
 * - Restores thumbnail generation logic.
 */

export function initDirectUpload(dotNetHelper, eventId, userId) {
    if (!eventId) {
        dotNetHelper.invokeMethodAsync('LogError', 'No Event ID provided. Please return to Event Details and try again.');
        return;
    }

    if (typeof window.Uppy === 'undefined') {
        console.error("Uppy is not loaded!");
        dotNetHelper.invokeMethodAsync('LogError', 'Uppy library missing.');
        return;
    }

    // --- MONKEYPATCH: Fake ETag for Mux (CORS Fix) ---
    if (!window._uppy_mux_hack_applied) {
        const originalOpen = XMLHttpRequest.prototype.open;
        XMLHttpRequest.prototype.open = function (method, url) {
            this._url = url;
            return originalOpen.apply(this, arguments);
        };

        const originalGetHeader = XMLHttpRequest.prototype.getResponseHeader;
        XMLHttpRequest.prototype.getResponseHeader = function (name) {
            const lowerName = name.toLowerCase();
            const isMux = this._url && this._url.includes('mux.com');

            // Mux doesn't expose ETag/Location via CORS. 
            // Calling originalGetHeader for these specifically triggers a browser warning.
            if (isMux && (lowerName === 'etag' || lowerName === 'location')) {
                if (lowerName === 'etag') return '"dummy-etag"';
                return null;
            }

            try {
                return originalGetHeader.apply(this, arguments);
            } catch (e) {
                return null;
            }
        };
        window._uppy_mux_hack_applied = true;
    }

    const UppyCore = window.Uppy.Uppy;
    const Dashboard = window.Uppy.Dashboard;
    const AwsS3 = window.Uppy.AwsS3;

    if (!AwsS3) {
        dotNetHelper.invokeMethodAsync('LogError', 'AwsS3 plugin missing from Uppy bundle.');
        return;
    }

    const uppy = new UppyCore({
        id: 'direct-uploader',
        autoProceed: true,
        debug: false,
        restrictions: {
            maxNumberOfFiles: 500,
            allowedFileTypes: ['video/*'],
            maxFileSize: 50 * 1024 * 1024 * 1024
        }
    });

    uppy.use(Dashboard, {
        inline: true,
        target: '#uppy-direct-dashboard',
        theme: 'dark',
        height: 500,
        showProgressDetails: true,
        note: 'High Quality (1080p/4K) - Unlimited Files',
        proudlyDisplayPoweredByUppy: false
    });

    const thumbnailTasks = {};

    uppy.use(AwsS3, {
        limit: 5,
        shouldUseMultipart: false,
        getUploadParameters: async (file) => {
            try {
                const response = await fetch('/api/admin/VideoCompression/get-direct-upload-url', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ title: file.name, userId: userId })
                });
                if (!response.ok) throw new Error("Backend response error");

                const data = await response.json();
                file.meta.muxUploadId = data.uploadId;
                file.meta.clipId = data.clipId;

                return {
                    url: data.url,
                    method: 'PUT',
                    headers: {
                        'Content-Type': file.type || 'video/mp4'
                    }
                };
            } catch (err) {
                console.error("[DirectUpload] Failed to get upload parameters", err);
                throw err;
            }
        }
    });

    // --- RESTORED: Thumbnail Generation Flow ---
    uppy.on('file-added', (file) => {
        const task = processThumbnails(file.data, file.name, dotNetHelper);
        thumbnailTasks[file.id] = task;
    });

    uppy.on('upload-success', async (file, response) => {
        try {
            const thumbKeys = await thumbnailTasks[file.id];

            // Fetch current price/event info from Blazor
            const uploadInfo = await dotNetHelper.invokeMethodAsync('GetUploadInfo');

            const payload = {
                muxUploadId: file.meta.muxUploadId,
                clipId: file.meta.clipId,
                eventId: uploadInfo.eventId,
                title: file.name,
                priceCents: uploadInfo.priceCents,
                priceCommercialCents: uploadInfo.priceCommercialCents,
                userId: uploadInfo.userId,
                allowGifSale: uploadInfo.allowGifSale,
                gifPriceCents: uploadInfo.gifPriceCents,
                lastModified: new Date(file.data.lastModified).toISOString(),
                thumbnailKeys: thumbKeys || []
            };

            const confirmResp = await fetch('/api/admin/VideoCompression/confirm-direct-upload', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (confirmResp.ok) {
                const data = await confirmResp.json();
                dotNetHelper.invokeMethodAsync('LogSuccess', `DONE: ${file.name} (Clip: ${data.clipId})`);
            } else {
                throw new Error('Confirmation API failed');
            }
        } catch (err) {
            console.error("[DirectUpload] Confirmation failed", err);
            dotNetHelper.invokeMethodAsync('LogError', `Confirm failed: ${file.name}`);
        } finally {
            delete thumbnailTasks[file.id];
        }
    });
}

/**
 * RESTORED: Smart Sampling for Direct Upload
 */
async function processThumbnails(fileBlob, originalName, dotNetHelper) {
    try {
        const thumbBlobs = await generateVideoScreenshots(fileBlob, [0.05, 0.25, 0.5]);
        const uploadedKeys = [];

        for (let i = 0; i < thumbBlobs.length; i++) {
            const blob = thumbBlobs[i];
            const fileName = `${originalName}_thumb_${i}.jpg`;

            const urlResp = await dotNetHelper.invokeMethodAsync('GetR2UploadUrl', fileName, 'image/jpeg');
            const data = JSON.parse(urlResp);

            await fetch(data.url, {
                method: 'PUT',
                body: blob,
                headers: { 'Content-Type': 'image/jpeg' }
            });

            uploadedKeys.push(data.key);
        }
        return uploadedKeys;
    } catch (e) {
        console.error("[DirectUpload] Thumbnail sampling failed", e);
        return [];
    }
}

async function generateVideoScreenshots(file, percentages) {
    return new Promise((resolve) => {
        const video = document.createElement('video');
        video.preload = 'metadata';
        video.src = URL.createObjectURL(file);
        video.muted = true;
        video.playsInline = true;

        video.onloadedmetadata = async () => {
            const duration = video.duration;
            const snapshots = [];
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');

            for (const p of percentages) {
                video.currentTime = duration * p;
                await new Promise(r => video.onseeked = r);

                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

                const blob = await new Promise(r => canvas.toBlob(r, 'image/jpeg', 0.8));
                snapshots.push(blob);
            }

            URL.revokeObjectURL(video.src);
            resolve(snapshots);
        };
    });
}
