# ClipCore Development Roadmap: "The Incremental Path"

This document outlines how we will build and test ClipCore capability-by-capability, ensuring we don't break the existing experience while enabling the new multi-tenant features.

## 1. The Local Testing Setup (How to "See" It)
To test multi-tenancy locally, you cannot just use `localhost:5095`. You need to simulate real domains.

### Step A: Configure Local Subdomains
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

## 2. The Seeding Strategy ("Project65 First")
We will use your original brand, **Project65 Studios**, as the "First Tenant". This proves migration works.

### Update `DataSeeder.cs`
Instead of a generic "Demo Store", we will seed two distinct tenants to verify isolation.

| Tenant | Subdomain | Purpose | Data |
| :--- | :--- | :--- | :--- |
| **Project65 Studios** | `project65` | **Legacy/Production Test** | Your existing 5-10 clips. This ensures the "Old App" still looks perfect. |
| **Speed Racing** | `racing` | **New Tenant Test** | A completely new store. Empty initially, then we add 1 clip. |

---

## 3. Incremental Development Steps

### Step 1: UI Awareness (The Header Test)
**Goal**: Identify which store we are in visually.
*   **Action**: Update `MainLayout.razor` to inject `TenantContext`.
*   **Test**: 
    1. Visit `project65.clipcore.local` -> Header says **"Project65 Studios"**.
    2. Visit `racing.clipcore.local` -> Header says **"Speed Racing"**.
*   *If this works, the plumbing is solid.*

### Step 2: Product Isolation (The Data Test)
**Goal**: Verify Store A doesn't see Store B's products.
*   **Action**: 
    1. Seed "Urban Neon Loop" clip to **Project65**.
    2. Seed "Race Car Drift" clip to **Speed Racing**.
*   **Test**:
    1. Browse `project65...` -> See only "Urban Neon".
    2. Browse `racing...` -> See only "Race Car".
*   *If this works, Global Query Filters are solid.*

### Step 3: The "Shop" Flow (The Money Test)
**Goal**: Verify the Cart and Checkout work per-tenant.
*   **Action**: Attempt to buy a clip on **Speed Racing**.
*   **Test**: 
    1. Add to Cart.
    2. Verify `Order` is saved with `TenantId = [RacingGuid]`.
*   *This ensures money goes to the right place.*

### Step 4: Branding (The "Vibe" Test)
**Goal**: Different colors for different stores.
*   **Action**: Update `Project65` tenant with `Theme: "Dark/Blue"` and `Racing` with `Theme: "Red"`.
*   **Test**: Switch tabs. The entire app feel should change instantly.

---

## 4. Summary of "Next Actions"
1.  **Update Seeder**: Replace "Demo" with "Project65" and "Racing".
2.  **Verify UI**: Change the App Header to show `Tenant.Name`.
3.  **Verify Data**: Check the Home Page listings.

This approach lets you test "Little by Little". You don't need to build the "Create Store" Admin UI yet. We act as the Admin by seeding the database directly.
