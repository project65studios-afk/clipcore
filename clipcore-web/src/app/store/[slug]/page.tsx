'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { marketplace } from '@/lib/api';
import type { StorefrontPublic } from '@/types';
import ClipCard from '@/components/shared/ClipCard';
import Spinner from '@/components/shared/Spinner';

export default function StorefrontPage() {
  const { slug }                    = useParams<{ slug: string }>();
  const [sf, setSf]                 = useState<StorefrontPublic | null>(null);
  const [loading, setLoading]       = useState(true);
  const [notFound, setNotFound]     = useState(false);

  useEffect(() => {
    marketplace.getStorefront(slug)
      .then(data => setSf(data))
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false));
  }, [slug]);

  if (loading) return <Spinner center />;

  if (notFound || !sf) {
    return (
      <div className="text-center" style={{ padding: '5rem 1rem' }}>
        <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>🔍</div>
        <h3>Storefront not found</h3>
        <p className="text-muted" style={{ marginBottom: '1.5rem' }}>
          This store doesn&apos;t exist or isn&apos;t published yet.
        </p>
        <Link href="/" className="btn btn-primary">Browse All Footage</Link>
      </div>
    );
  }

  // Group clips by collection (collectionName is on MarketplaceClip)
  const groupedClips = sf.clips.reduce<Record<string, typeof sf.clips>>((acc, clip) => {
    const key = clip.collectionName ?? 'Uncategorized';
    if (!acc[key]) acc[key] = [];
    acc[key].push(clip);
    return acc;
  }, {});

  const totalClips = sf.clips.length;

  return (
    <>
      {/* Storefront header */}
      <div className="storefront-header">
        {sf.bannerUrl ? (
          <div className="storefront-banner" style={{ backgroundImage: `url(${sf.bannerUrl})` }} />
        ) : (
          <div
            className="storefront-banner"
            style={{ background: `linear-gradient(135deg, ${sf.accentColor ?? 'var(--bg-subtle)'} 0%, var(--bg-main) 100%)` }}
          />
        )}

        <div className="storefront-profile">
          {sf.logoUrl ? (
            <img src={sf.logoUrl} alt={sf.displayName} className="storefront-logo" />
          ) : (
            <div className="storefront-logo-placeholder">
              {sf.displayName[0]?.toUpperCase() ?? 'S'}
            </div>
          )}
          <div className="storefront-info" style={{ flex: 1 }}>
            <h1 className="storefront-name">{sf.displayName}</h1>
            {sf.bio && <p className="storefront-bio">{sf.bio}</p>}
            <div className="storefront-stats">
              <span><strong>{totalClips}</strong> clips</span>
              {sf.isTrusted && (
                <>
                  <span className="meta-dot">•</span>
                  <span className="badge badge-success">✓ Verified Seller</span>
                </>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Clips */}
      {sf.clips.length === 0 ? (
        <div className="text-center" style={{ padding: '4rem 1rem' }}>
          <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>🎬</div>
          <h3>No footage yet</h3>
          <p className="text-muted">Check back soon for new footage.</p>
        </div>
      ) : (
        Object.entries(groupedClips).map(([collName, groupClips]) => (
          <section key={collName} style={{ marginBottom: '3rem' }}>
            <h2 className="section-title">{collName}</h2>
            <div className="video-grid">
              {groupClips.map(clip => (
                <ClipCard
                  key={clip.id}
                  clip={clip}
                  collectionName={collName}
                  storefrontSlug={slug}
                />
              ))}
            </div>
          </section>
        ))
      )}
    </>
  );
}
