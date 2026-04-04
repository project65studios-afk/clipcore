'use client';

import React, { useEffect, useState, useRef } from 'react';
import { clips as clipsApi, collections as collectionsApi } from '@/lib/api';
import type { CollectionItem, CreateClipRequest } from '@/types';
import Spinner from '@/components/shared/Spinner';
import SellerNav from '@/components/layout/SellerNav';

interface UploadJob {
  file: File;
  clipId: string;
  title: string;
  progress: number;
  status: 'pending' | 'uploading' | 'done' | 'error';
  error?: string;
}

export default function SellerUploadPage() {
  const [collections, setCollections]   = useState<CollectionItem[]>([]);
  const [collId,      setCollId]        = useState('');
  const [loading,     setLoading]       = useState(true);
  const [jobs,        setJobs]          = useState<UploadJob[]>([]);
  const [dragging,    setDragging]      = useState(false);
  const inputRef                        = useRef<HTMLInputElement>(null);

  useEffect(() => {
    collectionsApi.list()
      .then(cs => { setCollections(cs); if (cs.length > 0) setCollId(cs[0].id); })
      .finally(() => setLoading(false));
  }, []);

  function updateJob(idx: number, patch: Partial<UploadJob>) {
    setJobs(prev => prev.map((j, i) => i === idx ? { ...j, ...patch } : j));
  }

  async function uploadFile(file: File, idx: number) {
    const coll = collections.find(c => c.id === collId);
    if (!coll) return;

    const title = file.name.replace(/\.[^/.]+$/, '');

    // 1. Create clip record
    let clipId: string;
    try {
      const req: CreateClipRequest = {
        collectionId: collId,
        title,
        priceCents: coll.defaultPriceCents,
        priceCommercialCents: coll.defaultPriceCommercialCents,
        allowGifSale: coll.defaultAllowGifSale,
        gifPriceCents: coll.defaultGifPriceCents,
      };
      const res = await clipsApi.create(req);
      clipId = res.clipId;
    } catch (err: unknown) {
      updateJob(idx, { status: 'error', error: 'Failed to create clip: ' + (err as Error).message });
      return;
    }

    // 2. Get Mux upload URL
    let uploadUrl: string;
    try {
      const res = await clipsApi.getMuxUploadUrl(clipId);
      uploadUrl = res.uploadUrl;
    } catch (err: unknown) {
      updateJob(idx, { status: 'error', error: 'Failed to get upload URL: ' + (err as Error).message });
      return;
    }

    // 3. Upload via XHR for progress tracking
    updateJob(idx, { clipId, title, status: 'uploading' });
    await new Promise<void>((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      xhr.upload.onprogress = e => {
        if (e.lengthComputable) updateJob(idx, { progress: Math.round((e.loaded / e.total) * 100) });
      };
      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          updateJob(idx, { status: 'done', progress: 100 });
          resolve();
        } else {
          updateJob(idx, { status: 'error', error: `Upload failed: HTTP ${xhr.status}` });
          reject();
        }
      };
      xhr.onerror = () => {
        updateJob(idx, { status: 'error', error: 'Upload failed (network error)' });
        reject();
      };
      xhr.open('PUT', uploadUrl);
      xhr.setRequestHeader('Content-Type', file.type || 'video/mp4');
      xhr.send(file);
    });
  }

  function addFiles(files: FileList | null) {
    if (!files || !collId) return;
    const newJobs: UploadJob[] = Array.from(files)
      .filter(f => f.type.startsWith('video/'))
      .map(f => ({
        file: f,
        clipId: '',
        title: f.name.replace(/\.[^/.]+$/, ''),
        progress: 0,
        status: 'pending' as const,
      }));
    const startIdx = jobs.length;
    setJobs(prev => [...prev, ...newJobs]);
    newJobs.forEach((_, i) => uploadFile(files[i], startIdx + i));
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault(); setDragging(false);
    addFiles(e.dataTransfer.files);
  }

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Upload Clips</h1>
          <p className="dashboard-subtitle">Upload video files directly to Mux.</p>
        </div>
      </div>

      <SellerNav />

      {/* Collection picker */}
      <div className="form-group mb-6" style={{ maxWidth: 400 }}>
        <label className="form-label">Upload to Collection</label>
        <select className="form-control" value={collId} onChange={e => setCollId(e.target.value)}>
          {collections.map(c => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        {collections.length === 0 && (
          <p className="text-muted" style={{ fontSize: 'var(--font-size-xs)', marginTop: '0.35rem' }}>
            Create a collection first before uploading.
          </p>
        )}
      </div>

      {/* Drop zone */}
      {collId && (
        <div
          className={`upload-zone${dragging ? ' drag-over' : ''}`}
          style={{ marginBottom: '2rem' }}
          onClick={() => inputRef.current?.click()}
          onDragOver={e => { e.preventDefault(); setDragging(true); }}
          onDragLeave={() => setDragging(false)}
          onDrop={handleDrop}
        >
          <div className="upload-icon">📤</div>
          <div className="upload-title">Drop video files here or click to browse</div>
          <div className="upload-hint">MP4, MOV, MXF — up to 1080p recommended</div>
          <input ref={inputRef} type="file" accept="video/*" multiple style={{ display: 'none' }}
            onChange={e => addFiles(e.target.files)} />
        </div>
      )}

      {/* Upload queue */}
      {jobs.length > 0 && (
        <div className="flex flex-col gap-3">
          <h3 className="section-title">Upload Queue</h3>
          {jobs.map((job, i) => (
            <div key={i} style={{
              background: 'var(--bg-surface)', border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-md)', padding: '1rem',
            }}>
              <div className="flex justify-between items-center mb-2">
                <span style={{ fontWeight: 600, fontSize: 'var(--font-size-sm)' }}>{job.title}</span>
                <span className={`badge ${job.status === 'done' ? 'badge-success' : job.status === 'error' ? 'badge-danger' : 'badge-primary'}`}>
                  {job.status === 'done' ? '✓ Done' : job.status === 'error' ? '✕ Error' : job.status === 'uploading' ? `${job.progress}%` : 'Pending'}
                </span>
              </div>
              {(job.status === 'uploading' || job.status === 'done') && (
                <div className="progress-bar-wrapper">
                  <div className="progress-bar-fill" style={{ width: `${job.progress}%` }} />
                </div>
              )}
              {job.error && <p className="text-danger" style={{ fontSize: 'var(--font-size-xs)', marginTop: '0.4rem' }}>{job.error}</p>}
              {job.status === 'done' && (
                <p className="text-muted" style={{ fontSize: 'var(--font-size-xs)', marginTop: '0.4rem' }}>
                  Processing by Mux — clip will appear in your collection shortly.
                </p>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
