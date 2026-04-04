'use client';

import React, { useEffect, useState } from 'react';
import { seller as sellerApi, formatPrice } from '@/lib/api';
import type { PurchaseItem } from '@/types';
import Spinner from '@/components/shared/Spinner';
import SellerNav from '@/components/layout/SellerNav';

export default function SellerSalesPage() {
  const [purchases, setPurchases] = useState<PurchaseItem[]>([]);
  const [loading,   setLoading]   = useState(true);

  useEffect(() => {
    sellerApi.getPurchases()
      .then(setPurchases)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner center />;

  const totalRevenue = purchases.reduce((s, p) => s + p.pricePaidCents, 0);

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Sales</h1>
          <p className="dashboard-subtitle">Your recent sales and payouts.</p>
        </div>
      </div>

      <SellerNav />

      <div className="stats-grid mb-6">
        <div className="stat-card">
          <div className="stat-value">{purchases.length}</div>
          <div className="stat-label">Total Sales</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatPrice(totalRevenue)}</div>
          <div className="stat-label">Gross Revenue</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatPrice(Math.round(totalRevenue * 0.9))}</div>
          <div className="stat-label">Your Payout (90%)</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{purchases.filter(p => p.fulfillmentStatus === 'Pending').length}</div>
          <div className="stat-label">Pending Fulfillment</div>
        </div>
      </div>

      <div className="data-table-wrapper">
        {purchases.length === 0 ? (
          <div className="text-center" style={{ padding: '3rem', color: 'var(--text-muted)' }}>No sales yet.</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr><th>Clip</th><th>Collection</th><th>License</th><th>Amount</th><th>Status</th><th>Date</th></tr>
            </thead>
            <tbody>
              {purchases.map(p => (
                <tr key={p.id}>
                  <td style={{ fontWeight: 600 }}>{p.clipTitle}</td>
                  <td className="text-muted">{p.collectionName ?? '—'}</td>
                  <td>
                    <span className={`badge ${p.isGif ? 'badge-secondary' : p.licenseType === 'Commercial' ? 'badge-purple' : 'badge-primary'}`}>
                      {p.isGif ? 'GIF' : p.licenseType}
                    </span>
                  </td>
                  <td style={{ fontWeight: 700, color: 'var(--accent-primary)' }}>{formatPrice(p.pricePaidCents)}</td>
                  <td>
                    <span className={`badge ${p.fulfillmentStatus === 'Fulfilled' ? 'badge-success' : p.fulfillmentStatus === 'Failed' ? 'badge-danger' : 'badge-warning'}`}>
                      {p.fulfillmentStatus}
                    </span>
                  </td>
                  <td className="text-muted" style={{ whiteSpace: 'nowrap' }}>
                    {new Date(p.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
