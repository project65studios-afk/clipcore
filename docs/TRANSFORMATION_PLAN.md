# Transformation Plan: Project65 → ClipCore Platform

**Objective**: Convert the single-storefront "Project65" MVP into "ClipCore," a multi-tenant SaaS where *anyone* can create their own video storefront (like PhotoReflect, but for video).

## 1. Naming & Repository
Since you want a fresh start based on the current code:
*   **Repo Name**: `ClipCore` (Clean, professional).
*   **Alternative**: `ReflectOS` (if you want it to sound like an infrastructure product).

## 2. The Migration Strategy
We will not just "copy-paste". We will **lift and shift**.

### Phase 1: The Lift (Cloning)
1.  Create usage new, empty git repository (`ClipCore`).
2.  Copy the **entire** working `Project65` solution into it.
3.  Rename `Project65` namespaces to `ClipCore`.
4.  Verify the "Single Store" version runs perfectly in the new home.

### Phase 2: The Shift (Multi-Tenancy)
This is the "Hard Part". We need to teach the app that there is more than one store.

#### A. Database Changes
We must add a `TenantId` (or `StoreId`) to **every single table**:
- `Events` → `StoreId`
- `Clips` → `StoreId`
- `Orders` → `StoreId`
- `Settings` → `StoreId` (This replaces the "Global Settings" logic)

#### B. The "Tenant Context" Middleware
We need a piece of code that runs *before* every page load:
1.  **Check URL**: Is it `carlos.clipcore.com` or `alex.clipcore.com`?
2.  **Load Context**: "Okay, we are in Carlos's Store."
3.  **Filter Data**: Tell the Database "Only show me Events where StoreId = Carlos".

#### C. User Roles
*   **Platform Admin**: YOU. You see everything, manage billing for store owners.
*   **Store Owner**: The videographer. Can only see *their* events/settings.
*   **Customer**: The parent buying a clip.

## 3. Immediate "Dont Proceed Yet" Checklist
Before we pull the trigger:
- [ ] **Backup**: Ensure Project65 `v1.0-MVP` is tagged and safe.
- [ ] **Architecture Decision**: Do we use **Subdomains** (`store.app.com`) or **Paths** (`app.com/store`)? (Subdomains are more "Pro").
- [ ] **Tech Stack**: Are we keeping SQLite? (No, for SaaS we likely need PostgreSQL for better concurrency).

## 4. Why this approach?
It allows you to keep all the beautiful UI work (Glassmorphism, Video Players, Cart Flow) while changing the "engine" underneath to support millions of stores instead of just one.
