# Implementation Plan: Client-Side Video Compression (WASM)

**Status**: Future Feature / Advanced Optimization
**Prerequisites**: "Direct Upload" (Phase 1) should be implemented first.

## Goal
Enable **High-Speed Bulk Uploads** (300+ clips) by compressing video files inside the browser *before* uploading. This replicates the performance benefits of a Desktop App without requiring any installation.

## Technical Complexity & Requirements
1.  **SharedArrayBuffer & Cross-Origin Isolation**:
    *   To use Multi-Threading (essential for video encoding speed), the browser requires a strict security context mechanism called `SharedArrayBuffer`.
    *   **Requirement**: We must serve the website with specific headers (`Cross-Origin-Opener-Policy: same-origin`, `Cross-Origin-Embedder-Policy: require-corp`).
    *   **Side Effect**: This *breaks* loading images/scripts from cross-origin domains (like CDN images, Google Maps, external scripts) unless those resources explicitly opt-in via CORP headers. We will need to proxy or configure all external assets correctly.

2.  **Browser Memory Limits**:
    *   WASM has a hard memory limit (usually 2GB or 4GB). Compressing 4K video can sometimes hit this limit, causing the tab to crash. We need robust error handling to fallback or warn the user.

3.  **UI/UX Threading**:
    *   Encoding blocks the main thread. We must run the transcoding in a dedicated **Web Worker** loop to keep the UI responsive.

## Implementation Steps

### 1. Security Headers Configuration (The "Plumbing")
To enable multi-threading (making compression 4x-10x faster), we must update `Program.cs` to send strict isolation headers.

*   **Modify**: `Program.cs` (Middleware)
*   **Action**: Add middleware to inject headers on all document requests.
    ```csharp
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
        context.Response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
        await next();
    });
    ```
*   **Mitigation**: Audit all external image sources (Mux, R2, Google Auth avatars). If they break, we must setup a simple Image Proxy or ensure they support CORP.

### 2. Frontend Integration (The "Engine")
We will integrate `@ffmpeg/ffmpeg` (v0.12+) and `@ffmpeg/util`.

*   **New File**: `wwwroot/js/video-processor.js` (Web Worker).
*   **Logic**:
    1.  User Selects Files -> Uppy UI shows "Waiting..."
    2.  **Queue Manager**: Takes 1-2 files at a time.
    3.  **Transcode**: Sends file to Web Worker.
        *   Command: `ffmpeg -i input.mp4 -vf scale=-2:540 -c:v libx264 -crf 28 output.mp4`
    4.  **Result**: Returns a `Blob` (the compressed file).
    5.  **Upload**: Uppy takes this new `Blob` and uploads it to Mux/Server (via Direct Upload or existing endpoint).

### 3. UI/UX Updates (`Upload.razor`)
*   **Visual Feedback**:
    *   Add a new state: **"Compressing... (45%)"** distinct from "Uploading...".
    *   Total ETA calculation needs to account for (Compression Time + Upload Time).
*   **Concurrency**:
    *   **Compression**: Limit to 1 simultaneous file (CPU intensive).
    *   **Upload**: Limit to 4 simultaneous files (Network intensive).

## Gap Analysis & Risks
| Risk | Solution |
| :--- | :--- |
| **External Images Break** | Test Mux/R2 images immediately after enabling COEP. If broken, enable CORS/CORP on R2 bucket. |
| **Slow Hardware** | Users on old laptops will encode slowly. | **Feature Flag**: Add a "Fast Upload (Low Quality)" checkbox. If unchecked, skip compression and upload Raw. |
| **Browser Support** | Safari support for WASM threads is inconsistent. | Fallback to Single-Threaded mode (slower but works) or Raw Upload. |
