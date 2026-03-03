import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { Lightbulb, Plus, Loader2 } from 'lucide-react';
import { SuggestionBoxService } from '../services/suggestionBoxService';

const SuggestionBoxes = () => {
    const navigate = useNavigate();
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);
    const [suggestionBoxes, setSuggestionBoxes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [creating, setCreating] = useState(false);

    // Form state for creating a new suggestion box
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        organizationId: ''
    });

    // Fetch suggestion boxes on mount
    useEffect(() => {
        fetchSuggestionBoxes();
    }, []);

    const fetchSuggestionBoxes = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await SuggestionBoxService.getAll();
            setSuggestionBoxes(data);
        } catch (err) {
            console.error('Error fetching suggestion boxes:', err);
            setError('Failed to load suggestion boxes. Please check that the backend services are running.');
        } finally {
            setLoading(false);
        }
    };

    const handleCreateBox = async () => {
        if (!formData.name || !formData.organizationId) return;

        try {
            setCreating(true);
            await SuggestionBoxService.create({
                name: formData.name,
                description: formData.description,
                organizationId: formData.organizationId
            });
            // Reset form and close modal
            setFormData({ name: '', description: '', organizationId: '' });
            setBoxModalOpen(false);
            // Refresh the list
            await fetchSuggestionBoxes();
        } catch (err) {
            console.error('Error creating suggestion box:', err);
            alert('Failed to create suggestion box. Please try again.');
        } finally {
            setCreating(false);
        }
    };

    const columns = [
        {
            header: 'Box Name',
            accessor: 'name',
            render: (row) => <strong style={{ color: 'var(--text-primary)' }}>{row.name}</strong>
        },
        {
            header: 'ID',
            accessor: 'id',
            render: (row) => <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{row.id?.substring(0, 8)}...</span>
        },
        {
            header: 'Description',
            accessor: 'description',
            render: (row) => <span>{row.description || '—'}</span>
        },
        {
            header: 'Created At',
            accessor: 'createdAt',
            render: (row) => <span>{row.createdAt ? new Date(row.createdAt).toLocaleDateString() : '—'}</span>
        },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => <StatusBadge type="status" status={row.status === 'Active' || !row.status ? 'Resolved' : 'Closed'} />
        }
    ];

    const handleRowClick = (row) => {
        navigate(`/admin/boxes/${row.id}`);
    };

    if (loading) {
        return (
            <div className="animate-fade-in" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '400px' }}>
                <div style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                    <Loader2 size={32} style={{ animation: 'spin 1s linear infinite', marginBottom: '1rem' }} />
                    <p>Loading suggestion boxes...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="animate-fade-in" style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
            <div className="page-header">
                <div>
                    <h1 className="page-title" style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                        <Lightbulb size={28} style={{ color: 'var(--accent-primary)' }} />
                        Suggestion Boxes
                    </h1>
                    <p className="page-description">Review feature requests, cultural improvements, and community ideas.</p>
                </div>
                <button className="btn btn-primary" onClick={() => setBoxModalOpen(true)}>
                    <Plus size={18} className="mr-2" style={{ marginRight: '0.5rem' }} /> Create New Box
                </button>
            </div>

            {error ? (
                <div className="glass-panel" style={{ padding: '2rem', textAlign: 'center', color: 'var(--danger)' }}>
                    <p>{error}</p>
                    <button className="btn btn-ghost" onClick={fetchSuggestionBoxes} style={{ marginTop: '1rem' }}>
                        Retry
                    </button>
                </div>
            ) : (
                <div className="table-wrapper">
                    <DataTable
                        title="All Suggestion Boxes"
                        data={suggestionBoxes}
                        columns={columns}
                        onRowClick={handleRowClick}
                        searchPlaceholder="Search by box name..."
                    />
                </div>
            )}

            <Modal
                isOpen={isBoxModalOpen}
                onClose={() => setBoxModalOpen(false)}
                title="Create Suggestion Box"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setBoxModalOpen(false)}>Cancel</button>
                        <button
                            className="btn btn-primary"
                            onClick={handleCreateBox}
                            disabled={creating || !formData.name || !formData.organizationId}
                        >
                            {creating ? 'Creating...' : 'Create Box'}
                        </button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Box Name</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="e.g. Feature Ideas"
                        value={formData.name}
                        onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    />
                </div>
                <div className="form-group">
                    <label>Organization ID</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="Enter organization GUID"
                        value={formData.organizationId}
                        onChange={(e) => setFormData({ ...formData, organizationId: e.target.value })}
                    />
                </div>
                <div className="form-group">
                    <label>Description (Internal)</label>
                    <textarea
                        className="form-control"
                        rows="3"
                        placeholder="What is this box used for?"
                        value={formData.description}
                        onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    />
                </div>
            </Modal>
        </div>
    );
};

export default SuggestionBoxes;
