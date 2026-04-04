'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { useCart } from '@/lib/cart';
import { useAuth } from '@/lib/auth';

export default function CheckoutSuccessPage() {
  const searchParams   = useSearchParams();
  const sessionId      = searchParams.get('session_id');
  const { clearCart }  = useCart();
  const { user }       = useAuth();

  useEffect(() => {
    clearCart();
  }, []);

  return (
    <div className="text-center" style={{ maxWidth: 520, margin: '0 auto', paddingTop: '4rem' }}>
      <div style={{ fontSize: '4rem', marginBottom: '1rem' }}>🎉</div>
      <h1 style={{ fontWeight: 800, marginBottom: '0.75rem' }}>Payment Received!</h1>
      <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem', fontSize: '1.05rem' }}>
        {user
          ? 'Your clips are being fulfilled. Check your purchases dashboard for download links.'
          : 'Check your email for your download links. Orders are typically fulfilled within a few minutes.'}
      </p>

      {sessionId && (
        <p style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-muted)', marginBottom: '2rem' }}>
          Order reference: <code style={{ color: 'var(--accent-primary)' }}>{sessionId}</code>
        </p>
      )}

      <div className="flex gap-3 justify-center flex-wrap">
        {user ? (
          <Link href="/my-purchases" className="btn btn-primary">View My Purchases</Link>
        ) : (
          <Link href="/auth/register" className="btn btn-primary">Create Account to Save Purchases</Link>
        )}
        <Link href="/" className="btn btn-secondary">Continue Browsing</Link>
      </div>
    </div>
  );
}
