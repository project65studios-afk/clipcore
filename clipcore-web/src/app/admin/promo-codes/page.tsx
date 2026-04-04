'use client';

import React, { useEffect, useState } from 'react';
import { admin as adminApi } from '@/lib/api';
import type { PromoCode } from '@/types';
import Spinner from '@/components/shared/Spinner';
import AdminNav from '@/components/layout/AdminNav';

const EMPTY_FORM = {
  code: '', discountType: 'Percentage' as 'Percentage' | 'Fixed',
  value: 10, expiresAt: '', maxUses: '' as string | number, isActive: true,
};

export default function AdminPromoCodesPage() {
  const [codes,    setCodes]    = useState<PromoCode[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [modal,    setModal]    = useState(false);
  const [form,     setForm]     = useState(EMPTY_FORM);
  const [saving,   setSaving]   = useState(false);
  const [error,    setError]    = useState('');

  async function load() {
    setLoading(true);
    try { setCodes(await adminApi.getPromoCodes()); }
    finally { setLoading(false); }
  }
  useEffect(() => { load(); }, []);

  async function create() {
    setSaving(true); setError('');
    try {
      await adminApi.createPromoCode({
        code:         form.code.toUpperCase(),
        discountType: form.discountType,
        value:        Number(form.value),
        expiresAt:    form.expiresAt || undefined,
        maxUses:      form.maxUses !== '' ? Number(form.maxUses) : undefined,
        isActive:     form.isActive,
      });
      setModal(false);
      setForm(EMPTY_FORM);
      load();
    } catch (err: unknown) { setError((err as Error).message); }
    finally { setSaving(false); }
  }

  async function toggle(id: number, isActive: boolean) {
    await adminApi.togglePromoCode(id, isActive);
    setCodes(prev => prev.map(c => c.id === id ? { ...c, isActive } : c));
  }

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Promo Codes</h1>
          <p className="dashboard-subtitle">Create discount codes for buyers.</p>
        </div>
        <button className="btn btn-primary btn-sm" onClick={() => setModal(true)}>+ New Code</button>
      </div>
      <AdminNav />

      <div className="data-table-wrapper">
        {codes.length === 0 ? (
          <div className="text-center" style={{ padding: '3rem', color: 'var(--text-muted)' }}>No promo codes yet.</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr><th>Code</th><th>Type</th><th>Value</th><th>Uses</th><th>Expires</th><th>Status</th><th></th></tr>
            </thead>
            <tbody>
              {codes.map(c => (
                <tr key={c.id}>
                  <td><code style={{ color: 'var(--accent-primary)', fontWeight: 700 }}>{c.code}</code></td>
                  <td className="text-muted">{c.discountType}</td>
                  <td>{c.discountType === 'Percentage' ? `${c.value}%` : `$${(c.value / 100).toFixed(2)}`}</td>
                  <td className="text-muted">{c.useCount}{c.maxUses ? ` / ${c.maxUses}` : ''}</td>
                  <td className="text-muted">{c.expiresAt ? new Date(c.expiresAt).toLocaleDateString() : '—'}</td>
                  <td>
                    <span className={`badge ${c.isActive ? 'badge-success' : 'badge-danger'}`}>
                      {c.isActive ? 'Active' : 'Disabled'}
                    </span>
                  </td>
                  <td>
                    <button
                      className={`btn btn-sm ${c.isActive ? 'btn-danger' : 'btn-primary'}`}
                      onClick={() => toggle(c.id, !c.isActive)}
                    >
                      {c.isActive ? 'Disable' : 'Enable'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modal && (
        <div className="modal-backdrop" onClick={() => setModal(false)}>
          <div className="modal-box" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">New Promo Code</h2>
              <button className="modal-close" onClick={() => setModal(false)}>✕</button>
            </div>
            {error && <div className="alert alert-danger mb-4">{error}</div>}
            <div className="flex flex-col gap-4">
              <div className="form-group">
                <label className="form-label">Code</label>
                <input type="text" className="form-control" value={form.code}
                  onChange={e => setForm(p => ({ ...p, code: e.target.value.toUpperCase() }))}
                  placeholder="SUMMER25" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div className="form-group">
                  <label className="form-label">Discount Type</label>
                  <select className="form-control" value={form.discountType}
                    onChange={e => setForm(p => ({ ...p, discountType: e.target.value as 'Percentage' | 'Fixed' }))}>
                    <option value="Percentage">Percentage (%)</option>
                    <option value="Fixed">Fixed Amount ($)</option>
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Value</label>
                  <input type="number" className="form-control" min="0" step={form.discountType === 'Percentage' ? '1' : '0.01'}
                    value={form.value} onChange={e => setForm(p => ({ ...p, value: Number(e.target.value) }))} />
                </div>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div className="form-group">
                  <label className="form-label">Expires</label>
                  <input type="date" className="form-control" value={form.expiresAt}
                    onChange={e => setForm(p => ({ ...p, expiresAt: e.target.value }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Max Uses</label>
                  <input type="number" className="form-control" min="1"
                    value={form.maxUses} onChange={e => setForm(p => ({ ...p, maxUses: e.target.value }))}
                    placeholder="Unlimited" />
                </div>
              </div>
              <button className="btn btn-primary" onClick={create} disabled={saving || !form.code}>
                {saving ? 'Creating…' : 'Create Code'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
