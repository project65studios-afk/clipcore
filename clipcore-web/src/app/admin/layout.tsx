'use client';

import React, { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/auth';
import Spinner from '@/components/shared/Spinner';

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const router              = useRouter();
  const { user, isLoading } = useAuth();

  useEffect(() => {
    if (isLoading) return;
    if (!user || user.role !== 'Admin') {
      router.push('/auth/login?redirect=/admin');
    }
  }, [user, isLoading]);

  if (isLoading || !user) return <Spinner center />;
  if (user.role !== 'Admin') return null;

  return <>{children}</>;
}
