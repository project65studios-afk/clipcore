'use client';

import React from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';

// Buyer registration is just a redirect to seller registration,
// or a simple form for guest buyers (ClipCore is primarily seller-facing for auth)
export default function RegisterPage() {
  const router = useRouter();
  return (
    <div style={{ maxWidth: 440, margin: '0 auto', paddingTop: '2rem' }}>
      <div className="card" style={{ padding: '2rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.5rem' }}>Create Account</h1>
        <p className="text-muted" style={{ marginBottom: '1.75rem', fontSize: 'var(--font-size-sm)' }}>
          Choose how you&apos;d like to use ClipCore.
        </p>

        <div className="flex flex-col gap-3">
          <button
            className="btn btn-primary btn-full"
            style={{ padding: '1rem', fontSize: '1rem' }}
            onClick={() => router.push('/seller/register')}
          >
            <div style={{ fontWeight: 800 }}>📹 Sell Footage</div>
            <div style={{ fontSize: 'var(--font-size-xs)', fontWeight: 400, opacity: 0.85, marginTop: '0.25rem' }}>
              Upload and sell event clips on your own storefront
            </div>
          </button>

          <div className="text-muted text-center" style={{ fontSize: 'var(--font-size-xs)' }}>
            Buyers don&apos;t need an account — you can check out as a guest.
          </div>
        </div>

        <div style={{ marginTop: '1.5rem', textAlign: 'center', fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)' }}>
          Already have an account?{' '}
          <Link href="/auth/login" style={{ color: 'var(--accent-primary)', fontWeight: 600 }}>Sign In</Link>
        </div>
      </div>
    </div>
  );
}
