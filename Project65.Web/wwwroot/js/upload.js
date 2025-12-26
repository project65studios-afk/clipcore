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
            note: 'PhotoReflect Mode: Videos will be compressed on server and uploaded as 480p proofs.'
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

                for (const id of fileIDs) {
                    const file = uppy.getFile(id);

                    try {
                        uppy.info(`Uploading ${file.name} to server...`, 'info', 0);

                        const startTime = performance.now();

                        // Create FormData
                        const formData = new FormData();
                        formData.append('file', file.data);
                        formData.append('eventId', uploadInfo.eventId);
                        formData.append('masterFileName', file.name);
                        formData.append('priceCents', uploadInfo.priceCents);
                        formData.append('userId', uploadInfo.userId);

                        // Upload to server with progress tracking
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

                                // Update file state for Uppy Dashboard to show progress
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
                                uppy.info(`Uploading ${file.name}: ${percentage}% (${(e.loaded / 1024 / 1024).toFixed(1)}MB / ${(e.total / 1024 / 1024).toFixed(1)}MB)`, 'info', 1000);
                            }
                        });

                        const uploadPromise = new Promise((resolve, reject) => {
                            xhr.addEventListener('load', () => {
                                if (xhr.status >= 200 && xhr.status < 300) {
                                    const response = JSON.parse(xhr.responseText);
                                    const totalTime = ((performance.now() - startTime) / 1000).toFixed(1);
                                    console.log(`[Server] Complete for ${file.name} in ${totalTime}s. Original: ${(response.originalSize / 1024 / 1024).toFixed(2)}MB, Compressed: ${(response.compressedSize / 1024 / 1024).toFixed(2)}MB, Compression: ${response.compressionTime.toFixed(1)}s`);

                                    // Update file state to show 100% complete
                                    uppy.setFileState(id, {
                                        progress: { uploadComplete: true, uploadStarted: Date.now() }
                                    });

                                    uppy.emit('upload-success', file, {
                                        clipId: response.clipId,
                                        fileName: file.name
                                    });

                                    // Show success notification
                                    uppy.info(`✓ ${file.name} uploaded and compressed successfully!`, 'success', 5000);

                                    resolve(response);
                                } else {
                                    reject(new Error(`Server returned ${xhr.status}: ${xhr.responseText}`));
                                }
                            });

                            xhr.addEventListener('error', () => {
                                reject(new Error('Network error'));
                            });

                            xhr.open('POST', '/api/admin/VideoCompression/compress-and-upload');
                            xhr.send(formData);
                        });

                        await uploadPromise;

                    } catch (err) {
                        console.error(`[Server] Failed ${file.name}:`, err);
                        uppy.emit('upload-error', file, err);
                    }
                }
            };
        }

        uppyInstance.use(ServerCompressionPlugin);

        uppyInstance.on('upload-success', (file, response) => {
            console.log(`[Uppy] Upload success for ${file.fileName}`);
            // No need to invoke OnUppyUploadSuccess anymore - server handles everything
        });
    }
};

// Generic Direct Upload to Mux helper (Outside any closure to be safe)
window.uploadToMux = function (file, uploadUrl, dotnetRef) {
    console.log("Starting upload to Mux", file.name, uploadUrl);

    const upload = UpChunk.createUpload({
        endpoint: uploadUrl,
        file: file,
        chunkSize: 256 * 1024, // 256KB (Max seems to be ~500KB based on error)
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
