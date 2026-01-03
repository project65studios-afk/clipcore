# ClipCore Development Roadmap: "The Incremental Path"

This document outlines how we will build and test ClipCore capability-by-capability, ensuring we don't break the existing experience while enabling the new multi-tenant features.

## 1. The Local Testing Setup (How to "See" It) ✅ Completed
To test multi-tenancy locally, you cannot just use `localhost:5095`. You need to simulate real domains.

### Step A: Configure Local Subdomains ✅ Completed
We will edit your hosts file to map subdomains to your local machine.
**Command**: `sudo nano /etc/hosts`
**Add this line**:
```text
127.0.0.1   clipcore.local
127.0.0.1   project65.clipcore.local
127.0.0.1   racing.clipcore.local
```
*Now, when you visit `http://project65.clipcore.local:5095`, the browser goes to your app, but sends the Host header `project65.clipcore.local`.*

---

## 2. The Seeding Strategy ("Project65 First") ✅ Completed
We will use your original brand, **Project65 Studios**, as the "First Tenant". This proves migration works.

### Update `DataSeeder.cs` ✅ Completed
Instead of a generic "Demo Store", we will seed two distinct tenants to verify isolation.

| Tenant | Subdomain | Purpose | Data |
| :--- | :--- | :--- | :--- |
| **Project65 Studios** | `project65` | **Legacy/Production Test** | Your existing 5-10 clips. This ensures the "Old App" still looks perfect. |
| **Speed Racing** | `racing` | **New Tenant Test** | A completely new store. Empty initially, then we add 1 clip. |

---

## 3. Incremental Development Steps ✅ Completed

### Step 1: UI Awareness (The Header Test)
**Goal**: Identify which store we are in visually.
*   **Action**: Update `MainLayout.razor` to inject `TenantContext`.
*   **Test**: 
    1. Visit `project65.clipcore.local` -> Header says **"Project65 Studios"**.
    2. Visit `racing.clipcore.local` -> Header says **"Speed Racing"**.
*   *If this works, the plumbing is solid.*

### Step 2: Product Isolation (The Data Test) ✅ Completed
**Goal**: Verify Store A doesn't see Store B's products.
*   **Action**: 
    1. Seed "Urban Neon Loop" clip to **Project65**.
    2. Seed "Race Car Drift" clip to **Speed Racing**.
*   **Test**:
    1. Browse `project65...` -> See only "Urban Neon".
    2. Browse `racing...` -> See only "Race Car".
*   *If this works, Global Query Filters are solid.*

### Step 3: The "Shop" Flow (The Money Test) ✅ Completed
**Goal**: Verify the Cart and Checkout work per-tenant.
*   **Action**: Attempt to buy a clip on **Speed Racing**.
*   **Test**: 
    1. Add to Cart.
    2. Verify `Order` is saved with `TenantId = [RacingGuid]`.
*   *This ensures money goes to the right place.*

### Step 4: Branding (The "Vibe" Test) ✅ Completed
**Goal**: Different colors for different stores.
*   **Action**: Update `Project65` tenant with `Theme: "Dark/Blue"` and `Racing` with `Theme: "Red"`.
*   **Test**: Switch tabs. The entire app feel should change instantly.

---

---

## 4. Phase 4: Scaling to a Platform ("The PhotoReflect Path")

Once the "Shop Flow" is verified, we move from a multi-store engine to a scalable platform.

### Step 5: The Ingestion Engine (Bulk Uploads)
**Goal**: Help sellers manage 1,000+ clips per weekend.
*   **Bulk Ingestion**: Dedicated parallel uploader to R2 and Mux.
*   **AI Auto-Tagging**: Using AI Vision to automatically read car/bib numbers from clips for instant categorization.

### Step 6: Marketplace & Discovery
**Goal**: Create a central hub for all events.
*   **Global Search**: A landing page at `clipcore.com` to search for events/tags across all tenants.
*   **Universal Accounts**: A customer portal to view all purchased videos from different sellers in one library.

### Step 7: Revenue Maximization (Bundling)
**Goal**: Increase Average Order Value (AOV).
*   **"Buy All" Packages**: One-click purchase for all clips in an event or all clips matching a specific tag (e.g., "All my Track Day clips" for $49).
*   **Dynamic Discounts**: Merchant-defined bundles (e.g., 3 clips for 20% off).

### Step 8: Self-Service Scale
**Goal**: Grow to 1,000+ sellers without manual intervention.
*   **Merchant Sign-up**: Public registration flow that auto-provisions subdomains.
*   **Onboarding Checklist**: Guiding sellers through Stripe Connect and Store Customization.

## 5. Enterprise Readiness & Security ("The Platform Trust Path")

As the platform grows, we move from "Functional" to "Enterprise Grade." We have already laid the foundation for this with the `TenantAuthorizationService` bypass.

### Step 9: Advanced Security & Auditing
**Goal**: Ensure platform-wide stability and prevent administrative abuse.
*   **Audit Logging (High Priority)**: We already have an `AuditService`. To be Enterprise-grade, every time a Super Admin touches a store that isn't their own, we must log: `"Super Admin [X] modified Product [Y] on Store [Z] at [Time]."`
*   **MFA for Super Admins**: Never allow a Super Admin to log in without a physical security key (like a Yubikey) or an Authenticator app.
*   **The "System" Dashboard**: A dedicated portal (e.g., `system.clipcore.test`) where platform owners can see a list of all tenants, their total sales, and their Stripe connection status in one view.

### Why do this?
*   **Trust**: Sellers need to know that while the platform owner *can* assist them, every action is tracked and accountable.
*   **Risk Mitigation**: A single compromised "Super Admin" credential is a catastrophic risk. MFA and a dedicated, isolated System Dashboard minimize this blast radius.
*   **Operational Efficiency**: At 1,000+ stores, you cannot look up tenant details in the database manually. You need a centralized cockpit.
