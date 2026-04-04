'use client';

import Link from 'next/link';

export default function Footer() {
  const year = new Date().getFullYear();
  return (
    <footer className="site-footer">
      <span>© {year} ClipCore. All rights reserved.</span>
      <Link href="/privacy" className="footer-link">Privacy Policy</Link>
      <Link href="/terms"   className="footer-link">Terms of Service</Link>
      <Link href="/faq"     className="footer-link">FAQ</Link>
    </footer>
  );
}
