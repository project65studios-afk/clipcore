'use client';

import React, { useEffect, useState } from 'react';
import { collections as collectionsApi } from '@/lib/api';
import type { CollectionItem, CreateCollectionRequest } from '@/types';
import Spinner from '@/components/shared/Spinner';
import SellerNav from '@/components/layout/SellerNav';

const DEFAULT_FORM: CreateCollectionRequest = {
  name: '', date: new Date().toISOString().slice(0, 10),
  location: '', summary: '',
  defaultPriceCents: 1000, defaultPriceCommercialCents: 4900,
  defaultAllowGifSale: false, defaultGifPriceCents: 199,
};

export default function SellerCollectionsPage() {
  const [items,    setItems]    = useState<CollectionItem[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [modal,    setModal]    = useState<'create' | 'edit' | null>(null);
  const [editing,  setEditing]  = useState<CollectionItem | null>(null);
  const [form,     setForm]     = useState(DEFAULT_FORM);
  const [saving,   setSaving]   = useState(false);
  const [error,    setError]    = useState('');
  const [success,  setSuccess]  = useState('');

  async function load() {
    setLoading(true);
    try { setItems(await collectionsApi.list()); }
    finally { setLoading(false); }
  }
  useEffect(() => { load(); }, []);

  function openCreate() {
    setForm(DEFAULT_FORM);
    setEditing(null);
    setModal('create');
    setError('');
  }
  function openEdit(c: CollectionItem) {
    setForm({
      name: c.name, date: c.date, location: c.location ?? '', summary: c.summary ?? '',
      defaultPriceCents: c.defaultPriceCents,
      defaultPriceCommercialCents: c.defaultPriceCommercialCents,
      defaultAllowGifSale: c.defaultAllowGifSale,
      defaultGifPriceCents: c.defaultGifPriceCents,
    });
    setEditing(c);
    setModal('edit');
    setError('');
  }
  function f(key: keyof typeof form) {
    return {
      value: form[key] as string | number | boolean,
      onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        const val = e.target.type === 'checkbox'
          ? (e.target as HTMLInputElement).checked
          : e.target.type === 'number'
          ? Number(e.target.value)
          : e.target.value;
        setForm(prev => ({ ...prev, [key]: val }));
      },
    };
  }

  async function save() {
    setSaving(true); setError('');
    try {
      if (editing) {
        await collectionsApi.update({ ...form, collectionId: editing.id });
        setSuccess('Collection updated.');
      } else {
        await collectionsApi.create(form);
        setSuccess('Collection created.');
      }
      setModal(null);
      load();
    } catch (err: unknown) { setError((err as Error).message); }
    finally { setSaving(false); }
  }

  async function deleteCollection(id: string) {
    if (!confirm('Delete this collection and all its clips? This cannot be undone.')) return;
    try {
      await collectionsApi.delete(id);
      setSuccess('Collection deleted.');
      load();
    } catch (err: unknown) { alert((err as Error).message); }
  }

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">My Collections</h1>
          <p className="dashboard-subtitle">Organise clips into events or shoots.</p>
        </div>
        <button className="btn btn-primary btn-sm" onClick={openCreate}>+ New Collection</button>
      </div>

      <SellerNav />

      {success && <div className="alert alert-success mb-4">{success}</div>}

      <div className="data-table-wrapper">
        {items.length === 0 ? (
          <div className="text-center" style={{ padding: '3rem', color: 'var(--text-muted)' }}>
            No collections yet. Create your first one!
          </div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th><th>Date</th><th>Location</th><th>Clips</th><th>Default Price</th><th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(c => (
                <tr key={c.id}>
                  <td style={{ fontWeight: 600 }}>{c.name}</td>
                  <td>{new Date(c.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}</td>
                  <td>{c.location ?? '—'}</td>
                  <td>{c.clipCount}</td>
                  <td>${(c.defaultPriceCents / 100).toFixed(2)}</td>
                  <td>
                    <div className="flex gap-2">
                      <button className="btn btn-outline btn-sm" onClick={() => openEdit(c)}>Edit</button>
                      <button className="btn btn-danger btn-sm" onClick={() => deleteCollection(c.id)}>Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Modal */}
      {modal && (
        <div className="modal-backdrop" onClick={() => setModal(null)}>
          <div className="modal-box" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">{editing ? 'Edit Collection' : 'New Collection'}</h2>
              <button className="modal-close" onClick={() => setModal(null)}>✕</button>
            </div>

            {error && <div className="alert alert-danger mb-4">{error}</div>}

            <div className="flex flex-col gap-4">
              <div className="form-group">
                <label className="form-label">Name *</label>
                <input type="text" className="form-control" required {...f('name') as React.InputHTMLAttributes<HTMLInputElement>} />
              </div>
              <div className="form-group">
                <label className="form-label">Event Date *</label>
                <input type="date" className="form-control" required {...f('date') as React.InputHTMLAttributes<HTMLInputElement>} />
              </div>
              <div className="form-group">
                <label className="form-label">Location</label>
                <input type="text" className="form-control" {...f('location') as React.InputHTMLAttributes<HTMLInputElement>} placeholder="e.g. Laguna Seca" />
              </div>
              <div className="form-group">
                <label className="form-label">Summary</label>
                <textarea className="form-control" rows={3} value={form.summary} onChange={e => setForm(p => ({ ...p, summary: e.target.value }))} />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div className="form-group">
                  <label className="form-label">Default Price ($)</label>
                  <input type="number" className="form-control" step="0.01" min="0"
                    value={form.defaultPriceCents / 100}
                    onChange={e => setForm(p => ({ ...p, defaultPriceCents: Math.round(Number(e.target.value) * 100) }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Commercial Price ($)</label>
                  <input type="number" className="form-control" step="0.01" min="0"
                    value={form.defaultPriceCommercialCents / 100}
                    onChange={e => setForm(p => ({ ...p, defaultPriceCommercialCents: Math.round(Number(e.target.value) * 100) }))} />
                </div>
              </div>
              <label className="flex items-center gap-2" style={{ cursor: 'pointer' }}>
                <input type="checkbox" checked={form.defaultAllowGifSale}
                  onChange={e => setForm(p => ({ ...p, defaultAllowGifSale: e.target.checked }))} />
                <span style={{ fontSize: 'var(--font-size-sm)' }}>Allow GIF Sales</span>
              </label>
              {form.defaultAllowGifSale && (
                <div className="form-group">
                  <label className="form-label">GIF Price ($)</label>
                  <input type="number" className="form-control" step="0.01" min="0"
                    value={form.defaultGifPriceCents / 100}
                    onChange={e => setForm(p => ({ ...p, defaultGifPriceCents: Math.round(Number(e.target.value) * 100) }))} />
                </div>
              )}
              <button className="btn btn-primary" onClick={save} disabled={saving}>
                {saving ? 'Saving…' : editing ? 'Update Collection' : 'Create Collection'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
