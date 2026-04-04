'use client';

import React, { useState, useEffect, useRef } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useAuth } from '@/lib/auth';
import { useCart } from '@/lib/cart';
import { useTheme } from '@/lib/theme';

export default function TopNav() {
  const pathname           = usePathname();
  const router             = useRouter();
  const { user, logout }   = useAuth();
  const { count }          = useCart();
  const { mode, toggleMode } = useTheme();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [search, setSearch]         = useState('');
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // Close user menu on outside click
  useEffect(() => {
    function handler(e: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  // Close mobile nav on route change
  useEffect(() => { setMobileOpen(false); }, [pathname]);

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    if (search.trim()) router.push(`/search?q=${encodeURIComponent(search.trim())}`);
  }

  const isActive = (href: string) =>
    pathname === href ? 'nav-link nav-link-active' : 'nav-link';

  return (
    <>
      <nav className="top-nav">
        {/* Brand */}
        <Link href="/" className="nav-brand">ClipCore</Link>

        {/* Search */}
        <form className="nav-search" onSubmit={handleSearch}>
          <span className="nav-search-icon">🔍</span>
          <input
            type="text"
            placeholder="Search footage..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </form>

        {/* Desktop links */}
        <div className="nav-links">
          <Link href="/" className={isActive('/')}>Home</Link>

          {user?.role === 'Admin' && (
            <Link href="/admin" className={isActive('/admin')}>Admin</Link>
          )}
          {(user?.role === 'Seller' || user?.role === 'Admin') && (
            <Link href="/seller/dashboard" className={isActive('/seller/dashboard')}>Dashboard</Link>
          )}
          {user && (
            <Link href="/my-purchases" className={isActive('/my-purchases')}>My Purchases</Link>
          )}

          {/* Theme toggle */}
          <button
            className="theme-toggle-btn"
            onClick={toggleMode}
            title={`Switch to ${mode === 'dark' ? 'light' : 'dark'} mode`}
            aria-label="Toggle theme"
          >
            {mode === 'dark' ? '☀️' : '🌙'}
          </button>

          {/* Cart */}
          <Link href="/cart" className="cart-btn" aria-label={`Cart (${count} items)`}>
            🛒
            {count > 0 && <span className="cart-count">{count > 9 ? '9+' : count}</span>}
          </Link>

          {/* Auth */}
          {user ? (
            <div style={{ position: 'relative' }} ref={userMenuRef}>
              <button
                className="btn btn-outline btn-sm"
                onClick={() => setUserMenuOpen(o => !o)}
                style={{ gap: '0.4rem' }}
              >
                👤 {user.email.split('@')[0]}
              </button>
              {userMenuOpen && (
                <div style={{
                  position: 'absolute', top: 'calc(100% + 8px)', right: 0,
                  background: 'var(--bg-surface)', border: '1px solid var(--border-color)',
                  borderRadius: 'var(--radius-md)', padding: '0.5rem',
                  minWidth: '180px', boxShadow: 'var(--shadow-md)', zIndex: 200,
                }}>
                  {user.role === 'Seller' || user.role === 'Admin' ? (
                    <Link href="/seller/dashboard" className="nav-link" style={{ display: 'block', padding: '0.5rem 0.75rem' }}>
                      Seller Dashboard
                    </Link>
                  ) : null}
                  {user.role === 'Admin' && (
                    <Link href="/admin" className="nav-link" style={{ display: 'block', padding: '0.5rem 0.75rem' }}>
                      Admin Portal
                    </Link>
                  )}
                  <Link href="/my-purchases" className="nav-link" style={{ display: 'block', padding: '0.5rem 0.75rem' }}>
                    My Purchases
                  </Link>
                  <hr style={{ border: 'none', borderTop: '1px solid var(--border-subtle)', margin: '0.35rem 0' }} />
                  <button
                    onClick={() => { logout(); setUserMenuOpen(false); }}
                    className="nav-link"
                    style={{ display: 'block', width: '100%', textAlign: 'left',
                             background: 'none', border: 'none', cursor: 'pointer',
                             padding: '0.5rem 0.75rem', color: 'var(--status-danger)' }}
                  >
                    Sign Out
                  </button>
                </div>
              )}
            </div>
          ) : (
            <>
              <Link href="/auth/login"    className="nav-link">Sign In</Link>
              <Link href="/seller/register" className="btn btn-primary btn-sm">Sell Footage</Link>
            </>
          )}
        </div>

        {/* Hamburger */}
        <button
          className="hamburger-btn"
          onClick={() => setMobileOpen(o => !o)}
          aria-label="Toggle menu"
          style={{ marginLeft: 'auto' }}
        >
          <span />
          <span />
          <span />
        </button>
      </nav>

      {/* Mobile nav */}
      {mobileOpen && (
        <nav className="mobile-nav open">
          <form onSubmit={handleSearch} style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <input
              className="form-control"
              type="text"
              placeholder="Search footage..."
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
            <button type="submit" className="btn btn-primary btn-sm">Go</button>
          </form>

          <Link href="/" className={isActive('/')}>🏠 Home</Link>
          {user && <Link href="/my-purchases" className="nav-link">🎬 My Purchases</Link>}
          {(user?.role === 'Seller' || user?.role === 'Admin') && (
            <Link href="/seller/dashboard" className="nav-link">📊 Dashboard</Link>
          )}
          {user?.role === 'Admin' && (
            <Link href="/admin" className="nav-link">⚙️ Admin</Link>
          )}
          <Link href="/cart" className="nav-link">🛒 Cart {count > 0 && `(${count})`}</Link>

          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', padding: '0.5rem 0.75rem' }}>
            <span style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>
              {mode === 'dark' ? 'Dark Mode' : 'Light Mode'}
            </span>
            <button className="theme-toggle-btn" onClick={toggleMode}>
              {mode === 'dark' ? '☀️' : '🌙'}
            </button>
          </div>

          <hr style={{ border: 'none', borderTop: '1px solid var(--border-subtle)', margin: '0.25rem 0' }} />
          {user ? (
            <button
              onClick={logout}
              className="nav-link"
              style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--status-danger)', textAlign: 'left' }}
            >
              Sign Out ({user.email})
            </button>
          ) : (
            <>
              <Link href="/auth/login" className="nav-link">Sign In</Link>
              <Link href="/seller/register" className="nav-link" style={{ color: 'var(--accent-primary)' }}>
                Sell Footage
              </Link>
            </>
          )}
        </nav>
      )}
    </>
  );
}
