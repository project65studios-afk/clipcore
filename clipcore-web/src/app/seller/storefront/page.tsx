'use client';

import React, { useEffect, useState } from 'react';
import { seller as sellerApi, storage as storageApi } from '@/lib/api';
import type { SellerProfile, StorefrontSettingsRequest } from '@/types';
import Spinner from '@/components/shared/Spinner';
import SellerNav from '@/components/layout/SellerNav';

export default function SellerStorefrontPage() {
  const [profile,  setProfile]  = useState<SellerProfile | null>(null);
  const [form,     setForm]     = useState<StorefrontSettingsRequest>({
    displayName: '', logoUrl: '', bannerUrl: '', accentColor: '', bio: '', isPublished: false,
  });
  const [loading,  setLoading]  = useState(true);
  const [saving,   setSaving]   = useState(false);
  const [success,  setSuccess]  = useState('');
  const [error,    setError]    = useState('');

  useEffect(() => {
    sellerApi.getProfile().then(p => {
      setProfile(p);
      setForm({
        displayName: p.displayName,
        logoUrl:     p.logoUrl ?? '',
        bannerUrl:   p.bannerUrl ?? '',
        accentColor: p.accentColor ?? '#06b6d4',
        bio:         p.bio ?? '',
        isPublished: p.isPublished,
      });
    }).finally(() => setLoading(false));
  }, []);

  function field(key: keyof StorefrontSettingsRequest) {
    return {
      value: form[key] as string,
      onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
        setForm(prev => ({ ...prev, [key]: e.target.value })),
    };
  }

  async function uploadImage(file: File, field: 'logoUrl' | 'bannerUrl') {
    try {
      const { url, key } = await storageApi.getUploadUrl(file.name, file.type);
      await fetch(url, { method: 'PUT', body: file, headers: { 'Content-Type': file.type } });
      // Derive public URL from key (R2 public bucket or presigned)
      const publicUrl = url.split('?')[0];
      setForm(prev => ({ ...prev, [field]: publicUrl }));
    } catch (err: unknown) {
      alert('Image upload failed: ' + (err as Error).message);
    }
  }

  async function save() {
    setSaving(true); setError(''); setSuccess('');
    try {
      await sellerApi.updateStorefront(form);
      setSuccess('Storefront updated successfully.');
    } catch (err: unknown) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  }

  if (loading || !profile) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Your Storefront</h1>
          <p className="dashboard-subtitle">Customise how your store looks to buyers.</p>
        </div>
        {profile.slug && (
          <a href={`/store/${profile.slug}`} target="_blank" rel="noreferrer" className="btn btn-outline btn-sm">
            Preview →
          </a>
        )}
      </div>

      <SellerNav />

      {success && <div className="alert alert-success mb-4">{success}</div>}
      {error   && <div className="alert alert-danger  mb-4">{error}</div>}

      <div className="card" style={{ padding: '2rem', maxWidth: 640 }}>
        <div className="flex flex-col gap-5">
          {/* Published toggle */}
          <label className="flex items-center gap-3" style={{ cursor: 'pointer' }}>
            <input type="checkbox" checked={form.isPublished}
              onChange={e => setForm(p => ({ ...p, isPublished: e.target.checked }))}
              style={{ width: 18, height: 18, accentColor: 'var(--accent-primary)' }} />
            <div>
              <div style={{ fontWeight: 600 }}>Published</div>
              <div style={{ fontSize: 'var(--font-size-xs)', color: 'var(--text-muted)' }}>Make your storefront visible to the public.</div>
            </div>
          </label>

          <div className="form-group">
            <label className="form-label">Display Name *</label>
            <input type="text" className="form-control" required {...field('displayName')} />
          </div>

          <div className="form-group">
            <label className="form-label">Bio / Description</label>
            <textarea className="form-control" rows={3} {...field('bio') as React.TextareaHTMLAttributes<HTMLTextAreaElement>} />
          </div>

          <div className="form-group">
            <label className="form-label">Accent Colour</label>
            <div className="flex items-center gap-3">
              <input type="color" value={form.accentColor ?? '#06b6d4'}
                onChange={e => setForm(p => ({ ...p, accentColor: e.target.value }))}
                style={{ width: 48, height: 36, padding: 2, borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-color)', cursor: 'pointer', background: 'var(--bg-subtle)' }} />
              <span style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>{form.accentColor}</span>
            </div>
          </div>

          {/* Logo upload */}
          <div className="form-group">
            <label className="form-label">Logo</label>
            {form.logoUrl && (
              <img src={form.logoUrl} alt="Logo" style={{ width: 80, height: 80, borderRadius: '50%', objectFit: 'cover', marginBottom: '0.5rem', border: '2px solid var(--border-color)' }} />
            )}
            <input type="text" className="form-control" {...field('logoUrl')} placeholder="Paste image URL or upload below" />
            <input type="file" accept="image/*" style={{ marginTop: '0.4rem', fontSize: 'var(--font-size-xs)', color: 'var(--text-muted)' }}
              onChange={e => e.target.files?.[0] && uploadImage(e.target.files[0], 'logoUrl')} />
          </div>

          {/* Banner upload */}
          <div className="form-group">
            <label className="form-label">Banner Image</label>
            {form.bannerUrl && (
              <img src={form.bannerUrl} alt="Banner" style={{ width: '100%', height: 120, objectFit: 'cover', borderRadius: 'var(--radius-md)', marginBottom: '0.5rem' }} />
            )}
            <input type="text" className="form-control" {...field('bannerUrl')} placeholder="Paste banner URL or upload below" />
            <input type="file" accept="image/*" style={{ marginTop: '0.4rem', fontSize: 'var(--font-size-xs)', color: 'var(--text-muted)' }}
              onChange={e => e.target.files?.[0] && uploadImage(e.target.files[0], 'bannerUrl')} />
          </div>

          <button className="btn btn-primary" onClick={save} disabled={saving}>
            {saving ? 'Saving…' : 'Save Changes'}
          </button>
        </div>
      </div>
    </div>
  );
}
