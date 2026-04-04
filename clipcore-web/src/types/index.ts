// ── Auth ─────────────────────────────────────────────────────────────────────

export interface AuthUser {
  userId: string;
  email: string;
  role: 'Admin' | 'Seller' | 'Buyer' | string;
  sellerId?: number;
  token: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  role: string;
  sellerId?: number;
}

// ── Marketplace / Public ──────────────────────────────────────────────────────

export interface StorefrontPublic {
  slug: string;
  displayName: string;
  logoUrl?: string;
  bannerUrl?: string;
  accentColor?: string;
  bio?: string;
  isTrusted: boolean;
  clips: MarketplaceClip[];
}

export interface MarketplaceClip {
  id: string;
  title: string;
  playbackIdTeaser?: string;
  thumbnailFileName?: string;
  priceCents: number;
  priceCommercialCents: number;
  allowGifSale: boolean;
  gifPriceCents: number;
  durationSec?: number;
  collectionName?: string;
  storefrontSlug: string;
}

export interface MarketplaceSearchRequest {
  searchTerm?: string;
  page: number;
  pageSize: number;
}

export interface MarketplaceSearchResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  clips: MarketplaceClip[];
}

// ── Collections ───────────────────────────────────────────────────────────────

export interface CollectionItem {
  id: string;
  name: string;
  date: string;           // ISO date string
  location?: string;
  summary?: string;
  defaultPriceCents: number;
  defaultPriceCommercialCents: number;
  defaultAllowGifSale: boolean;
  defaultGifPriceCents: number;
  heroClipId?: string;
  clipCount: number;
  createdAt: string;
}

export interface CollectionDetail extends CollectionItem {
  clips: ClipItem[];
}

export interface CreateCollectionRequest {
  name: string;
  date: string;
  location?: string;
  summary?: string;
  defaultPriceCents: number;
  defaultPriceCommercialCents: number;
  defaultAllowGifSale: boolean;
  defaultGifPriceCents: number;
}

export interface UpdateCollectionRequest extends CreateCollectionRequest {
  collectionId: string;
  heroClipId?: string;
}

// ── Clips ─────────────────────────────────────────────────────────────────────

export interface ClipItem {
  id: string;
  collectionId: string;
  title: string;
  playbackIdTeaser?: string;
  playbackIdSigned?: string;
  muxAssetId?: string;
  thumbnailFileName?: string;
  masterFileName?: string;
  priceCents: number;
  priceCommercialCents: number;
  allowGifSale: boolean;
  gifPriceCents: number;
  durationSec?: number;
  recordingStartedAt?: string;
  isArchived: boolean;
  tagsJson?: string;
  sellerId?: number;
}

export interface ClipDetail extends ClipItem {
  collectionName?: string;
  collectionDate?: string;
  storefrontSlug?: string;
  sellerDisplayName?: string;
}

export interface CreateClipRequest {
  collectionId: string;
  title: string;
  priceCents: number;
  priceCommercialCents: number;
  allowGifSale: boolean;
  gifPriceCents: number;
  recordingStartedAt?: string;
  tagsJson?: string;
}

export interface UpdateClipRequest {
  clipId: string;
  title: string;
  priceCents: number;
  priceCommercialCents: number;
  allowGifSale: boolean;
  gifPriceCents: number;
  recordingStartedAt?: string;
  tagsJson?: string;
}

export interface BatchSettingsRequest {
  collectionId: string;
  priceCents: number;
  priceCommercialCents: number;
  allowGifSale: boolean;
  gifPriceCents: number;
}

// ── Sellers ───────────────────────────────────────────────────────────────────

export interface SellerProfile {
  id: number;
  userId: string;
  email: string;
  isTrusted: boolean;
  createdAt: string;
  slug: string;
  displayName: string;
  logoUrl?: string;
  bannerUrl?: string;
  accentColor?: string;
  bio?: string;
  isPublished: boolean;
}

export interface StorefrontSettingsRequest {
  displayName: string;
  logoUrl?: string;
  bannerUrl?: string;
  accentColor?: string;
  bio?: string;
  isPublished: boolean;
}

export interface SellerSalesStats {
  totalSales: number;
  totalRevenueCents: number;
  totalPayoutCents: number;
  pendingFulfillment: number;
}

// ── Purchases ────────────────────────────────────────────────────────────────

export type LicenseType = 'Personal' | 'Commercial' | 'Gif';
export type FulfillmentStatus = 'Pending' | 'Fulfilled' | 'Failed';

export interface PurchaseItem {
  id: number;
  clipId?: string;
  clipTitle: string;
  collectionName?: string;
  collectionDate?: string;
  pricePaidCents: number;
  licenseType: LicenseType;
  fulfillmentStatus: FulfillmentStatus;
  createdAt: string;
  highResDownloadUrl?: string;
  isGif: boolean;
}

export interface PurchaseDetail extends PurchaseItem {
  customerEmail?: string;
  customerName?: string;
  platformFeeCents: number;
  sellerPayoutCents: number;
  fulfilledAt?: string;
  stripeSessionId?: string;
  orderId?: string;
  gifStartTime?: number;
  gifEndTime?: number;
  brandedPlaybackId?: string;
}

// ── Admin ─────────────────────────────────────────────────────────────────────

export interface PlatformStats {
  totalSellers: number;
  totalClips: number;
  totalPurchases: number;
  totalRevenueCents: number;
}

export interface SellerSalesSummary {
  sellerId: number;
  displayName: string;
  slug: string;
  salesCount: number;
  totalRevenueCents: number;
  platformFeeCents: number;
  sellerPayoutCents: number;
}

export interface DailyRevenue {
  date: string;
  totalCents: number;
}

export interface PromoCode {
  id: number;
  code: string;
  discountType: 'Percentage' | 'Fixed';
  value: number;
  expiresAt?: string;
  maxUses?: number;
  useCount: number;
  isActive: boolean;
  createdAt: string;
}

// ── Cart ─────────────────────────────────────────────────────────────────────

export interface CartItem {
  id: string;
  title: string;
  collectionId: string;
  collectionName: string;
  collectionDate?: string;
  priceCents: number;
  licenseType: LicenseType;
  isGif: boolean;
  gifStartTime?: number;
  gifEndTime?: number;
  playbackId?: string;
  durationSec?: number;
  masterFileName?: string;
  thumbnailFileName?: string;
}

// ── Checkout ─────────────────────────────────────────────────────────────────

export interface CreateCheckoutRequest {
  items: CartItem[];
  successUrl: string;
  cancelUrl: string;
  promoCode?: string;
}

export interface CheckoutResponse {
  checkoutUrl: string;
}

// ── Theme ─────────────────────────────────────────────────────────────────────

export interface ThemeVars {
  [key: string]: string;
}

export interface ThemeConfig {
  dark: ThemeVars;
  light: ThemeVars;
}
