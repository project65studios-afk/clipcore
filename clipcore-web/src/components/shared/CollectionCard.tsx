import React from 'react';
import Link from 'next/link';
import type { CollectionItem } from '@/types';

interface Props {
  collection: CollectionItem;
  index?: number;
  thumbnailUrl?: string;
}

export default function CollectionCard({ collection, index = 0, thumbnailUrl }: Props) {
  const date = new Date(collection.date).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  });

  return (
    <Link
      href={`/collections/${collection.id}`}
      className="collection-card"
      style={{ animationDelay: `${index * 0.05}s` }}
    >
      <div className="card-img-container">
        {thumbnailUrl ? (
          <img src={thumbnailUrl} alt={collection.name} className="event-thumb" />
        ) : (
          <div style={{
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            width: '100%', height: '100%',
          }}>
            <span style={{ fontSize: '2.5rem', color: 'var(--text-muted)' }}>🎬</span>
          </div>
        )}
        <div className="card-overlay">
          <span className="view-btn">View Collection</span>
        </div>
      </div>

      <div className="card-body">
        <h3 className="card-title">{collection.name}</h3>
        {collection.location && (
          <div className="card-location">📍 {collection.location}</div>
        )}
        <div className="card-meta">
          <span>{date}</span>
          <span className="meta-dot">•</span>
          <span>{collection.clipCount} clips</span>
        </div>
        {collection.summary && (
          <p className="card-summary">{collection.summary}</p>
        )}
      </div>
    </Link>
  );
}
