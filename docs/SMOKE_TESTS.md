# ClipCore - Quality Assurance & Smoke Test Plan

This document outlines the highest-priority "Smoke Tests" to ensure application stability and protect the "Money Path."

---

## 🎯 The Smoke Test Suite
A smoke test is a thin, fast layer of tests that validates your application's core pulse. If these fail, the site is effectively "dead" for users.

### 1. The "Discovery" Path (Visibility)
*   **Home Page Pulse**: Verify the landing page loads with at least one event card visible.
*   **Media Health**: Navigate to an Event Details page and verify the `<mux-player>` element is present and has a valid `playback-id`.
*   **Search Connectivity**: Perform a search for a known car/tag and verify results are returned without a 500 error.

### 2. The "Money Path" (Revenue Protection)
*   **Cart Persistence**: Add a clip to the cart on one page, navigate to another, and verify the cart count remains correct.
*   **Stripe Handshake**: Click "Checkout" in the cart and verify the browser is redirected to a `checkout.stripe.com` URL. *This is the most important test in the suite.*
*   **Receipt Accessibility**: Log in as a user with a purchase and verify the "My Clips" page lists their purchased assets.

### 3. Media & CORS Stability (High Fragility Area)
*   **R2 Thumbnail Integrity**: Verify that `<img>` tags for clip thumbnails have a valid signed R2 URL and successfully load an image (200 OK).
*   **Hover Preview (Event Details)**: Verify that hovering over a `ClipCard` on the Event page triggers the `<mux-player>` and successfully initializes playback without "Missing Token" errors.
*   **Deep-Link Preview (Clip Details)**: Verify that navigating directly to `/clips/{id}` loads the full-size preview player and starts playback.
*   **Console Cleanliness**: Automated "no-error" check for Event Details and Clip Details pages. Specifically watches for:
    *   CORS blockages (Access-Control-Allow-Origin).
    *   Google Cast extension errors (`chrome-extension://invalid`).
    *   Missing Mux token warnings.

### 4. The "Content Path" (Admin Ops)
*   **Upload Readiness**: Verify the Admin Upload page loads and the "Select Files" button is interactive.
*   **API Health**: Verify the `VideoService` can successfully reach the Mux API for a new token request.

---

## 🛠 Implementation Roadmap

### Phase 1: The Infrastructure (Foundation)
1.  **Add Test Projects**: Initialize `ClipCore.Tests` (xUnit) and `ClipCore.E2E` (Playwright) projects.
2.  **Mocking Layer**: Implement a "Mocked" `IVideoService` for CI environments so we don't spend real money/tokens during automated test runs.
3.  **Database Isolation**: Configure a secondary `project65_test.db` with a clean seeder that creates a predictable test environment.

### Phase 2: Playwright E2E Scripts (The Browser)
1.  **Scenario: The Guest Buyer**: Automated script that lands on home, navigates, adds a clip to cart, and hits the Stripe redirect.
2.  **Scenario: The Returning Customer**: Automated script that logs in, navigates to "My Clips," and verifies playback IDs are correctly signed.

### Phase 3: bUnit Component Testing (The UI)
1.  **ClipCard Validation**: Verify that the `ClipCard` correctly toggles between "Purchased," "In Cart," and "Quick Add" states based on the user's data.
2.  **Header Sync**: Verify the Shopping Cart icon updates immediately when `CartService` signals a change.

---

## 🚦 Execution Strategy
-   **Local**: Run `dotnet test` before every `git push`.
-   **Cloud (Future)**: Integrate with GitHub Actions to block merging any pull request that hasn't passed the Smoke Suite.

> [!IMPORTANT]
> This plan focuses on **High Impact** and **Low Maintenance**. We only test the things that, if broken, would cause loss of revenue or data.
