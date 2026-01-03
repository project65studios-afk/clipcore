# Platform Strategy & Business Plan

**Status**: Planning Phase (Do Not Proceed Code Yet)
**Goal**: Transform "Project65" into a Multi-Tenant SaaS Platform for Event Videographers.

---

## 1. The Opportunity
**"Video is harder than Photo."**
The market for event photography (PhotoReflect, SmugMug) is saturated, but automated event video is underserved.
*   **Target Niches**: Track Days, Dance/Cheer Competitions, Equestrian Events.
*   **The Problem**: Videographers have 100+ clips. Manually editing, rendering, and emailing them takes 10+ hours.
*   **Our Solution**: Automated "Healing", Mux processing, R2 delivery, and instant commerce.

## 2. Naming Ideas (Not "ClipCore")
We need a name that implies infrastructure, speed, or motion.
*   **ReflectOS**
*   **ClipFlow**
*   **MotionMarket**
*   **VantagePoint**
*   **AutoReel**

## 3. Monetization Models
How this business makes money.

### A. The Transaction Fee ("Stripe Model") - *Recommended for Launch*
*   **Fee**: 5-10% of every sale.
*   **Pros**: Low friction. Sellers only pay when they succeed.
*   **Cons**: Revenue depends entirely on seller volume.

### B. The SaaS Subscription ("Shopify Model")
*   **Cost**: $29-$79/month.
*   **Pros**: Predictable MRR (Monthly Recurring Revenue).
*   **Cons**: Harder to sell to new users.

### C. Cloud Usage Fee ("AWS Model")
*   **Cost**: Small markup on GB storage or Mux encoding minutes.
*   **Pros**: Protects you from "heavy" users costing you money.

## 4. Marketing Strategy (Budget: $100-$500)
**"The Referral Loop"**: Get 5 sellers, offering them 0% fees if they refer others.

1.  **Direct Snipe**: Contact photographers on SmugMug/PhotoReflect. "Sell your video clips too, automatically."
2.  **Niche Communities**: Facebook groups for Track Days/Equine. Post "Sales Dashboards" showing revenue, not tech specs.
3.  **In-Person**: Visit one major event, buy the photographer a coffee, show the QR code demo.

## 5. Trust & Safety: The "AI Safety Gate" 🛡️
**Critical Feature for Public Platforms**
Implementing OpenAI Vision to moderate content automatically.
1.  **Scan**: Every upload is analyzed by AI.
2.  **Prompt**: "Does this image contain nudity, violence, or illegal acts?"
3.  **Action**:
    *   **Safe**: Auto-publish.
    *   **Unsafe**: Flag as "Pending Review" (Hidden). Alert Admin.
4.  **Legal**: Strict Terms of Service acceptance on upload.

## 6. Technical Roadmap (The Pivot)
**Estimate**: 1.5 - 2.5 Weeks to MVP Platform.

### Milestone 1: The Shell (Multi-Tenancy)
*   Refactor DB: Add `StoreId` to all tables (Events, Clips, Orders).
*   Middleware: Route `store.app.com` to the correct `StoreId`.

### Milestone 2: Branding
*   Dynamic Settings: Move colors/logos from CSS to Database.
*   Seller Dashboard: Restricted view for store owners.

### Milestone 3: Money (Stripe Connect)
*   **Stripe Connect**: Split payments automatically (90% to seller, 10% to Platform).

### Milestone 4: Onboarding
*   Self-service signup flow.
*   Automated subdomains.
