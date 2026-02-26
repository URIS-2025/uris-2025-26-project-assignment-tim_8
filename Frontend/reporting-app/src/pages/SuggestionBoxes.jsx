import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { Lightbulb, Plus } from 'lucide-react';

const mockSuggestionBoxes = [
    { id: 'BOX-103', name: 'Product Ideas', organization: 'Tech Corp', activeIdeas: 56, status: 'Active', lastActivity: '10 mins ago' },
    { id: 'BOX-104', name: 'Culture & Events', organization: 'Tech Corp', activeIdeas: 12, status: 'Active', lastActivity: '1 day ago' },
    { id: 'BOX-202', name: 'Process Improvements', organization: 'Design Studio LLC', activeIdeas: 8, status: 'Active', lastActivity: '3 hours ago' },
    { id: 'BOX-302', name: 'Wellness Initiatives', organization: 'Tech Corp', activeIdeas: 24, status: 'Active', lastActivity: '2 days ago' },
    { id: 'BOX-402', name: 'General Feedback', organization: 'Platform', activeIdeas: 156, status: 'Archived', lastActivity: '2 weeks ago' },
];

const SuggestionBoxes = () => {
    const navigate = useNavigate();
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);

    const columns = [
        { header: 'Box Name', accessor: 'name', render: (row) => <strong style={{ color: 'var(--text-primary)' }}>{row.name}</strong> },
        { header: 'ID', accessor: 'id', render: (row) => <span>{row.id}</span> },
        { header: 'Organization', accessor: 'organization' },
        {
            header: 'Active Ideas',
            accessor: 'activeIdeas',
            render: (row) => (
                <span style={{
                    color: row.activeIdeas > 50 ? 'var(--accent-primary)' : row.activeIdeas > 0 ? 'var(--text-primary)' : 'var(--text-muted)',
                    fontWeight: row.activeIdeas > 0 ? 'bold' : 'normal'
                }}>
                    {row.activeIdeas}
                </span>
            )
        },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => <StatusBadge type="status" status={row.status === 'Active' ? 'Resolved' : 'Closed'} />
        },
        { header: 'Last Activity', accessor: 'lastActivity' }
    ];

    const handleRowClick = (row) => {
        navigate(`/admin/boxes/${row.id}`);
    };

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

            <div className="table-wrapper">
                <DataTable
                    title="All Suggestion Boxes"
                    data={mockSuggestionBoxes}
                    columns={columns}
                    onRowClick={handleRowClick}
                    searchPlaceholder="Search by box name or organization..."
                />
            </div>

            <Modal
                isOpen={isBoxModalOpen}
                onClose={() => setBoxModalOpen(false)}
                title="Create Suggestion Box"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setBoxModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setBoxModalOpen(false)}>Create Box</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Box Name</label>
                    <input type="text" className="form-control" placeholder="e.g. Feature Ideas" />
                </div>
                <div className="form-group">
                    <label>Organization</label>
                    <select className="form-control">
                        <option value="ORG-001">Tech Corp International</option>
                        <option value="ORG-002">Design Studio LLC</option>
                    </select>
                </div>
                <div className="form-group">
                    <label>Description (Internal)</label>
                    <textarea className="form-control" rows="3" placeholder="What is this box used for?"></textarea>
                </div>
            </Modal>
        </div>
    );
};

export default SuggestionBoxes;
