# ClipCore.API — Next Steps

Complete these steps in order before going live.

---

## 1. EF Identity Migration

Run in your dev environment (not this server — no .NET SDK here):

```bash
cd /path/to/ClipCore

dotnet ef migrations add InitIdentity \
  --context AppIdentityDbContext \
  --project ClipCore.API \
  --startup-project ClipCore.API

dotnet ef database update \
  --context AppIdentityDbContext \
  --startup-project ClipCore.API
```

This creates the `AspNetUsers`, `AspNetRoles`, etc. tables.
All business tables (`Clips`, `Collections`, `Purchases`, ...) already exist from the prior Blazor migration — do not recreate them.

---

## 2. Deploy PostgreSQL Functions

Run against your Neon database:

```bash
psql "$DATABASE_URL" -f ClipCore.API/sql/functions/cc_functions.sql
```

Then verify:

```sql
SELECT routine_name
FROM information_schema.routines
WHERE routine_schema = 'public'
ORDER BY routine_name;
```

You should see ~30 `cc_` functions and procedures.

Also apply the unique constraint required by `cc_u_usage_increment` (skip if it already exists):

```sql
ALTER TABLE "DailyWatchUsages" ADD UNIQUE ("IpAddress", "Date");
```

---

## 3. SSM Parameters

Add these new parameters to AWS SSM (`/clipcore/*`):

| Parameter | Value |
|---|---|
| `/clipcore/Jwt__Secret` | A random 256-bit string (e.g. `openssl rand -base64 32`) |

All other parameters (`/clipcore/Mux__*`, `/clipcore/Stripe__*`, `/clipcore/R2__*`, `/clipcore/AWS__*`) carry over from the Blazor deployment — no changes needed.

---

## 4. Update Dockerfile

Change the entrypoint from `ClipCore.Web.dll` to `ClipCore.API.dll`:

```dockerfile
ENTRYPOINT ["dotnet", "ClipCore.API.dll"]
```

Also update the `COPY` and `dotnet publish` lines to target `ClipCore.API` instead of `ClipCore.Web`.

---

## 5. Mux Webhook

In the Mux dashboard → Settings → Webhooks:

- URL: `https://api.clipcore.com/api/webhooks/mux`
- Events: `video.asset.ready`, `video.asset.errored`
- Copy the signing secret → save to SSM as `/clipcore/Mux__WebhookSecret` (already exists if Blazor webhook was configured — just verify it's current)

---

## 6. Stripe Webhook

In the Stripe dashboard → Developers → Webhooks:

- URL: `https://api.clipcore.com/api/webhooks/stripe`
- Event: `checkout.session.completed`
- Copy the signing secret → save to SSM as `/clipcore/Stripe__WebhookSecret`

---

## 7. CORS / AllowedOrigins

In `appsettings.json` (or SSM as `/clipcore/AllowedOrigins__0`, `__1`):

```json
"AllowedOrigins": [
  "http://localhost:3000",
  "https://clipcore.com"
]
```

Add the Next.js production domain once known.

---

## 8. Smoke Tests (Swagger)

Hit `https://api.clipcore.com/swagger` and verify:

| Test | Expected |
|---|---|
| `POST /Authenticate` (admin@clipcore.com) | 200 + JWT |
| `POST /RegisterSeller` (new slug) | 200 + JWT |
| `GET /GetFeaturedClips` | 200 + clip array |
| `GET /GetStorefront?slug=<slug>` | 200 or 404 |
| `GET /GetPlatformStats` (Admin token) | 200 + stats |
| `GET /GetClips` (Seller token) | 200 + seller clips |
| `POST /api/webhooks/mux` (invalid sig) | 401 |

---

## 9. Frontend (clipcore-web)

The Next.js 15 frontend lives at `clipcore-web/`. See `clipcore-web/README.md` for full setup.

Quick start:
```bash
cd clipcore-web
cp .env.local.example .env.local   # set NEXT_PUBLIC_API_URL
npm install
npm run dev                        # http://localhost:3000
```

For production, set `NEXT_PUBLIC_API_URL=https://api.clipcore.com` and run `npm run build && npm start`.

### Still needed before go-live

| Feature | What to build |
|---|---|
| Password reset | `POST /ForgotPassword` + `POST /ResetPassword` — models exist in `AuthModels.cs` |
| Promo code validation | `POST /ValidatePromoCode` endpoint + wire into `/cart` page |
| Admin fulfillment UI | `/admin/fulfillment` page using existing `GetAllPurchases` + a new `FulfillPurchase` endpoint |
| Order lookup (guest) | `/order-lookup` page using `PurchaseData.GetPurchasesByEmail` |
| Delivery page | `/delivery/[sessionId]` — download links for a completed order |
| Stripe Connect (Phase 2) | Automated seller payouts |
| CORS update | Add production Next.js URL to `AllowedOrigins` in SSM |

---

## Architecture Notes

- **No snake_case mapping.** Tables and columns are PascalCase. Never add `DefaultTypeMap.MatchNamesWithUnderscores`.
- **Identity tables only in EF.** All business data goes through Dapper + PostgreSQL functions.
- **Singleton data classes** resolve scoped services (MuxService, R2) via `IServiceScopeFactory` — see `CollectionData.DeleteCollection`.
- **JWT `seller_id` claim** is set at login time from the `Sellers` table. All seller-scoped endpoints read this claim — no additional DB lookup needed per request.
