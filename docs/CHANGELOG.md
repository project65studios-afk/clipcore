# ClipCore Changelog

All notable changes to this project will be documented in this file.

## [2026-01-01] - Performance & Security Stabilization

### Added
- **Response Compression**: Enabled Brotli and Gzip compression for all HTTP responses (HTML, JS, CSS, and SignalR binary traffic) to reduce payload sizes and improve load times.
- **Advanced Rate Limiting**: Implemented a global rate limiter with strict policies for `/Identity` (login/register) and administrative endpoints (video upload/compression).
- **Subresource Integrity (SRI)**: Added SHA384 hashes and `crossorigin="anonymous"` to all external CDN resources (Mux, Uppy, Stripe, Chart.js).
- **Anti-CSRF Protection**: Automated XSRF token generation and validation for administrative API calls.
- **Google Cast Support**: Updated CSP to allow `chrome-extension:` and `gstatic.com` for full Google Cast functionality.

### Changed
- **Parallel Data Fetching**:
    - `Home.razor`: Parallelized Mux token fetching for event thumbnails using `Task.WhenAll`.
    - `ClipCard.razor`: Refactored to fetch video, thumbnail, and storyboard tokens concurrently.
    - `EventDetails.razor`: Parallelized initialization of event data and watermark settings.
- **EF Core Optimization**:
    - Added `AsSplitQuery()` to `EventRepository` to prevent Cartesian explosion when loading large collections.
    - Added `AsNoTracking()` to read-only paths in `ClipRepository`.
- **Frontend Optimization**: 
    - Moved non-critical scripts to the end of the `<body>` in `App.razor`.
    - Added `defer` to non-blocking head scripts.
    - Implemented thumbnail URL pre-caching to remove synchronous I/O from the render loop.
- **Security Hardening**:
    - Hardened Identity cookies with `SameSite=Strict`, `HttpOnly`, and `Secure` flags.
    - Removed `'unsafe-eval'` from Content Security Policy.
    - Tightened R2 Storage CORS configuration to specific application origins.

### Fixed
- **Mux Token Lifecycle**: Resolved "Missing expected thumbnail token" errors by ensuring all signed tokens are ready before initializing `mux-player`.
- **Scoped Service Scope**: Fixed issues where `Task.Run` was breaking DI scope in Blazor Server components by migrating to safe async patterns.
- **R2 CORS Blockage**: Fixed "No Access-Control-Allow-Origin header" errors by standardizing R2 CORS and exposing correct headers for media streaming.
- **App Startup**: Ensured `appsettings.json` is a valid JSON object to prevent Kestrel startup failures.
