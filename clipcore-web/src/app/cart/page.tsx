'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useCart } from '@/lib/cart';
import { useAuth } from '@/lib/auth';
import { purchases as purchasesApi, formatPrice } from '@/lib/api';
import type { LicenseType } from '@/types';

function licenseBadgeStyle(licenseType: LicenseType, isGif: boolean) {
  if (isGif)                       return { bg: 'var(--accent-secondary-dim)', color: 'var(--accent-secondary)' };
  if (licenseType === 'Commercial') return { bg: 'var(--accent-purple-dim)',    color: 'var(--accent-purple)' };
  return { bg: 'var(--accent-primary-dim)', color: 'var(--accent-primary)' };
}

export default function CartPage() {
  const router                                = useRouter();
  const { user }                              = useAuth();
  const { items, count, removeItem,
          subTotalCents, discountCents,
          totalCents, bundleDiscountApplied,
          promoCode, removePromo }            = useCart();
  const [promoInput, setPromoInput]           = useState('');
  const [promoError, setPromoError]           = useState('');
  const [applyingPromo, setApplyingPromo]     = useState(false);
  const [checkingOut, setCheckingOut]         = useState(false);

  async function applyPromo() {
    if (!promoInput.trim()) return;
    setApplyingPromo(true);
    setPromoError('');
    try {
      // TODO: validate promo code via API
      setPromoError('Promo code validation coming soon.');
    } finally {
      setApplyingPromo(false);
    }
  }

  async function checkout() {
    setCheckingOut(true);
    try {
      const successUrl = `${window.location.origin}/checkout/success?session_id={CHECKOUT_SESSION_ID}`;
      const cancelUrl  = `${window.location.origin}/cart`;

      const discountFactor = discountCents > 0 ? totalCents / subTotalCents : 1.0;

      const res = await purchasesApi[user ? 'checkout' : 'guestCheckout']({
        items: items.map(i => ({
          ...i,
          priceCents: Math.round(i.priceCents * discountFactor),
        })),
        successUrl,
        cancelUrl,
        promoCode: promoCode ?? undefined,
      });
      window.location.href = res.checkoutUrl;
    } catch (err: unknown) {
      alert((err as Error).message ?? 'Checkout failed. Please try again.');
    } finally {
      setCheckingOut(false);
    }
  }

  return (
    <div style={{ maxWidth: 800, margin: '0 auto' }}>
      <h1 style={{ marginBottom: '2rem' }}>Your Cart</h1>

      {count === 0 ? (
        <div style={{
          textAlign: 'center', padding: '4rem 1rem',
          background: 'var(--bg-surface)', borderRadius: 'var(--radius-lg)',
          border: '1px dashed var(--border-color)',
        }}>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '1.5rem', fontSize: '1.1rem' }}>
            Your cart is empty.
          </p>
          <Link href="/" className="btn btn-primary">Browse Footage</Link>
        </div>
      ) : (
        <>
          {/* Cart items */}
          <div className="flex flex-col gap-3 mb-6">
            {items.map(item => {
              const style = licenseBadgeStyle(item.licenseType, item.isGif);
              return (
                <div key={`${item.id}:${item.licenseType}:${item.isGif}`} className="cart-item">
                  <div className="cart-thumb">
                    {item.playbackId && (
                      <img
                        src={`https://image.mux.com/${item.playbackId}/thumbnail.jpg?width=200`}
                        alt={item.title}
                      />
                    )}
                  </div>
                  <div className="cart-info">
                    <div className="cart-title">{item.title}</div>
                    {item.collectionName && (
                      <div style={{ fontSize: 'var(--font-size-xs)', color: 'var(--accent-primary)', marginBottom: '0.25rem' }}>
                        {item.collectionName}
                      </div>
                    )}
                    {item.durationSec && (
                      <div style={{ fontSize: 'var(--font-size-xs)', color: 'var(--text-muted)' }}>
                        Duration: {Math.floor(item.durationSec / 60)}:{String(Math.floor(item.durationSec % 60)).padStart(2, '0')}
                      </div>
                    )}
                    <span className="badge" style={{
                      background: style.bg, color: style.color,
                      border: `1px solid ${style.color}`, marginTop: '0.35rem',
                    }}>
                      {item.isGif ? 'GIF License' : `${item.licenseType} License`}
                    </span>
                  </div>
                  <div style={{ textAlign: 'right', flexShrink: 0 }}>
                    <div className="cart-price" style={{ marginBottom: '0.5rem' }}>
                      {formatPrice(item.priceCents)}
                    </div>
                    <button
                      className="btn btn-ghost btn-sm"
                      style={{ color: 'var(--status-danger)', fontSize: 'var(--font-size-xs)' }}
                      onClick={() => removeItem(item.id, item.licenseType, item.isGif)}
                    >
                      Remove
                    </button>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Order summary */}
          <div className="order-summary">
            {/* Promo code */}
            {!promoCode && (
              <div className="flex gap-2 mb-4">
                <input
                  className="form-control"
                  placeholder="Promo Code"
                  value={promoInput}
                  onChange={e => setPromoInput(e.target.value.toUpperCase())}
                  disabled={applyingPromo}
                />
                <button
                  className="btn btn-secondary"
                  style={{ whiteSpace: 'nowrap' }}
                  onClick={applyPromo}
                  disabled={!promoInput.trim() || applyingPromo}
                >
                  {applyingPromo ? '…' : 'Apply'}
                </button>
              </div>
            )}
            {promoError && <p className="text-danger" style={{ fontSize: 'var(--font-size-xs)', marginBottom: '0.75rem' }}>{promoError}</p>}

            {/* Bundle discount notice */}
            {bundleDiscountApplied ? (
              <div className="alert alert-success mb-4">🎉 Volume Discount Unlocked: 25% Off!</div>
            ) : promoCode ? (
              <div className="alert alert-success mb-4" style={{ justifyContent: 'space-between' }}>
                <span>🎟️ Promo Applied: <strong>{promoCode}</strong></span>
                <button className="btn btn-ghost btn-sm" style={{ color: 'var(--status-danger)' }} onClick={removePromo}>Remove</button>
              </div>
            ) : count < 3 ? (
              <div className="alert alert-info mb-4">
                Add <strong>{3 - count}</strong> more item{3 - count !== 1 ? 's' : ''} to unlock 25% off!
              </div>
            ) : null}

            {/* Pricing rows */}
            <div className="summary-row">
              <span style={{ color: 'var(--text-muted)' }}>Subtotal</span>
              <span style={{ textDecoration: discountCents > 0 ? 'line-through' : undefined }}>
                {formatPrice(subTotalCents)}
              </span>
            </div>
            {discountCents > 0 && (
              <div className="summary-row" style={{ color: 'var(--status-success)' }}>
                <span>{bundleDiscountApplied ? 'Volume Discount (25%)' : 'Promo Discount'}</span>
                <span>−{formatPrice(discountCents)}</span>
              </div>
            )}
            <div className="summary-row summary-total">
              <span>Total</span>
              <span style={{ color: 'var(--accent-primary)' }}>{formatPrice(totalCents)}</span>
            </div>

            <button
              className="btn btn-primary btn-full"
              style={{ marginTop: '1.25rem', fontSize: '1.05rem', padding: '0.85rem' }}
              onClick={checkout}
              disabled={checkingOut}
            >
              {checkingOut ? 'Redirecting…' : 'Proceed to Checkout'}
            </button>

            {!user && (
              <p className="text-muted text-center" style={{ fontSize: 'var(--font-size-xs)', marginTop: '0.75rem' }}>
                <Link href="/auth/login" style={{ color: 'var(--accent-primary)' }}>Sign in</Link> to save your purchases to your account.
              </p>
            )}
          </div>
        </>
      )}
    </div>
  );
}
