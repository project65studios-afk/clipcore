# 🗺️ Project65 Roadmap & Improvements

This document outlines the strategic technical roadmap for Project65. It prioritizes stability, user experience, and operational excellence for the post-MVP phase.

## 🔴 High Priority (Resilience & Reliability)

These items address potential failure points in a production environment.

### 1. HTTP Resilience (Polly) - ✅ Completed
- **Problem**: Transient network errors (e.g., Stripe API timeout) currently cause immediate user-facing failures.
- **Solution**: Implement `Microsoft.Extensions.Http.Polly` policies.
- **Details**: Added "Wait and Retry" (Exponential Backoff: 2s, 4s, 8s) to `MuxVideoService` and `StripePaymentService` using `Polly.ResiliencePipeline`.

### 2. Distributed Caching (Redis)
- **Problem**: `IMemoryCache` is local to the server. If we scale to 2+ instances, cache data (like Mux tokens) is not shared.
- **Solution**: Migrate to **Amazon ElastiCache (Redis)**.
- **Details**: Switch `AddMemoryCache` to `AddStackExchangeRedisCache`. Essential for the SignalR Backplane.

### 3. Database Migration Safety
- **Problem**: Currently, `context.Database.MigrateAsync()` runs on app startup. In a multi-instance cloud environment, this can cause race conditions or locking issues during deployment.
- **Solution**: Decouple migrations from startup.
- **Details**: Move database migrations to a separate "Release Phase" job in the GitHub Actions CI/CD pipeline (using a `dotnet ef database update` bundle).

### 4. Robustness & Error Monitoring (Sentry)
- **Problem**: Background errors (especially Mux/R2 uploads) might fail silently or only appear in console logs.
- **Solution**: Integrate **Sentry** (or similar APM).
- **Details**: Capture unhandled exceptions and background task failures to catch issues before users report them.

### 5. Upload Validation (Match on Fulfillment)
- **Problem**: Risk of admin uploading the wrong file for a specific clip fulfillment.
- **Solution**: Implement filename/metadata matching.
- **Details**: Block upload if the filename does not match the expected pattern or Clip ID.

### 6. [x] **Refine Watch Limits (Tiered)**  ✅ Completed
   - **Goal**: Prevent scraping without hurting UX.
   - **Status**: Done (200 Anon / 400 Auth / Unlimited Admin + Free Previews).r IP to prevent scraping/abuse.
- **Solution**: Enhance `MuxVideoService` logic.
- **Details**: Track total seconds watched (or tokens issued) per IP and enforce a strict daily cap.

---

## 🟡 Medium Priority (UX & Features)

Enhancements to increase user engagement and perceived performance.

### 7. Skeleton Loading Screens - ✅ Completed
- **Problem**: Users see simple text "Loading..." or empty space while waiting for data.
- **Solution**: Implement "Skeleton" UI components (pulsing gray placeholders).
- **Details**: Create a `<SkeletonCard />` component to mimic the layout of `ClipCard` during `OnInitializedAsync`.

### 8. Netflix-Style Hover Preview
- **Problem**: Current hover is static or simple.
- **Solution**: Implement "Expand and Preview".
- **Details**: On hover, the clip card should expand slightly, play a muted preview, and show quick stats/actions, similar to Netflix/YouTube.

### 9. Upload Progress Bar Fixes ✅ Completed
- **Problem**: Progress bars in the Admin Upload event don't accurately reflect individual clip progress vs. overall bulk upload progress.
- **Solution**: Refactor `Uppy` or upload logic.
- **Details**: Ensure granular progress events are effectively bubbled up and visualized for each file in the queue.
- also after event upload, thumbnails do not instantly show up and hover previews do not work. i have to refresh the page to see the changes.

### 10. Advanced Search (Fuzzy Matching) ✅ Completed
- **Problem**: Search requires exact substrings. A typo like "weedding" returns zero results for "wedding".
- **Solution**: Implement Fuzzy Search.
- **Details**: Use PostgreSQL's `pg_trgm` (trigram) extension or integrate a lightweight search engine like Algolia/Typesense.

### 11. User Dashboard & Invoices ✅ Completed
- **Problem**: "My Purchases" is a simple list. Users cannot retrieve receipts.
- **Solution**: Expand the User Dashboard.
- **Details**: Add PDF Invoice generation (using `QuestPDF`) and a detailed order history view.

### 12. AI Response Caching
- **Problem**: `OpenAIVisionService` calls are expensive and slow.
- **Solution**: Cache AI analysis results aggressively.
- **Details**: Store the AI summary/tags in the database immediately and implement a "Regenerate" button for Admins.

---

## 🟢 Long Term (Ops & Architecture)

Investments in maintainability, security, and observability.

### 13. WAF & Bot Protection
- **Problem**: Public sites are targets for automated scanning and DoS.
- **Solution**: Deploy behind **Cloudflare WAF**.
- **Details**: configure rules to block automated bot traffic and common exploit patterns at the edge.

### 14. Monitoring & Alerting
- **Problem**: We rely on user reports for issues.
- **Solution**: Real-time alerting.
- **Details**: Implement alerts for repeated rate-limiting triggers, failed login spikes, or payment failures.

### 15. Security Scanning
- **Problem**: Dependencies age and develop vulnerabilities.
- **Solution**: Automated Security Scanning.
- **Details**: Integrate tools like OWASP ZAP or Snyk into the CI/CD pipeline.

### 16. Database Encryption (At-Rest)
- **Problem**: Regulatory requirements for data protection.
- **Solution**: Enable Transparent Data Encryption (TDE) in RDS.
- **Details**: If self-hosting SQLite in the future, use SQLCipher.

### 17. Structured Logging (Serilog)
- **Problem**: Default logging is unstructured text.
- **Solution**: Replace default logger with **Serilog**.
- **Details**: Log JSON objects for queryable insights.

### 18. Health Checks Dashboard
- **Problem**: No visual way to check if services are healthy.
- **Solution**: Implement `AspNet.HealthChecks.UI`.
- **Details**: Expose a `/health` endpoint.

### 19. Architecture (CQRS/MediatR)
- **Problem**: Coupled service logic.
- **Solution**: Refactor to **MediatR** pattern.
- **Details**: Decouple UI from business logic using Command/Query handlers.

### 20. Feature Flags
- **Problem**: Deploying to toggle features.
- **Solution**: Integrate `Microsoft.FeatureManagement`.
- **Details**: Control features via configuration changes.
