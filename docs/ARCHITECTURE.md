# Project65 Studios - Application Architecture

This document provides a comprehensive overview of the Project65 application, its components, tech stack, and service integrations.

## 🏗 System Overview

Project65 is a high-performance video storefront built on **ASP.NET Core Blazor Server**. It follows a **Clean Architecture** pattern, separating concerns into three primary layers:

1.  **Project65.Web**: The presentation layer (Blazor Components, Controllers, SignalR Hubs).
2.  **Project65.Infrastructure**: The implementation layer (External APIs, Database, Repository Implementations).
3.  **Project65.Core**: The domain layer (Entities, Interfaces, Shared DTOs).

---

## 🛠 Tech Stack

- **Framework**: .NET 10 (ASP.NET Core)
- **Frontend**: Blazor Server (InteractiveServer mode)
- **Database**: SQLite (EF Core)
- **Authentication**: ASP.NET Core Identity (with Google/Facebook support)
- **Real-time**: SignalR (for video processing status updates)
- **Styling**: Vanilla CSS (Custom Variable-based system)

---

## 💾 Core Domain Layer (`Project65.Core`)

### Key Entities
- **Event**: Represents a physical event (e.g., a car rally) containing multiple clips.
- **Clip**: A specific video file with metadata, Mux playback IDs, and AI-generated tags.
- **Purchase**: Records of user transactions, including Stripe session IDs and license types.
- **ExternalProduct**: Featured products linked to events (e.g., merch from Shopify/Printful).
- **Setting**: Key-value store for application configuration (Watermarks, API toggles).

---

## 🔌 Infrastructure & Services (`Project65.Infrastructure`)

### External Service Integrations
- **Mux Video (`IVideoService`)**:
    - Handles video hosting and streaming.
    - Generates signed JWT tokens for secure playback, thumbnails, and storyboards.
    - Manages direct uploads via authenticated URLs.
- **Cloudflare R2 (`IStorageService`)**:
    - S3-compatible storage for master video files and high-res thumbnails.
    - Configured with strict CORS to prevent unauthorized hotlinking.
- **Stripe (`IPaymentService`)**:
    - Manages Checkout sessions and Webhook processing.
- **Postmark/SendGrid (`IEmailService`)**:
    - Transactional email delivery for receipts and account notifications.

### Data Access
- **Repository Pattern**: Heavily utilized to abstract EF Core logic.
- **Optimizations**: Uses `AsSplitQuery()` for complex Eager Loading and `AsNoTracking()` for performance-critical read paths.

---

## 🌐 Web Layer (`Project65.Web`)

### Key Components
- **Home.razor**: High-performance landing page with parallelized media token fetching.
- **EventDetails.razor**: Multi-collection view optimized with query splitting.
- **ClipCard.razor**: Shared component featuring hover-previews and deferred token loading.
- **CartService**: Manages the user's shopping experience across navigations.
- **VideoHealingService**: A background background scavenger that detects and fixes broken Mux assets/tokens automatically.

---

## 🛡 Security & Performance

### Security Posture
- **Content Security Policy (CSP)**: Strict policy allowing only trusted domains (Mux, Stripe, Cloudflare).
- **Subresource Integrity (SRI)**: All CDN assets verified via SHA384 hashes.
- **Rate Limiting**: Protection against brute-force on login and resource exhaustion on upload.
- **Anti-CSRF**: Automated token management for both Razor Pages and API controllers.

### Performance Measures
- **Response Compression**: Brotli/Gzip enabled for all payloads.
- **Zero-Blocking Rendering**: Pre-cached media URLs and deferred script loading to ensure an instant-feeling UI.
- **SignalR Optimization**: Configured to handle binary traffic efficiently for high-interactivity states.

---

## 📁 Project Structure

```text
/Project65
├── Project65.Core          # Entities & Interfaces
├── Project65.Infrastructure    # DB & External Service Impls
└── Project65.Web           # Blazor UI, APIs, & Webhooks
    ├── Components          # Shared UI & Layouts
    ├── Pages               # Main Application Routes
    ├── Services            # Web-specific background logic
    └── wwwroot             # Static assets (JS/CSS/Images)
```
