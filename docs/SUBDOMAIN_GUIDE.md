# Subdomain Architecture Guide

This guide explains how `carlos.clipcore.com` maps to `Carlos Store`.

## 1. The Request Flow (Production)
When a user visits `https://carlos.clipcore.com`:

1.  **DNS (Cloudflare)**:
    - You have a Wildcard `A` Record: `*.clipcore.com -> 104.21.x.x` (Your Server IP).
    - This tells the browser: "Anything ending in .clipcore.com goes to this server."

2.  **Web Server (Reverse Proxy/Kestrel)**:
    - Kestrel receives the request.
    - It reads the HTTP Header `Host: carlos.clipcore.com`.

3.  **Middleware Application Logic**:
    - We write custom Middleware: `TenantResolutionMiddleware`.
    - It splits the host: `carlos.clipcore.com`.
    - It extracts the first part: `carlos`.
    - It queries the database: `SELECT * FROM Tenants WHERE Subdomain = 'carlos'`.
    - If found, it sets the `CurrentTenant` in the HTTP Context.
    - If not found, it redirects to `https://www.clipcore.com/not-found`.

## 2. Local Development (The "Hosts" Trick)
Since you don't own `.com` on localhost, we fake it using your machine's **Hosts File** (`/etc/hosts`).

### Setup
We map these domains to `127.0.0.1` (Your machine):
```text
# /etc/hosts
127.0.0.1   clipcore.local
127.0.0.1   carlos.clipcore.local
127.0.0.1   alex.clipcore.local
```

### Flow
1. You browse to `http://carlos.clipcore.local:5000`.
2. Middleware sees `Host: carlos.clipcore.local`.
3. It extracts `carlos`.
4. It finds the Tenant in your local DB.

## 3. Custom Domains (Advanced)
If a seller wants `carlosracing.com`:
1. They add a `CNAME` record: `store.carlosracing.com -> domains.clipcore.com`.
2. Middleware sees `Host: store.carlosracing.com`.
3. It queries: `SELECT * FROM Tenants WHERE CustomDomain = 'store.carlosracing.com'`.
4. It loads the "Carlos" tenant.
