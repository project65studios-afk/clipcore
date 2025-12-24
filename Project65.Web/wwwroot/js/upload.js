let uppyInstance = null;

window.muxUpload = {
    initUppy: function (dotNetHelper, containerId, userId) {
        if (uppyInstance) {
            uppyInstance.close();
        }

        uppyInstance = new Uppy.Uppy({
            id: 'mux-uploader',
            autoProceed: false,
            allowMultipleUploadBatches: true,
            restrictions: {
                maxNumberOfFiles: 50,
                allowedFileTypes: ['video/*']
            }
        });

        uppyInstance.use(Uppy.Dashboard, {
            target: '#' + containerId,
            inline: true,
            showProgressDetails: true,
            proudlyDisplayPoweredByUppy: false,
            theme: 'dark',
            width: '100%',
            height: 450,
            note: 'Large files (up to 1GB) supported. Batch processed one-by-one for maximum speed.'
        });

        // Use XHRUpload for direct PUT to Mux
        uppyInstance.use(Uppy.XHRUpload, {
            method: 'PUT',
            formData: false,
            limit: 1, // Sequential batch as requested
            getResponseError(responseText, xhr) {
                return new Error(responseText || 'Upload failed');
            }
        });

        // Before each file starts, get a signed URL from Blazor
        uppyInstance.addPreProcessor(async (fileIds) => {
            for (const id of fileIds) {
                const file = uppyInstance.getFile(id);
                try {
                    const result = await dotNetHelper.invokeMethodAsync('GetMuxUploadUrl', file.name, file.id);
                    // result: { url: "...", uploadId: "..." }
                    uppyInstance.setFileState(id, {
                        xhrUpload: { endpoint: result.url },
                        meta: { ...file.meta, muxUploadId: result.uploadId, clipId: result.clipId, recordingDate: new Date(file.data.lastModified || Date.now()).toISOString() }
                    });
                } catch (err) {
                    console.error("Failed to get Mux URL", err);
                    uppyInstance.info("Failed to prepare upload for " + file.name, 'error', 5000);
                    throw err; // Stop the upload if we can't get a URL
                }
            }
        });

        uppyInstance.on('upload-success', (file, response) => {
            console.log('Upload success:', file.name);
            dotNetHelper.invokeMethodAsync('OnUppyUploadSuccess', {
                fileName: file.name,
                muxUploadId: file.meta.muxUploadId,
                clipId: file.meta.clipId,
                recordingDate: file.meta.recordingDate
            });
        });

        uppyInstance.on('complete', (result) => {
            console.log('Batch complete:', result.successful.length, 'files uploaded');
        });
    }
};
