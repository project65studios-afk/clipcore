# Stripe Connect Integration Guide

ClipCore uses **Stripe Connect** (Standard Accounts) to enable a multi-tenant video storefront. This architecture allows the platform to take a commission while sellers receive their funds directly and securely.

## 1. Multi-Tenant Architecture

We use **Destination Charges** through a centralized OAuth callback hub.
- **Single Redirect URI**: All tenants use `https://{domain}/api/stripe/connect/callback`.
- **State-Based Routing**: The `state` parameter in the OAuth flow carries the `TenantId`. The controller uses this to identify the tenant and redirect the user back to their specific subdomain (e.g., `racing.clipcore.test/admin/settings`).
- **Wildcard Cookies**: Authentication persists across subdomains using `SameSite=Lax` and `.domain.test` cookie scoping.

## 2. Stripe Dashboard Setup

To enable this flow, configure your **Stripe Connect Settings**:
1.  **OAuth Settings**:
    *   Enable OAuth in the Connect settings.
    *   Add the **Redirect URI**: `http://localhost:5094/api/stripe/connect/callback` (and the production equivalent).
2.  **Branding**: Configure your platform name and icon; these will appear on the "Connect" screen.

## 3. Configuration (`appsettings.json`)

Add these keys to your `Stripe` section:

```json
"Stripe": {
    "SecretKey": "sk_test_...",
    "ClientId": "ca_...",
    "RedirectUri": "http://project65.clipcore.test:5094/api/stripe/connect/callback",
    "PlatformFeePercent": 15
},
"CookieDomain": ".clipcore.test"
```

*   **ClientId**: Your Connect "ca_..." ID from Settings.
*   **RedirectUri**: The CANONICAL callback URL (usually using your main project subdomain).
*   **PlatformFeePercent**: The % cut the platform takes (e.g., `15`).
*   **CookieDomain**: Must start with a dot (e.g., `.clipcore.test`) to allow login persistence across subdomains.

## 4. Onboarding Workflow

The system provides a professional, pre-filled onboarding experience:
1.  **Initiate**: Admin clicks "Connect with Stripe" in Settings.
2.  **Pre-fill**: The app sends the seller's business name, website URL, and email to Stripe to reduce friction.
3.  **Handoff**: After authorization, the user is routed through the centralized hub and back to their specific tenant dashboard.

## 5. Payment Splits (Economics)

When a customer buys a clip:
- **Customer Pays**: 100% of the price.
- **Platform Cut**: 15% (Configurable) goes to your Stripe account.
- **Seller Cut**: 85% goes to the seller's Connect account.
- **Transaction Fees**: The seller pays the standard Stripe processing fees (2.9% + 30¢) from their 85% cut.

## 6. Security

- **State Validation**: The OAuth `state` is validated to prevent CSRF.
- **SameSite=Lax**: Required to allow the browser to send the auth cookie during the cross-site redirect from Stripe.
