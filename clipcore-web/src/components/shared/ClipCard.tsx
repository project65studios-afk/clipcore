'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import MuxPlayer from '@mux/mux-player-react';
import type { ClipItem, MarketplaceClip, LicenseType } from '@/types';
import { useCart } from '@/lib/cart';
import { formatDuration, formatPrice, muxThumbnailUrl } from '@/lib/api';

interface Props {
  clip: ClipItem | MarketplaceClip;
  isPurchased?: boolean;
  watermarkUrl?: string;
  // For marketplace clips we don't have collectionId etc on the same shape
  collectionName?: string;
  collectionDate?: string;
  storefrontSlug?: string;
}

function isClipItem(c: ClipItem | MarketplaceClip): c is ClipItem {
  return 'collectionId' in c;
}

export default function ClipCard({ clip, isPurchased, watermarkUrl, collectionName, collectionDate, storefrontSlug }: Props) {
  const { addItem, removeItem, hasItem } = useCart();
  const [hovered, setHovered] = useState(false);

  // Determine playback ID for preview (teaser is always public)
  const previewId = (clip as ClipItem).playbackIdTeaser ?? (clip as MarketplaceClip).playbackIdTeaser;
  const thumbUrl  = previewId ? muxThumbnailUrl(previewId) : undefined;
  const duration  = clip.durationSec;

  // Parse tags
  let tags: string[] = [];
  if (isClipItem(clip) && clip.tagsJson) {
    try { tags = JSON.parse(clip.tagsJson); } catch {}
  }

  const inCartPersonal    = hasItem(clip.id, 'Personal',    false);
  const inCartCommercial  = hasItem(clip.id, 'Commercial',  false);
  const inCartGif         = hasItem(clip.id, 'Gif',         true);
  const inCart = inCartPersonal || inCartCommercial || inCartGif;

  function quickAdd(e: React.MouseEvent) {
    e.preventDefault();
    e.stopPropagation();
    if (inCartPersonal) {
      removeItem(clip.id, 'Personal', false);
      return;
    }
    addItem({
      id:              clip.id,
      title:           clip.title,
      collectionId:    isClipItem(clip) ? clip.collectionId : '',
      collectionName:  collectionName ?? (clip as MarketplaceClip).collectionName ?? '',
      collectionDate,
      priceCents:      clip.priceCents,
      licenseType:     'Personal',
      isGif:           false,
      playbackId:      previewId,
      durationSec:     duration ?? undefined,
      masterFileName:  isClipItem(clip) ? clip.masterFileName : undefined,
      thumbnailFileName: clip.thumbnailFileName ?? undefined,
    });
  }

  // Status badges
  const isProcessing = isClipItem(clip) && !clip.playbackIdTeaser && !clip.playbackIdSigned;
  const isError      = false; // Handled by mux webhook

  const href = `/clips/${clip.id}`;

  return (
    <Link
      href={href}
      className={`video-card${hovered ? '' : ''}`}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      {/* Thumbnail / Preview */}
      <div className="video-thumb">
        {thumbUrl && !hovered && (
          <img src={thumbUrl} alt={clip.title} />
        )}
        {previewId && hovered && (
          <MuxPlayer
            playbackId={previewId}
            style={{ width: '100%', height: '100%' }}
            autoPlay
            muted
            loop
            className="preview-player"
          />
        )}
        {!thumbUrl && !previewId && (
          <div style={{
            width: '100%', height: '100%',
            background: 'var(--bg-subtle)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            color: 'var(--text-muted)', fontSize: '2rem',
          }}>🎬</div>
        )}

        {/* Duration */}
        {duration && (
          <span className="video-duration">{formatDuration(duration)}</span>
        )}

        {/* Status badges */}
        {isPurchased && (
          <span className="thumb-badge thumb-badge-right badge-purchased">✓ Purchased</span>
        )}
        {!isPurchased && inCart && (
          <span className="thumb-badge thumb-badge-right badge-in-cart">In Cart</span>
        )}
        {isProcessing && (
          <span className="thumb-badge thumb-badge-right badge-processing">Processing</span>
        )}
        {isError && (
          <span className="thumb-badge thumb-badge-right badge-error">Error</span>
        )}

        {/* Quick-add button */}
        {!isPurchased && !isProcessing && (
          <button
            className="quick-add-btn"
            onClick={quickAdd}
            title={inCartPersonal ? 'Remove from cart' : 'Add to cart'}
          >
            {inCartPersonal ? '✕' : '+'}
          </button>
        )}

        {/* Watermark grid */}
        {watermarkUrl && !isPurchased && (
          <div className="watermark-grid">
            {Array.from({ length: 36 }).map((_, i) => (
              <div key={i} className="watermark-cell" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <img src={watermarkUrl} alt="" />
              </div>
            ))}
          </div>
        )}

        {/* Vehicle tags */}
        {tags.length > 0 && (
          <div className="tags-container">
            {tags.slice(0, 3).map(tag => (
              <span key={tag} className="vehicle-tag">🏎 {tag}</span>
            ))}
          </div>
        )}
      </div>

      {/* Info */}
      <div className="video-info">
        <div className="video-title">{clip.title}</div>
        <div className="video-meta">
          <span className="price-tag">{formatPrice(clip.priceCents)}</span>
          {clip.priceCommercialCents > clip.priceCents && (
            <>
              <span className="dot" />
              <span>Commercial {formatPrice(clip.priceCommercialCents)}</span>
            </>
          )}
          {clip.allowGifSale && (
            <>
              <span className="dot" />
              <span>GIF {formatPrice(clip.gifPriceCents)}</span>
            </>
          )}
        </div>
        {collectionName && (
          <div className="video-meta" style={{ fontSize: '0.75rem', marginTop: '0.1rem' }}>
            <span style={{ color: 'var(--text-muted)' }}>{collectionName}</span>
          </div>
        )}
      </div>
    </Link>
  );
}
