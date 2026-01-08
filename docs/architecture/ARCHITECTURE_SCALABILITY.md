# Architecture & Scalability Strategy: Mass Video Ingestion

**Status**: Proposal / Future Roadmap
**Goal**: Enable reliable bulk upload of high-volume video clips (50-500+ items) via the browser without server bottlenecks.

## The Problem: "Server-Side Bottleneck"
The current architecture (Client -> Server -> Mux) places the heavy burden of video processing on the Web Server.
- **CPU**: `ffmpeg` processes consume 100% CPU.
- **Disk**: Storing 300x 100MB inputs fills ephemeral disk space.
- **Concurrency**: A standard web server can handle ~4-6 parallel encodes. 300 files will cause timeouts and crashes.

## The Solution: Client-Side Compression (WASM)

To achieve "Desktop App" performance inside a web browser, we propose utilizing **WebAssembly (FFmpeg.WASM)** to shift compute to the user's device.

### 1. High-Level Workflow
1.  **Drop**: User drops 300 raw video files (Total: 30GB) into the browser.
2.  **Local Transcode**: The browser (via Web Worker) compresses files locally.
    *   *Input*: 100MB 4K MP4.
    *   *Output*: 10MB 540p MP4.
    *   *Speed*: Depends on User CPU, vastly faster than upload bandwidth alone.
3.  **Direct Upload**: Browser uploads the *small* 10MB file directly to Mux (preserving bandwidth).
4.  **Metadata**: Server receives confirmation and queues background jobs for AI/Thumbnails.

### 2. Technical Requirements
Implementing this requires enabling **Cross-Origin Isolation** to unlock `SharedArrayBuffer` (required for Multi-Threaded WASM).

#### A. Security Headers
The application must serve these headers on the document request:
```http
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

#### B. Asset Implications
Because of `COEP` (Embedder Policy), **all external resources** (images, scripts) loaded by the app must explicitly opt-in via CORS/CORP headers.
- **Mux/R2 Images**: Must send `Cross-Origin-Resource-Policy: cross-origin`.
- **Google Auth**: Avatars must support CORP (usually do).
- **Scripts**: CDNs (Bootstrap, htmx) must send proper CORS headers.

### 3. Implementation Roadmap
1.  **Phase 1: Direct Upload (No Compression)**
    *   Switch `Upload.razor` to use Uppy + Mux Direct Upload.
    *   *Benefit*: Removes Server CPU load instantly.
    *   *Cost*: Bandwidth (uploading 30GB raw).
2.  **Phase 2: Enable Isolation**
    *   Add Headers to `Program.cs`.
    *   Audit and fix broken images/scripts.
3.  **Phase 3: Activate WASM**
    *   Integrate `@ffmpeg/ffmpeg`.
    *   Build Web Worker pipeline.
    *   Inject Compression step *before* Upload step in Uppy.

### 4. Comparison to Desktop App
| Feature | Desktop App | WASM (Browser) |
| :--- | :--- | :--- |
| **Performance** | Native (100%) | Near-Native (80-90%) |
| **Installation** | Required (High Friction) | None (Zero Friction) |
| **Maintenance** | High (Updates, OS versions) | Low (Web Deploy) |

### Recommendation
For Project65, **Phase 1 (Direct Upload)** is the immediate robust fix. **Phase 3 (WASM)** is the long-term scalability solution if user bandwidth becomes a primary complaint.
