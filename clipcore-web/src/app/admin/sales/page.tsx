'use client';

import React, { useEffect, useState } from 'react';
import { admin as adminApi, formatPrice } from '@/lib/api';
import type { PurchaseDetail, SellerSalesSummary, DailyRevenue } from '@/types';
import Spinner from '@/components/shared/Spinner';
import AdminNav from '@/components/layout/AdminNav';

export default function AdminSalesPage() {
  const [summary,  setSummary]  = useState<SellerSalesSummary[]>([]);
  const [revenue,  setRevenue]  = useState<DailyRevenue[]>([]);
  const [purchases,setPurchases]= useState<PurchaseDetail[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [search,   setSearch]   = useState('');

  useEffect(() => {
    Promise.all([
      adminApi.getSellerSalesSummary(),
      adminApi.getDailyRevenue(30),
      adminApi.getRecentSales(50),
    ])
      .then(([s, r, p]) => { setSummary(s); setRevenue(r); setPurchases(p); })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner center />;

  const totalRevenue = summary.reduce((a, s) => a + Number(s.totalRevenueCents), 0);
  const totalFee     = summary.reduce((a, s) => a + Number(s.platformFeeCents), 0);

  const filtered = purchases.filter(p =>
    !search ||
    p.clipTitle?.toLowerCase().includes(search.toLowerCase()) ||
    p.customerEmail?.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Sales Report</h1>
          <p className="dashboard-subtitle">Platform-wide revenue and seller breakdowns.</p>
        </div>
      </div>
      <AdminNav />

      {/* Summary stats */}
      <div className="stats-grid mb-6">
        <div className="stat-card">
          <div className="stat-value">{formatPrice(totalRevenue)}</div>
          <div className="stat-label">Gross Revenue</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatPrice(totalFee)}</div>
          <div className="stat-label">Platform Fees (10%)</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatPrice(totalRevenue - totalFee)}</div>
          <div className="stat-label">Seller Payouts</div>
        </div>
      </div>

      {/* Seller breakdown */}
      <h3 className="section-title">By Seller</h3>
      <div className="data-table-wrapper mb-6">
        <table className="data-table">
          <thead>
            <tr><th>Seller</th><th>Sales</th><th>Revenue</th><th>Platform Fee</th><th>Payout</th></tr>
          </thead>
          <tbody>
            {summary.map(s => (
              <tr key={s.sellerId}>
                <td style={{ fontWeight: 600 }}>{s.displayName}</td>
                <td>{Number(s.salesCount)}</td>
                <td>{formatPrice(Number(s.totalRevenueCents))}</td>
                <td className="text-muted">{formatPrice(Number(s.platformFeeCents))}</td>
                <td style={{ color: 'var(--status-success)' }}>{formatPrice(Number(s.sellerPayoutCents))}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* All purchases */}
      <div className="flex justify-between items-center mb-3">
        <h3 className="section-title" style={{ marginBottom: 0 }}>All Purchases</h3>
        <input className="form-control" style={{ maxWidth: 280 }} placeholder="Filter…"
          value={search} onChange={e => setSearch(e.target.value)} />
      </div>
      <div className="data-table-wrapper">
        <table className="data-table">
          <thead>
            <tr><th>Clip</th><th>Buyer</th><th>License</th><th>Amount</th><th>Status</th><th>Date</th></tr>
          </thead>
          <tbody>
            {filtered.map(p => (
              <tr key={p.id}>
                <td style={{ fontWeight: 600 }}>{p.clipTitle}</td>
                <td className="text-muted">{p.customerEmail ?? '—'}</td>
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
      </div>
    </div>
  );
}
