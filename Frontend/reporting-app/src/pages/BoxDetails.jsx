import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { SuggestionBoxService } from '../services/suggestionBoxService';
import { SuggestionService } from '../services/suggestionService';
import './BoxDetails.css';

// Map numeric status to readable label
const statusMap = {
    0: 'New',
    1: 'In Progress',
    2: 'Reviewing',
    3: 'Resolved',
    4: 'Closed'
};

const BoxDetails = () => {
    const { boxId } = useParams();
    const navigate = useNavigate();

    const [box, setBox] = useState(null);
    const [suggestions, setSuggestions] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [searchValue, setSearchValue] = useState('');

    useEffect(() => {
        fetchBoxData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [boxId]);

    const fetchBoxData = async () => {
        try {
            setLoading(true);
            setError(null);

            // Fetch box details and all suggestions in parallel
            const [boxData, allSuggestions] = await Promise.all([
                SuggestionBoxService.getById(boxId),
                SuggestionService.getAll()
            ]);

            setBox(boxData);

            // Filter suggestions that belong to this suggestion box
            const boxSuggestions = allSuggestions.filter(
                (s) => s.suggestionBoxId === boxId
            );
            setSuggestions(boxSuggestions);
        } catch (err) {
            console.error('Error fetching box data:', err);
            setError('Failed to load box details. Please check that the backend services are running.');
        } finally {
            setLoading(false);
        }
    };

    const handleDeleteSuggestion = async (id) => {
        if (!window.confirm('Are you sure you want to delete this suggestion?')) return;
        try {
            await SuggestionService.delete(id);
            setSuggestions((prev) => prev.filter((s) => s.id !== id));
        } catch (err) {
            console.error('Error deleting suggestion:', err);
            alert('Failed to delete suggestion.');
        }
    };

    const columns = [
        {
            header: 'Title',
            accessor: 'title',
            render: (row) => <span style={{ fontWeight: 500 }}>{row.title}</span>
        },
        {
            header: 'Description',
            accessor: 'description',
            render: (row) => (
                <span style={{ color: 'var(--text-secondary)' }}>
                    {row.description?.length > 60
                        ? row.description.substring(0, 60) + '...'
                        : row.description || '—'}
                </span>
            )
        },
        {
            header: 'Status',
            accessor: 'status',
            width: '130px',
            render: (row) => {
                const label = typeof row.status === 'number' ? statusMap[row.status] || 'Unknown' : row.status;
                return <StatusBadge type="status" status={label} />;
            }
        },
        {
            header: 'Created',
            accessor: 'createdAt',
            width: '140px',
            render: (row) => <span>{row.createdAt ? new Date(row.createdAt).toLocaleDateString() : '—'}</span>
        },
        {
            header: 'Categories',
            accessor: 'categories',
            render: (row) => (
                <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                    {row.categories && row.categories.length > 0
                        ? row.categories.map((c) => c.name).join(', ')
                        : '—'}
                </span>
            )
        },
        {
            header: 'Actions',
            accessor: 'actions',
            width: '100px',
            render: (row) => (
                <button
                    className="btn btn-ghost"
                    style={{ color: 'var(--danger)', padding: '0.25rem 0.5rem', fontSize: '0.8rem' }}
                    onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteSuggestion(row.id);
                    }}
                >
                    Delete
                </button>
            )
        }
    ];

    const handleRowClick = (row) => {
        navigate(`/admin/submissions/${row.id}`);
    };

    if (loading) {
        return (
            <div className="box-details-container animate-fade-in" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '400px' }}>
                <div style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                    <Loader2 size={32} style={{ animation: 'spin 1s linear infinite', marginBottom: '1rem' }} />
                    <p>Loading box details...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="box-details-container animate-fade-in">
                <div className="back-link" onClick={() => navigate(-1)}>
                    <ArrowLeft size={16} /> Back to Boxes
                </div>
                <div className="glass-panel" style={{ padding: '2rem', textAlign: 'center', color: 'var(--danger)' }}>
                    <p>{error}</p>
                    <button className="btn btn-ghost" onClick={fetchBoxData} style={{ marginTop: '1rem' }}>
                        Retry
                    </button>
                </div>
            </div>
        );
    }

    // Compute stats from real data
    const totalSuggestions = suggestions.length;
    const newCount = suggestions.filter((s) => s.status === 0).length;
    const resolvedCount = suggestions.filter((s) => s.status === 3).length;
    const inProgressCount = suggestions.filter((s) => s.status === 1).length;

    return (
        <div className="box-details-container animate-fade-in">
            <div className="back-link" onClick={() => navigate(-1)}>
                <ArrowLeft size={16} /> Back to Boxes
            </div>

            <div className="page-header">
                <div>
                    <h1 className="page-title">{box?.name || 'Suggestion Box'}</h1>
                    <p className="page-description">
                        {box?.description && <>{box.description} &bull; </>}
                        Created by: {box?.createdBy || 'Unknown'}
                        {box?.createdAt && <> &bull; {new Date(box.createdAt).toLocaleDateString()}</>}
                    </p>
                </div>
            </div>

            {/* Box info panel */}
            {box && (
                <div className="glass-panel" style={{ padding: '1rem 1.5rem', display: 'flex', flexWrap: 'wrap', gap: '1.5rem', fontSize: '0.85rem', marginBottom: '0.5rem' }}>
                    <div>
                        <span style={{ color: 'var(--text-muted)' }}>Box ID: </span>
                        <code style={{ color: 'var(--text-secondary)' }}>{box.id}</code>
                    </div>
                    <div>
                        <span style={{ color: 'var(--text-muted)' }}>Organization ID: </span>
                        <code style={{ color: 'var(--text-secondary)' }}>{box.organizationId}</code>
                    </div>
                    {box.boxAccessLinkId && box.boxAccessLinkId !== '00000000-0000-0000-0000-000000000000' && (
                        <div>
                            <span style={{ color: 'var(--text-muted)' }}>Access Link ID: </span>
                            <code style={{ color: 'var(--accent-primary)' }}>{box.boxAccessLinkId}</code>
                        </div>
                    )}
                    <div>
                        <span style={{ color: 'var(--text-muted)' }}>Theme: </span>
                        <span>{box.isDarkTheme ? '🌙 Dark' : '☀️ Light'}</span>
                    </div>
                </div>
            )}

            <div className="box-stats-row">
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">Total Suggestions</span>
                    <span className="mini-stat-value">{totalSuggestions}</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">New</span>
                    <span className="mini-stat-value" style={{ color: 'var(--accent-primary)' }}>{newCount}</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">In Progress</span>
                    <span className="mini-stat-value" style={{ color: 'var(--warning)' }}>{inProgressCount}</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">Resolved</span>
                    <span className="mini-stat-value" style={{ color: 'var(--success)' }}>{resolvedCount}</span>
                </div>
            </div>

            <div className="table-wrapper">
                <DataTable
                    title="All Suggestions"
                    data={suggestions.filter(s =>
                        s.title?.toLowerCase().includes(searchValue.toLowerCase()) ||
                        s.description?.toLowerCase().includes(searchValue.toLowerCase())
                    )}
                    columns={columns}
                    onRowClick={handleRowClick}
                    searchPlaceholder="Search by title or description..."
                    searchValue={searchValue}
                    onSearchChange={setSearchValue}
                    showExport={true}
                />
            </div>
        </div>
    );
};

export default BoxDetails;
