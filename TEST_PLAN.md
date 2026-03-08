# ClipCore Pre-Launch Test Plan

Test in a local dev environment with `USE_FAKE_VIDEO=true` first, then repeat critical flows against staging/prod with real Mux + Stripe.

---

## 0. Setup

### Test Accounts to Create
| Role | Email | Notes |
|------|-------|-------|
| Admin | admin@test.com | Seed via `dotnet ef` or direct DB insert with Admin role |
| Seller A | sellera@test.com | Approved seller |
| Seller B | sellerb@test.com | Pending (not yet approved) |
| Buyer | buyer@test.com | Regular registered user |
| Guest | — | No account, just a browser |

### Prerequisites
- App running locally (`dotnet run`)
- DB migrations applied
- `USE_FAKE_VIDEO=true` in dev for non-video tests
- At least one Collection + Clip seeded by Admin

---

## 1. Public / Guest Flows

### 1.1 Homepage
- [ ] Loads without error
- [ ] Shows collection cards with thumbnails (or ▶ placeholder if no thumb)
- [ ] Collections from **pending** sellers (IsTrusted=false) do NOT appear
- [ ] Collections from Admin (no SellerId) DO appear
- [ ] Collections from **approved** sellers (IsTrusted=true) DO appear
- [ ] Click a card → navigates to `/collections/{id}`
- [ ] "Sell Your Footage" button → `/seller/register`
- [ ] Theme toggle (dark/light) works and persists on refresh

### 1.2 Search
- [ ] Nav search bar → navigates to `/search?q=...`
- [ ] Returns matching Events and Clips
- [ ] Clips/Collections from pending sellers do NOT appear in results
- [ ] No results state shows correctly
- [ ] Empty query → clears results

### 1.3 Collection Detail (`/collections/{id}`)
- [ ] Shows collection name, date, location, clips grid
- [ ] Seller attribution breadcrumb links to `/store/{slug}`
- [ ] Clips show thumbnail, duration, price (formatted `$15.00`)
- [ ] Processing clips show amber "Processing" badge
- [ ] Failed clips show red "Upload Failed" badge

### 1.4 Clip Detail (`/clips/{id}`)
- [ ] Shows clip title, price, duration, collection name
- [ ] Watermarked preview plays (Mux player)
- [ ] "Add to Cart" button works
- [ ] Page title includes clip name

### 1.5 Storefront (`/store/{slug}`)
- [ ] Approved seller storefront loads correctly
- [ ] Banner, logo placeholder (or image if set), display name, bio, stats shown
- [ ] Collections grid renders
- [ ] Pending/unpublished storefront → "Storefront not found" message
- [ ] Invalid slug → "Storefront not found"

### 1.6 Static Pages
- [ ] `/faq` loads
- [ ] `/privacy` — no project65 references, support@clipcore.com contact
- [ ] `/terms` — no project65 references, clipcore.com domain in text
- [ ] `/order-lookup` loads and accepts email + order ID

---

## 2. Auth Flows

### 2.1 Registration
- [ ] `/Account/Register` — page title shows store name (not "ClipCore Studios")
- [ ] Register new user → lands on home
- [ ] Email confirmation (if enabled) works

### 2.2 Login / Logout
- [ ] Email + password login works
- [ ] Google OAuth login works (if configured)
- [ ] Logout clears session
- [ ] Login page title shows store name

### 2.3 Access Control
- [ ] `/seller/dashboard` redirects unauthenticated user to login
- [ ] `/admin` redirects non-admin to Access Denied
- [ ] Seller cannot access `/admin/*`
- [ ] Buyer cannot access `/seller/*`

---

## 3. Buyer Flows

### 3.1 Cart
- [ ] Add clip → cart badge count increments in nav
- [ ] `/cart` shows item with title, price (`$15.00`), thumbnail
- [ ] Remove item → cart updates
- [ ] Promo code field accepts valid code, applies discount
- [ ] Cart persists across page navigation

