import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { Lightbulb, Plus, Loader2 } from 'lucide-react';
import { SuggestionBoxService } from '../services/suggestionBoxService';
import { useAuth } from '../context/AuthContext';
import './SuggestionBoxes.css';

// Map numeric box status to readable label
const boxStatusMap = {
    0: 'Active',
    1: 'Inactive'
};

const SuggestionBoxes = () => {
    const navigate = useNavigate();
    const { user } = useAuth();
    const isManager = (user?.role || 'user') === 'manager';
    const orgId = user?.organizationId || null;
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);
    const [suggestionBoxes, setSuggestionBoxes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [creating, setCreating] = useState(false);
    const [searchValue, setSearchValue] = useState('');

    // Form state matching SuggestionBoxCreateDTO
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        password: '',
        createdBy: '',
        organizationId: isManager ? (orgId || '') : ''
    });

    useEffect(() => {
        fetchSuggestionBoxes();
    }, []);

    const fetchSuggestionBoxes = async () => {
        try {
            setLoading(true);
            setError(null);
            if (isManager && !orgId) {
                setSuggestionBoxes([]);
                setError('No organization is assigned to your account.');
                return;
            }
            const data = isManager
                ? await SuggestionBoxService.getByOrganizationId(orgId)
                : await SuggestionBoxService.getAll();
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
                password: formData.password,
                createdBy: formData.createdBy,
                organizationId: formData.organizationId
            });
            setFormData({ name: '', description: '', password: '', createdBy: '', organizationId: isManager ? (orgId || '') : '' });
            setBoxModalOpen(false);
            await fetchSuggestionBoxes();
        } catch (err) {
            console.error('Error creating suggestion box:', err);
            alert('Failed to create suggestion box. Please try again.');
        } finally {
            setCreating(false);
        }
    };

    const handleDeleteBox = async (id) => {
        if (!window.confirm('Are you sure you want to delete this suggestion box?')) return;
        try {
            await SuggestionBoxService.delete(id);
            setSuggestionBoxes((prev) => prev.filter((b) => b.id !== id));
        } catch (err) {
            console.error('Error deleting suggestion box:', err);
            alert('Failed to delete suggestion box.');
        }
    };

    const columns = [
        {
            header: 'Box Name',
            accessor: 'name',
            render: (row) => <strong className="box-page-name">{row.name}</strong>
        },
        {
            header: 'Description',
            accessor: 'description',
            render: (row) => <span>{row.description || '—'}</span>
        },
        {
            header: 'Created By',
            accessor: 'createdBy',
            render: (row) => <span>{row.createdBy || '—'}</span>
        },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => {
                const label = typeof row.status === 'number' ? boxStatusMap[row.status] || 'Unknown' : row.status;
                return <StatusBadge type="status" status={label} />;
            }
        },
        {
            header: 'Created At',
            accessor: 'createdAt',
            render: (row) => <span>{row.createdAt ? new Date(row.createdAt).toLocaleDateString() : '—'}</span>
        },
        {
            header: 'Actions',
            accessor: 'actions',
            width: '100px',
            render: (row) => (
                <button
                    className="btn btn-ghost box-page-delete-btn"
                    onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteBox(row.id);
                    }}
                >
                    Delete
                </button>
            )
        }
    ];

    const handleRowClick = (row) => {
        navigate(`/admin/boxes/${row.id}`);
    };

    if (loading) {
        return (
            <div className="animate-fade-in box-page-loading">
                <div className="box-page-loading-inner">
                    <Loader2 size={32} style={{ animation: 'spin 1s linear infinite' }} />
                    <p>Loading suggestion boxes...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="animate-fade-in box-page">
            <div className="page-header">
                <div>
                    <h1 className="page-title box-page-title">
                        <Lightbulb size={28} className="box-page-icon" />
                        Suggestion Boxes
                    </h1>
                    <p className="page-description">Review feature requests, cultural improvements, and community ideas.</p>
                </div>
                <button className="btn btn-primary" onClick={() => setBoxModalOpen(true)}>
                    <Plus size={18} className="mr-2" /> Create New Box
                </button>
            </div>

            {error ? (
                <div className="glass-panel box-page-error">
                    <p>{error}</p>
                    <button className="btn btn-ghost box-page-error-retry" onClick={fetchSuggestionBoxes}>
                        Retry
                    </button>
                </div>
            ) : (
                <div className="table-wrapper">
                    <DataTable
                        title="All Suggestion Boxes"
                        data={suggestionBoxes.filter(box =>
                            box.name?.toLowerCase().includes(searchValue.toLowerCase()) ||
                            box.description?.toLowerCase().includes(searchValue.toLowerCase())
                        )}
                        columns={columns}
                        onRowClick={handleRowClick}
                        searchPlaceholder="Search by box name or description..."
                        searchValue={searchValue}
                        onSearchChange={setSearchValue}
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
                    <label>Box Name <span className="text-danger">*</span></label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="e.g. Feature Ideas"
                        value={formData.name}
                        onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    />
                </div>
                <div className="form-group">
                    <label>Organization ID <span className="text-danger">*</span></label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="Enter organization GUID"
                        value={formData.organizationId}
                        onChange={(e) => setFormData({ ...formData, organizationId: e.target.value })}
                        readOnly={isManager}
                    />
                </div>
                <div className="form-group">
                    <label>Created By</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="e.g. Admin User"
                        value={formData.createdBy}
                        onChange={(e) => setFormData({ ...formData, createdBy: e.target.value })}
                    />
                </div>
                <div className="form-group">
                    <label>Description</label>
                    <textarea
                        className="form-control"
                        rows="3"
                        placeholder="What is this box used for?"
                        value={formData.description}
                        onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    />
                </div>
                <div className="form-group">
                    <label>Password (Optional)</label>
                    <input
                        type="password"
                        className="form-control"
                        placeholder="Set a password for this box"
                        value={formData.password}
                        onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    />
                </div>
            </Modal>
        </div>
    );
};

export default SuggestionBoxes;
