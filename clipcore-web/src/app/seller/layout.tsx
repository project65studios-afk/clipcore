'use client';

import React, { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/auth';
import Spinner from '@/components/shared/Spinner';

export default function SellerLayout({ children }: { children: React.ReactNode }) {
  const router                   = useRouter();
  const { user, isLoading }      = useAuth();

  useEffect(() => {
    if (isLoading) return;
    if (!user || (user.role !== 'Seller' && user.role !== 'Admin')) {
      router.push('/auth/login?redirect=' + encodeURIComponent(window.location.pathname));
    }
  }, [user, isLoading]);

  if (isLoading || !user) return <Spinner center />;
  if (user.role !== 'Seller' && user.role !== 'Admin') return null;

  return <>{children}</>;
}
