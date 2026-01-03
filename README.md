# ClipCore Studios

ClipCore Studios is a premium, high-performance video storefront built with Blazor Server. It allows creators to sell high-quality video clips from events with integrated AI analysis, seamless payments, and secure streaming.

## 🚀 Quick Start

1.  **Configure environment**: Update `appsettings.json` with your Mux, Stripe, and R2 credentials.
2.  **Restore & Build**:
    ```bash
    dotnet restore
    dotnet build
    ```
3.  **Run**:
    ```bash
    dotnet run --project ClipCore.Web
    ```

## 📖 Documentation

- **[Architecture Overview](docs/ARCHITECTURE.md)**: Deep dive into the system design, tech stack, and service integrations.
- **[Stripe Connect Integration](docs/STRIPE_CONNECT.md)**: setup, configuration, and multi-tenant OAuth flow.
- **[Audit Report](docs/AUDIT_REPORT.md)**: 360-degree technical health check (Security, Speed, Performance, UX).
- **[AWS Deployment Guide](docs/DEPLOYMENT_AWS.md)**: Production-ready roadmap and CI/CD setup for AWS.
- **[QA & Testing Guide](docs/TESTING.md)**: Procedures for automated (Unit/E2E) and manual verification.
- **[Smoke Test Requirements](docs/SMOKE_TESTS.md)**: Roadmap and requirements for core user flow protection.
- **[Changelog](docs/CHANGELOG.md)**: Track record of recent security and performance optimizations.

## 🛠 Key Features

- **Video Streaming**: Powered by Mux with signed URL protection.
- **Secure Storage**: Cloudflare R2 for master file archival.
- **Payments**: Full Stripe Checkout integration.
- **Performance**: Sub-second page loads via response compression and parallel data fetching.
- **Security**: Hardened CSP, SRI hashes, and advanced rate limiting.

## 🏗 Project Structure

- `ClipCore.Web`: Blazor Server UI & SignalR Hubs.
- `ClipCore.Infrastructure`: Data access and external API implementations.
- `ClipCore.Core`: Domain entities and interfaces.

---
© 2026 ClipCore Studios. All rights reserved.
