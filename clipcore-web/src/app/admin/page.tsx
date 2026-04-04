'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { admin as adminApi, formatPrice } from '@/lib/api';
import type { PlatformStats, PurchaseDetail } from '@/types';
import Spinner from '@/components/shared/Spinner';
import AdminNav from '@/components/layout/AdminNav';

export default function AdminPortalPage() {
  const [stats,   setStats]   = useState<PlatformStats | null>(null);
  const [recent,  setRecent]  = useState<PurchaseDetail[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([adminApi.getPlatformStats(), adminApi.getRecentSales(10)])
      .then(([s, r]) => { setStats(s); setRecent(r); })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Admin Portal</h1>
          <p className="dashboard-subtitle">Platform overview and management.</p>
        </div>
      </div>

      <AdminNav />

      {/* Platform stats */}
      {stats && (
        <div className="stats-grid mb-6">
          <div className="stat-card">
            <div className="stat-value">{stats.totalSellers}</div>
            <div className="stat-label">Sellers</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{stats.totalClips}</div>
            <div className="stat-label">Total Clips</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{stats.totalPurchases}</div>
            <div className="stat-label">Total Sales</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{formatPrice(stats.totalRevenueCents)}</div>
            <div className="stat-label">Platform Revenue</div>
          </div>
        </div>
      )}

      {/* Quick links */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: '1rem', marginBottom: '2rem' }}>
        {[
          { href: '/admin/sellers',     label: 'Manage Sellers',  icon: '👤' },
          { href: '/admin/sales',       label: 'Sales Report',    icon: '📊' },
          { href: '/admin/promo-codes', label: 'Promo Codes',     icon: '🎟' },
          { href: '/admin/theme',       label: 'Theme Editor',    icon: '🎨' },
          { href: '/admin/settings',    label: 'Settings',        icon: '⚙️' },
          { href: '/admin/audit-logs',  label: 'Audit Logs',      icon: '📋' },
        ].map(l => (
          <Link key={l.href} href={l.href} className="card" style={{ padding: '1.25rem', textDecoration: 'none', transition: 'var(--transition-base)' }}
            onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--accent-primary)')}
            onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border-color)')}>
            <div style={{ fontSize: '1.75rem', marginBottom: '0.4rem' }}>{l.icon}</div>
            <div style={{ fontWeight: 600, fontSize: 'var(--font-size-sm)' }}>{l.label}</div>
          </Link>
        ))}
      </div>

      {/* Recent sales */}
      <h3 className="section-title">Recent Sales</h3>
      <div className="data-table-wrapper">
        {recent.length === 0 ? (
          <div className="text-center" style={{ padding: '2rem', color: 'var(--text-muted)' }}>No sales yet.</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr><th>Clip</th><th>Buyer</th><th>License</th><th>Amount</th><th>Platform Fee</th><th>Date</th></tr>
            </thead>
            <tbody>
              {recent.map(p => (
                <tr key={p.id}>
                  <td style={{ fontWeight: 600 }}>{p.clipTitle}</td>
                  <td className="text-muted">{p.customerEmail ?? '—'}</td>
                  <td>
                    <span className={`badge ${p.isGif ? 'badge-secondary' : p.licenseType === 'Commercial' ? 'badge-purple' : 'badge-primary'}`}>
                      {p.isGif ? 'GIF' : p.licenseType}
                    </span>
                  </td>
                  <td style={{ fontWeight: 700, color: 'var(--accent-primary)' }}>{formatPrice(p.pricePaidCents)}</td>
                  <td>{formatPrice(p.platformFeeCents)}</td>
                  <td className="text-muted" style={{ whiteSpace: 'nowrap' }}>
                    {new Date(p.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
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
