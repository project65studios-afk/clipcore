import type {
  AuthResponse,
  StorefrontPublic,
  MarketplaceClip,
  MarketplaceSearchRequest,
  MarketplaceSearchResponse,
  CollectionItem,
  CollectionDetail,
  CreateCollectionRequest,
  UpdateCollectionRequest,
  ClipItem,
  ClipDetail,
  CreateClipRequest,
  UpdateClipRequest,
  BatchSettingsRequest,
  SellerProfile,
  StorefrontSettingsRequest,
  SellerSalesStats,
  PurchaseItem,
  PurchaseDetail,
  PlatformStats,
  SellerSalesSummary,
  DailyRevenue,
  PromoCode,
  CreateCheckoutRequest,
  CheckoutResponse,
} from '@/types';

const BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('cc_token');
}

interface FetchOptions extends RequestInit {
  auth?: boolean;
}

async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  const { auth = false, ...init } = options;
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(init.headers as Record<string, string>),
  };
  if (auth) {
    const token = getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;
  }
  const res = await fetch(`${BASE}${path}`, { ...init, headers });
  if (!res.ok) {
    let msg = `API error ${res.status}`;
    try { const body = await res.json(); msg = body.title ?? body.message ?? msg; } catch {}
    throw new Error(msg);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

// ── Auth ─────────────────────────────────────────────────────────────────────

export const auth = {
  login: (email: string, password: string) =>
    apiFetch<AuthResponse>('/api/Auth/Authenticate', {
      method: 'POST', body: JSON.stringify({ email, password }),
    }),

  registerSeller: (email: string, password: string, displayName: string, slug: string) =>
    apiFetch<AuthResponse>('/api/Auth/RegisterSeller', {
      method: 'POST', body: JSON.stringify({ email, password, displayName, slug }),
    }),
};

// ── Marketplace ───────────────────────────────────────────────────────────────

export const marketplace = {
  getStorefront: (slug: string) =>
    apiFetch<StorefrontPublic>(`/GetStorefront?slug=${encodeURIComponent(slug)}`),

  getFeaturedClips: (limit = 24) =>
    apiFetch<MarketplaceClip[]>(`/GetFeaturedClips?limit=${limit}`),

  search: (req: MarketplaceSearchRequest) =>
    apiFetch<MarketplaceSearchResponse>('/SearchClips', {
      method: 'POST', body: JSON.stringify(req),
    }),
};

// ── Collections ───────────────────────────────────────────────────────────────

export const collections = {
  list: () =>
    apiFetch<CollectionItem[]>('/GetCollections', { auth: true }),

  getDetail: (collectionId: string) =>
    apiFetch<CollectionDetail>(`/GetCollectionDetail?collectionId=${encodeURIComponent(collectionId)}`, { auth: true }),

  getPublic: (collectionId: string) =>
    apiFetch<CollectionDetail>(`/GetPublicCollection?collectionId=${encodeURIComponent(collectionId)}`),

  create: (req: CreateCollectionRequest) =>
    apiFetch<{ collectionId: string }>('/CreateCollection', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  update: (req: UpdateCollectionRequest) =>
    apiFetch<void>('/UpdateCollection', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  delete: (collectionId: string) =>
    apiFetch<void>(`/DeleteCollection?collectionId=${encodeURIComponent(collectionId)}`, {
      method: 'DELETE', auth: true,
    }),
};

// ── Clips ─────────────────────────────────────────────────────────────────────

export const clips = {
  list: () =>
    apiFetch<ClipItem[]>('/GetClips', { auth: true }),

  getDetail: (clipId: string) =>
    apiFetch<ClipDetail>(`/GetClipDetail?clipId=${encodeURIComponent(clipId)}`, { auth: true }),

  getPublic: (clipId: string) =>
    apiFetch<ClipDetail>(`/GetPublicClipDetail?clipId=${encodeURIComponent(clipId)}`),

  getMuxUploadUrl: (clipId: string) =>
    apiFetch<{ uploadUrl: string; uploadId: string }>(
      `/GetMuxUploadUrl?clipId=${encodeURIComponent(clipId)}`, { auth: true }),

  create: (req: CreateClipRequest) =>
    apiFetch<{ clipId: string }>('/CreateClip', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  update: (req: UpdateClipRequest) =>
    apiFetch<void>('/UpdateClip', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  updateBatchSettings: (req: BatchSettingsRequest) =>
    apiFetch<void>('/UpdateBatchSettings', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  delete: (clipId: string) =>
    apiFetch<void>(`/DeleteClip?clipId=${encodeURIComponent(clipId)}`, {
      method: 'DELETE', auth: true,
    }),
};

// ── Seller ────────────────────────────────────────────────────────────────────

export const seller = {
  getProfile: () =>
    apiFetch<SellerProfile>('/GetSellerProfile', { auth: true }),

  updateStorefront: (req: StorefrontSettingsRequest) =>
    apiFetch<void>('/UpdateStorefrontSettings', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  getSalesStats: () =>
    apiFetch<SellerSalesStats>('/GetSellerSalesStats', { auth: true }),

  getPurchases: () =>
    apiFetch<PurchaseItem[]>('/GetSellerPurchases', { auth: true }),
};

// ── Storage ───────────────────────────────────────────────────────────────────

export const storage = {
  getUploadUrl: (fileName: string, contentType: string) =>
    apiFetch<{ url: string; key: string }>(
      `/GetUploadUrl?fileName=${encodeURIComponent(fileName)}&contentType=${encodeURIComponent(contentType)}`,
      { auth: true }),
};

// ── Purchases (buyer) ─────────────────────────────────────────────────────────

export const purchases = {
  getMyPurchases: () =>
    apiFetch<PurchaseItem[]>('/GetMyPurchases', { auth: true }),

  checkout: (req: CreateCheckoutRequest) =>
    apiFetch<CheckoutResponse>('/CreateCheckout', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  guestCheckout: (req: CreateCheckoutRequest) =>
    apiFetch<CheckoutResponse>('/CreateCheckout', {
      method: 'POST', body: JSON.stringify(req),
    }),
};

// ── Admin ─────────────────────────────────────────────────────────────────────

export const admin = {
  getSellers: () =>
    apiFetch<SellerProfile[]>('/GetSellers', { auth: true }),

  getPlatformStats: () =>
    apiFetch<PlatformStats>('/GetPlatformStats', { auth: true }),

  approveSeller: (sellerId: number) =>
    apiFetch<void>('/ApproveSeller', {
      method: 'POST', body: JSON.stringify({ sellerId }), auth: true,
    }),

  revokeSeller: (sellerId: number) =>
    apiFetch<void>('/RevokeSeller', {
      method: 'POST', body: JSON.stringify({ sellerId }), auth: true,
    }),

  getSellerSalesSummary: () =>
    apiFetch<SellerSalesSummary[]>('/GetSellerSalesSummary', { auth: true }),

  getDailyRevenue: (days = 30) =>
    apiFetch<DailyRevenue[]>(`/GetDailyRevenue?days=${days}`, { auth: true }),

  getRecentSales: (count = 20) =>
    apiFetch<PurchaseDetail[]>(`/GetRecentSales?count=${count}`, { auth: true }),

  getAllPurchases: (params?: { status?: number; since?: string; search?: string }) => {
    const q = new URLSearchParams();
    if (params?.status != null) q.set('status', String(params.status));
    if (params?.since) q.set('since', params.since);
    if (params?.search) q.set('search', params.search);
    return apiFetch<PurchaseDetail[]>(`/GetAllPurchases?${q}`, { auth: true });
  },

  getPromoCodes: () =>
    apiFetch<PromoCode[]>('/GetPromoCodes', { auth: true }),

  createPromoCode: (req: Omit<PromoCode, 'id' | 'useCount' | 'createdAt'>) =>
    apiFetch<PromoCode>('/CreatePromoCode', {
      method: 'POST', body: JSON.stringify(req), auth: true,
    }),

  togglePromoCode: (id: number, isActive: boolean) =>
    apiFetch<void>('/TogglePromoCode', {
      method: 'POST', body: JSON.stringify({ id, isActive }), auth: true,
    }),

  getSettings: () =>
    apiFetch<Record<string, string>>('/GetSettings', { auth: true }),

  updateSetting: (key: string, value: string) =>
    apiFetch<void>('/UpdateSetting', {
      method: 'POST', body: JSON.stringify({ key, value }), auth: true,
    }),

  getTheme: () =>
    apiFetch<{ dark: Record<string, string>; light: Record<string, string> }>('/GetTheme', { auth: true }),

  saveTheme: (dark: Record<string, string>, light: Record<string, string>) =>
    apiFetch<void>('/SaveTheme', {
      method: 'POST', body: JSON.stringify({ dark, light }), auth: true,
    }),

  getAuditLogs: (params?: { page?: number; pageSize?: number }) => {
    const q = new URLSearchParams();
    if (params?.page) q.set('page', String(params.page));
    if (params?.pageSize) q.set('pageSize', String(params.pageSize));
    return apiFetch<Array<{ id: number; userId: string; action: string; detail: string; createdAt: string }>>(
      `/GetAuditLogs?${q}`, { auth: true });
  },
};

// ── Mux helpers ───────────────────────────────────────────────────────────────

export function muxThumbnailUrl(playbackId: string, token?: string, width = 400): string {
  const base = `https://image.mux.com/${playbackId}/thumbnail.jpg`;
  const p = new URLSearchParams({ width: String(width) });
  if (token) p.set('token', token);
  return `${base}?${p}`;
}

export function formatDuration(sec?: number): string {
  if (!sec) return '--:--';
  const m = Math.floor(sec / 60);
  const s = Math.floor(sec % 60);
  return `${m}:${s.toString().padStart(2, '0')}`;
}

export function formatPrice(cents: number): string {
  return `$${(cents / 100).toFixed(2)}`;
}
