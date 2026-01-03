# Identity & Authentication Architecture

This document outlines how ClipCore manages users, sessions, and administrative permissions across a multi-tenant environment using a Centralized Identity model.

## 1. Centralized Identity Model
ClipCore uses a **platform-wide identity system**. This means that a user account is global and not tied to any specific store (subdomain) at the time of creation.

- **One Account, Many Stores**: A user can register at `racing.clipcore.test` and immediately use those same credentials to log in at `project65.clipcore.test`.
- **SSO (Single Sign-On)**: Because the authentication cookie is configured with `.clipcore.test` as the domain, logging in once authenticates the user across all subdomains.

## 2. Multi-Tenant Authorization
While identity is global, **permissions are local**. Just because a user is logged in doesn't mean they can see everything.

### Tenant Memberships
We use a `TenantMembership` entity to bridge users and tenants. This allows for a "Many-to-Many" relationship:
- One Storefront can have **multiple** Admins or Owners.
- One User can be an Admin for **multiple** different Storefronts.

| Column | Description |
| :--- | :--- |
| `UserId` | The Global ID of the user. |
| `TenantId` | The ID of the store they belong to. |
| `Role` | `Owner`, `Admin`, or `Member`. |

### The "TenantAdmin" Policy
All administrative pages are protected by a custom requirement. To access an admin page (e.g., `/admin/products`):
1. The user **must** be authenticated.
2. The user **must** have the global "Admin" role.
3. The user **must** have a record in `TenantMemberships` for the **current** tenant being visited.

### Authorization Handlers
Permissions are enforced using custom `AuthorizationHandler` implementations:
- `TenantAdminHandler`: Validates membership for any admin-level access.
- `TenantOwnerHandler`: Restricts access to sensitive pages (Team Management, Branding) to store owners only.

> [!TIP]
> **Super Admin Bypass**: The `admin@clipcore.com` account is hardcoded in `TenantAuthorizationService` to bypass membership checks, allowing platform operators to troubleshoot any store.

## 3. Login, Logout, and Registration

### Registration
- When a user registers on any subdomain, a new `ApplicationUser` is created in the global pool.
- **Isolation**: By default, new users have no memberships. They are simply platform customers.

### Login
- Login happens at the tenant level but creates a global session.
- After logging in, the user is redirected back to the specific store they were visiting.

### Logout
- **Global Sign-Out**: Logging out clears the `.clipcore.test` cookie, effectively logging the user out of every subdomain simultaneously.
- **Dedicated Endpoint**: Handled by a Minimal API at `/Account/Logout` in `Program.cs`. This endpoint explicitly disables antiforgery validation to prevent 400 Bad Request errors when switching between subdomains.

## 4. Administrative Workflow
To answer technical questions: **"Is there only one admin?"**
> [!NOTE]
> **No.** You can have as many admins as you want. By adding a row to `TenantMemberships`, you can grant "Admin" access to an assistant, a manager, or a business partner without sharing your own password.

### Admin vs. Owner
- **Owner**: Can manage branding, Stripe Connect settings, and add other members.
- **Admin**: Can upload clips, manage events, and handle fulfillment.
- **Member**: Standard customer with no dashboard access (used for future "privileged customer" features).

### Team Management (Owners Only)
Storefront owners can manage their management team via the **Admin Portal -> Team** link.
- **Granting Access**: Enter the email of any existing platform user to grant them `Admin` rights to your store.
- **Global Auth**: Adding a member automatically ensures the user has the system-level `Admin` role required to access secure areas.
- **Granular Control**: Revoke access at any time. Owners cannot be removed from their own store by other admins.

## 5. Seeded Account Examples (for Testing)

Use these accounts to verify the platform behavior across subdomains:

| Account Type | Email | Password | Access |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `admin@clipcore.com` | `Admin123!` | Platform-wide access (Super User) |
| **P65 Owner** | `owner@project65.com` | `Admin123!` | Full access to **Project65** Admin |
| **Racing Owner** | `owner@racing.com` | `Racing123!` | Full access to **Racing** Admin |
| **Racing Staff** | `staff@racing.com` | `Staff123!` | Staff access on **Racing** |
| **Global Customer** | `customer@clipcore.com` | `User123!` | Storefront browsing only |
