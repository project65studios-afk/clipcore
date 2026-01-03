# Testing & Environment Guide

This document explains how to switch between Development and Testing modes, and how to verify the **Multi-Tenant** architecture of ClipCore.

## 🏁 Environment Switching

ClipCore uses the standard `ASPNETCORE_ENVIRONMENT` variable to toggle between live integrations and mock/test data.

### 1. Development Mode (Default)
Used for your active work with real data and real API keys.
- **Command**: `dotnet run --project ClipCore.Web/ClipCore.Web.csproj`
- **Config**: Reads `appsettings.Development.json`
- **Database**: `project65.db`
- **Services**: Uses real `MuxVideoService` and `StripePaymentService`.
- **Tenants**:
  - `http://project65.clipcore.test:5094`
  - `http://racing.clipcore.test:5094`

### 2. Testing Mode
Used for automated E2E tests.
- **Config**: Reads `appsettings.Testing.json`
- **Database**: `project65_test.db` (Clean isolation)
- **Services**: Uses `FakeVideoService` (signed IDs start with `fake_`) and `FakePaymentService`.

---

## 🧪 Automated Testing Strategy

We use **Playwright** to verify the critical "Money Path" and Tenant Isolation.

### Running the Suite
```bash
dotnet test tests/ClipCore.E2ETests/ClipCore.E2ETests.csproj
```

### Test Categories

#### 1. Tenant Isolation (`TenantIsolationTests.cs`)
Verifies that the multi-tenant architecture is secure.
- **Data Isolation**: Ensures events from the "Racing" tenant do not appear on the "Project65" tenant.
- **Admin Security**: Verifies that an admin of Tenant A receives "Access Denied" when trying to access Tenant B's admin portal.
- **Legacy/SSO**: *Currently skipped locally* due to browser security restrictions on `.test` domains without HTTPS/DNS. Can be verified manually by modifying `Program.cs` or deploying to staging.

#### 2. Smoke Tests (`SmokeTests.cs`)
Verifies the core e-commerce flow on the `project65` subdomain.
- **Discovery**: Home page loads with correct branding ("PROJECT65 STUDIOS").
- **Revenue**: Cart persistence, Promo codes, and Volume Discounts.
- **Integrity**: Validates R2/Mux thumbnail URLs.

---

## 🛠 Troubleshooting

- **"Access Denied" on Admin**: Ensure you are logged in with the correct tenant owner email (e.g., `owner@project65.com` vs `owner@racing.com`).
- **Empty Page Title**: Ensure the `<PageTitle>` component is present in the layout or page.
- **Cookie/SSO Failures**: Cross-subdomain cookies require a valid top-level domain. Localhost subdomains often fail in browsers. We use `.clipcore.test` in `/etc/hosts` for simulation, but it has limits.