### 3.2 Checkout (Stripe)
- [ ] Checkout button → redirects to Stripe Checkout
- [ ] Complete payment with test card `4242 4242 4242 4242`
- [ ] Redirects to `/checkout/success`
- [ ] Success page shows order ID, clip list, download links
- [ ] Purchase recorded in DB with correct: UserId, ClipId, PricePaidCents, SellerId, PlatformFeeCents (10%), SellerPayoutCents (90%)

### 3.3 My Purchases (`/my-purchases`)
- [ ] Shows purchased clips
- [ ] Download link works (R2 presigned URL or Mux)
- [ ] Price shows `$15.00` format

### 3.4 Order Lookup (`/order-lookup`)
- [ ] Enter email + order ID → shows matching purchases
- [ ] Wrong combination → "no orders found"

### 3.5 Guest Purchase
- [ ] Guest can add to cart and checkout
- [ ] After purchase, can find order via `/order-lookup` with email + order ID

---

## 4. Seller Flows

### 4.1 Registration (`/seller/register`)
- [ ] Authenticated user can register as seller
- [ ] Store URL prefix shows dynamic domain (not hardcoded "clipcore.com/store/")
- [ ] Slug validation: rejects spaces, uppercase, reserved words
- [ ] Slug availability check works
- [ ] Display name + bio saved
- [ ] After registration: user gets Seller role, storefront created, IsTrusted=false
- [ ] Redirected to `/seller/dashboard`

### 4.2 Seller Dashboard (`/seller/dashboard`)
- [ ] Shows storefront name, clip count, recent sales
- [ ] Links to upload, collections, sales, storefront settings

### 4.3 Upload (`/seller/upload`)
- [ ] Uppy widget loads
- [ ] Rejects files > 3GB (pre-upload validation)
- [ ] Rejects video > 90 seconds (pre-upload duration check via HTML5 video element)
- [ ] Accepts valid video file → uploads directly to Mux via UpChunk
- [ ] Upload progress shows in Uppy UI
- [ ] On success: clip created in DB with Processing state, shown in seller's collection
- [ ] Mux webhook `video.asset.ready` fires → clip status updates to Ready (SignalR notification)
- [ ] Mux webhook for >90s clip → clip auto-deleted

### 4.4 Collections (`/seller/collections`)
- [ ] Lists seller's own collections
- [ ] Can create a new collection
- [ ] Can edit collection name, date, location, summary

