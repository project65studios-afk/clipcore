/**
 * upload-seller.js
 * Seller direct-to-Mux upload using UpChunk (chunked, retryable).
 * - Pre-upload validation: file size <= 3 GB, duration <= 90s
 * - 3 concurrent uploads
 * - Calls Blazor JSInvokable for upload URL + completion
 */

window.sellerUpload = {
    _uppyInstance: null,

    initUppy: function (dotNetHelper, containerId) {
        if (this._uppyInstance) {
            this._uppyInstance.close();
            this._uppyInstance = null;
        }

        const MAX_FILE_SIZE = 3 * 1024 * 1024 * 1024; // 3 GB
        const MAX_DURATION_SEC = 90;
        const MAX_CONCURRENT = 3;

        const uppy = new Uppy.Uppy({
            id: 'seller-uploader',
            autoProceed: false,
            restrictions: {
                maxNumberOfFiles: 500,
                allowedFileTypes: ['video/*'],
                maxFileSize: MAX_FILE_SIZE
            }
        });

        this._uppyInstance = uppy;

        uppy.use(Uppy.Dashboard, {
            target: '#' + containerId,
            inline: true,
            showProgressDetails: true,
            theme: 'dark',
            width: '100%',
            height: 450,
            note: 'Direct upload · Max 3 GB · Max 90 seconds · Up to 3 at a time'
        });

        // Duration validation: read video metadata before uploading
        uppy.on('files-added', async (files) => {
            for (const file of files) {
                try {
                    const duration = await getVideoDuration(file.data);
                    if (duration > MAX_DURATION_SEC) {
                        uppy.removeFile(file.id);
                        uppy.info(`"${file.name}" is ${Math.round(duration)}s — max is ${MAX_DURATION_SEC}s`, 'error', 6000);
                    }
                } catch (e) {
                    // Can't read duration (e.g. unsupported codec) — allow through, server will enforce
                    console.warn(`[SellerUpload] Could not read duration for ${file.name}:`, e);
                }
            }
        });

        // Custom uploader plugin
        const self = this;
        function SellerUploaderPlugin(uppy) {
            this.id = 'SellerUploader';
            this.type = 'uploader';
            this.install = () => uppy.addUploader(this.upload.bind(this));
            this.uninstall = () => uppy.removeUploader(this.upload.bind(this));
            this.update = () => {};

            this.upload = async (fileIDs) => {
                const queue = [...fileIDs];
                let active = 0;

                const processNext = async () => {
                    if (queue.length === 0) return;
                    const id = queue.shift();
                    const file = uppy.getFile(id);
                    active++;

                    try {
                        uppy.emit('upload-started', file);
                        const startTime = Date.now();

                        // 1. Get Mux upload URL from Blazor
                        const uploadResult = await dotNetHelper.invokeMethodAsync('GetMuxUploadUrl', {
                            fileName: file.name,
                            fileSize: file.size,
                            masterFileName: file.name
                        });

                        if (!uploadResult || !uploadResult.url) {
                            throw new Error('Failed to get upload URL');
                        }

                        file.meta.clipId = uploadResult.clipId;
                        file.meta.uploadId = uploadResult.uploadId;

                        // 2. Upload via UpChunk (chunked, retryable)
                        await new Promise((resolve, reject) => {
                            const upload = UpChunk.createUpload({
                                endpoint: uploadResult.url,
                                file: file.data,
                                chunkSize: 8 * 1024, // 8 MB chunks
                                maxFileSize: MAX_FILE_SIZE / 1024, // in KB
                                attempts: 5,
                                delayBeforeAttempt: 3
                            });

                            upload.on('progress', ({ detail: percentage }) => {
                                uppy.setFileState(id, {
                                    progress: {
                                        uploadStarted: startTime,
                                        uploadComplete: false,
                                        percentage: Math.round(percentage),
                                        bytesUploaded: Math.round(file.size * percentage / 100),
                                        bytesTotal: file.size
                                    }
                                });
                                uppy.emit('upload-progress', file, {
                                    uploader: this,
                                    bytesUploaded: Math.round(file.size * percentage / 100),
                                    bytesTotal: file.size
                                });
                            });

                            upload.on('success', () => {
                                uppy.setFileState(id, {
                                    progress: { uploadStarted: startTime, uploadComplete: true, percentage: 100 }
                                });
                                resolve();
                            });

                            upload.on('error', ({ detail: message }) => {
                                reject(new Error(message));
                            });
                        });

                        // 3. Notify Blazor — creates Clip record in DB
                        await dotNetHelper.invokeMethodAsync('OnUppyUploadSuccess', {
                            fileName: file.name,
                            muxUploadId: uploadResult.uploadId,
                            clipId: uploadResult.clipId,
                            masterFileName: file.name,
                            recordingDate: ''
                        });

                        uppy.emit('upload-success', file, { clipId: uploadResult.clipId });
                        uppy.info(`✓ ${file.name} uploaded — processing...`, 'success', 5000);

                    } catch (err) {
                        console.error(`[SellerUpload] Failed ${file.name}:`, err);
                        uppy.emit('upload-error', file, err);
                        uppy.info(`✗ Failed: ${file.name}`, 'error', 6000);
                    } finally {
                        active--;
                        if (queue.length > 0) await processNext();
                    }
                };

                // Start initial batch of concurrent uploads
                const initialBatch = [];
                const batchSize = Math.min(fileIDs.length, MAX_CONCURRENT);
                for (let i = 0; i < batchSize; i++) {
                    initialBatch.push(processNext());
                }
                await Promise.all(initialBatch);
            };
        }

        uppy.use(SellerUploaderPlugin);
    }
};

/**
 * Read video duration using an HTML5 video element.
 * Returns duration in seconds, or throws if not readable.
 */
function getVideoDuration(file) {
    return new Promise((resolve, reject) => {
        const url = URL.createObjectURL(file);
        const video = document.createElement('video');
        video.preload = 'metadata';
        video.src = url;
        video.muted = true;

        const cleanup = () => URL.revokeObjectURL(url);

        video.onloadedmetadata = () => {
            const dur = video.duration;
            cleanup();
            if (isFinite(dur) && dur > 0) resolve(dur);
            else reject(new Error('Could not determine duration'));
        };

        video.onerror = () => {
            cleanup();
            reject(new Error('Video load error'));
        };

        // Timeout after 8 seconds
        setTimeout(() => {
            cleanup();
            reject(new Error('Duration read timeout'));
        }, 8000);
    });
}
