'use client';

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

type ThemeMode = 'dark' | 'light';

// Variables that users/admins can configure per theme.
// Keys match the CSS custom property names (without the --).
export const CONFIGURABLE_VARS: Array<{ key: string; label: string; group: string }> = [
  // Backgrounds
  { key: 'bg-main',    label: 'Main Background',    group: 'Backgrounds' },
  { key: 'bg-surface', label: 'Surface Background', group: 'Backgrounds' },
  { key: 'bg-subtle',  label: 'Subtle Background',  group: 'Backgrounds' },
  // Accents
  { key: 'accent-primary',    label: 'Primary Accent',    group: 'Accents' },
  { key: 'accent-secondary',  label: 'Secondary Accent',  group: 'Accents' },
  { key: 'accent-purple',     label: 'Purple Accent',     group: 'Accents' },
  // Aurora
  { key: 'aurora-color-1', label: 'Aurora Color 1', group: 'Aurora' },
  { key: 'aurora-color-2', label: 'Aurora Color 2', group: 'Aurora' },
  { key: 'aurora-color-3', label: 'Aurora Color 3', group: 'Aurora' },
  { key: 'aurora-color-4', label: 'Aurora Color 4', group: 'Aurora' },
  // Text
  { key: 'text-main',      label: 'Main Text',      group: 'Text' },
  { key: 'text-secondary', label: 'Secondary Text', group: 'Text' },
  { key: 'text-muted',     label: 'Muted Text',     group: 'Text' },
  // Nav
  { key: 'nav-bg',         label: 'Nav Background', group: 'Navigation' },
  { key: 'nav-link',       label: 'Nav Link Color', group: 'Navigation' },
  // Status
  { key: 'status-success', label: 'Success Color', group: 'Status' },
  { key: 'status-danger',  label: 'Danger Color',  group: 'Status' },
  { key: 'status-warning', label: 'Warning Color', group: 'Status' },
];

const THEME_KEY    = 'cc_theme_mode';
const OVERRIDES_KEY = 'cc_theme_overrides';

interface ThemeOverrides {
  dark:  Record<string, string>;
  light: Record<string, string>;
}

interface ThemeContextValue {
  mode: ThemeMode;
  toggleMode: () => void;
  setMode: (m: ThemeMode) => void;
  overrides: ThemeOverrides;
  setOverride: (mode: ThemeMode, key: string, value: string) => void;
  resetOverrides: (mode: ThemeMode) => void;
  applyOverrides: (overrides: ThemeOverrides) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [mode, setModeState]       = useState<ThemeMode>('dark');
  const [overrides, setOverrides]  = useState<ThemeOverrides>({ dark: {}, light: {} });

  // Initialise from localStorage
  useEffect(() => {
    const savedMode = localStorage.getItem(THEME_KEY) as ThemeMode | null;
    if (savedMode === 'light' || savedMode === 'dark') setModeState(savedMode);

    try {
      const raw = localStorage.getItem(OVERRIDES_KEY);
      if (raw) setOverrides(JSON.parse(raw));
    } catch {}
  }, []);

  // Apply data-theme attribute
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', mode);
    localStorage.setItem(THEME_KEY, mode);
  }, [mode]);

  // Inject custom property overrides as inline style on <html>
  useEffect(() => {
    const vars = overrides[mode];
    const style = Object.entries(vars)
      .map(([k, v]) => `--${k}: ${v}`)
      .join('; ');
    document.documentElement.style.cssText = style;
    localStorage.setItem(OVERRIDES_KEY, JSON.stringify(overrides));
  }, [overrides, mode]);

  const toggleMode = useCallback(() =>
    setModeState(m => (m === 'dark' ? 'light' : 'dark')), []);

  const setMode = useCallback((m: ThemeMode) => setModeState(m), []);

  const setOverride = useCallback((m: ThemeMode, key: string, value: string) => {
    setOverrides(prev => ({
      ...prev,
      [m]: { ...prev[m], [key]: value },
    }));
  }, []);

  const resetOverrides = useCallback((m: ThemeMode) => {
    setOverrides(prev => ({ ...prev, [m]: {} }));
  }, []);

  const applyOverrides = useCallback((o: ThemeOverrides) => {
    setOverrides(o);
  }, []);

  return (
    <ThemeContext.Provider value={{
      mode, toggleMode, setMode,
      overrides, setOverride, resetOverrides, applyOverrides,
    }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used inside ThemeProvider');
  return ctx;
}
