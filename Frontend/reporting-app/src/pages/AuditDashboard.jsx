import React, { useState, useEffect, useCallback } from 'react';
import { Loader2, ShieldCheck, ChevronLeft, ChevronRight, RotateCcw } from 'lucide-react';
import Modal from '../components/Modal';
import { AuditService } from '../services/auditService';
import { useAuth } from '../context/AuthContext';
import {
    decisionLabel, outcomeLabel, confirmationLabel,
    decisionTone, outcomeTone, toneColor,
} from '../utils/auditEnums';
import './AuditDashboard.css';

const PAGE_SIZE = 25;
const EMPTY_FILTERS = { toolName: '', decision: '', outcome: '', from: '', to: '', organizationId: '' };

// A small semantic pill; colors are runtime-computed from the enum tone (styling.md allows inline
// style for computed colors — the palette still comes from CSS variables via toneColor()).
const Pill = ({ label, tone }) => (
    <span className="audit-pill" style={{ color: toneColor(tone), borderColor: toneColor(tone) }}>
        {label}
    </span>
);

const fmtTime = (ts) => (ts ? new Date(ts).toLocaleString() : '—');

const AuditDashboard = () => {
    const { user } = useAuth();
    const isAdmin = (user?.role || '') === 'admin';
    const isManager = (user?.role || '') === 'manager';
    const orgId = user?.organizationId || null;

    const [form, setForm] = useState(EMPTY_FILTERS);       // live filter inputs
    const [applied, setApplied] = useState(EMPTY_FILTERS); // snapshot actually fetched
    const [page, setPage] = useState(0);

    const [data, setData] = useState({ total: 0, items: [] });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [selected, setSelected] = useState(null); // row for the detail modal

    const fetchAudit = useCallback(async () => {
        try {
            setLoading(true);
            setError(null);
            // Mirror the ProblemBoxes guard: a manager with no org can't be scoped (backend would 403).
            if (isManager && !orgId) {
                setData({ total: 0, items: [] });
                setError('Nalogu nije dodeljena organizacija.');
                return;
            }
            const res = await AuditService.getAudit(
                { ...applied, page, pageSize: PAGE_SIZE }, { isAdmin });
            // Response is camelCase on the wire (AuditPageDTO { total, items }).
            const total = res.total ?? 0;
            setData({ total, items: res.items ?? [] });
            // Clamp: if the current page fell past the end (e.g. filters narrowed the set), step back
            // to the last valid page instead of stranding the user on an empty "Strana N od 1".
            const lastPage = Math.max(0, Math.ceil(total / PAGE_SIZE) - 1);
            if (page > lastPage) setPage(lastPage);
        } catch (err) {
            console.error('Error fetching audit log:', err);
            setError('Neuspešno učitavanje audit zapisa. Proverite da li backend radi i da ste prijavljeni.');
        } finally {
            setLoading(false);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [applied, page, isAdmin, isManager, orgId]);

    useEffect(() => { fetchAudit(); }, [fetchAudit]);

    // Clear any stale error eagerly here too: resetting while already at defaults would not change
    // `applied`/`page`, so the fetch effect wouldn't re-fire to clear it on its own.
    const applyFilters = () => { setError(null); setPage(0); setApplied(form); };
    const resetFilters = () => { setError(null); setForm(EMPTY_FILTERS); setPage(0); setApplied(EMPTY_FILTERS); };

    const totalPages = Math.max(1, Math.ceil((data.total || 0) / PAGE_SIZE));
    const canPrev = page > 0;
    const canNext = page < totalPages - 1;

    return (
        <div className="animate-fade-in audit-page">
            <div className="page-header">
                <div>
                    <h1 className="page-title">
                        <ShieldCheck size={28} style={{ color: 'var(--accent-primary)' }} /> Audit Log
                    </h1>
                    <p className="page-description">
                        Svaki poziv alata kroz MCP gateway — ko (agent + korisnik), koji alat, odluka i ishod.
                        {isManager && ' Prikazani su samo zapisi vaše organizacije.'}
                    </p>
                </div>
            </div>

            {/* Filter bar */}
            <div className="glass-panel audit-filters">
                <input className="form-control" placeholder="Alat (npr. list_boxes)"
                    value={form.toolName} onChange={(e) => setForm({ ...form, toolName: e.target.value })} />
                <select className="form-control" value={form.decision}
                    onChange={(e) => setForm({ ...form, decision: e.target.value })}>
                    <option value="">Odluka: sve</option>
                    <option value="0">Allow</option>
                    <option value="1">Deny</option>
                </select>
                <select className="form-control" value={form.outcome}
                    onChange={(e) => setForm({ ...form, outcome: e.target.value })}>
                    <option value="">Ishod: svi</option>
                    <option value="0">Success</option>
                    <option value="1">Error</option>
                    <option value="2">NotExecuted</option>
                </select>
                <input className="form-control" type="datetime-local" title="Od"
                    value={form.from} onChange={(e) => setForm({ ...form, from: e.target.value })} />
                <input className="form-control" type="datetime-local" title="Do"
                    value={form.to} onChange={(e) => setForm({ ...form, to: e.target.value })} />
                {/* organizationId filter only for an admin — a manager is force-scoped server-side. */}
                {isAdmin && (
                    <input className="form-control" placeholder="OrganizationId (opciono)"
                        value={form.organizationId}
                        onChange={(e) => setForm({ ...form, organizationId: e.target.value })} />
                )}
                <button className="btn btn-primary" onClick={applyFilters}>Primeni</button>
                <button className="btn btn-ghost" onClick={resetFilters} title="Reset">
                    <RotateCcw size={16} />
                </button>
            </div>

            {loading ? (
                <div className="audit-loading">
                    <Loader2 size={32} style={{ animation: 'spin 1s linear infinite' }} />
                    <p>Učitavanje audit zapisa…</p>
                </div>
            ) : error ? (
                <div className="glass-panel audit-error">
                    <p>{error}</p>
                    <button className="btn btn-ghost" onClick={fetchAudit}>Pokušaj ponovo</button>
                </div>
            ) : (
                <div className="data-table-container glass-panel">
                    <div className="table-responsive">
                        <table className="custom-table audit-table">
                            <thead>
                                <tr>
                                    <th>Vreme</th><th>Agent</th><th>Korisnik</th><th>Rola</th>
                                    <th>Alat</th><th>Tip</th><th>Odluka</th><th>Ishod</th><th style={{ textAlign: 'right' }}>Trajanje</th>
                                </tr>
                            </thead>
                            <tbody>
                                {data.items.length > 0 ? data.items.map((r) => (
                                    <tr key={r.id} className="clickable-row" onClick={() => setSelected(r)}>
                                        <td>{fmtTime(r.timestamp)}</td>
                                        <td>{r.agentId}</td>
                                        <td>{r.userId}</td>
                                        <td>{r.userRole}</td>
                                        <td><code>{r.toolName}</code></td>
                                        <td>{r.isWrite ? 'Write' : 'Read'}</td>
                                        <td><Pill label={decisionLabel(r.decision)} tone={decisionTone(r.decision)} /></td>
                                        <td><Pill label={outcomeLabel(r.outcome)} tone={outcomeTone(r.outcome)} /></td>
                                        <td style={{ textAlign: 'right' }}>{r.durationMs != null ? `${r.durationMs} ms` : '—'}</td>
                                    </tr>
                                )) : (
                                    <tr><td colSpan={9} className="empty-state">Nema audit zapisa za zadate filtere.</td></tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                    <div className="table-pagination">
                        <span className="pagination-info">
                            Strana {page + 1} od {totalPages} • {data.total} zapisa
                        </span>
                        <div className="pagination-controls">
                            <button className="btn btn-ghost icon-btn small" disabled={!canPrev}
                                onClick={() => setPage((p) => Math.max(0, p - 1))}>
                                <ChevronLeft size={16} />
                            </button>
                            <button className="btn btn-ghost icon-btn small" disabled={!canNext}
                                onClick={() => setPage((p) => p + 1)}>
                                <ChevronRight size={16} />
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Detail modal */}
            <Modal isOpen={!!selected} onClose={() => setSelected(null)} title="Audit zapis — detalji">
                {selected && (
                    <div className="audit-detail">
                        <Field label="Vreme" value={fmtTime(selected.timestamp)} />
                        <Field label="Agent" value={selected.agentId} />
                        <Field label="Korisnik (OBO)" value={`${selected.userId} (${selected.userRole})`} />
                        <Field label="Organizacija" value={selected.organizationId || '— (globalno)'} />
                        <Field label="Alat" value={`${selected.toolName} · ${selected.isWrite ? 'Write' : 'Read'}`} />
                        <Field label="Odluka">
                            <Pill label={decisionLabel(selected.decision)} tone={decisionTone(selected.decision)} />
                        </Field>
                        <Field label="Razlog odluke" value={selected.decisionReason} />
                        <Field label="Sažetak argumenata" value={selected.argsSummary || '—'} />
                        {confirmationLabel(selected.confirmation) && (
                            <Field label="Potvrda (HITL)" value={confirmationLabel(selected.confirmation)} />
                        )}
                        <Field label="Ishod">
                            <Pill label={outcomeLabel(selected.outcome)} tone={outcomeTone(selected.outcome)} />
                        </Field>
                        {selected.error && <Field label="Greška" value={selected.error} />}
                        <Field label="Trajanje" value={selected.durationMs != null ? `${selected.durationMs} ms` : '—'} />
                        <Field label="Audit Id" value={selected.id} />
                    </div>
                )}
            </Modal>
        </div>
    );
};

const Field = ({ label, value, children }) => (
    <div className="audit-detail-row">
        <span className="audit-detail-label">{label}</span>
        <span className="audit-detail-value">{children ?? value}</span>
    </div>
);

export default AuditDashboard;
