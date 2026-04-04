'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { marketplace } from '@/lib/api';
import type { MarketplaceClip } from '@/types';
import ClipCard from '@/components/shared/ClipCard';
import Spinner from '@/components/shared/Spinner';

export default function HomePage() {
  const [clips, setClips]     = useState<MarketplaceClip[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    marketplace.getFeaturedClips(24)
      .then(setClips)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  return (
    <>
      {/* ── Hero ──────────────────────────────────────────────────── */}
      <section className="hero-section" style={{ marginLeft: '-2rem', marginRight: '-2rem', marginTop: '-2rem', paddingTop: 'calc(var(--nav-height) + 4rem)' }}>
        <div className="aurora-glow" />
        <div className="grid-bg" />

        <div className="hero-content">
          <h1 className="hero-title">
            Exclusive Footage,<br />
            <span className="gradient-text">Instantly Available</span>
          </h1>
          <p className="hero-subtitle">
            Browse and purchase high-quality event footage directly from verified sellers.
            Download your clips in full 1080p resolution.
          </p>
          <div className="hero-actions">
            <Link href="/search" className="btn btn-primary btn-lg">Browse Footage</Link>
            <Link href="/seller/register" className="btn btn-outline btn-lg">Sell Your Footage</Link>
          </div>
        </div>
      </section>

      {/* ── Featured Clips ────────────────────────────────────────── */}
      <section style={{ paddingTop: '3rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h2 className="section-title" style={{ marginBottom: 0 }}>Featured Footage</h2>
          <Link href="/search" className="btn btn-ghost btn-sm">View All →</Link>
        </div>

        {loading ? (
          <Spinner center />
        ) : clips.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '4rem 1rem', color: 'var(--text-muted)' }}>
            <p style={{ fontSize: '1.1rem', marginBottom: '1rem' }}>No clips available yet.</p>
            <Link href="/seller/register" className="btn btn-primary">Be the first to sell footage</Link>
          </div>
        ) : (
          <div className="video-grid">
            {clips.map(clip => (
              <ClipCard
                key={clip.id}
                clip={clip}
                collectionName={clip.collectionName}
                storefrontSlug={clip.storefrontSlug}
              />
            ))}
          </div>
        )}
      </section>
    </>
  );
}
