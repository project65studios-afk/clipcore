# Mux Video Pricing Model & Cost Analysis

**Last Updated**: 2026-01-05
**Context**: Validating feasibility of "Auto-Fulfillment" strategy (Storing Masters on Mux).

## 1. How Mux Pricing Works
Mux charges based on three pillars. Crucially, they bill by the **Second**, without rounding up.

1.  **Input (Encoding)**: One-time cost when you upload.
    *   *Basic (720p)*: Free.
    *   *Plus (1080p)*: ~$0.03 / min.
    *   *Premium (4K)*: ~$0.10 / min.
2.  **Storage**: Recurring monthly cost to keep files online.
    *   *Basic*: $0.0024 / min ($0.14 / hour).
    *   *4K (Professional)*: $0.0096 / min ($0.57 / hour).
3.  **Delivery**: Cost when someone watches a video.

## 2. Project65 Cost Projection
**Scenario**: "The Busy Wedding Season"
*   **Events**: 50 per year.
*   **Volume**: 300 clips per event.
*   **Duration**: 20 seconds average per clip.

### The Math
1.  **Total Minutes per Event**:
    *   300 clips * 20 sec = 6,000 seconds = **100 minutes**.
2.  **Total Annual Volume**:
    *   50 events * 100 minutes = **5,000 minutes added per year**.

### Estimated Bills

#### A. One-Time Upload Costs (Encoding)
*   **Scenario 1 (Basic/Social)**: $0.
*   **Scenario 2 (Professional 1080p)**: 5,000 * $0.03 = **$150 / year**.
*   **Scenario 3 (Professional 4K)**: 5,000 * $0.10 = **$500 / year**.

#### B. Monthly Storage Costs (Recurring)
This accumulates over time as your library grows.

| Library Size | Basic Storage (720p) | Plus Storage (1080p) | Premium Storage (4K) |
| :--- | :--- | :--- | :--- |
| **End of Year 1** (5,000 mins) | **$12 / month** | $15 / month | $48 / month |
| **End of Year 2** (10,000 mins) | $24 / month | $30 / month | $96 / month |
| **End of Year 5** (25,000 mins) | $60 / month | $75 / month | $240 / month |

## 3. Conclusions
*   **Feasibility**: ✅ **YES, Sustainable**.
*   **Why**: Because clips are short (20s), the per-minute pricing works in your favor. Even storing 5 years of 4K footage (~15,000 clips) would only cost ~$240/month.
*   **Optimization**: Mux offers "Automatic Cold Storage" which reduces prices by 40-60% for videos not watched in 30 days. This means your actual bill will likely be **half** the estimates above.

## 4. Recommendation
Stick to **Mux Only** (Auto-Fulfillment). The operational simplicity (no R2 sync code, no engineering overhead) is worth the extremely reasonable storage cost.
