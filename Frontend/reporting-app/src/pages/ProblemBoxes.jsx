import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { AlertOctagon, Plus, Loader2 } from 'lucide-react';
import { ProblemBoxService } from '../services/problemBoxService';
import { OrganizationService } from '../services/organizationService';

// Map numeric status to readable label
const statusMap = {
    0: 'New',
    1: 'In Progress',
    2: 'Reviewing',
    3: 'Resolved',
    4: 'Closed'
};

const ProblemBoxes = () => {
    const navigate = useNavigate();
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);
    const [problemBoxes, setProblemBoxes] = useState([]);
    const [organizations, setOrganizations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [creating, setCreating] = useState(false);
    const [searchValue, setSearchValue] = useState('');
    const [currentPage, setCurrentPage] = useState(1);

    // Form state matching ProblemBoxCreationDTO
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        isDarkTheme: false,
        password: '',
        createdBy: '',
        organizationId: ''
    });

    useEffect(() => {
        fetchProblemBoxes();
        fetchOrganizations();
    }, []);

    const fetchProblemBoxes = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await ProblemBoxService.getAll();
            setProblemBoxes(data);
        } catch (err) {
            console.error('Error fetching problem boxes:', err);
            setError('Failed to load problem boxes. Please check that the backend services are running.');
        } finally {
            setLoading(false);
        }
    };

    const fetchOrganizations = async () => {
        try {
            const data = await OrganizationService.getAll();
            setOrganizations(data);
        } catch (err) {
            console.error('Error fetching organizations:', err);
        }
    };

    const handleCreateBox = async () => {
        if (!formData.name || !formData.organizationId) return;

        try {
            setCreating(true);
            await ProblemBoxService.create({
                name: formData.name,
                description: formData.description,
                isDarkTheme: formData.isDarkTheme,
                password: formData.password,
                createdBy: formData.createdBy,
                organizationId: formData.organizationId
            });
            setFormData({ name: '', description: '', isDarkTheme: false, password: '', createdBy: '', organizationId: '' });
            setBoxModalOpen(false);
            await fetchProblemBoxes();
        } catch (err) {
            console.error('Error creating problem box:', err);
            alert('Failed to create problem box. Please try again.');
        } finally {
            setCreating(false);
        }
    };

    const handleDeleteBox = async (id) => {
        if (!window.confirm('Are you sure you want to delete this problem box?')) return;
        try {
            await ProblemBoxService.delete(id);
            setProblemBoxes((prev) => prev.filter((b) => b.id !== id));
        } catch (err) {
            console.error('Error deleting problem box:', err);
            alert('Failed to delete problem box.');
        }
    };

    const columns = [
        {
            header: 'Box Name',
            accessor: 'name',
            render: (row) => <strong style={{ color: 'var(--text-primary)' }}>{row.name}</strong>
        },
        {
            header: 'Description',
            accessor: 'description',
            render: (row) => <span>{row.description || '—'}</span>
        },
        {
            header: 'Theme',
            accessor: 'isDarkTheme',
            render: (row) => (
                <span style={{
                    padding: '0.25rem 0.5rem',
                    borderRadius: 'var(--radius-sm)',
                    fontSize: '0.8rem',
                    background: row.isDarkTheme ? 'rgba(255,255,255,0.1)' : 'rgba(255,255,255,0.05)',
                    color: row.isDarkTheme ? 'var(--accent-primary)' : 'var(--text-muted)'
                }}>
                    {row.isDarkTheme ? '🌙 Dark' : '☀️ Light'}
                </span>
            )
        },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => {
                const label = typeof row.status === 'number' ? statusMap[row.status] || 'Unknown' : row.status;
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
                    className="btn btn-ghost"
                    style={{ color: 'var(--danger)', padding: '0.25rem 0.5rem', fontSize: '0.8rem' }}
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

    // Filter data by search
    const filteredData = problemBoxes.filter(box =>
        box.name?.toLowerCase().includes(searchValue.toLowerCase()) ||
        box.description?.toLowerCase().includes(searchValue.toLowerCase())
    );

    // Reset to page 1 when search changes
    useEffect(() => {
        setCurrentPage(1);
    }, [searchValue]);

    if (loading) {
        return (
            <div className="animate-fade-in" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '400px' }}>
                <div style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                    <Loader2 size={32} style={{ animation: 'spin 1s linear infinite', marginBottom: '1rem' }} />
                    <p>Loading problem boxes...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="animate-fade-in" style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
            <div className="page-header">
                <div>
                    <h1 className="page-title" style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                        <AlertOctagon size={28} className="text-danger" />
                        Problem Boxes
                    </h1>
                    <p className="page-description">Manage and monitor all incident reporting boxes across organizations.</p>
                </div>
                <button className="btn btn-primary" onClick={() => setBoxModalOpen(true)}>
                    <Plus size={18} className="mr-2" style={{ marginRight: '0.5rem' }} /> Create New Box
                </button>
            </div>

            {error ? (
                <div className="glass-panel" style={{ padding: '2rem', textAlign: 'center', color: 'var(--danger)' }}>
                    <p>{error}</p>
                    <button className="btn btn-ghost" onClick={fetchProblemBoxes} style={{ marginTop: '1rem' }}>
                        Retry
                    </button>
                </div>
            ) : (
                <div className="table-wrapper">
                    <DataTable
                        title="All Problem Boxes"
                        data={filteredData}
                        columns={columns}
                        onRowClick={handleRowClick}
                        searchPlaceholder="Search by box name or description..."
                        searchValue={searchValue}
                        onSearchChange={setSearchValue}
                        currentPage={currentPage}
                        onPageChange={setCurrentPage}
                        pageSize={5}
                    />
                </div>
            )}

            <Modal
                isOpen={isBoxModalOpen}
                onClose={() => setBoxModalOpen(false)}
                title="Create Problem Box"
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
                    <label>Box Name <span style={{ color: 'var(--danger)' }}>*</span></label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="e.g. Facilities & Maintenance"
                        value={formData.name}
                        onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    />
                </div>
                <div className="form-group">
                    <label>Organization <span style={{ color: 'var(--danger)' }}>*</span></label>
                    <select
                        className="form-control"
                        value={formData.organizationId}
                        onChange={(e) => setFormData({ ...formData, organizationId: e.target.value })}
                    >
                        <option value="">Select an organization...</option>
                        {organizations.map((org) => (
                            <option key={org.id} value={org.id}>{org.name}</option>
                        ))}
                    </select>
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
                <div className="form-group" style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                    <input
                        type="checkbox"
                        id="pbIsDarkTheme"
                        checked={formData.isDarkTheme}
                        onChange={(e) => setFormData({ ...formData, isDarkTheme: e.target.checked })}
                        style={{ width: 'auto' }}
                    />
                    <label htmlFor="pbIsDarkTheme" style={{ marginBottom: 0 }}>Enable Dark Theme</label>
                </div>
            </Modal>
        </div>
    );
};

export default ProblemBoxes;
