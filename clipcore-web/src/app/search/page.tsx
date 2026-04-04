'use client';

import React, { useEffect, useState, useRef } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { marketplace } from '@/lib/api';
import type { MarketplaceClip } from '@/types';
import ClipCard from '@/components/shared/ClipCard';
import Spinner from '@/components/shared/Spinner';

const PAGE_SIZE = 24;

export default function SearchPage() {
  const searchParams = useSearchParams();
  const router       = useRouter();
  const initialQ     = searchParams.get('q') ?? '';

  const [query,   setQuery]   = useState(initialQ);
  const [input,   setInput]   = useState(initialQ);
  const [clips,   setClips]   = useState<MarketplaceClip[]>([]);
  const [total,   setTotal]   = useState(0);
  const [page,    setPage]    = useState(1);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    marketplace.search({ searchTerm: query || undefined, page, pageSize: PAGE_SIZE })
      .then(res => {
        setClips(res.clips);
        setTotal(res.totalCount);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [query, page]);

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setPage(1);
    setQuery(input.trim());
    router.push(`/search?q=${encodeURIComponent(input.trim())}`);
  }

  const totalPages = Math.ceil(total / PAGE_SIZE);

  return (
    <>
      <h1 style={{ marginBottom: '1.5rem' }}>Search Footage</h1>

      {/* Search bar */}
      <form onSubmit={handleSearch} style={{ display: 'flex', gap: '0.75rem', maxWidth: 600, marginBottom: '2rem' }}>
        <input
          className="form-control"
          value={input}
          onChange={e => setInput(e.target.value)}
          placeholder="Search by title, event name, location…"
          autoFocus
        />
        <button type="submit" className="btn btn-primary" style={{ whiteSpace: 'nowrap' }}>Search</button>
      </form>

      {/* Results count */}
      {!loading && (
        <p className="text-muted" style={{ marginBottom: '1.25rem', fontSize: 'var(--font-size-sm)' }}>
          {query ? `${total} result${total !== 1 ? 's' : ''} for "${query}"` : `${total} clips available`}
        </p>
      )}

      {loading ? (
        <Spinner center />
      ) : clips.length === 0 ? (
        <div className="text-center" style={{ padding: '4rem 1rem', color: 'var(--text-muted)' }}>
          <p style={{ fontSize: '1.1rem', marginBottom: '0.5rem' }}>No results found.</p>
          <p style={{ fontSize: 'var(--font-size-sm)' }}>Try different keywords or browse all footage.</p>
        </div>
      ) : (
        <div className="video-grid">
          {clips.map(clip => (
            <ClipCard key={clip.id} clip={clip} collectionName={clip.collectionName} />
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-center gap-2" style={{ marginTop: '2.5rem' }}>
          <button
            className="btn btn-outline btn-sm"
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
          >
            ← Previous
          </button>
          <span style={{ padding: '0.4rem 0.75rem', fontSize: 'var(--font-size-sm)', color: 'var(--text-secondary)' }}>
            Page {page} of {totalPages}
          </span>
          <button
            className="btn btn-outline btn-sm"
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
          >
            Next →
          </button>
        </div>
      )}
    </>
  );
}
