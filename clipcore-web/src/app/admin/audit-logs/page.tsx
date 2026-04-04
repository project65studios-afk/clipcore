'use client';

import React, { useEffect, useState } from 'react';
import { admin as adminApi } from '@/lib/api';
import Spinner from '@/components/shared/Spinner';
import AdminNav from '@/components/layout/AdminNav';

interface AuditLog {
  id: number;
  userId: string;
  action: string;
  detail: string;
  createdAt: string;
}

export default function AdminAuditLogsPage() {
  const [logs,     setLogs]     = useState<AuditLog[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [page,     setPage]     = useState(1);
  const PAGE_SIZE = 50;

  useEffect(() => {
    setLoading(true);
    adminApi.getAuditLogs({ page, pageSize: PAGE_SIZE })
      .then(setLogs)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [page]);

  if (loading) return <Spinner center />;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div>
          <h1 className="dashboard-title">Audit Logs</h1>
          <p className="dashboard-subtitle">Track admin and system actions.</p>
        </div>
      </div>
      <AdminNav />

      <div className="data-table-wrapper">
        {logs.length === 0 ? (
          <div className="text-center" style={{ padding: '3rem', color: 'var(--text-muted)' }}>No audit logs found.</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr><th>ID</th><th>User</th><th>Action</th><th>Detail</th><th>Timestamp</th></tr>
            </thead>
            <tbody>
              {logs.map(l => (
                <tr key={l.id}>
                  <td className="text-muted" style={{ fontFamily: 'monospace' }}>{l.id}</td>
                  <td style={{ fontFamily: 'monospace', fontSize: 'var(--font-size-xs)', color: 'var(--text-muted)', maxWidth: 140, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {l.userId}
                  </td>
                  <td>
                    <span className="badge badge-primary">{l.action}</span>
                  </td>
                  <td style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-secondary)', maxWidth: 320 }} className="truncate">
                    {l.detail}
                  </td>
                  <td className="text-muted" style={{ whiteSpace: 'nowrap', fontSize: 'var(--font-size-xs)' }}>
                    {new Date(l.createdAt).toLocaleString('en-US', {
                      month: 'short', day: 'numeric', year: 'numeric',
                      hour: 'numeric', minute: '2-digit',
                    })}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      <div className="flex justify-center gap-2" style={{ marginTop: '1.5rem' }}>
        <button className="btn btn-outline btn-sm" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>← Previous</button>
        <span style={{ padding: '0.4rem 0.75rem', fontSize: 'var(--font-size-sm)', color: 'var(--text-secondary)' }}>Page {page}</span>
        <button className="btn btn-outline btn-sm" onClick={() => setPage(p => p + 1)} disabled={logs.length < PAGE_SIZE}>Next →</button>
      </div>
    </div>
  );
}
