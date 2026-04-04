'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/auth';
import { purchases as purchasesApi, formatPrice } from '@/lib/api';
import type { PurchaseItem, LicenseType } from '@/types';
import Spinner from '@/components/shared/Spinner';

function licenseBadge(licenseType: LicenseType, isGif: boolean) {
  if (isGif)                       return { label: 'GIF License',        bg: 'var(--accent-secondary-dim)', color: 'var(--accent-secondary)' };
  if (licenseType === 'Commercial') return { label: 'Commercial License', bg: 'var(--accent-purple-dim)',    color: 'var(--accent-purple)' };
  return { label: 'Personal License', bg: 'var(--accent-primary-dim)', color: 'var(--accent-primary)' };
}

function groupByOrder(purchases: PurchaseItem[]): Map<string, PurchaseItem[]> {
  const map = new Map<string, PurchaseItem[]>();
  for (const p of purchases) {
    // Group by stripeSessionId if available, else individual
    const key = (p as any).stripeSessionId ?? String(p.id);
    const arr = map.get(key) ?? [];
    arr.push(p);
    map.set(key, arr);
  }
  return map;
}

export default function MyPurchasesPage() {
  const router         = useRouter();
  const { user, isLoading: authLoading } = useAuth();
  const [purchases, setPurchases] = useState<PurchaseItem[]>([]);
  const [loading, setLoading]     = useState(true);

  useEffect(() => {
    if (authLoading) return;
    if (!user) { router.push('/auth/login?redirect=/my-purchases'); return; }
    purchasesApi.getMyPurchases()
      .then(setPurchases)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [user, authLoading]);

  if (authLoading || loading) return <Spinner center />;

  const grouped = groupByOrder(purchases);
  // Sort by most recent first
  const orderedKeys = [...grouped.keys()].sort((a, b) => {
    const aDate = grouped.get(a)![0].createdAt;
    const bDate = grouped.get(b)![0].createdAt;
    return bDate.localeCompare(aDate);
  });

  return (
    <>
      <h1 style={{ marginBottom: '2rem' }}>My Purchases</h1>

      {purchases.length === 0 ? (
        <div style={{
          textAlign: 'center', padding: '4rem 1rem',
          background: 'var(--bg-surface)', borderRadius: 'var(--radius-lg)',
          border: '1px dashed var(--border-color)',
        }}>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '1.5rem', fontSize: '1.1rem' }}>
            You haven&apos;t purchased any clips yet.
          </p>
          <Link href="/" className="btn btn-primary">Browse Footage</Link>
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          {orderedKeys.map(orderKey => {
            const orderItems = grouped.get(orderKey)!;
            const first      = orderItems[0];
            const sessionId  = (first as any).stripeSessionId;
            const orderId    = sessionId
              ? `ORD-${sessionId.slice(-8).toUpperCase()}`
              : `ITEM-${first.id}`;
            const orderDate = new Date(first.createdAt).toLocaleDateString('en-US', {
              month: 'long', day: 'numeric', year: 'numeric',
            });
            const allFulfilled = orderItems.every(p => p.fulfillmentStatus === 'Fulfilled');

            return (
              <div key={orderKey} style={{
                background: 'var(--bg-surface)', borderRadius: 'var(--radius-lg)',
                border: '1px solid var(--border-color)', overflow: 'hidden',
              }}>
                {/* Order header */}
                <div style={{
                  background: 'color-mix(in srgb, var(--text-main), transparent 96%)',
                  padding: '1rem 1.5rem',
                  borderBottom: '1px solid var(--border-color)',
                  display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.75rem',
                }}>
                  <div>
                    <div style={{ fontWeight: 700 }}>{orderId}</div>
                    <div style={{ fontSize: 'var(--font-size-xs)', color: 'var(--text-muted)' }}>
                      {orderDate} · {orderItems.length} item{orderItems.length !== 1 ? 's' : ''}
                    </div>
                  </div>
                  {allFulfilled && sessionId && (
                    <Link href={`/delivery/${sessionId}`} target="_blank" className="btn btn-primary btn-sm">
                      View Files →
                    </Link>
                  )}
                </div>

                {/* Items */}
                {orderItems.map(p => {
                  const badge = licenseBadge(p.licenseType, p.isGif);
                  return (
                    <div key={p.id} style={{
                      display: 'flex', alignItems: 'center', gap: '1rem', padding: '1rem 1.5rem',
                      borderBottom: '1px solid var(--border-subtle)', flexWrap: 'wrap',
                    }}>
                      {/* Info */}
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ fontWeight: 600, marginBottom: '0.25rem' }}>
                          {p.clipTitle ?? 'Deleted Clip'}
                        </div>
                        {p.collectionName && (
                          <div style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-secondary)', marginBottom: '0.3rem' }}>
                            {p.collectionName}
                            {p.collectionDate && (
                              <span style={{ color: 'var(--text-muted)' }}>
                                {' '}· {new Date(p.collectionDate).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })}
                              </span>
                            )}
                          </div>
                        )}
                        <span className="badge" style={{ background: badge.bg, color: badge.color, border: `1px solid ${badge.color}` }}>
                          {badge.label}
                        </span>
                      </div>

                      {/* Price */}
                      <div style={{ fontWeight: 700, color: 'var(--accent-primary)', whiteSpace: 'nowrap' }}>
                        {formatPrice(p.pricePaidCents)}
                      </div>

                      {/* Actions */}
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem', minWidth: 140 }}>
                        {p.clipId && (
                          <Link href={`/clips/${p.clipId}`} className="btn btn-outline btn-sm">
                            Preview / Details
                          </Link>
                        )}
                        {p.fulfillmentStatus === 'Fulfilled' && !p.isGif && p.highResDownloadUrl && (
                          <a href={p.highResDownloadUrl} target="_blank" rel="noreferrer" className="btn btn-primary btn-sm">
                            ⬇ Download
                          </a>
                        )}
                        {p.fulfillmentStatus !== 'Fulfilled' && (
                          <div className="badge badge-warning" style={{ justifyContent: 'center' }}>
                            ⏳ Awaiting Fulfillment
                          </div>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            );
          })}
        </div>
      )}
    </>
  );
}
