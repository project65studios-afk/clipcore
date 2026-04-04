'use client';

import React, { useEffect, useState } from 'react';
import { admin as adminApi } from '@/lib/api';
import Spinner from '@/components/shared/Spinner';
import AdminNav from '@/components/layout/AdminNav';

const KNOWN_SETTINGS = [
  { key: 'StoreName',    label: 'Store Name',             hint: 'Displayed in the site title and footer.' },
  { key: 'WatermarkUrl', label: 'Watermark Image URL',    hint: 'Overlaid on clip previews before purchase.' },
  { key: 'SupportEmail', label: 'Support Email',          hint: 'Shown in order emails and the footer.' },
];

export default function AdminSettingsPage() {
  const [settings, setSettings] = useState<Record<string, string>>({});
  const [loading,  setLoading]  = useState(true);
  const [saving,   setSaving]   = useState<string | null>(null);
  const [success,  setSuccess]  = useState<string | null>(null);

  useEffect(() => {
    adminApi.getSettings()
      .then(setSettings)
      .finally(() => setLoading(false));
  }, []);

  async function save(key: string, value: string) {
    setSaving(key); setSuccess(null);
    try {
      await adminApi.updateSetting(key, value);
      setSuccess(key);
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: unknown) {
      alert('Save failed: ' + (err as Error).message);
    } finally {
      setSaving(null);
    }
  }

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Settings</h1>
          <p className="dashboard-subtitle">Global platform configuration.</p>
        </div>
      </div>
      <AdminNav />

      <div className="card" style={{ padding: '2rem', maxWidth: 600 }}>
        <div className="flex flex-col gap-6">
          {KNOWN_SETTINGS.map(({ key, label, hint }) => (
            <div key={key} className="form-group">
              <label className="form-label">{label}</label>
              {hint && <p style={{ fontSize: 'var(--font-size-xs)', color: 'var(--text-muted)', marginBottom: '0.35rem' }}>{hint}</p>}
              <div className="flex gap-2">
                <input
                  type="text"
                  className="form-control"
                  value={settings[key] ?? ''}
                  onChange={e => setSettings(prev => ({ ...prev, [key]: e.target.value }))}
                />
                <button
                  className="btn btn-primary btn-sm"
                  style={{ whiteSpace: 'nowrap' }}
                  disabled={saving === key}
                  onClick={() => save(key, settings[key] ?? '')}
                >
                  {saving === key ? '…' : success === key ? '✓' : 'Save'}
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
