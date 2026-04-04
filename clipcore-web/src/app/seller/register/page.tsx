'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { auth as authApi } from '@/lib/api';
import { useAuth } from '@/lib/auth';

export default function SellerRegisterPage() {
  const router        = useRouter();
  const { login }     = useAuth();

  const [form, setForm] = useState({
    email: '', password: '', confirmPassword: '',
    displayName: '', slug: '',
  });
  const [error,   setError]   = useState('');
  const [loading, setLoading] = useState(false);

  function field(key: keyof typeof form) {
    return {
      value: form[key],
      onChange: (e: React.ChangeEvent<HTMLInputElement>) =>
        setForm(f => ({ ...f, [key]: e.target.value })),
    };
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');

    if (form.password !== form.confirmPassword) {
      setError('Passwords do not match.'); return;
    }
    if (form.slug && !/^[a-z0-9-]+$/.test(form.slug)) {
      setError('Slug may only contain lowercase letters, numbers, and hyphens.'); return;
    }

    setLoading(true);
    try {
      await authApi.registerSeller(form.email, form.password, form.displayName, form.slug);
      await login(form.email, form.password);
      router.push('/seller/dashboard');
    } catch (err: unknown) {
      setError((err as Error).message ?? 'Registration failed. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 480, margin: '0 auto', paddingTop: '2rem' }}>
      <div className="card" style={{ padding: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.35rem' }}>Become a Seller</h1>
        <p className="text-muted" style={{ marginBottom: '1.75rem', fontSize: 'var(--font-size-sm)' }}>
          Create your storefront and start selling footage.
        </p>

        {error && <div className="alert alert-danger mb-4">{error}</div>}

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="form-group">
            <label className="form-label" htmlFor="displayName">Storefront Name</label>
            <input id="displayName" type="text" className="form-control" required {...field('displayName')}
              placeholder="e.g. Track Day Media" />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="slug">
              Store URL Slug
              <span className="text-muted" style={{ fontWeight: 400, marginLeft: '0.4rem' }}>(optional)</span>
            </label>
            <div style={{ display: 'flex', alignItems: 'center', gap: 0 }}>
              <span className="form-control" style={{ borderRadius: 'var(--radius-md) 0 0 var(--radius-md)', borderRight: 'none', width: 'auto', color: 'var(--text-muted)', fontSize: 'var(--font-size-sm)', whiteSpace: 'nowrap' }}>
                /store/
              </span>
              <input id="slug" type="text" className="form-control"
                style={{ borderRadius: '0 var(--radius-md) var(--radius-md) 0' }}
                placeholder="your-store-name" {...field('slug')} />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="email">Email</label>
            <input id="email" type="email" className="form-control" required {...field('email')}
              autoComplete="email" />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">Password</label>
            <input id="password" type="password" className="form-control" required {...field('password')}
              autoComplete="new-password" minLength={6} />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="confirmPassword">Confirm Password</label>
            <input id="confirmPassword" type="password" className="form-control" required {...field('confirmPassword')}
              autoComplete="new-password" />
          </div>

          <button type="submit" className="btn btn-primary btn-full" disabled={loading}>
            {loading ? 'Creating account…' : 'Create Seller Account'}
          </button>
        </form>

        <div style={{ marginTop: '1.5rem', textAlign: 'center', fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>
          Already have an account?{' '}
          <Link href="/auth/login" style={{ color: 'var(--accent-primary)', fontWeight: 600 }}>Sign In</Link>
        </div>
      </div>
    </div>
  );
}
