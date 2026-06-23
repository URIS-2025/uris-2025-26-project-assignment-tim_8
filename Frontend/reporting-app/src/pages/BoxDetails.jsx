import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { SuggestionBoxService } from '../services/suggestionBoxService';
import { SuggestionService } from '../services/suggestionService';
import { ProblemBoxService } from '../services/problemBoxService';
import { ProblemService } from '../services/problemService';
import './BoxDetails.css';

// Map numeric status to readable label
const statusMap = {
    0: 'New',
    1: 'In Progress',
    2: 'Reviewing',
    3: 'Resolved',
    4: 'Closed'
};

// Map numeric BOX status to readable label (Active/Inactive) — distinct from the submission statusMap above
const boxStatusMap = {
    0: 'Active',
    1: 'Inactive'
};

// Map numeric priority to readable label
const priorityMap = {
    0: 'Low',
    1: 'Medium',
    2: 'High',
    3: 'Critical'
};

const priorityColorMap = {
    0: 'var(--text-muted)',
    1: 'var(--accent-primary)',
    2: 'var(--warning)',
    3: 'var(--danger)'
};

const BoxDetails = () => {
    const { boxId } = useParams();
    const navigate = useNavigate();

    const [box, setBox] = useState(null);
    const [boxType, setBoxType] = useState(null); // 'suggestion' | 'problem'
    const [items, setItems] = useState([]); // suggestions or problems
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [searchValue, setSearchValue] = useState('');
    const [togglingStatus, setTogglingStatus] = useState(false);
    const [passwordModalOpen, setPasswordModalOpen] = useState(false);
    const [newBoxPassword, setNewBoxPassword] = useState('');
    const [savingPassword, setSavingPassword] = useState(false);

    useEffect(() => {
        fetchBoxData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [boxId]);

    const fetchBoxData = async () => {
        try {
            setLoading(true);
            setError(null);
            setCurrentPage(1);

            // Try to fetch as suggestion box first
            let boxData = null;
            let type = null;

            try {
                boxData = await SuggestionBoxService.getById(boxId);
                type = 'suggestion';
            } catch {
                // Not a suggestion box, try problem box
                try {
                    boxData = await ProblemBoxService.getById(boxId);
                    type = 'problem';
                } catch {
                    throw new Error('Box not found. It may have been deleted.');
                }
            }

            setBox(boxData);
            setBoxType(type);

            // Fetch items based on box type
            if (type === 'suggestion') {
                const allSuggestions = await SuggestionService.getAll();
                const boxSuggestions = allSuggestions.filter(
                    (s) => s.suggestionBoxId === boxId
                );
                setItems(boxSuggestions);
            } else {
                const problems = await ProblemService.getByProblemBoxId(boxId);
                setItems(problems);
            }
        } catch (err) {
            console.error('Error fetching box data:', err);
            setError(err.message || 'Failed to load box details. Please check that the backend services are running.');
        } finally {
            setLoading(false);
        }
    };

    const handleDeleteItem = async (id) => {
        const itemLabel = boxType === 'suggestion' ? 'suggestion' : 'problem';
        if (!window.confirm(`Are you sure you want to delete this ${itemLabel}?`)) return;
        try {
            if (boxType === 'suggestion') {
                await SuggestionService.delete(id);
            } else {
                await ProblemService.delete(id);
            }
            setItems((prev) => prev.filter((s) => s.id !== id));
        } catch (err) {
            console.error(`Error deleting ${itemLabel}:`, err);
            alert(`Failed to delete ${itemLabel}.`);
        }
    };

    const handleToggleStatus = async () => {
        if (!box) return;
        const newStatus = box.status === 0 ? 1 : 0;
        try {
            setTogglingStatus(true);
            if (boxType === 'problem') {
                await ProblemBoxService.setStatus(box.id, newStatus);
            } else {
                await SuggestionBoxService.setStatus(box.id, newStatus);
            }
            await fetchBoxData();
        } catch (err) {
            console.error('Error updating box status:', err);
            alert('Failed to update box status.');
        } finally {
            setTogglingStatus(false);
        }
    };

    const handleSavePassword = async () => {
        if (!box) return;
        try {
            setSavingPassword(true);
            if (boxType === 'problem') {
                await ProblemBoxService.setPassword(box.id, newBoxPassword);
            } else {
                await SuggestionBoxService.setPassword(box.id, newBoxPassword);
            }
            setPasswordModalOpen(false);
            await fetchBoxData();
        } catch (err) {
            console.error('Error updating box password:', err);
            alert('Failed to update box password.');
        } finally {
            setSavingPassword(false);
        }
    };

    // Columns for suggestion box
    const suggestionColumns = [
        {
            header: 'Title',
            accessor: 'title',
            render: (row) => <span className="box-cell-title">{row.title}</span>
        },
        {
            header: 'Description',
            accessor: 'description',
            render: (row) => (
                <span className="box-cell-description">
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
                <span className="box-cell-categories">
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
                    className="btn btn-ghost box-row-delete"
                    onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteItem(row.id);
                    }}
                >
                    Delete
                </button>
            )
        }
    ];

    // Columns for problem box
    const problemColumns = [
        {
            header: 'Title',
            accessor: 'title',
            render: (row) => <span className="box-cell-title">{row.title}</span>
        },
        {
            header: 'Description',
            accessor: 'description',
            render: (row) => (
                <span className="box-cell-description">
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
            header: 'Priority',
            accessor: 'priority',
            width: '120px',
            render: (row) => {
                const label = typeof row.priority === 'number' ? priorityMap[row.priority] || 'Unknown' : row.priority;
                const color = typeof row.priority === 'number' ? priorityColorMap[row.priority] || 'var(--text-muted)' : 'var(--text-muted)';
                return (
                    <span className="priority-pill" style={{ color: color, background: `${color}15` }}>
                        {label}
                    </span>
                );
            }
        },
        {
            header: 'Created',
            accessor: 'createdAt',
            width: '140px',
            render: (row) => <span>{row.createdAt ? new Date(row.createdAt).toLocaleDateString() : '—'}</span>
        },
        {
            header: 'Actions',
            accessor: 'actions',
            width: '100px',
            render: (row) => (
                <button
                    className="btn btn-ghost box-row-delete"
                    onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteItem(row.id);
                    }}
                >
                    Delete
                </button>
            )
        }
    ];

    const columns = boxType === 'problem' ? problemColumns : suggestionColumns;

    const handleRowClick = (row) => {
        navigate(`/admin/submissions/${row.id}`);
    };

    if (loading) {
        return (
            <div className="box-details-container box-details-loading animate-fade-in">
                <div className="box-details-loading-inner">
                    <Loader2 size={32} style={{ animation: 'spin 1s linear infinite' }} />
                    <p>Loading box details...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="box-details-container animate-fade-in">
                <button type="button" className="back-link" onClick={() => navigate(-1)}>
                    <ArrowLeft size={16} /> Back to Boxes
                </button>
                <div className="glass-panel box-details-error">
                    <p>{error}</p>
                    <button className="btn btn-ghost" onClick={fetchBoxData}>
                        Retry
                    </button>
                </div>
            </div>
        );
    }

    // Compute stats from real data
    const totalItems = items.length;
    const newCount = items.filter((s) => s.status === 0).length;
    const resolvedCount = items.filter((s) => s.status === 3).length;
    const inProgressCount = items.filter((s) => s.status === 1).length;

    // Dynamic labels based on box type
    const isSuggestion = boxType === 'suggestion';
    const itemTypeLabel = isSuggestion ? 'Suggestions' : 'Problems';
    const boxTypeLabel = isSuggestion ? 'Suggestion Box' : 'Problem Box';

    return (
        <div className="box-details-container animate-fade-in">
            <button type="button" className="back-link" onClick={() => navigate(-1)}>
                <ArrowLeft size={16} /> Back to Boxes
            </button>

            <div className="page-header">
                <div>
                    <h1 className="page-title">{box?.name || boxTypeLabel}</h1>
                    <p className="page-description">
                        {box?.description && <>{box.description} &bull; </>}
                        {isSuggestion && box?.createdBy && <>Created by: {box.createdBy} &bull; </>}
                        {box?.createdAt && <>{new Date(box.createdAt).toLocaleDateString()}</>}
                        {' '}&bull; <span style={{
                            color: isSuggestion ? 'var(--accent-primary)' : 'var(--warning)',
                            fontWeight: 500
                        }}>{boxTypeLabel}</span>
                    </p>
                </div>
            </div>

            {/* Box info panel */}
            {box && (
                <div className="glass-panel box-info-panel">
                    <div>
                        <span className="box-info-label">Box ID: </span>
                        <code>{box.id}</code>
                    </div>
                    <div>
                        <span className="box-info-label">Organization ID: </span>
                        <code>{box.organizationId}</code>
                    </div>
                    <div className="box-info-item">
                        <span className="box-info-label">Status: </span>
                        <StatusBadge type="status" status={typeof box.status === 'number' ? boxStatusMap[box.status] || 'Unknown' : box.status} />
                        <button
                            className="btn btn-ghost box-info-action"
                            onClick={handleToggleStatus}
                            disabled={togglingStatus}
                        >
                            {togglingStatus ? 'Saving…' : (box.status === 0 ? 'Deactivate' : 'Activate')}
                        </button>
                    </div>
                    <div className="box-info-item">
                        <span className="box-info-label">Access: </span>
                        <span style={{ color: box.hasPassword ? 'var(--warning)' : 'var(--text-secondary)' }}>
                            {box.hasPassword ? '🔒 Password-protected' : '🌐 Public'}
                        </span>
                        <button
                            className="btn btn-ghost box-info-action"
                            onClick={() => { setNewBoxPassword(''); setPasswordModalOpen(true); }}
                        >
                            {box.hasPassword ? 'Change password' : 'Set password'}
                        </button>
                    </div>
                </div>
            )}

            <div className="box-stats-row">
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">Total {itemTypeLabel}</span>
                    <span className="mini-stat-value">{totalItems}</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">New</span>
                    <span className="mini-stat-value mini-stat-new">{newCount}</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">In Progress</span>
                    <span className="mini-stat-value mini-stat-progress">{inProgressCount}</span>
                </div>
                <div className="mini-stat glass-panel">
                    <span className="mini-stat-label">Resolved</span>
                    <span className="mini-stat-value mini-stat-resolved">{resolvedCount}</span>
                </div>
            </div>

            <div className="table-wrapper">
                <DataTable
                    title={`All ${itemTypeLabel}`}
                    data={items.filter(s =>
                        s.title?.toLowerCase().includes(searchValue.toLowerCase()) ||
                        s.description?.toLowerCase().includes(searchValue.toLowerCase())
                    )}
                    columns={columns}
                    onRowClick={handleRowClick}
                    searchPlaceholder="Search by title or description..."
                    searchValue={searchValue}
                    onSearchChange={setSearchValue}
                    showExport={true}
                    currentPage={currentPage}
                    onPageChange={setCurrentPage}
                    pageSize={5}
                />
            </div>

            <Modal
                isOpen={passwordModalOpen}
                onClose={() => setPasswordModalOpen(false)}
                title={box?.hasPassword ? 'Change box password' : 'Set box password'}
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setPasswordModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={handleSavePassword} disabled={savingPassword}>
                            {savingPassword ? 'Saving…' : 'Save'}
                        </button>
                    </>
                }
            >
                <div className="form-group">
                    <label>New password</label>
                    <input
                        type="password"
                        className="form-control"
                        placeholder="Leave empty to remove protection (make public)"
                        value={newBoxPassword}
                        onChange={(e) => setNewBoxPassword(e.target.value)}
                    />
                    <p className="box-password-hint">
                        Submitters must enter this password to submit to this box. Leave empty to make the box public.
                    </p>
                </div>
            </Modal>
        </div>
    );
};

export default BoxDetails;
