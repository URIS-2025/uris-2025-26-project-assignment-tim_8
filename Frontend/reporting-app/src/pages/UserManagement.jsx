import React, { useState } from 'react';
import { Users, Shield, UserPlus, Lock } from 'lucide-react';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';

// Mock Data matching the aggregates
const mockSystemUsers = [
    { id: 'U-001', name: 'Eleanor SystemAdmin', email: 'eleanor@platform.com', role: 'Global Admin', entity: 'Platform', status: 'Active', lastLogin: '1 hour ago' },
    { id: 'U-002', name: 'Alice Walker', email: 'alice@techcorp.com', role: 'Head Manager', entity: 'Tech Corp International', status: 'Active', lastLogin: '2 days ago' },
    { id: 'U-003', name: 'Bob Smith', email: 'bob@techcorp.com', role: 'Manager', entity: 'Tech Corp International', status: 'Offline', lastLogin: '1 week ago' },
    { id: 'U-004', name: 'Charlie Davis', email: 'charlie@designstudio.com', role: 'Billing Manager', entity: 'Design Studio LLC', status: 'Active', lastLogin: '5 mins ago' },
    { id: 'U-005', name: 'Diana Prince', email: 'diana@platform.com', role: 'Admin', entity: 'Platform', status: 'Suspended', lastLogin: '1 month ago' }
];

const UserManagement = () => {
    const [isInviteModalOpen, setInviteModalOpen] = useState(false);

    const columns = [
        { header: 'User', accessor: 'name', render: (row) => <strong>{row.name}</strong> },
        { header: 'Email', accessor: 'email' },
        {
            header: 'Role',
            accessor: 'role',
            render: (row) => (
                <span className="flex items-center gap-2">
                    {row.role.includes('Admin') ? <Shield size={14} className="text-secondary" /> : <Users size={14} className="text-muted" />}
                    {row.role}
                </span>
            )
        },
        { header: 'Assigned Entity', accessor: 'entity' },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => <StatusBadge type="status" status={row.status === 'Active' ? 'Resolved' : row.status === 'Offline' ? 'Low' : 'Past Due'} />
        },
        { header: 'Last Login', accessor: 'lastLogin' },
        {
            header: 'Actions',
            accessor: 'actions',
            render: (row) => (
                <button className="btn btn-ghost icon-btn small" title="Security Settings">
                    <Lock size={16} />
                </button>
            )
        }
    ];

    return (
        <div className="animate-fade-in" style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
            <div className="page-header">
                <div>
                    <h1 className="page-title">System Users</h1>
                    <p className="page-description">Manage Platform Administrators and Organization Managers.</p>
                </div>
                <button className="btn btn-primary" onClick={() => setInviteModalOpen(true)}>
                    <UserPlus size={18} className="mr-2" /> Invite New User
                </button>
            </div>

            <div className="table-wrapper">
                <DataTable
                    title="All System Users"
                    data={mockSystemUsers}
                    columns={columns}
                    searchPlaceholder="Search users by name, email, or entity..."
                />
            </div>

            <Modal
                isOpen={isInviteModalOpen}
                onClose={() => setInviteModalOpen(false)}
                title="Invite System User"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setInviteModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setInviteModalOpen(false)}>Send Invitation</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Email Address</label>
                    <input type="email" className="form-control" placeholder="user@example.com" />
                </div>
                <div className="form-group">
                    <label>System Role</label>
                    <select className="form-control">
                        <option value="manager">Organization Manager</option>
                        <option value="head_manager">Head Organization Manager</option>
                        <option value="billing">Billing Manager</option>
                        <option value="admin">Platform Admin</option>
                    </select>
                </div>
                <div className="form-group">
                    <label>Assign to Organization (if applicable)</label>
                    <select className="form-control">
                        <option value="">-- Select Organization --</option>
                        <option value="ORG-001">Tech Corp International</option>
                        <option value="ORG-002">Design Studio LLC</option>
                    </select>
                    <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>Platform Admins do not need an assigned organization.</p>
                </div>
            </Modal>
        </div>
    );
};

export default UserManagement;
