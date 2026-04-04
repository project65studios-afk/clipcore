'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { collections as collectionsApi } from '@/lib/api';
import type { CollectionDetail } from '@/types';
import ClipCard from '@/components/shared/ClipCard';
import Spinner from '@/components/shared/Spinner';
import { useAuth } from '@/lib/auth';
import { purchases as purchasesApi } from '@/lib/api';
import type { PurchaseItem } from '@/types';

type SortKey = 'TimeAsc' | 'TimeDesc' | 'PriceAsc' | 'PriceDesc' | 'DurAsc' | 'DurDesc';

export default function CollectionDetailsPage() {
  const { id }                          = useParams<{ id: string }>();
  const { user }                        = useAuth();
  const [collection, setCollection]     = useState<CollectionDetail | null>(null);
  const [loading, setLoading]           = useState(true);
  const [sortBy, setSortBy]             = useState<SortKey>('TimeAsc');
  const [purchasedIds, setPurchasedIds] = useState<Set<string>>(new Set());
  const [copied, setCopied]             = useState(false);

  useEffect(() => {
    collectionsApi.getPublic(id)
      .then(setCollection)
      .catch(() => setCollection(null))
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    if (!user) return;
    purchasesApi.getMyPurchases()
      .then(ps => setPurchasedIds(new Set(ps.map(p => p.clipId).filter(Boolean) as string[])))
      .catch(() => {});
  }, [user]);

  const sorted = collection?.clips ? [...collection.clips].sort((a, b) => {
    switch (sortBy) {
      case 'TimeAsc':  return (a.recordingStartedAt ?? '').localeCompare(b.recordingStartedAt ?? '');
      case 'TimeDesc': return (b.recordingStartedAt ?? '').localeCompare(a.recordingStartedAt ?? '');
      case 'PriceAsc':  return a.priceCents - b.priceCents;
      case 'PriceDesc': return b.priceCents - a.priceCents;
      case 'DurAsc':  return (a.durationSec ?? 0) - (b.durationSec ?? 0);
      case 'DurDesc': return (b.durationSec ?? 0) - (a.durationSec ?? 0);
      default: return 0;
    }
  }) : [];

  async function copyLink() {
    await navigator.clipboard.writeText(window.location.href);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  async function share() {
    try {
      await navigator.share({ title: collection?.name, url: window.location.href });
    } catch {
      copyLink();
    }
  }

  if (loading) return <Spinner center />;

  if (!collection) {
    return (
      <div className="text-center" style={{ padding: '5rem 1rem' }}>
        <p>Collection not found.</p>
        <Link href="/" className="btn btn-primary" style={{ marginTop: '1rem' }}>Go Home</Link>
      </div>
    );
  }

  const date = new Date(collection.date).toLocaleDateString('en-US', {
    month: 'long', day: 'numeric', year: 'numeric',
  });

  return (
    <>
      {/* Breadcrumb */}
      <div className="flex items-center gap-3 mb-4 flex-wrap" style={{ fontSize: 'var(--font-size-sm)' }}>
        <Link href="/" style={{ color: 'var(--text-secondary)' }}>← Back to Collections</Link>
      </div>

      {/* Header card */}
      <div className="premium-card">
        <h1 style={{ fontSize: 'clamp(1.5rem, 4vw, 2.5rem)', fontWeight: 800, marginBottom: '0.75rem' }}>
          {collection.name}
        </h1>
        <div className="flex items-center gap-3 flex-wrap mb-4" style={{ color: 'var(--text-secondary)', fontSize: 'var(--font-size-sm)' }}>
          <span>📅 {date}</span>
          {collection.location && (
            <>
              <span className="meta-dot">•</span>
              <span>📍 {collection.location}</span>
            </>
          )}
          <span className="meta-dot">•</span>
          <span>🎬 {collection.clips.length} clips</span>
        </div>
        {collection.summary && (
          <p style={{ color: 'var(--text-secondary)', marginBottom: '1.5rem', maxWidth: 600 }}>
            {collection.summary}
          </p>
        )}
        <div className="flex gap-3 flex-wrap">
          <button className="btn btn-primary" onClick={share}>Share Collection</button>
          <button className="btn btn-secondary" onClick={copyLink}>
            {copied ? '✓ Link Copied!' : 'Copy Link'}
          </button>
        </div>
      </div>

      {/* Bundle discount banner */}
      {collection.clips.length >= 3 && (
        <div style={{
          background: 'var(--accent-primary-dim)', border: '1px solid var(--accent-primary)',
          borderRadius: 'var(--radius-md)', padding: '1rem', marginBottom: '2rem',
          display: 'flex', alignItems: 'center', gap: '1rem',
        }}>
          <span style={{ fontSize: '1.5rem' }}>⭐</span>
          <div>
            <div style={{ fontWeight: 700 }}>Collection Bundle Discount!</div>
            <div style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-secondary)' }}>
              Add 3 or more clips to your cart and get{' '}
              <span style={{ color: 'var(--accent-primary)', fontWeight: 700 }}>25% OFF</span> your entire order.
            </div>
          </div>
        </div>
      )}

      {/* Clips header */}
      <div className="flex justify-between items-center mb-4 flex-wrap gap-3">
        <h3 className="section-title" style={{ marginBottom: 0 }}>All Clips</h3>
        <select
          className="form-control"
          style={{ width: 'auto' }}
          value={sortBy}
          onChange={e => setSortBy(e.target.value as SortKey)}
        >
          <option value="TimeAsc">Time: Oldest First</option>
          <option value="TimeDesc">Time: Newest First</option>
          <option value="PriceAsc">Price: Low to High</option>
          <option value="PriceDesc">Price: High to Low</option>
          <option value="DurAsc">Duration: Shortest First</option>
          <option value="DurDesc">Duration: Longest First</option>
        </select>
      </div>

      {sorted.length === 0 ? (
        <p className="text-muted">No clips available yet.</p>
      ) : (
        <div className="video-grid">
          {sorted.map(clip => (
            <ClipCard
              key={clip.id}
              clip={clip}
              isPurchased={purchasedIds.has(clip.id)}
              collectionName={collection.name}
              collectionDate={collection.date}
            />
          ))}
        </div>
      )}
    </>
  );
}
