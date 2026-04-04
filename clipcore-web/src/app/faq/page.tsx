import React from 'react';

const FAQS = [
  { q: 'What licenses are available?', a: 'Personal licenses are for private, non-commercial use. Commercial licenses allow use in business projects, ads, or media production. GIF licenses let you create animated GIFs for social sharing.' },
  { q: 'How do I download my clips after purchase?', a: 'After a successful payment, you\'ll receive an email with download links. If you have an account, you can also access downloads from your My Purchases dashboard.' },
  { q: 'What video quality will I receive?', a: 'All purchased clips are delivered as full 1080p master files. Preview clips shown on the site are lower-resolution watermarked versions.' },
  { q: 'Can I get a refund?', a: 'Due to the digital nature of video files, refunds are not available once a download link has been accessed. Please contact support if you have any issues.' },
  { q: 'How do I become a seller?', a: 'Click "Sell Footage" in the navigation bar to register as a seller. Once approved, you can upload footage and create your own storefront.' },
  { q: 'What payment methods are accepted?', a: 'We accept all major credit and debit cards through Stripe. Your payment information is never stored on our servers.' },
];

export default function FaqPage() {
  return (
    <div style={{ maxWidth: 720, margin: '0 auto' }}>
      <h1 style={{ marginBottom: '0.5rem' }}>Frequently Asked Questions</h1>
      <p className="text-muted" style={{ marginBottom: '2.5rem' }}>Everything you need to know about buying and selling on ClipCore.</p>

      <div className="flex flex-col gap-4">
        {FAQS.map((faq, i) => (
          <div key={i} className="card" style={{ padding: '1.5rem' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.6rem', color: 'var(--accent-primary)' }}>
              {faq.q}
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: 'var(--font-size-sm)', lineHeight: 1.6 }}>
              {faq.a}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}
