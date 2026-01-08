# Product Strategy: The Video Workflow Dilemma

**Created**: 2026-01-05
**Status**: Decision Required

## 1. The Core Product Dilemma: "Proofs vs. Masters"
We must decide between two fundamentally different e-commerce workflows. This decision drives the technical architecture.

### Option A: Manual Fulfillment ("The Photographer Model")
*   **Workflow**:
    1.  **Upload**: Admin uploads a fast, lower-quality "Proof" (or heavily watermarked version) to the event page.
    2.  **Sale**: Customer buys a clip.
    3.  **Fulfillment**: System notifies Admin. Admin manually finds the original 4K file on their hard drive and uploads it specifically for that order.
*   **Pros**:
    *   **IP Protection**: Master 4K files are never online until bought.
    *   **Upload Speed**: Uploading small proofs (e.g., 540p) is fast even on slow connections.
*   **Cons**:
    *   **High Labor**: Admin must "work" for every sale.
    *   **Not Scalable**: Cannot handle 100 sales/day easily.
    *   **Customer Friction**: Customer waits hours/days for their file.

### Option B: Auto-Fulfillment ("The Modern SaaS Model")
*   **Workflow**:
    1.  **Upload**: Admin uploads the **Original 4K Master** immediately to the event page.
    2.  **Display**: System (Mux) automatically creates a lower-quality streaming version for preview.
    3.  **Sale**: Customer buys. System instantly generates a secure download link for the Original 4K file.
*   **Pros**:
    *   **Zero Labor**: Upload once, sell forever.
    *   **Instant Gratification**: Customer downloads immediately.
    *   **Scalable**: Handles 10,000 sales as easily as 1.
*   **Cons**:
    *   **Bandwidth**: Admin must upload massive 4K files (30GB+) upfront.
    *   **Storage Cost**: Hosting 300GB of source files costs money (via Mux or R2) even if nobody buys them.

---

## 2. Technical Solution: Mass Ingestion Options
Regardless of the workflow chosen, we need a way to upload files. The choice depends on **User Internet Speed** vs. **Development Complexity**.

### Option 1: Current Architecture (Proxy)
*   User -> **Your Server** -> Mux/R2
*   **Verdict**: ⛔️ **Unsafe for Bulk**.
*   **Why**: Processing 50+ videos crashes standard web servers. Only works for single, small files.

### Option 2: Direct Upload (The "WeTransfer" Experience)
*   User -> **Mux/R2** (Directly)
*   **Verdict**: ✅ **Recommended for Fiber Users**.
*   **Pros**: Simple, Reliable, No Server Crash.
*   **Cons**: No compression. Pushes raw file size. Great for fiber (5 mins for 30GB), painful for ADSL (Hours).

### Option 3: Client-Side Compression (WASM)
*   User (Browser CPU compresses) -> **Mux/R2**
*   **Verdict**: ⚠️ **High Complexity / "Desktop" Power**.
*   **Pros**: Reduces file size by 90% *before* upload. Makes "Option B" (Auto-Fulfillment) viable even on slow connections.
*   **Cons**: Technologically complex ("Bleeding Edge"). Requires strict security headers that may break external images.

### Option 4: Desktop App
*   User App -> Mux/R2
*   **Verdict**: 🛑 **Not Recommended**.
*   **Why**: Maintaining Mac/Windows apps is a separate business in itself. High friction for new users.

---

## 3. The Grand Analysis
The central tension is **Bandwidth vs. Labor**.

| Scenario | Recommended Strategy |
| :--- | :--- |
| **I have fast Internet** (Fiber) | **Option B + Option 2**. (Auto-Fulfillment + Direct Upload). Fastest workflow, least work. |
| **I have slow Internet** | **Option A** (Manual Fulfillment). Upload small proofs fast. Only upload big files when paid. |
| **I have slow Int. but hate work** | **Option B + Option 3**. (WASM Compression). Spend CPU time to save Bandwidth time. |

### Recommendation
**Build "Option B + Option 2" (Auto-Fulfillment + Direct Upload).**
*   It is the standard behavior for modern platforms (YouTube, Dropbox, Wetransfer).
*   It scales best.
*   If users complain about upload speed, we can add **Option 3 (WASM)** later as an enhancement.
