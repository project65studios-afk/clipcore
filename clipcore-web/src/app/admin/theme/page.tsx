'use client';

import React, { useState } from 'react';
import { admin as adminApi } from '@/lib/api';
import { useTheme, CONFIGURABLE_VARS } from '@/lib/theme';
import AdminNav from '@/components/layout/AdminNav';

// Default CSS values for dark and light themes
const DARK_DEFAULTS: Record<string, string> = {
  'bg-main':    '#0f1129', 'bg-surface': '#171936', 'bg-subtle': '#1e204a',
  'accent-primary': '#06b6d4', 'accent-secondary': '#ec4899', 'accent-purple': '#a855f7',
  'aurora-color-1': '#06b6d4', 'aurora-color-2': '#6366f1',
  'aurora-color-3': '#ec4899', 'aurora-color-4': '#a855f7',
  'text-main': '#e2e8f0', 'text-secondary': '#94a3b8', 'text-muted': '#64748b',
  'nav-bg': 'rgba(15, 17, 41, 0.88)', 'nav-link': '#94a3b8',
  'status-success': '#10b981', 'status-danger': '#ef4444', 'status-warning': '#f59e0b',
};
const LIGHT_DEFAULTS: Record<string, string> = {
  'bg-main':    '#f8fafc', 'bg-surface': '#ffffff', 'bg-subtle': '#f1f5f9',
  'accent-primary': '#06b6d4', 'accent-secondary': '#ec4899', 'accent-purple': '#a855f7',
  'aurora-color-1': '#0891b2', 'aurora-color-2': '#4f46e5',
  'aurora-color-3': '#db2777', 'aurora-color-4': '#9333ea',
  'text-main': '#0f172a', 'text-secondary': '#475569', 'text-muted': '#94a3b8',
  'nav-bg': 'rgba(248, 250, 252, 0.92)', 'nav-link': '#475569',
  'status-success': '#10b981', 'status-danger': '#ef4444', 'status-warning': '#f59e0b',
};

export default function AdminThemePage() {
  const { mode, setMode, overrides, setOverride, resetOverrides, applyOverrides } = useTheme();
  const [editMode, setEditMode] = useState<'dark' | 'light'>(mode);
  const [saving,   setSaving]   = useState(false);
  const [saved,    setSaved]    = useState(false);

  const defaults = editMode === 'dark' ? DARK_DEFAULTS : LIGHT_DEFAULTS;

  async function save() {
    setSaving(true); setSaved(false);
    try {
      await adminApi.saveTheme(overrides.dark, overrides.light);
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (err: unknown) {
      alert('Failed to save theme: ' + (err as Error).message);
    } finally {
      setSaving(false);
    }
  }

  function reset() {
    if (confirm(`Reset ${editMode} theme overrides to defaults?`)) {
      resetOverrides(editMode);
    }
  }

  // Group vars by group
  const groups = [...new Set(CONFIGURABLE_VARS.map(v => v.group))];

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Theme Editor</h1>
          <p className="dashboard-subtitle">Customise CSS variables for dark and light modes.</p>
        </div>
        <div className="flex gap-2">
          <button className="btn btn-outline btn-sm" onClick={reset}>Reset to Defaults</button>
          <button className="btn btn-primary btn-sm" onClick={save} disabled={saving}>
            {saving ? 'Saving…' : saved ? '✓ Saved' : 'Save Theme'}
          </button>
        </div>
      </div>

      <AdminNav />

      {/* Mode picker */}
      <div className="flex gap-2 mb-6">
        <button
          className={`btn ${editMode === 'dark'  ? 'btn-primary' : 'btn-outline'}`}
          onClick={() => setEditMode('dark')}
        >🌙 Dark Mode (Aurora Night)</button>
        <button
          className={`btn ${editMode === 'light' ? 'btn-primary' : 'btn-outline'}`}
          onClick={() => setEditMode('light')}
        >☀️ Light Mode (Daybreak)</button>
      </div>

      {/* Live preview note */}
      <div className="alert alert-info mb-6">
        Changes apply live to the current page. Click &quot;Save Theme&quot; to persist across sessions.
        <button className="btn btn-ghost btn-sm" style={{ marginLeft: '0.75rem' }} onClick={() => setMode(editMode)}>
          Switch to {editMode} mode
        </button>
      </div>

      {/* Variable groups */}
      {groups.map(group => (
        <div key={group} className="card" style={{ padding: '1.5rem', marginBottom: '1.25rem' }}>
          <h3 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '1rem', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.06em' }}>
            {group}
          </h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: '1rem' }}>
            {CONFIGURABLE_VARS.filter(v => v.group === group).map(v => {
              const current = overrides[editMode][v.key] ?? defaults[v.key] ?? '';
              const isColor = !current.startsWith('rgba') && !current.startsWith('rgb') && (
                current.startsWith('#') || CSS.supports('color', current)
              );
              return (
                <div key={v.key}>
                  <label style={{ fontSize: 'var(--font-size-xs)', fontWeight: 600, color: 'var(--text-muted)', display: 'block', marginBottom: '0.35rem' }}>
                    {v.label}
                  </label>
                  <div className="flex items-center gap-2">
                    {isColor && (
                      <input
                        type="color"
                        value={current.startsWith('#') ? current : '#000000'}
                        onChange={e => setOverride(editMode, v.key, e.target.value)}
                        style={{ width: 32, height: 32, padding: 2, borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-color)', cursor: 'pointer', background: 'var(--bg-subtle)', flexShrink: 0 }}
                      />
                    )}
                    <input
                      type="text"
                      className="form-control"
                      style={{ fontSize: 'var(--font-size-xs)' }}
                      value={current}
                      placeholder={defaults[v.key]}
                      onChange={e => setOverride(editMode, v.key, e.target.value)}
                    />
                  </div>
                  <div style={{ fontSize: '0.65rem', color: 'var(--text-muted)', marginTop: '0.2rem', fontFamily: 'monospace' }}>
                    --{v.key}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
