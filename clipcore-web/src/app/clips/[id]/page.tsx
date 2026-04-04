'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams, useSearchParams } from 'next/navigation';
import MuxPlayer from '@mux/mux-player-react';
import { clips as clipsApi, formatDuration, formatPrice } from '@/lib/api';
import type { ClipDetail, LicenseType } from '@/types';
import { useCart } from '@/lib/cart';
import { useAuth } from '@/lib/auth';
import Spinner from '@/components/shared/Spinner';
import { purchases as purchasesApi } from '@/lib/api';

export default function ClipDetailsPage() {
  const { id }                          = useParams<{ id: string }>();
  const searchParams                    = useSearchParams();
  const { user }                        = useAuth();
  const { addItem, removeItem, hasItem } = useCart();

  const [clip, setClip]                         = useState<ClipDetail | null>(null);
  const [loading, setLoading]                   = useState(true);
  const [selectedLicense, setSelectedLicense]   = useState<LicenseType>('Personal');
  const [selectedGif, setSelectedGif]           = useState(false);
  const [isPurchased, setIsPurchased]           = useState(false);
  const [showSuccess, setShowSuccess]           = useState(false);

  useEffect(() => {
    if (searchParams.get('success') === '1') setShowSuccess(true);
  }, [searchParams]);

  useEffect(() => {
    clipsApi.getPublic(id)
      .then(setClip)
      .catch(() => setClip(null))
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    if (!user || !clip) return;
    purchasesApi.getMyPurchases()
      .then(ps => {
        setIsPurchased(ps.some(p => p.clipId === clip.id && p.fulfillmentStatus === 'Fulfilled'));
      })
      .catch(() => {});
  }, [user, clip]);

  if (loading) return <Spinner center />;

  if (!clip) {
    return (
      <div className="text-center" style={{ padding: '5rem 1rem' }}>
        <div className="alert alert-danger" style={{ display: 'inline-block' }}>Clip not found.</div>
      </div>
    );
  }

  // Determine which playback ID to show (teaser for preview, signed only if purchased)
  const previewId = clip.playbackIdTeaser;
  const signedId  = isPurchased ? clip.playbackIdSigned : undefined;
  const activeId  = signedId ?? previewId;

  const selectedPrice = selectedGif
    ? clip.gifPriceCents
    : selectedLicense === 'Commercial'
    ? clip.priceCommercialCents
    : clip.priceCents;

  const inCart = hasItem(clip.id, selectedGif ? 'Gif' : selectedLicense, selectedGif);

  function toggleCart() {
    if (inCart) {
      removeItem(clip!.id, selectedGif ? 'Gif' : selectedLicense, selectedGif);
    } else {
      addItem({
        id:             clip!.id,
        title:          clip!.title,
        collectionId:   clip!.collectionId,
        collectionName: clip!.collectionName ?? '',
        collectionDate: clip!.collectionDate,
        priceCents:     selectedPrice,
        licenseType:    selectedGif ? 'Gif' : selectedLicense,
        isGif:          selectedGif,
        playbackId:     previewId ?? undefined,
        durationSec:    clip!.durationSec ?? undefined,
        masterFileName: clip!.masterFileName ?? undefined,
        thumbnailFileName: clip!.thumbnailFileName ?? undefined,
      });
    }
  }

  const date = clip.collectionDate
    ? new Date(clip.collectionDate).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
    : null;

  const time = clip.recordingStartedAt
    ? new Date(clip.recordingStartedAt).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
    : null;

  return (
    <>
      {/* Back link */}
      <div className="mb-4" style={{ fontSize: 'var(--font-size-sm)' }}>
        <Link href={`/collections/${clip.collectionId}`} style={{ color: 'var(--text-secondary)' }}>
          ← Back to Event
        </Link>
      </div>

      {/* Success banner */}
      {showSuccess && (
        <div className="alert alert-success mb-6">
          <span style={{ fontSize: '1.5rem' }}>✓</span>
          <div>
            <div style={{ fontWeight: 700 }}>Order Received!</div>
            <div style={{ fontSize: 'var(--font-size-sm)' }}>
              {user ? 'Your order is being processed. Check your purchases dashboard.' : 'Your order is being processed. Check your email for updates.'}
            </div>
          </div>
        </div>
      )}

      <div style={{ maxWidth: 1000, margin: '0 auto' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr min(360px, 40%)', gap: '2rem', alignItems: 'start' }}>

          {/* ── Left: Player ───────────────────────────────── */}
          <div>
            <div style={{ borderRadius: 'var(--radius-lg)', overflow: 'hidden', border: '1px solid var(--border-color)', boxShadow: 'var(--shadow-lg)' }}>
              {activeId ? (
                <MuxPlayer
                  playbackId={activeId}
                  metadata={{ video_title: clip.title }}
                  primaryColor="var(--accent-primary)"
                  accentColor="var(--accent-secondary)"
                  style={{ width: '100%', aspectRatio: '16/9' }}
                />
              ) : (
                <div style={{
                  aspectRatio: '16/9', background: 'var(--bg-subtle)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  flexDirection: 'column', gap: '0.5rem',
                }}>
                  <span style={{ fontSize: '3rem' }}>⏳</span>
                  <p className="text-muted" style={{ fontSize: 'var(--font-size-sm)' }}>Video processing…</p>
                </div>
              )}
            </div>

            {!isPurchased && (
              <p className="text-muted text-center" style={{ fontSize: 'var(--font-size-xs)', marginTop: '0.5rem' }}>
                Preview only. Purchase to download the full 1080p master.
              </p>
            )}
          </div>

          {/* ── Right: Purchase panel ─────────────────────── */}
          <div className="card" style={{ padding: '1.5rem' }}>
            <h1 style={{ fontSize: '1.35rem', fontWeight: 800, marginBottom: '0.75rem' }}>{clip.title}</h1>

            {/* Meta */}
            <div className="flex flex-col gap-1 mb-4" style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-secondary)' }}>
              {clip.collectionName && <span>📁 {clip.collectionName}</span>}
              {date && <span>📅 {date}</span>}
              {time && <span>🕐 {time}</span>}
              {clip.durationSec && <span>⏱ {formatDuration(clip.durationSec)}</span>}
              {clip.storefrontSlug && (
                <Link href={`/store/${clip.storefrontSlug}`} style={{ color: 'var(--accent-primary)', fontWeight: 600 }}>
                  🏪 {clip.sellerDisplayName ?? clip.storefrontSlug}
                </Link>
              )}
            </div>

            {isPurchased ? (
              <div className="alert alert-success mb-4">
                ✓ You own this clip. Download from My Purchases.
              </div>
            ) : (
              <>
                {/* License selection */}
                <div className="flex flex-col gap-2 mb-4">
                  {/* Personal */}
                  <div
                    className={`license-choice${!selectedGif && selectedLicense === 'Personal' ? ' active' : ''}`}
                    onClick={() => { setSelectedLicense('Personal'); setSelectedGif(false); }}
                  >
                    <div>
                      <div className="license-name">Personal License</div>
                      <div className="license-desc">Personal, non-commercial use only.</div>
                    </div>
                    <div className="license-price">{formatPrice(clip.priceCents)}</div>
                  </div>

                  {/* Commercial */}
                  <div
                    className={`license-choice${!selectedGif && selectedLicense === 'Commercial' ? ' active' : ''}`}
                    onClick={() => { setSelectedLicense('Commercial'); setSelectedGif(false); }}
                  >
                    <div>
                      <div className="license-name">Commercial License</div>
                      <div className="license-desc">Use in commercial projects, ads, media.</div>
                    </div>
                    <div className="license-price">{formatPrice(clip.priceCommercialCents)}</div>
                  </div>

                  {/* GIF */}
                  {clip.allowGifSale && (
                    <div
                      className={`license-choice${selectedGif ? ' active' : ''}`}
                      onClick={() => { setSelectedGif(true); setSelectedLicense('Personal'); }}
                    >
                      <div>
                        <div className="license-name">GIF License</div>
                        <div className="license-desc">Animated GIF for social media sharing.</div>
                      </div>
                      <div className="license-price">{formatPrice(clip.gifPriceCents)}</div>
                    </div>
                  )}
                </div>

                {/* CTA */}
                <button
                  className={`btn btn-full${inCart ? ' btn-secondary' : ' btn-primary'}`}
                  style={{ marginBottom: '0.75rem', fontSize: '1rem', padding: '0.85rem' }}
                  onClick={toggleCart}
                >
                  {inCart ? '✕ Remove from Cart' : `🛒 Add to Cart — ${formatPrice(selectedPrice)}`}
                </button>
                <Link href="/cart" className="btn btn-outline btn-full">View Cart</Link>
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
