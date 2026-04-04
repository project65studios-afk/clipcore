'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { admin as adminApi } from '@/lib/api';
import type { SellerProfile } from '@/types';
import Spinner from '@/components/shared/Spinner';
import AdminNav from '@/components/layout/AdminNav';

export default function AdminSellersPage() {
  const [sellers,  setSellers]  = useState<SellerProfile[]>([]);
  const [search,   setSearch]   = useState('');
  const [loading,  setLoading]  = useState(true);

  async function load() {
    setLoading(true);
    try { setSellers(await adminApi.getSellers()); }
    finally { setLoading(false); }
  }
  useEffect(() => { load(); }, []);

  async function approve(id: number) {
    await adminApi.approveSeller(id);
    setSellers(prev => prev.map(s => s.id === id ? { ...s, isTrusted: true } : s));
  }
  async function revoke(id: number) {
    await adminApi.revokeSeller(id);
    setSellers(prev => prev.map(s => s.id === id ? { ...s, isTrusted: false } : s));
  }

  const filtered = sellers.filter(s =>
    !search || s.displayName.toLowerCase().includes(search.toLowerCase()) ||
    s.email.toLowerCase().includes(search.toLowerCase()) ||
    s.slug.toLowerCase().includes(search.toLowerCase())
  );

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Sellers</h1>
          <p className="dashboard-subtitle">Manage seller accounts and approval.</p>
        </div>
      </div>
      <AdminNav />

      <div style={{ maxWidth: 400, marginBottom: '1.5rem' }}>
        <input className="form-control" placeholder="Search sellers…" value={search}
          onChange={e => setSearch(e.target.value)} />
      </div>

      <div className="data-table-wrapper">
        <table className="data-table">
          <thead>
            <tr><th>Name</th><th>Email</th><th>Slug</th><th>Status</th><th>Joined</th><th></th></tr>
          </thead>
          <tbody>
            {filtered.map(s => (
              <tr key={s.id}>
                <td style={{ fontWeight: 600 }}>
                  <Link href={`/store/${s.slug}`} target="_blank" style={{ color: 'var(--accent-primary)' }}>
                    {s.displayName}
                  </Link>
                </td>
                <td className="text-muted">{s.email}</td>
                <td className="text-muted">/store/{s.slug}</td>
                <td>
                  <span className={`badge ${s.isTrusted ? 'badge-success' : 'badge-warning'}`}>
                    {s.isTrusted ? '✓ Verified' : 'Pending'}
                  </span>
                  {!s.isPublished && (
                    <span className="badge badge-danger" style={{ marginLeft: '0.4rem' }}>Unlisted</span>
                  )}
                </td>
                <td className="text-muted" style={{ whiteSpace: 'nowrap' }}>
                  {new Date(s.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                </td>
                <td>
                  {s.isTrusted ? (
                    <button className="btn btn-danger btn-sm" onClick={() => revoke(s.id)}>Revoke</button>
                  ) : (
                    <button className="btn btn-primary btn-sm" onClick={() => approve(s.id)}>Approve</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
