import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { Building2, Users, MessageSquareWarning, AlertOctagon, ArrowLeft, Plus, Settings } from 'lucide-react';
import './OrganizationDetails.css';

// Mock Data
const mockOrg = {
    id: 'ORG-001',
    name: 'Tech Corp International',
    created: 'Jan 15, 2023',
    status: 'Active',
    plan: 'Enterprise'
};

const mockManagers = [
    { id: 'M-101', name: 'Alice Walker', email: 'alice@techcorp.com', role: 'Head Manager', added: 'Feb 01, 2023' },
    { id: 'M-102', name: 'Bob Smith', email: 'bob@techcorp.com', role: 'Manager', added: 'Mar 15, 2023' },
];

const mockBoxes = [
    { id: 'BOX-201', name: 'Facilities & Maintenance', type: 'Problem', status: 'Active', count: 124 },
    { id: 'BOX-202', name: 'HR / Employee Relations', type: 'Suggestion', status: 'Active', count: 45 },
    { id: 'BOX-203', name: 'IT Infrastructure Issues', type: 'Problem', status: 'Paused', count: 89 },
];

const OrganizationDetails = () => {
    const { orgId } = useParams();
    const navigate = useNavigate();

    // Modal states
    const [isManagerModalOpen, setManagerModalOpen] = useState(false);
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);
    const [activeTab, setActiveTab] = useState('boxes'); // 'boxes' or 'managers'

    const managerColumns = [
        { header: 'Name', accessor: 'name' },
        { header: 'Email', accessor: 'email' },
        {
            header: 'Role',
            accessor: 'role',
            render: (row) => <StatusBadge type="role" status="Manager" />
        },
        { header: 'Added On', accessor: 'added' }
    ];

    const boxColumns = [
        { header: 'Box Name', accessor: 'name', render: (row) => <strong>{row.name}</strong> },
        {
            header: 'Type',
            accessor: 'type',
            render: (row) => (
                <span className="flex items-center gap-2">
                    {row.type === 'Problem' ? <AlertOctagon size={16} className="text-warning" /> : <MessageSquareWarning size={16} className="text-success" />}
                    {row.type}
                </span>
            )
        },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => <StatusBadge type="status" status={row.status === 'Active' ? 'Open' : 'Closed'} />
        },
        { header: 'Total Reports', accessor: 'count' }
    ];

    return (
        <div className="org-details-container animate-fade-in">
            <div className="back-link" onClick={() => navigate('/admin/dashboard')}>
                <ArrowLeft size={16} /> Back to Dashboard
            </div>

            {/* Organization Header */}
            <div className="org-header-card glass-panel">
                <div className="org-icon">
                    <Building2 size={32} />
                </div>
                <div className="org-info">
                    <div className="org-title-row">
                        <h1>{mockOrg.name}</h1>
                        <StatusBadge type="status" status="Open" />
                    </div>
                    <p className="org-meta">ID: {orgId || mockOrg.id} &bull; Created: {mockOrg.created} &bull; Plan: {mockOrg.plan}</p>
                </div>
                <div className="org-actions">
                    <button className="btn btn-ghost icon-btn" title="Settings">
                        <Settings size={20} />
                    </button>
                </div>
            </div>

            {/* Tabs */}
            <div className="org-tabs">
                <button
                    className={`tab-btn ${activeTab === 'boxes' ? 'active' : ''}`}
                    onClick={() => setActiveTab('boxes')}
                >
                    <MessageSquareWarning size={18} /> Suggestion & Problem Boxes
                </button>
                <button
                    className={`tab-btn ${activeTab === 'managers' ? 'active' : ''}`}
                    onClick={() => setActiveTab('managers')}
                >
                    <Users size={18} /> Managers
                </button>
            </div>

            {/* Tab Content */}
            <div className="tab-content">
                {activeTab === 'boxes' ? (
                    <div className="content-section fade-in">
                        <div className="section-header">
                            <h2>Active Boxes</h2>
                            <button className="btn btn-primary" onClick={() => setBoxModalOpen(true)}>
                                <Plus size={16} /> Create New Box
                            </button>
                        </div>
                        <DataTable
                            data={mockBoxes}
                            columns={boxColumns}
                            onRowClick={(row) => navigate(`/admin/boxes/${row.id}`)}
                            searchPlaceholder="Search boxes..."
                        />
                    </div>
                ) : (
                    <div className="content-section fade-in">
                        <div className="section-header">
                            <h2>Assigned Managers</h2>
                            <button className="btn btn-primary" onClick={() => setManagerModalOpen(true)}>
                                <Plus size={16} /> Add Manager
                            </button>
                        </div>
                        <DataTable
                            data={mockManagers}
                            columns={managerColumns}
                            onActionClick={(row) => console.log('Manager action', row.id)}
                            searchPlaceholder="Search managers by name or email..."
                        />
                    </div>
                )}
            </div>

            {/* Add Manager Modal */}
            <Modal
                isOpen={isManagerModalOpen}
                onClose={() => setManagerModalOpen(false)}
                title="Add New Manager"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setManagerModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setManagerModalOpen(false)}>Send Invite</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Manager Email Address</label>
                    <input type="email" className="form-control" placeholder="manager@organization.com" />
                </div>
                <div className="form-group">
                    <label>Role / Permissions</label>
                    <select className="form-control">
                        <option>Standard Manager</option>
                        <option>Head Manager (Can invite others)</option>
                    </select>
                </div>
            </Modal>

            {/* Create Box Modal */}
            <Modal
                isOpen={isBoxModalOpen}
                onClose={() => setBoxModalOpen(false)}
                title="Create New Box"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setBoxModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setBoxModalOpen(false)}>Create Box</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Box Name</label>
                    <input type="text" className="form-control" placeholder="e.g. Facilities Feedback" />
                </div>
                <div className="form-group">
                    <label>Box Type</label>
                    <select className="form-control">
                        <option value="suggestion">Suggestion Box (General feedback & ideas)</option>
                        <option value="problem">Problem Box (Reporting concrete issues)</option>
                    </select>
                </div>
                <div className="form-group">
                    <label>Description (Optional)</label>
                    <textarea className="form-control" rows="3" placeholder="Brief context for users..."></textarea>
                </div>
            </Modal>

        </div>
    );
};

export default OrganizationDetails;
