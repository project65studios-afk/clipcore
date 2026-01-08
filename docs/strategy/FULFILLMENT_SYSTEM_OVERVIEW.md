# Fulfillment System Overview: Mux vs. Legacy R2

Project65 now supports two distinct ways to deliver high-resolution video files to customers. This hybrid system allow you to choose between maximum efficiency (Mux) and custom control (R2).

---

## 🚀 1. The Direct Upload Flow (Mux)
This flow is optimized for speed and simplicity. It uses Mux as both the hosting provider and the fulfillment delivery system.

### **How it works:**
1.  **Upload**: You use the "Direct Upload" tool to send a high-quality master (4K/1080p) directly to Mux.
2.  **Tracking**: The system automatically captures the Mux Asset ID and links it to your database Clip.
3.  **Fulfillment**: On the Fulfillment page, these items appear with a yellow **`Ready (Mux)`** badge.
4.  **Completion**: Click **"Auto-Fulfill"**.
5.  **Delivery**: The customer receives an email. When they visit their delivery page, the system dynamically generates a temporary, high-quality download link directly from Mux.

### **Benefits:**
- **Zero Double-Uploading**: No need to re-upload files you already sent to Mux.
- **Cost Effective**: Saves storage space and egress on Cloudflare R2.
- **Instant**: Fulfillment takes a single click and is immediately available to the user.

---

## 💾 2. The Legacy Fulfillment Flow (R2)
This flow is used for files that were server-compressed or when you need to provide a custom file (like a edited version or a specific format).

### **How it works:**
1.  **Upload**: You upload a clip using the standard Admin tool (which creates teasers and compressed previews).
2.  **Fulfillment**: These items appear as **`Pending`**.
3.  **Action**:
    *   **Sync**: If a master was already uploaded to the "Global" bucket, click **"Sync from Global"** to copy it into the unique Order Folder.
    *   **Manual**: If no master exists, click **"Fulfill"** to upload a file directly from your computer to the Order Folder.
4.  **Completion**: Once the file shows **`Ready (R2)`**, click **"Complete & Send Email"**.
5.  **Delivery**: The system generates a presigned download link for the specific file in your Cloudflare R2 bucket.

### **Benefits:**
- **Full Control**: You can swap or update the specific file for a single customer without affecting the global clip.
- **Customization**: Great for "personalized" edits or standard MP4 delivery.

---

## 📊 Summary Comparison

| Metric | **Direct Upload (Mux)** | **Legacy Upload (R2)** |
| :--- | :--- | :--- |
| **Effort Level** | ⚡ **Low** (Automated) | 🛠️ **Medium** (Manual Step) |
| **Primary Storage** | Mux Dashboard | Cloudflare R2 |
| **Quality Control** | Fixed to the Master in Mux | Flexible (Upload any file) |
| **Visibility** | Dashboard & Player Sync | Admin-managed Folders |
| **Ideal For** | High-volume master delivery | Custom edits or legacy content |

---

## 🧹 System Maintenance: Mux Cleanup
Because the Direct Upload flow can generate "Errored" assets in Mux (e.g., if a browser tab is closed mid-upload), a cleanup tool is provided.

- **Location**: **Admin Settings > System Maintenance**
- **Function**: Scans your entire Mux account and permanently deletes all assets with an "Errored" status.
- **UX**: Provides a progress spinner and a batch-completion count.

---

## 🛠️ Technical Implementation Notes
- **FulfillmentMuxAssetId**: A property on the `Purchase` entity that, if present, overrides the R2 storage logic in `Delivery.razor`.
- **IsDirectUpload**: A flag on the `Clip` entity that triggers the "Auto-Fulfill" UI logic.
- **Dynamic Downloads**: Mux download links are generated with `master_access: temporary` to ensure security while minimizing storage overhead.
