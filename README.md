# Project65 Studios

Project65 Studios is a premium, high-performance video storefront built with Blazor Server. It allows creators to sell high-quality video clips from events with integrated AI analysis, seamless payments, and secure streaming.

## 🚀 Quick Start

1.  **Configure environment**: Update `appsettings.json` with your Mux, Stripe, and R2 credentials.
2.  **Restore & Build**:
    ```bash
    dotnet restore
    dotnet build
    ```
3.  **Run**:
    ```bash
    dotnet run --project Project65.Web
    ```

## 📖 Documentation

- **[Architecture Overview](ARCHITECTURE.md)**: Deep dive into the system design, tech stack, and service integrations.
- **[Audit Report](AUDIT_REPORT.md)**: 360-degree technical health check (Security, Speed, Performance, UX).
- **[Changelog](CHANGELOG.md)**: History of recent security and performance optimizations.
- **[Security Audit](brain/dc0acd33-2c27-4d48-a94c-2aed2efc59b3/security_audit.md)**: Detailed security posture and implemented fixes.
- **[Performance Audit](brain/dc0acd33-2c27-4d48-a94c-2aed2efc59b3/performance_audit.md)**: Optimization results and infrastructure tuning.

## 🛠 Key Features

- **Video Streaming**: Powered by Mux with signed URL protection.
- **Secure Storage**: Cloudflare R2 for master file archival.
- **Payments**: Full Stripe Checkout integration.
- **Performance**: Sub-second page loads via response compression and parallel data fetching.
- **Security**: Hardened CSP, SRI hashes, and advanced rate limiting.

## 🏗 Project Structure

- `Project65.Web`: Blazor Server UI & SignalR Hubs.
- `Project65.Infrastructure`: Data access and external API implementations.
- `Project65.Core`: Domain entities and interfaces.

---
© 2026 Project65 Studios. All rights reserved.
