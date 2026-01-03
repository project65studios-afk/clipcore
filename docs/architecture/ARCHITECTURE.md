# ClipCore Architecture

## Multi-Tenancy Strategy
ClipCore uses a **Logical Isolation** model (Single Database, Shared Schema) to handle multiple storefronts efficiently.

### Core Concepts
1.  **Single Shared Database**: All tenants (Project65, Speed Racing, etc.) share the same database tables.
2.  **Row-Level Security**: Every entity (`Event`, `Clip`, `Purchase`) has a `TenantId` column.
3.  **Automatic Filtering**: The application automatically filters all database queries to strictly show data for the current tenant.

### Key Components

#### 1. Tenant Resolution (`TenantResolutionMiddleware`)
*   **Role**: Identifies *who* is making the request.
*   **Mechanism**: Inspects the `Host` header (e.g., `racing.clipcore.test`).
*   **Logic**:
    *   Extracts subdomain (`racing`).
    *   Looks up `Tenant` in database.
    *   Sets `TenantContext.CurrentTenant`.
    *   *Optimization*: Skips static assets (`.js`, `.css`, etc.) to improve performance.

#### 2. Tenant Context (`ITenantProvider`)
*   **Role**: A Scoped Service that holds the verification "Badge" for the current request.
*   **Lifetime**: Exists only for the duration of a single HTTP request.

#### 3. Data Isolation (EF Core Global Filters)
*   **Role**: The Enforcer.
*   **Implementation**: In `AppDbContext`, a global rule is applied to all entities:
    ```csharp
    modelBuilder.Entity<Event>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
    ```
*   **Result**: It is impossible to accidentally query another tenant's data. Even `db.Events.ToListAsync()` only returns the current tenant's events.

### Database Specifics (SQLite)
*   **GUID Handling**: SQLite stores GUIDs as 16-byte Blobs by default. To ensure compatibility with our text-based queries, we force a conversion to `TEXT` strings in `AppDbContext` when running in SQLite mode.

## Development Environment
*   **Local Domains**: We use `.test` domains (e.g., `project65.clipcore.test`) to avoid MacOS Bonjour (`.local`) conflicts.
*   **Port**: Applications run on port `5095` to avoid conflicts with other local services.
