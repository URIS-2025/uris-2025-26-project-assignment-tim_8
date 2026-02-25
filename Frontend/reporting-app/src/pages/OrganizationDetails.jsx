import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { Building2, Users, MessageSquareWarning, AlertOctagon, ArrowLeft, Plus, Settings } from 'lucide-react';
import { OrganizationService } from '../services/organizationService';
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

    const [organization, setOrganization] = useState(null);
    const [isLoading, setIsLoading] = useState(true);

    // Modal states
    const [isManagerModalOpen, setManagerModalOpen] = useState(false);
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);
    const [isSettingsModalOpen, setSettingsModalOpen] = useState(false);
    const [editOrg, setEditOrg] = useState({ name: '', themeColor: '', customLogoUrl: '' });
    const [activeTab, setActiveTab] = useState('boxes'); // 'boxes' or 'managers'

    useEffect(() => {
        fetchOrganization();
    }, [orgId]);

    const fetchOrganization = async () => {
        try {
            setIsLoading(true);
            const data = await OrganizationService.getById(orgId);
            setOrganization(data);
            setEditOrg({
                name: data.name,
                themeColor: data.themeColor || '#6366f1',
                customLogoUrl: data.customLogoUrl || ''
            });
        } catch (error) {
            console.error("Failed to fetch organization", error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleUpdateOrganization = async () => {
        if (!editOrg.name.trim()) return;
        try {
            await OrganizationService.update({
                id: orgId,
                name: editOrg.name,
                themeColor: editOrg.themeColor,
                customLogoUrl: editOrg.customLogoUrl
            });
            setSettingsModalOpen(false);
            fetchOrganization();
        } catch (error) {
            console.error("Failed to update organization", error);
            alert("Failed to update organization");
        }
    };

    const handleDeleteOrganization = async () => {
        if (window.confirm("Are you sure you want to delete this organization? This action cannot be undone.")) {
            try {
                await OrganizationService.delete(orgId);
                navigate('/admin/dashboard');
            } catch (error) {
                console.error("Failed to delete organization", error);
                alert("Failed to delete organization");
            }
        }
    };

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
            {isLoading ? (
                <div className="org-header-card glass-panel flex justify-center items-center py-8">
                    <p>Loading organization details...</p>
                </div>
            ) : organization ? (
                <div className="org-header-card glass-panel" style={{ borderLeft: `4px solid ${organization.themeColor || 'var(--accent-primary)'}` }}>
                    <div className="org-icon">
                        {organization.customLogoUrl ? (
                            <img src={organization.customLogoUrl} alt="Logo" style={{ width: 32, height: 32, objectFit: 'contain' }} />
                        ) : (
                            <Building2 size={32} />
                        )}
                    </div>
                    <div className="org-info">
                        <div className="org-title-row">
                            <h1>{organization.name}</h1>
                            <StatusBadge type="status" status="Open" />
                        </div>
                        <p className="org-meta">ID: {organization.id} &bull; Created: {new Date(organization.createdAt || Date.now()).toLocaleDateString()}</p>
                    </div>
                    <div className="org-actions">
                        <button className="btn btn-ghost icon-btn" title="Settings" onClick={() => setSettingsModalOpen(true)}>
                            <Settings size={20} />
                        </button>
                    </div>
                </div>
            ) : (
                <div className="org-header-card glass-panel">
                    <p className="text-danger">Organization not found.</p>
                </div>
            )}

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

            {/* Organization Settings Modal */}
            <Modal
                isOpen={isSettingsModalOpen}
                onClose={() => setSettingsModalOpen(false)}
                title="Organization Settings"
                footer={
                    <div className="flex justify-between w-full">
                        <button className="btn btn-danger" onClick={handleDeleteOrganization}>Delete Organization</button>
                        <div className="flex gap-2">
                            <button className="btn btn-ghost" onClick={() => setSettingsModalOpen(false)}>Cancel</button>
                            <button className="btn btn-primary" onClick={handleUpdateOrganization}>Save Changes</button>
                        </div>
                    </div>
                }
            >
                <div className="form-group">
                    <label>Organization Name *</label>
                    <input
                        type="text"
                        className="form-control"
                        value={editOrg.name}
                        onChange={(e) => setEditOrg({ ...editOrg, name: e.target.value })}
                        required
                    />
                </div>
                <div className="form-group">
                    <label>Theme Color</label>
                    <div className="flex items-center gap-3">
                        <input
                            type="color"
                            className="form-control"
                            style={{ width: '60px', padding: '0 4px', height: '40px' }}
                            value={editOrg.themeColor}
                            onChange={(e) => setEditOrg({ ...editOrg, themeColor: e.target.value })}
                        />
                        <span className="text-sm opacity-70">{editOrg.themeColor}</span>
                    </div>
                </div>
                <div className="form-group">
                    <label>Logo URL (Optional)</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="https://..."
                        value={editOrg.customLogoUrl}
                        onChange={(e) => setEditOrg({ ...editOrg, customLogoUrl: e.target.value })}
                    />
                </div>
            </Modal>

        </div>
    );
};

export default OrganizationDetails;
