let uppyInstance = null;

window.muxUpload = {
    initUppy: function (dotNetHelper, containerId, userId) {
        if (uppyInstance) {
            uppyInstance.close();
        }

        uppyInstance = new Uppy.Uppy({
            id: 'mux-uploader',
            autoProceed: false,
            restrictions: {
                maxNumberOfFiles: 500,
                allowedFileTypes: ['video/*']
            }
        });

        uppyInstance.use(Uppy.Dashboard, {
            target: '#' + containerId,
            inline: true,
            showProgressDetails: true,
            theme: 'dark',
            width: '100%',
            height: 450,
            note: 'Standard Mode: Videos will be compressed on server and uploaded as 480p proofs.'
        });

        function ServerCompressionPlugin(uppy) {
            this.id = 'ServerCompression';
            this.type = 'uploader';

            this.install = () => {
                uppy.addUploader(this.upload);
            };

            this.uninstall = () => {
                uppy.removeUploader(this.upload);
            };

            this.update = () => { };

            this.upload = async (fileIDs) => {
                // Get event and price from Blazor component
                const uploadInfo = await dotNetHelper.invokeMethodAsync('GetUploadInfo');

                // Concurrency control
                const MAX_CONCURRENT_UPLOADS = 6;
                let activeUploads = 0;
                const queue = [...fileIDs]; // Clone array to manage queue

                // Process queue
                const processNext = async () => {
                    if (queue.length === 0) return;

                    const id = queue.shift();
                    const file = uppy.getFile(id);
                    activeUploads++;

                    try {
                        // --- Upload Logic Start ---
                        uppy.info(`Uploading ${file.name} to server...`, 'info', 0);
                        const startTime = performance.now();

                        // Create FormData
                        if (!uploadInfo || !uploadInfo.eventId) {
                            console.error('Missing eventId in uploadInfo', uploadInfo);
                        }

                        const formData = new FormData();
                        formData.append('file', file.data);
                        formData.append('eventId', uploadInfo.eventId);
                        formData.append('masterFileName', file.name);
                        formData.append('priceCents', uploadInfo.priceCents);
                        formData.append('userId', uploadInfo.userId);

                        // Capture Modified Date
                        if (file.data.lastModified) {
                            const modifiedDate = new Date(file.data.lastModified).toISOString();
                            formData.append('lastModified', modifiedDate);
                        }

                        // Upload to server with progress tracking
                        await new Promise((resolve, reject) => {
                            const xhr = new XMLHttpRequest();

                            // Mark file as uploading
                            uppy.setFileState(id, {
                                progress: {
                                    uploadStarted: Date.now(),
                                    uploadComplete: false,
                                    percentage: 0,
                                    bytesUploaded: 0,
                                    bytesTotal: file.size
                                }
                            });

                            uppy.emit('upload-started', file);

                            xhr.upload.addEventListener('progress', (e) => {
                                if (e.lengthComputable) {
                                    const percentage = Math.round((e.loaded / e.total) * 100);
                                    uppy.setFileState(id, {
                                        progress: {
                                            uploadStarted: startTime,
                                            uploadComplete: false,
                                            percentage: percentage,
                                            bytesUploaded: e.loaded,
                                            bytesTotal: e.total
                                        }
                                    });
                                    uppy.emit('upload-progress', file, {
                                        uploader: this,
                                        bytesUploaded: e.loaded,
                                        bytesTotal: e.total
                                    });
                                }
                            });

                            xhr.addEventListener('load', () => {
                                if (xhr.status >= 200 && xhr.status < 300) {
                                    const response = JSON.parse(xhr.responseText);
                                    uppy.setFileState(id, {
                                        progress: { uploadComplete: true, uploadStarted: Date.now(), percentage: 100 }
                                    });
                                    uppy.emit('upload-success', file, {
                                        clipId: response.clipId,
                                        fileName: file.name
                                    });
                                    uppy.info(`✓ ${file.name} uploaded!`, 'success', 3000);
                                    resolve(response);
                                } else {
                                    reject(new Error(`Server returned ${xhr.status}: ${xhr.responseText}`));
                                }
                            });

                            xhr.addEventListener('error', () => reject(new Error('Network error')));
                            xhr.open('POST', '/api/admin/VideoCompression/compress-and-upload');

                            // Add CSRF Token
                            const token = document.cookie.split('; ').find(row => row.startsWith('XSRF-TOKEN='))?.split('=')[1];
                            if (token) xhr.setRequestHeader('X-XSRF-TOKEN', decodeURIComponent(token));

                            xhr.send(formData);
                        });
                        // --- Upload Logic End ---

                    } catch (err) {
                        console.error(`[Server] Failed ${file.name}:`, err);
                        uppy.emit('upload-error', file, err);
                        uppy.info(`Failed to upload ${file.name}`, 'error', 5000);
                    } finally {
                        activeUploads--;
                        // If there are more items in queue, start next one
                        if (queue.length > 0) {
                            await processNext();
                        }
                    }
                };

                // Start initial batch of uploads
                const initialBatch = [];
                const batchSize = Math.min(fileIDs.length, MAX_CONCURRENT_UPLOADS);

                for (let i = 0; i < batchSize; i++) {
                    initialBatch.push(processNext());
                }

                // Wait for all initial threads (which will recursively process the rest) to finish
                // Note: This logic waits for the 'chains' to complete.
                await Promise.all(initialBatch);
            };
        }

        uppyInstance.use(ServerCompressionPlugin);

        uppyInstance.on('upload-success', (file, response) => {
            // No need to invoke OnUppyUploadSuccess anymore - server handles everything
        });
    }
};

