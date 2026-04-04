'use client';

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import type { CartItem, LicenseType } from '@/types';

const CART_KEY = 'cc_cart';

interface CartContextValue {
  items: CartItem[];
  count: number;
  subTotalCents: number;
  discountCents: number;
  totalCents: number;
  promoCode: string | null;
  bundleDiscountApplied: boolean;
  addItem: (item: CartItem) => void;
  removeItem: (clipId: string, licenseType: LicenseType, isGif: boolean) => void;
  clearCart: () => void;
  applyPromo: (code: string, discountCents: number) => void;
  removePromo: () => void;
  hasItem: (clipId: string, licenseType: LicenseType, isGif: boolean) => boolean;
}

const CartContext = createContext<CartContextValue | null>(null);

function cartKey(clipId: string, licenseType: LicenseType, isGif: boolean): string {
  return `${clipId}:${isGif ? 'gif' : licenseType}`;
}

export function CartProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<CartItem[]>([]);
  const [promoCode, setPromoCode] = useState<string | null>(null);
  const [promoDiscountCents, setPromoDiscountCents] = useState(0);
  const [ready, setReady] = useState(false);

  // Load from localStorage on mount
  useEffect(() => {
    try {
      const raw = localStorage.getItem(CART_KEY);
      if (raw) setItems(JSON.parse(raw));
    } catch {}
    setReady(true);
  }, []);

  // Persist to localStorage whenever items change
  useEffect(() => {
    if (!ready) return;
    localStorage.setItem(CART_KEY, JSON.stringify(items));
  }, [items, ready]);

  const subTotalCents = items.reduce((s, i) => s + i.priceCents, 0);

  // 25% bundle discount if ≥3 items
  const bundleDiscountApplied = items.length >= 3;
  const bundleDiscountCents = bundleDiscountApplied ? Math.round(subTotalCents * 0.25) : 0;

  // Best discount wins
  const discountCents = Math.max(bundleDiscountCents, promoDiscountCents);
  const totalCents    = Math.max(0, subTotalCents - discountCents);

  const addItem = useCallback((item: CartItem) => {
    setItems(prev => {
      const key = cartKey(item.id, item.licenseType, item.isGif);
      if (prev.some(i => cartKey(i.id, i.licenseType, i.isGif) === key)) return prev;
      return [...prev, item];
    });
  }, []);

  const removeItem = useCallback((clipId: string, licenseType: LicenseType, isGif: boolean) => {
    const key = cartKey(clipId, licenseType, isGif);
    setItems(prev => prev.filter(i => cartKey(i.id, i.licenseType, i.isGif) !== key));
  }, []);

  const clearCart = useCallback(() => {
    setItems([]);
    setPromoCode(null);
    setPromoDiscountCents(0);
  }, []);

  const applyPromo = useCallback((code: string, discountCents: number) => {
    setPromoCode(code);
    setPromoDiscountCents(discountCents);
  }, []);

  const removePromo = useCallback(() => {
    setPromoCode(null);
    setPromoDiscountCents(0);
  }, []);

  const hasItem = useCallback((clipId: string, licenseType: LicenseType, isGif: boolean) => {
    const key = cartKey(clipId, licenseType, isGif);
    return items.some(i => cartKey(i.id, i.licenseType, i.isGif) === key);
  }, [items]);

  return (
    <CartContext.Provider value={{
      items,
      count: items.length,
      subTotalCents,
      discountCents,
      totalCents,
      promoCode,
      bundleDiscountApplied,
      addItem,
      removeItem,
      clearCart,
      applyPromo,
      removePromo,
      hasItem,
    }}>
      {children}
    </CartContext.Provider>
  );
}

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart must be used inside CartProvider');
  return ctx;
}
