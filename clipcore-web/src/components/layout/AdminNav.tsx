'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

const LINKS = [
  { href: '/admin',             label: 'Portal'      },
  { href: '/admin/sales',       label: 'Sales'       },
  { href: '/admin/sellers',     label: 'Sellers'     },
  { href: '/admin/promo-codes', label: 'Promo Codes' },
  { href: '/admin/audit-logs',  label: 'Audit Logs'  },
  { href: '/admin/theme',       label: 'Theme'       },
  { href: '/admin/settings',    label: 'Settings'    },
];

export default function AdminNav() {
  const pathname = usePathname();
  return (
    <nav className="seller-nav" style={{ marginBottom: '1.5rem' }}>
      {LINKS.map(l => (
        <Link key={l.href} href={l.href} className={pathname === l.href ? 'active' : ''}>
          {l.label}
        </Link>
      ))}
    </nav>
  );
}
