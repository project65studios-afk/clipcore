# ClipCore Web — Next.js Frontend

Next.js 15 App Router frontend for [ClipCore](https://clipcore.com) — a multi-tenant video clip marketplace.

## Quick Start

```bash
cd clipcore-web
cp .env.local.example .env.local
# Edit .env.local: set NEXT_PUBLIC_API_URL to your API base URL
npm install
npm run dev        # http://localhost:3000
```

## Environment Variables

| Variable | Description | Example |
|---|---|---|
| `NEXT_PUBLIC_API_URL` | Base URL of the ClipCore.API backend | `http://localhost:5000` |

## Project Structure

```
src/
  app/                    # Next.js App Router pages
    page.tsx              # Home — featured clips + hero
    store/[slug]/         # Public seller storefront
    collections/[id]/     # Collection detail + clip grid
    clips/[id]/           # Clip detail + Mux player + license selector
    cart/                 # Shopping cart
    my-purchases/         # Buyer purchase history
    checkout/success/     # Post-payment confirmation
    search/               # Full-text clip search
    auth/
      login/              # JWT login
      register/           # Redirects to seller registration
    seller/               # Seller dashboard (requires Seller role)
      register/           # Seller sign-up
      dashboard/          # Stats overview
      collections/        # CRUD collections
      upload/             # Direct Mux upload via drag-and-drop
      storefront/         # Branding editor (logo, banner, accent colour)
      sales/              # Sales history + payout breakdown
    admin/                # Admin portal (requires Admin role)
      page.tsx            # Overview + quick links
      sellers/            # Approve / revoke seller accounts
      sales/              # Platform revenue report
      promo-codes/        # Create and toggle discount codes
      settings/           # Store name, watermark URL, etc.
      theme/              # Live CSS variable editor (dark + light)
      audit-logs/         # Paginated audit log viewer
    faq/                  # FAQ page

  components/
    layout/
      TopNav.tsx          # Sticky glass nav, search, cart badge, theme toggle
      Footer.tsx          # Copyright + legal links
      SellerNav.tsx       # Seller section tab bar
      AdminNav.tsx        # Admin section tab bar
    shared/
      ClipCard.tsx        # Video card with Mux preview, quick-add, watermark grid
      CollectionCard.tsx  # Collection card with thumbnail overlay
      Spinner.tsx         # Loading spinner

  lib/
    api.ts                # Typed API client for all ClipCore.API endpoints
    auth.tsx              # AuthProvider + useAuth — JWT parsing, login/logout
    cart.tsx              # CartProvider + useCart — localStorage cart, bundle discount
    theme.tsx             # ThemeProvider + useTheme — dark/light mode + CSS var overrides

  types/
    index.ts              # TypeScript types mirroring API models
```

## CSS Design System

All visual values are CSS custom properties defined in `src/app/globals.css`.

### Themes

| Theme | Selector | Character |
|---|---|---|
| Dark (Aurora Night) | `:root` (default) | Deep navy, cyan/pink accents, aurora animations, glass morphism |
| Light (Daybreak) | `[data-theme="light"]` | Clean white/slate, same accent palette |

Every colour, spacing, radius, shadow, and animation is a variable — nothing is hardcoded.

### Runtime Overrides (Admin Configurable)

The `ThemeProvider` reads override maps from localStorage (initially loaded from the DB via `GET /GetTheme`) and injects them as inline CSS on `<html>`. Admins can edit any variable in real time at `/admin/theme` and save back to the DB.

This means:
- Dark mode has its own fully independent set of overrides
- Light mode has its own fully independent set of overrides
- Changes are reflected live on every page without a reload
- Overrides survive deploys (stored in DB, not code)

## Authentication

- JWT stored in `localStorage` under key `cc_token`
- `AuthProvider` decodes the token on mount, exposes `user`, `login()`, `logout()`
- Seller/admin routes are protected by layout guards (`seller/layout.tsx`, `admin/layout.tsx`)
- Role check: `user.role === 'Admin' | 'Seller' | 'Buyer'`

## Cart

- Stored in `localStorage` as JSON under `cc_cart`
- Bundle discount: 3+ items → 25% off (calculated client-side, encoded in Stripe line item prices)
- Checkout calls `POST /CreateCheckout` → redirects to Stripe Checkout URL

## Video

- Clip previews use `@mux/mux-player-react` with the public teaser playback ID
- Full signed playback only rendered for purchased clips
- Uploads use direct-to-Mux via XHR PUT to a presigned URL obtained from `GET /GetMuxUploadUrl`

## API Endpoints Used

See `src/lib/api.ts` for the full list. Key public endpoints:

```
GET  /GetFeaturedClips
GET  /GetStorefront?slug=
POST /SearchClips
GET  /GetPublicCollection?collectionId=
GET  /GetPublicClipDetail?clipId=
POST /api/Auth/Authenticate
POST /api/Auth/RegisterSeller
POST /CreateCheckout
GET  /GetMyPurchases          (requires auth)
```
