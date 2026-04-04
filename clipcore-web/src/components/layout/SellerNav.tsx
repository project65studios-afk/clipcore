'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

const LINKS = [
  { href: '/seller/dashboard',  label: 'Dashboard'  },
  { href: '/seller/collections',label: 'Collections'},
  { href: '/seller/upload',     label: 'Upload'     },
  { href: '/seller/storefront', label: 'Storefront' },
  { href: '/seller/sales',      label: 'Sales'      },
];

export default function SellerNav() {
  const pathname = usePathname();
  return (
    <nav className="seller-nav">
      {LINKS.map(l => (
        <Link
          key={l.href}
          href={l.href}
          className={pathname === l.href ? 'active' : ''}
        >
          {l.label}
        </Link>
      ))}
    </nav>
  );
}