### 4.5 Sales (`/seller/sales`)
- [ ] Shows sales for this seller only (not other sellers' sales)
- [ ] Revenue, order count correct

### 4.6 Storefront Settings (`/seller/storefront`)
- [ ] Can update display name, bio
- [ ] Can upload logo / banner (R2 storage)
- [ ] Can toggle IsPublished
- [ ] Changes reflected immediately on `/store/{slug}`

---

## 5. Admin Flows

### 5.1 Admin Portal (`/admin`)
- [ ] Collections table loads
- [ ] Quick nav: Sales, Sellers, Promo Codes, Audit Logs, Theme, Settings all link correctly
- [ ] Search/filter works

### 5.2 Seller Management (`/admin/sellers`)
- [ ] Lists all sellers with Pending/Approved tabs and counts
- [ ] Pending tab shows Seller B (IsTrusted=false)
- [ ] Approve Seller B → badge changes to Approved, Seller B's clips now visible on marketplace
- [ ] Revoke Seller A → clips hidden from marketplace
- [ ] "View Clips" links to seller's clips page
- [ ] Actions logged in audit log

### 5.3 Sales Analytics (`/admin/sales`)
- [ ] Gross Revenue shows correct total (in `$X.XX` format)
- [ ] Platform Fees (10%) correct
- [ ] Total Orders correct
- [ ] Avg Order Value correct
- [ ] 7-Day revenue chart renders (Chart.js)
- [ ] Last 7 Days list shows daily totals
- [ ] Recent Transactions table shows last 20 purchases
- [ ] Seller Breakdown table shows per-seller revenue, fees, payout

### 5.4 Fulfillment (`/admin/fulfillment`)
- [ ] Shows pending orders
- [ ] Can mark fulfilled

### 5.5 Admin Upload (`/admin/upload`)
- [ ] Can create collection
- [ ] Can upload clip (admin path, server-side compression)

### 5.6 Theme Editor (`/admin/theme`)
- [ ] Color pickers load with current saved values
- [ ] Preview panel shows "ClipCore" (not "PROJECT65")
- [ ] Change primary color → preview updates live
- [ ] Save → theme applies site-wide immediately (no page reload needed)
- [ ] Reset to Default restores original values
- [ ] Light mode tab works independently

### 5.7 Settings (`/admin/settings`)
- [ ] Store name field saves and reflects in nav + footer + page titles
- [ ] Watermark URL saves (used on clip previews)
- [ ] Support email, base URL fields accept clipcore.com values
- [ ] Stripe fee % configurable (if implemented)

### 5.8 Promo Codes (`/admin/promo-codes`)
- [ ] Create a promo code with % discount
- [ ] Code works at checkout
- [ ] Expired/used-up codes rejected

### 5.9 Audit Logs (`/admin/audit-logs`)
- [ ] Shows seller approve/revoke actions
- [ ] Shows theme updates
- [ ] Filterable/searchable

---

## 6. Webhook Flows

### 6.1 Mux Webhook (`POST /api/webhooks/mux`)
- [ ] Invalid signature → 400 rejected
- [ ] `video.asset.ready` → clip updated with MuxAssetId, DurationSec, PlaybackIdSigned
- [ ] `video.asset.ready` for >90s clip → Mux asset deleted, clip marked errored
- [ ] `video.asset.errored` → clip MuxAssetId set to `errored:{assetId}`
- [ ] SignalR event fired → ClipCard badge updates in real-time without page refresh

### 6.2 Stripe Webhook (`POST /api/webhooks/stripe`)
- [ ] `checkout.session.completed` → Purchase created with correct fields
- [ ] SellerId stamped on purchase (if clip has SellerId)
- [ ] PlatformFeeCents = 10% of price
- [ ] SellerPayoutCents = 90% of price
- [ ] Clip.LastSoldAt updated
- [ ] Buyer email sent (Resend)

---

## 7. Edge Cases

- [ ] Clip with no thumbnail → shows ▶ placeholder (not broken img tag)
- [ ] Collection with no clips → shows empty state gracefully
- [ ] Seller with no collections → storefront shows "No collections yet"
- [ ] Cart with 0 items → checkout disabled or shows empty state
- [ ] Purchase of already-purchased clip → shows "Purchased" badge, no duplicate in cart
- [ ] Admin views `/store/{slug}` of a pending seller → still visible (admin sees all)
- [ ] Promo code with 100% discount → $0 checkout handled correctly
- [ ] Very long seller display name → doesn't break nav or cards
- [ ] Clip duration exactly 90s → allowed (>90s is the limit)

---

## 8. Visual / Cross-Browser

- [ ] Dark mode default looks correct (navy + cyan)
- [ ] Light mode toggle — all pages readable
- [ ] Mobile (375px): nav drawer, card grid 1-col, storefront profile stacks correctly
- [ ] Tablet (768px): card grid 2-col
- [ ] Desktop (1200px+): card grid 3+ col
- [ ] No "project65" or "ClipCore Studios" text visible anywhere on public pages

---

## Quick Regression Checklist (run after any deploy)

- [ ] Home loads, shows collections
- [ ] Search returns results
- [ ] Cart add/remove works
- [ ] Checkout completes (test card)
- [ ] My Purchases shows after checkout
- [ ] Admin portal accessible
- [ ] Seller dashboard accessible
- [ ] `/health` returns 200 "ok"
