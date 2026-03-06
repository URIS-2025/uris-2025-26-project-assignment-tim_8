import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { AlertOctagon, Plus } from 'lucide-react';

const mockProblemBoxes = [
    { id: 'BOX-101', name: 'Facilities & Maintenance', organization: 'Tech Corp', activeReports: 12, status: 'Active', lastActivity: '2 hours ago' },
    { id: 'BOX-102', name: 'IT Helpdesk', organization: 'Tech Corp', activeReports: 8, status: 'Active', lastActivity: '5 mins ago' },
    { id: 'BOX-201', name: 'Workplace Safety', organization: 'Design Studio LLC', activeReports: 3, status: 'Active', lastActivity: '1 day ago' },
    { id: 'BOX-301', name: 'HR Confidential', organization: 'Tech Corp', activeReports: 45, status: 'Active', lastActivity: '1 hour ago' },
    { id: 'BOX-401', name: 'Compliance & Ethics', organization: 'Platform', activeReports: 0, status: 'Archived', lastActivity: '1 month ago' },
];

const ProblemBoxes = () => {
    const navigate = useNavigate();
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);
    const [searchValue, setSearchValue] = useState('');

    const columns = [
        { header: 'Box Name', accessor: 'name', render: (row) => <strong style={{ color: 'var(--text-primary)' }}>{row.name}</strong> },
        { header: 'ID', accessor: 'id', render: (row) => <span>{row.id}</span> },
        { header: 'Organization', accessor: 'organization' },
        {
            header: 'Active Reports',
            accessor: 'activeReports',
            render: (row) => (
                <span style={{
                    color: row.activeReports > 10 ? 'var(--danger)' : row.activeReports > 0 ? 'var(--warning)' : 'var(--text-muted)',
                    fontWeight: row.activeReports > 0 ? 'bold' : 'normal'
                }}>
                    {row.activeReports}
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
                        <AlertOctagon size={28} className="text-danger" />
                        Problem Boxes
                    </h1>
                    <p className="page-description">Manage and monitor all incident reporting boxes across organizations.</p>
                </div>
                <button className="btn btn-primary" onClick={() => setBoxModalOpen(true)}>
                    <Plus size={18} className="mr-2" style={{ marginRight: '0.5rem' }} /> Create New Box
                </button>
            </div>

            <div className="table-wrapper">
                <DataTable
                    title="All Problem Boxes"
                    data={mockProblemBoxes.filter(box =>
                        box.name?.toLowerCase().includes(searchValue.toLowerCase()) ||
                        box.organization?.toLowerCase().includes(searchValue.toLowerCase())
                    )}
                    columns={columns}
                    onRowClick={handleRowClick}
                    searchPlaceholder="Search by box name or organization..."
                    searchValue={searchValue}
                    onSearchChange={setSearchValue}
                />
            </div>

            <Modal
                isOpen={isBoxModalOpen}
                onClose={() => setBoxModalOpen(false)}
                title="Create Problem Box"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setBoxModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setBoxModalOpen(false)}>Create Box</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Box Name</label>
                    <input type="text" className="form-control" placeholder="e.g. Facilities & Maintenance" />
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

export default ProblemBoxes;
