import React, { useState, useEffect } from 'react';
import { Users, Shield, UserPlus, Lock } from 'lucide-react';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { OrganizationService } from '../services/organizationService';
import { UserService } from '../services/userService';
import { UserRoleService } from '../services/userRoleService';

const UserManagement = () => {
    const [isInviteModalOpen, setInviteModalOpen] = useState(false);
    const [users, setUsers] = useState([]);
    const [organizations, setOrganizations] = useState([]);
    const [roles, setRoles] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    const [inviteData, setInviteData] = useState({ email: '', role: 'manager', organizationId: '' });

    // Manage Role Modal States
    const [isRoleModalOpen, setRoleModalOpen] = useState(false);
    const [selectedUser, setSelectedUser] = useState(null);
    const [newRole, setNewRole] = useState('');

    const [searchValue, setSearchValue] = useState('');

    useEffect(() => {
        fetchUsers();
        fetchOrganizations();
        fetchRoles();
    }, []);

    const fetchUsers = async () => {
        try {
            setIsLoading(true);
            const data = await UserService.getAll();
            setUsers(data);
        } catch (error) {
            console.error("Failed to fetch users", error);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchOrganizations = async () => {
        try {
            const data = await OrganizationService.getAll();
            setOrganizations(data);
        } catch (error) {
            console.error("Failed to fetch organizations", error);
        }
    };

    const fetchRoles = async () => {
        try {
            const data = await UserRoleService.getAll();
            setRoles(data);
        } catch (error) {
            console.error("Failed to fetch roles", error);
        }
    };

    const handleInviteUser = async () => {
        if (!inviteData.email) return;
        try {
            await UserService.invite({
                email: inviteData.email,
                role: inviteData.role,
                organizationId: inviteData.organizationId || null
            });
            setInviteModalOpen(false);
            setInviteData({ email: '', role: 'manager', organizationId: '' });
            fetchUsers();
            alert("User invited successfully!");
        } catch (error) {
            console.error("Failed to invite user", error);
            alert("Failed to invite user");
        }
    };

    const openRoleModal = (user) => {
        setSelectedUser(user);
        setNewRole(user.role || 'manager');
        setRoleModalOpen(true);
    };

    const handleUpdateRole = async () => {
        if (!selectedUser) return;
        try {
            await UserService.updateRole(selectedUser.id, { role: newRole });
            setRoleModalOpen(false);
            fetchUsers();
            alert("User role updated successfully!");
        } catch (error) {
            console.error("Failed to update user role", error);
            alert("Failed to update user role");
        }
    };

    const columns = [
        { header: 'Id', accessor: 'id' },
        { header: 'User', accessor: 'name', render: (row) => <strong>{row.name}</strong> },
        { header: 'Email', accessor: 'email' },
        {
            header: 'Role',
            accessor: 'role',
            render: (row) => {
                // If the backend returns a roleId instead of a role name string
                const roleName = row.role || roles.find(r => r.id === row.roleId)?.title || 'User';
                return (
                    <span className="flex items-center gap-2">
                        {roleName?.toLowerCase().includes('admin') ? <Shield size={14} className="text-secondary" /> : <Users size={14} className="text-muted" />}
                        {roleName}
                    </span>
                );
            }
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
                <button className="btn btn-ghost icon-btn small" title="Security Settings" onClick={() => openRoleModal(row)}>
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
                {isLoading ? (
                    <p className="p-4 text-center">Loading users...</p>
                ) : (
                    <DataTable
                        title="All System Users"
                        data={users.filter(user =>
                            user.name?.toLowerCase().includes(searchValue.toLowerCase()) ||
                            user.email?.toLowerCase().includes(searchValue.toLowerCase())
                        )}
                        columns={columns}
                        searchPlaceholder="Search users by name or email..."
                        searchValue={searchValue}
                        onSearchChange={setSearchValue}
                    />
                )}
            </div>

            <Modal
                isOpen={isInviteModalOpen}
                onClose={() => setInviteModalOpen(false)}
                title="Invite System User"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setInviteModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={handleInviteUser}>Send Invitation</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Email Address</label>
                    <input
                        type="email"
                        className="form-control"
                        placeholder="user@example.com"
                        value={inviteData.email}
                        onChange={(e) => setInviteData({ ...inviteData, email: e.target.value })}
                        required
                    />
                </div>
                <div className="form-group">
                    <label>System Role</label>
                    <select
                        className="form-control"
                        value={inviteData.role}
                        onChange={(e) => setInviteData({ ...inviteData, role: e.target.value })}
                    >
                        <option value="manager">Organization Manager</option>
                        <option value="head_manager">Head Organization Manager</option>
                        <option value="billing">Billing Manager</option>
                        <option value="admin">Platform Admin</option>
                    </select>
                </div>
                <div className="form-group">
                    <label>Assign to Organization (if applicable)</label>
                    <select
                        className="form-control"
                        value={inviteData.organizationId}
                        onChange={(e) => setInviteData({ ...inviteData, organizationId: e.target.value })}
                        disabled={inviteData.role === 'admin'}
                    >
                        <option value="">-- Select Organization --</option>
                        {organizations.map(org => (
                            <option key={org.id} value={org.id}>{org.name}</option>
                        ))}
                    </select>
                    <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>Platform Admins do not need an assigned organization.</p>
                </div>
            </Modal>

            <Modal
                isOpen={isRoleModalOpen}
                onClose={() => setRoleModalOpen(false)}
                title="Manage User Role"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setRoleModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={handleUpdateRole}>Save Changes</button>
                    </>
                }
            >
                {selectedUser && (
                    <>
                        <div className="mb-4">
                            <p><strong>User:</strong> {selectedUser.name}</p>
                            <p><strong>Email:</strong> {selectedUser.email}</p>
                        </div>
                        <div className="form-group">
                            <label>System Role *</label>
                            <select
                                className="form-control"
                                value={newRole}
                                onChange={(e) => setNewRole(e.target.value)}
                            >
                                <option value="manager">Organization Manager</option>
                                <option value="head_manager">Head Organization Manager</option>
                                <option value="billing">Billing Manager</option>
                                <option value="admin">Platform Admin</option>
                            </select>
                        </div>
                    </>
                )}
            </Modal>
        </div>
    );
};

export default UserManagement;
