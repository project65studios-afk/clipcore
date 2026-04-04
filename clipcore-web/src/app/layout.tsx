import type { Metadata } from 'next';
import './globals.css';
import { AuthProvider } from '@/lib/auth';
import { CartProvider } from '@/lib/cart';
import { ThemeProvider } from '@/lib/theme';
import TopNav from '@/components/layout/TopNav';
import Footer from '@/components/layout/Footer';

export const metadata: Metadata = {
  title: { default: 'ClipCore', template: '%s — ClipCore' },
  description: 'Your source for exclusive video footage.',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
      </head>
      <body>
        <ThemeProvider>
          <AuthProvider>
            <CartProvider>
              <TopNav />
              <main className="page-content content-wrapper">
                {children}
              </main>
              <Footer />
            </CartProvider>
          </AuthProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