// Generic Direct Upload to Mux helper (Outside any closure to be safe)
// Generic Direct Upload to Mux helper
window.uploadToMux = function (file, uploadUrl, dotnetRef) {

    const upload = UpChunk.createUpload({
        endpoint: uploadUrl,
        file: file,
        chunkSize: 8192 * 1024, // 8MB
    });

    upload.on('progress', (progress) => {
        dotnetRef.invokeMethodAsync('OnUploadProgress', progress.detail);
    });

    upload.on('success', () => {
        dotnetRef.invokeMethodAsync('OnUploadSuccess');
    });

    upload.on('error', (err) => {
        console.error("Mux Upload Error", err);
        dotnetRef.invokeMethodAsync('OnUploadError', err.detail.message);
    });
};

window.fulfillmentUpload = {
    initUppy: function (dotNetHelper, containerId) {
        // reuse global instance if needed, or create new. 
        // Ideally we destroy old one if exists to avoid ID conflicts.
        // For simplicity, let's assume one instance per page load or use strict IDs.

        const uppy = new Uppy.Uppy({
            id: 'fulfillment-uploader',
            autoProceed: false, // Wait for user to click button
            restrictions: {
                maxNumberOfFiles: 50,
                allowedFileTypes: ['video/*']
            }
        });

        uppy.use(Uppy.Dashboard, {
            target: '#' + containerId,
            inline: true,
            height: 350,
            width: '100%',
            theme: 'dark',
            showProgressDetails: true,
            note: 'Bulk Upload: Add all clips for this order. System will match by filename.',
            hideUploadButton: false // Show the button
        });

        // Custom Plugin to Bridge Uppy -> UpChunk (Mux)
        function FulfillmentBridge(uppy) {
            this.id = 'FulfillmentBridge';
            this.type = 'uploader';

            this.install = () => uppy.addUploader(this.upload);
            this.uninstall = () => uppy.removeUploader(this.upload);
            this.update = () => { };

            this.upload = async (fileIDs) => {
                for (const id of fileIDs) {
                    const file = uppy.getFile(id);
                    uppy.info(`Starting R2 fulfillment for ${file.name}...`, 'info');

                    try {
                        // 1. Get R2 Presigned PUT URL from Blazor
                        const uploadUrl = await dotNetHelper.invokeMethodAsync('GetR2UploadUrl', file.name, file.type);

                        if (!uploadUrl) {
                            const errorMsg = `No matching item found for "${file.name}"`;
                            uppy.info(errorMsg, 'error', 5000);
                            uppy.setFileState(id, {
                                error: errorMsg,
                                progress: { uploadComplete: false, percentage: 0 }
                            });
                            uppy.emit('upload-error', file, new Error(errorMsg));
                            continue;
                        }

                        // Emit start event for UI
                        uppy.emit('upload-started', file);
                        const startTime = performance.now();

                        // 2. Upload via Standard XHR (PUT)
                        await new Promise((resolve, reject) => {
                            const xhr = new XMLHttpRequest();
                            xhr.upload.addEventListener('progress', (e) => {
                                if (e.lengthComputable) {
                                    const percentage = Math.round((e.loaded / e.total) * 100);

                                    uppy.setFileState(id, {
                                        progress: {
                                            uploadStarted: startTime,
                                            uploadComplete: false,
                                            percentage: percentage,
                                            bytesUploaded: e.loaded,
                                            bytesTotal: e.total
                                        }
                                    });

                                    uppy.emit('upload-progress', file, {
                                        uploader: this,
                                        bytesTotal: e.total,
                                        bytesUploaded: e.loaded
                                    });
                                }
                            });

                            xhr.addEventListener('load', () => {
                                if (xhr.status >= 200 && xhr.status < 300) {
                                    uppy.setFileState(id, {
                                        progress: { uploadComplete: true, uploadStarted: Date.now(), percentage: 100 }
                                    });
                                    uppy.emit('upload-success', file, { uploadUrl });
                                    resolve();
                                } else {
                                    reject(new Error(`R2 returned ${xhr.status}`));
                                }
                            });

                            xhr.addEventListener('error', () => reject(new Error('Network error')));

                            xhr.open('PUT', uploadUrl);
                            xhr.setRequestHeader('Content-Type', file.type);
                            xhr.send(file.data);
                        });

                        // Notify Blazor of completion for this specific file
                        await dotNetHelper.invokeMethodAsync('OnUploadSuccess', file.name);

                    } catch (err) {
                        uppy.emit('upload-error', file, err);
                        console.error(err);
                    }
                }
            };
        }

        uppy.use(FulfillmentBridge);

        // Cleanup function for Blazor to call?
        return {
            dispose: () => uppy.close()
        };
    }
};
