# Plan: Direct Upload A/B Test (The "Thick Client" Strategy)

**Status**: Ready for Implementation
**Goal**: Enable high-speed bulk uploads (300+ clips) without server crashes, while retaining the old system as a backup.

## Core Philosophy
1.  **A/B Test**: We will not delete the old `Upload.razor`. We will add a new `UploadDirect.razor` so you can compare them side-by-side in Production.
2.  **Thick Client**: The browser does the heavy lifting (Thumbnail generation, Parallel Uploads). The server just saves the final record.
3.  **Zero Background Jobs**: No queues, no polling. Instant "Done" state.

## Architecture

### 1. New Page: `UploadDirect.razor`
*   **Route**: `/admin/upload-direct`
*   **Upload Engine**: Uppy (Dashboard UI).

#### The "Smart Sampling" Flow (Per File)
When a user drops a video:
1.  **Seek & Capture (Browser)**:
    *   The browser loads the video file locally.
    *   It seeks to **10%**, **50%**, and **90%** of the duration.
    *   It captures 3 discrete JPEG frames (Thumbnails).
    *   *Performance Impact*: ~0.2 seconds total (User perceives zero delay).
2.  **Parallel Uploads**:
    *   **Main Video**: Uploads directly to **Mux** (1080p Plus Tier).
    *   **Thumbnails**: Uploads the 3 JPEGs directly to **R2** (using Presigned URLs).
3.  **Completion**:
    *   Once all 4 uploads (1 Video + 3 Images) finish, the browser calls the API.

### 2. Backend API (`VideoCompressionController`)
*   **New Endpoint**: `ConfirmDirectUpload`
*   **Input**:
    *   `MuxUploadId`
    *   `List<string> ThumbnailUrls` (3 URLs)
*   **Synchronous Logic**:
    1.  **Create Clip**: Saves the basic record.
    2.  **AI Analysis**: Calls `_visionService.Analyze` on **all 3 thumbnails** in parallel.
    3.  **Tag Aggregation**: Merges the AI keywords from all 3 frames to drastically improve accuracy.
    4.  **Save & Return**: Returns "Success" to the UI.

### 3. Navigation Changes
*   **Admin Sidebar**:
    *   Link 1: "Upload (Legacy)" -> `/admin/upload`
    *   Link 2: "Upload (Direct 1080p)" -> `/admin/upload-direct` (New)


### 4. Database Changes (`Clip` Entity)
*   **Add Property**: `public bool IsDirectUpload { get; set; }`
*   **Purpose**: Differentiates between "Legacy Proxy Clips" (Low Res in Mux, nothing in R2) and "Direct Clips" (High Res in Mux, Status=Ready).

### 5. Fulfillment Logic Updates (The "One-Click Approval" Flow)
**Goal**: Admin retains control (Approve/Reject) but eliminates the labor of uploading files.

1.  **Fulfillment Page (`Fulfillment.razor`)**:
    *   **Visual Logic**:
        *   If `IsDirectUpload`: Show **"Ready (Mux)"** badge.
        *   If `Legacy`: Show **"Needs Upload"** badge.
    *   **Action**:
        *   Admin clicks **"Fulfill Order"**.
        *   System detects `IsDirectUpload`.
        *   **Outcome**: Instantly generates signed Mux URL and sends email. **Zero upload required.**
        *   *Override*: Admin can still choose to ignore Mux and upload a custom file to R2 if needed.
2.  **Customer Experience**:
    *   Order stays "Pending" until Admin clicks the button.
    *   Once clicked, they get the high-quality Mux download.

### 6. Video Player & Token Updates
**Issue**: Current `VideoService` requests tokens scoped to "540p" by default.
**Fix**:
1.  **Update `MuxVideoService`**:
    *   Method `GetPlaybackToken` should accept a `Resolution` parameter or check clip metadata.
    *   If `IsDirectUpload`, request token for **"1080p"** (or remove resolution restriction entirely).
2.  **Frontend**:
    *   `ClipCard` renders `<mux-player>`.
    *   Mux Player automatically adapts. If the asset is 1080p, it plays 1080p.
    *   *Result*: Direct Uploads will look crisp. Legacy uploads will look same as before.
* Result:
Legacy Clips -> Get 540p Token -> Stream at 540p.
Direct Clips -> Get 1080p Token -> Stream at 1080p.

## Benefits Comparison

| Feature | Legacy System | Direct Upload (New) |
| :--- | :--- | :--- |
| **Max Files** | ~6 (Crash Risk) | 500+ (Smooth) |
| **Upload Speed** | Server Bottleneck | Fiber Speed (Max) |
| **Thumbnails** | 1 (Server Generated) | 3 (Browser Generated) |
| **AI Accuracy** | Low (Single Frame) | High (Multi-Frame Context) |
| **Fulfillment** | Manual Upload | **One-Click Approval** |
| **Streaming** | 540p | **1080p** |

## Implementation Steps
1.  **Migration**: Add `IsDirectUpload` to Clip entity.
2.  **Frontend**: Create `UploadDirect.razor` (Smart Sampling + Parallel Uploads).
3.  **Backend**: Add `ConfirmDirectUpload` action.
4.  **Fulfillment**: Update `Fulfillment.razor` to show "One-Click Send" for Direct Uploads.
5.  **Player**: Update `MuxVideoService` to allow 1080p tokens.
