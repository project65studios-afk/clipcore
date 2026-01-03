# Seller Onboarding & Subscription Strategies

This document outlines the proposed workflows for integrating **seller subscriptions ($9/mo + 30-day trial)** with **Stripe Connect onboarding**.

## Option 1: The "PhotoReflect" (Integrated)
*   **Workflow**: Connect Stripe -> Immediate Redirect to Subscription Checkout -> Admin Dashboard.
*   **Pros**: Filters for serious sellers only; Zero cost for dormant accounts.
*   **Cons**: Higher initial friction; potential dropout during second payment screen.

## Option 2: The "Shopify/Professional" (Checklist-Based)
*   **Workflow**: Access Admin -> Clear "Next Steps" Checklist -> Seller connects Stripe and starts trial independently.
*   **Pros**: Lower friction; Sellers build trust with the platform before paying.
*   **Cons**: Platform incurs hosting/storage costs during the trial period for non-converting users.

## Option 3: The "Soft Gate" (Post-Launch Billing)
*   **Workflow**: Connect Stripe -> Store goes live immediately -> Billing required within 30 days to stay online.
*   **Pros**: Lowest friction; fastest time-to-market for sellers.
*   **Cons**: Hardest to enforce; complex "grace period" logic required.

---

### Current Status: **Postponed**
- **Decision**: Platform Fee is set to **15%** (Transaction-based).
- **Subscription**: The $9/mo fee is currently on hold to minimize onboarding friction for initial tenants.
