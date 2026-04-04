'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { seller as sellerApi, formatPrice } from '@/lib/api';
import type { SellerProfile, SellerSalesStats } from '@/types';
import Spinner from '@/components/shared/Spinner';
import SellerNav from '@/components/layout/SellerNav';

export default function SellerDashboardPage() {
  const [profile, setProfile]  = useState<SellerProfile | null>(null);
  const [stats,   setStats]    = useState<SellerSalesStats | null>(null);
  const [loading, setLoading]  = useState(true);

  useEffect(() => {
    Promise.all([sellerApi.getProfile(), sellerApi.getSalesStats()])
      .then(([p, s]) => { setProfile(p); setStats(s); })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      {/* Header */}
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Seller Dashboard</h1>
          <p className="dashboard-subtitle">Welcome back. Here&apos;s your store at a glance.</p>
        </div>
        <Link href="/seller/upload" className="btn btn-primary btn-sm">
          ☁ Upload Clips
        </Link>
      </div>

      <SellerNav />

      {/* Stats */}
      {stats && (
        <div className="stats-grid">
          <div className="stat-card">
            <div className="stat-value">{stats.totalSales}</div>
            <div className="stat-label">Total Sales</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{formatPrice(stats.totalRevenueCents)}</div>
            <div className="stat-label">Total Revenue</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{formatPrice(stats.totalPayoutCents)}</div>
            <div className="stat-label">Your Earnings</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{stats.pendingFulfillment}</div>
            <div className="stat-label">Pending Orders</div>
          </div>
        </div>
      )}

      {/* Quick links */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: '1rem' }}>
        <Link href="/seller/upload" className="card" style={{ padding: '1.5rem', textDecoration: 'none', transition: 'var(--transition-base)' }}
          onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--accent-primary)')}
          onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border-color)')}>
          <div style={{ fontSize: '2rem', marginBottom: '0.5rem' }}>📤</div>
          <div style={{ fontWeight: 700 }}>Upload Clips</div>
          <div style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>Add new footage</div>
        </Link>
        <Link href="/seller/collections" className="card" style={{ padding: '1.5rem', textDecoration: 'none', transition: 'var(--transition-base)' }}
          onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--accent-primary)')}
          onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border-color)')}>
          <div style={{ fontSize: '2rem', marginBottom: '0.5rem' }}>📁</div>
          <div style={{ fontWeight: 700 }}>My Collections</div>
          <div style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>Organise your events</div>
        </Link>
        <Link href="/seller/storefront" className="card" style={{ padding: '1.5rem', textDecoration: 'none', transition: 'var(--transition-base)' }}
          onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--accent-primary)')}
          onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border-color)')}>
          <div style={{ fontSize: '2rem', marginBottom: '0.5rem' }}>🏪</div>
          <div style={{ fontWeight: 700 }}>Your Storefront</div>
          <div style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>Customise your page</div>
        </Link>
        {profile?.slug && (
          <Link href={`/store/${profile.slug}`} target="_blank" className="card" style={{ padding: '1.5rem', textDecoration: 'none', transition: 'var(--transition-base)' }}
            onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--accent-primary)')}
            onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border-color)')}>
            <div style={{ fontSize: '2rem', marginBottom: '0.5rem' }}>🔗</div>
            <div style={{ fontWeight: 700 }}>Preview Store</div>
            <div style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>clipcore.com/store/{profile.slug}</div>
          </Link>
        )}
      </div>
    </div>
  );
}
