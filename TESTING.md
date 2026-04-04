# ClipCore — Testing Guide

## Prerequisites

- **Frontend**: Node.js 18+ (`node -v`) + npm
- **API (Option A — local)**: .NET 10 SDK (`dotnet --version`)
- **API (Option B — Docker)**: Docker Desktop

---

## Option A — Fastest: Point frontend at the deployed API

If the API is already running at `https://api.clipcore.com`:

```bash
cd clipcore-web
cp .env.local.example .env.local
# Edit .env.local:
#   NEXT_PUBLIC_API_URL=https://api.clipcore.com
npm install
npm run dev    # http://localhost:3000
```

CORS is already configured to allow `http://localhost:3000` in `appsettings.json`. No API changes needed.

---

## Option B — Local full-stack (dev machine with .NET 10 SDK)

**Terminal 1 — API:**

```bash
cd ClipCore.API

# Set required secrets (or put these in appsettings.Development.json)
export ConnectionStrings__DefaultConnection="Host=...;Database=clipcore;..."
export Jwt__Secret="your-256-bit-secret"
export Mux__TokenId="..."
export Mux__TokenSecret="..."
export Stripe__SecretKey="sk_test_..."
export AllowedOrigins__0="http://localhost:3000"

dotnet run    # listens on http://localhost:5000
```

**Terminal 2 — Frontend:**

```bash
cd clipcore-web
cp .env.local.example .env.local
# NEXT_PUBLIC_API_URL=http://localhost:5000
npm install
npm run dev    # http://localhost:3000
```

---

## Option C — Docker (API only, frontend pointed at it)

```bash
docker build -t clipcore-api .
docker run -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__Secret="..." \
  -e Mux__TokenId="..." \
  -e Mux__TokenSecret="..." \
  -e Stripe__SecretKey="sk_test_..." \
  -e AllowedOrigins__0="http://localhost:3000" \
  clipcore-api
```

Then run the frontend pointing at `http://localhost:5000`.

---

## Smoke Test Checklist

| Page | What to verify |
|---|---|
| `/` | Featured clips load, hero renders |
| `/search` | Full-text search returns results |
| `/store/[slug]` | Storefront banner, collection grid |
| `/collections/[id]` | Clip grid, bundle discount banner (add 3+ clips) |
| `/clips/[id]` | Mux teaser player, license selector |
| `/auth/login` | Login with `admin@clipcore.com` → redirect to home |
| `/cart` | Items show, subtotal updates, 3+ clips = 25% off |
| `/seller/dashboard` | Stats load (Seller role required) |
| `/seller/upload` | Drag-and-drop, Mux upload progress bar |
| `/admin/sellers` | Seller list, approve/revoke buttons |
| `/admin/theme` | Color pickers update CSS live |
| Stripe checkout | Cart → checkout → Stripe test card `4242 4242 4242 4242` |

For the API directly, open `http://localhost:5000/swagger` to test all endpoints.

---

## API Smoke Tests (Swagger)

| Request | Expected |
|---|---|
| `POST /api/Auth/Authenticate` (admin@clipcore.com) | 200 + JWT |
| `POST /api/Auth/RegisterSeller` (new slug) | 200 + JWT |
| `GET /GetFeaturedClips` | 200 + clip array |
| `GET /GetStorefront?slug=<slug>` | 200 or 404 |
| `GET /GetPlatformStats` (Admin token) | 200 + stats |
| `GET /GetClips` (Seller token) | 200 + seller clips |
| `POST /api/webhooks/mux` (invalid sig) | 401 |
